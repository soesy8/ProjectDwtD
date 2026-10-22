"""Build an editable .blend from the exact saved Unity prototype geometry.

Run with Blender --background --factory-startup --python this_file.
All intermediate outputs are confined to this ignored folder under Assets/M.
"""
import bpy
import json
import math
from collections import Counter
from pathlib import Path
from mathutils import Matrix, Quaternion, Vector

WORK = Path(__file__).resolve().parent
SOURCE = WORK / "Friend_Milo_Source.json"
OUTPUT = WORK / "Friend_Milo_Prototype.blend"
data = json.loads(SOURCE.read_text(encoding="utf-8"))

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.name = "Friend Milo - Prototype Modeling"
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1.0
scene.unit_settings.length_unit = "METERS"
scene.render.engine = "BLENDER_EEVEE"
scene.view_settings.view_transform = "Standard"
scene.view_settings.look = "None"

def collection(name):
    result = bpy.data.collections.new(name)
    scene.collection.children.link(result)
    return result

model_collection = collection("01_Character_Editable")
alternate_collection = collection("02_Alternate_Parts_Hidden")
studio_collection = collection("03_Preview_Studio")

# Unity: Y up, +Z character front. Blender: Z up, -Y character front.
conversion = Matrix(((1, 0, 0, 0), (0, 0, -1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))
conversion_inverse = conversion.inverted()
direction_conversion = conversion.to_3x3()

def srgb_channel(v):
    return v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4

def linear_color(color):
    return tuple(srgb_channel(v) for v in color[:3]) + (color[3] if len(color) > 3 else 1.0,)

sun_direction = Vector((-0.45, -0.7, 0.9)).normalized()
materials = {}
material_records = {m["id"]: m for m in data["materials"]}
for record in data["materials"]:
    color = record["colors"].get("_BaseColor", record["colors"].get("_Color", [0.7, 0.7, 0.7, 1]))
    material = bpy.data.materials.new(record["name"])
    material.diffuse_color = linear_color(color)
    material.use_nodes = True
    material["Unity_Material_Path"] = record["path"]
    material["Unity_Shader"] = record["shader"]
    material["Unity_BaseColor_sRGB"] = color
    material["Unity_Shader_Properties_JSON"] = json.dumps(record, ensure_ascii=False)
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    output.location = (600, 0)
    emission = nodes.new("ShaderNodeEmission")
    emission.location = (380, 0)
    emission.inputs["Strength"].default_value = 1
    links.new(emission.outputs[0], output.inputs["Surface"])

    geometry = nodes.new("ShaderNodeNewGeometry")
    geometry.location = (-700, 0)
    dot = nodes.new("ShaderNodeVectorMath")
    dot.operation = "DOT_PRODUCT"
    dot.inputs[1].default_value = sun_direction
    dot.location = (-500, 0)
    dot.label = "Preview sunlight direction"
    links.new(geometry.outputs["Normal"], dot.inputs[0])
    mapping = nodes.new("ShaderNodeMath")
    mapping.operation = "MULTIPLY_ADD"
    mapping.inputs[1].default_value = 0.5
    mapping.inputs[2].default_value = 0.5
    mapping.location = (-300, 0)
    links.new(dot.outputs["Value"], mapping.inputs[0])
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.location = (-80, 0)
    ramp.label = "Three toon bands (Blender preview)"
    links.new(mapping.outputs[0], ramp.inputs[0])
    links.new(ramp.outputs["Color"], emission.inputs["Color"])
    properties = record["floats"]
    shade = record["colors"].get("_ShadeColor", [0.75, 0.78, 0.76, 1])
    middle = properties.get("_MidTone", 0.9)
    unlit = properties.get("_EmissionStrength", 0)
    low = linear_color([color[i] * (shade[i] * (1 - unlit) + unlit) for i in range(3)] + [1])
    mid = linear_color([color[i] * (middle * (1 - unlit) + unlit) for i in range(3)] + [1])
    high = linear_color(color)
    shadow = (properties.get("_ShadowThreshold", 0.02) + 1) / 2
    light = (properties.get("_LightThreshold", 0.62) + 1) / 2
    edge = max(0.001, properties.get("_BandSoftness", 0.012)) / 2
    stops = [(0, low), (shadow - edge, low), (shadow + edge, mid),
             (light - edge, mid), (light + edge, high), (1, high)]
    ramp.color_ramp.interpolation = "LINEAR"
    for index, (position, value) in enumerate(stops):
        element = ramp.color_ramp.elements[index] if index < 2 else ramp.color_ramp.elements.new(position)
        element.position = position
        element.color = value
    materials[record["id"]] = material

parts_by_id = {p["id"]: p for p in data["parts"]}
objects = {}
validation_warnings = []
face_count = 0
vertex_count = 0
normal_api = None

def unity_path(part):
    names = [part["name"]]
    while part["parent"]:
        part = parts_by_id[part["parent"]]
        names.append(part["name"])
    return "/".join(reversed(names))

def assign_materials(obj, part):
    for material_id in part["materials"]:
        obj.data.materials.append(materials.get(material_id))

for part in data["parts"]:
    geometry = part["geometry"]
    if geometry:
        vertices = [direction_conversion @ Vector(v) for v in geometry["vertices"]]
        normals = [direction_conversion @ Vector(v) for v in geometry["normals"]]
        faces, face_materials = [], []
        for submesh_index, indices in enumerate(geometry["submeshes"]):
            for i in range(0, len(indices), 3):
                face = list(indices[i:i + 3])
                if normals:
                    a, b, c = (vertices[n] for n in face)
                    target_normal = sum((normals[n] for n in face), Vector())
                    if (b - a).cross(c - a).dot(target_normal) < 0:
                        face.reverse()
                faces.append(face)
                face_materials.append(submesh_index)
        mesh = bpy.data.meshes.new(part["name"] + "_EditableMesh")
        mesh.from_pydata(vertices, [], faces)
        mesh.update()
        # Keep source vertex counts, UV seams and triangulation; each part is single-user.
        for polygon, material_index in zip(mesh.polygons, face_materials):
            polygon.material_index = material_index
            polygon.use_smooth = True
        if geometry["uv"]:
            uv_layer = mesh.uv_layers.new(name="Unity_UV0")
            for loop in mesh.loops:
                uv_layer.data[loop.index].uv = geometry["uv"][loop.vertex_index]
        if normals:
            if hasattr(mesh, "normals_split_custom_set_from_vertices"):
                mesh.normals_split_custom_set_from_vertices(normals)
                normal_api = "normals_split_custom_set_from_vertices"
            elif hasattr(mesh, "normals_split_custom_set"):
                mesh.normals_split_custom_set([normals[loop.vertex_index] for loop in mesh.loops])
                normal_api = "normals_split_custom_set"
            else:
                validation_warnings.append("Custom normals unavailable: " + part["name"])
        obj = bpy.data.objects.new(part["name"], mesh)
        assign_materials(obj, part)
        obj["Unity_Mesh_Path"] = geometry["path"]
        obj["Unity_Mesh_Name"] = geometry["name"]
        face_count += len(mesh.polygons)
        vertex_count += len(mesh.vertices)
    elif part["line"]:
        line = part["line"]
        curve = bpy.data.curves.new(part["name"] + "_EditableCurve", "CURVE")
        curve.dimensions = "3D"
        curve.resolution_u = 2
        curve.bevel_depth = line["width"] * 0.5
        curve.bevel_resolution = 3
        curve.use_fill_caps = True
        spline = curve.splines.new("POLY")
        spline.points.add(len(line["positions"]) - 1)
        for point, position in zip(spline.points, line["positions"]):
            point.co = tuple(direction_conversion @ Vector(position)) + (1.0,)
        spline.use_cyclic_u = line["loop"]
        obj = bpy.data.objects.new(part["name"], curve)
        assign_materials(obj, part)
        obj["Export_Note"] = "Unity LineRenderer represented by editable bevelled curve."
    else:
        obj = bpy.data.objects.new(part["name"], None)
        obj.empty_display_size = 0.08
        obj.empty_display_type = "PLAIN_AXES"
    target_collection = model_collection if part["visible"] else alternate_collection
    target_collection.objects.link(obj)
    obj["Unity_Path"] = unity_path(part)
    obj["Unity_Source_ID"] = part["id"]
    obj["Unity_Active"] = part["active"]
    obj["Unity_Visible"] = part["visible"]
    if part["parent"]:
        obj.parent = objects[part["parent"]]
    p = Vector(part["localPosition"])
    q = part["localRotation"]
    q = Quaternion((q[3], q[0], q[1], q[2]))
    transform = Matrix.LocRotScale(p, q, Vector(part["localScale"]))
    obj.matrix_basis = conversion @ transform @ conversion_inverse
    obj.hide_render = not part["visible"]
    obj.hide_set(not part["visible"])
    objects[part["id"]] = obj

bpy.context.view_layer.update()
visible_meshes = [objects[p["id"]] for p in data["parts"] if p["geometry"] and p["visible"]]
all_corners = [obj.matrix_world @ Vector(corner) for obj in visible_meshes for corner in obj.bound_box]
lower = Vector(tuple(min(p[i] for p in all_corners) for i in range(3)))
upper = Vector(tuple(max(p[i] for p in all_corners) for i in range(3)))
dimensions = upper - lower
center = (upper + lower) * 0.5

def camera(name, location, target, scale):
    camera_data = bpy.data.cameras.new(name)
    obj = bpy.data.objects.new(name, camera_data)
    studio_collection.objects.link(obj)
    obj.location = location
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = scale
    camera_data.lens = 50
    return obj

front_camera = camera("Preview_Front", (0, -7, center.z), center, dimensions.z * 1.23)
threequarter_camera = camera("Preview_ThreeQuarter", (4.0, -7.5, 3.15), center, dimensions.z * 1.23)
scene.camera = threequarter_camera
world = bpy.data.worlds.new("Cream_Preview_Background")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = linear_color((1, 0.965, 0.86, 1))
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 1
scene.world = world

# Brown rendered outlines are a preview setting, separate from editable geometry.
scene.render.use_freestyle = True
freestyle = bpy.context.view_layer.freestyle_settings
freestyle.crease_angle = math.radians(135)
line_set = freestyle.linesets[0] if freestyle.linesets else freestyle.linesets.new("Brown_Ink")
if line_set.linestyle is None:
    line_set.linestyle = bpy.data.linestyles.new("Warm_Brown_Ink")
line_set.linestyle.color = linear_color((0.28627452, 0.2, 0.15294118, 1))[:3]
line_set.linestyle.thickness = 1.5
line_set.select_silhouette = True
line_set.select_border = True
line_set.select_contour = True
line_set.select_crease = False
scene.render.resolution_x = 900
scene.render.resolution_y = 1100
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False

readme = """친구 캐릭터 — Blender 모델링 시작 파일

원본: Assets/Test/Scenes/WetCorridor_Test.unity / Friend_Milo
저장된 현재 프로토타입의 자세, 부위별 메시, UV0, 노멀, 색상과 부모 계층을 추출했습니다.
Unity 원본 씬·자산에는 변경을 가하지 않았습니다.

01_Character_Editable: 직접 수정할 캐릭터 부품과 팔/다리 부모 오브젝트.
02_Alternate_Parts_Hidden: 숨겨진 앞머리 3개, 눈 흰자 2개와 놀란 입. 숨김 상태로 보존.
03_Preview_Studio: 정면/사선 미리보기 카메라. 모델링 시 숨겨도 됩니다.

미터 단위, Blender Z축 위 / -Y방향 앞. 캐릭터 루트는 원점에 있습니다.
Unity의 배치 위치와 루트 회전을 제거하고, 원래 1.1배 크기는 유지했습니다.
각 메시 데이터는 독립적이어서 한쪽 팔을 편집해도 다른 부품이 함께 변하지 않습니다.
원본 삼각형 구조를 유지했습니다. 리토폴로지, 결합, 리깅과 스키닝은 적용하지 않았습니다.
원본에는 뼈대 리깅이 없으며 팔·다리를 부모 Transform으로 움직이는 구조입니다.
주머니 스티치는 Unity LineRenderer를 편집 가능한 두께 있는 커브로 바꿨습니다.

뷰포트는 재질 색상으로 표시됩니다. Material Preview/Rendered 또는 F12에서
3단계 셀 명암을 확인할 수 있으며, F12 렌더에는 갈색 Freestyle 외곽선이 포함됩니다.
Blender의 미리보기 셰이더와 외곽선은 Unity URP 셰이더의 근사 재현입니다.
Unity 셰이더 속성 원본은 각 머티리얼의 Custom Properties에 JSON으로 보존했습니다.
외부 텍스처 의존성은 없습니다.

권장: 이 파일을 열어 다른 이름으로 저장한 뒤 필요한 부위부터 모델링을 진행하세요.
"""
bpy.data.texts.new("읽어주세요_README").write(readme)
bpy.data.texts.new("Unity_Export_Summary.json").write(json.dumps({
    "sourceScene": data["sourceScene"], "unityVersion": data["unityVersion"],
    "sourceRootPosition": data["rootPosition"], "sourceRootRotation": data["rootRotation"],
    "sourceMeshCount": sum(bool(p["geometry"]) for p in data["parts"]),
    "sourcePartCount": len(data["parts"]), "sourceMaterials": len(data["materials"]),
    "sourceHasArmature": data["hasArmature"], "warnings": data["warnings"],
}, ensure_ascii=False, indent=2))
(WORK / "README_친구캐릭터.txt").write_text(readme, encoding="utf-8-sig")

# Useful opening view: material colors, three-quarter angle, character selected.
for obj in bpy.context.selected_objects:
    obj.select_set(False)
for obj in visible_meshes:
    obj.select_set(True)
bpy.context.view_layer.objects.active = objects[data["parts"][0]["id"]]
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == "VIEW_3D":
            space = area.spaces.active
            space.shading.type = "SOLID"
            space.shading.color_type = "MATERIAL"
            space.shading.light = "STUDIO"
            space.shading.show_cavity = True
            space.overlay.show_extras = False
            space.region_3d.view_location = center
            space.region_3d.view_distance = dimensions.z * 1.65
            space.region_3d.view_rotation = threequarter_camera.rotation_euler.to_quaternion()
            space.region_3d.view_perspective = "ORTHO"

expected_vertices = sum(len(p["geometry"]["vertices"]) for p in data["parts"] if p["geometry"])
expected_meshes = sum(bool(p["geometry"]) for p in data["parts"])
assert vertex_count == expected_vertices
assert len([o for o in objects.values() if o.type == "MESH"]) == expected_meshes
assert len(materials) == len(data["materials"])
assert not data["hasArmature"]
assert not validation_warnings, validation_warnings
expected_dimensions = Vector((data["boundsSize"][0], data["boundsSize"][2], data["boundsSize"][1]))
# Root rotation can rotate X/Y bounds in Unity. The unrotated export is intentional.
report = {
    "blend": str(OUTPUT), "sourceScene": data["sourceScene"],
    "blenderVersion": bpy.app.version_string, "objectsFromUnity": len(objects),
    "meshes": expected_meshes, "visibleMeshes": len(visible_meshes),
    "vertices": vertex_count, "triangles": face_count, "materials": len(materials),
    "curves": sum(o.type == "CURVE" for o in objects.values()),
    "dimensionsMeters": list(dimensions), "sourceWorldBoundsConverted": list(expected_dimensions),
    "normalImportAPI": normal_api, "warnings": validation_warnings,
    "hiddenParts": [p["name"] for p in data["parts"] if not p["visible"]],
}
scene["Export_Source"] = data["sourceScene"] + "/Friend_Milo"
scene["Export_Report"] = json.dumps(report, ensure_ascii=False)
scene.render.filepath = "//Friend_Milo_Preview.png"
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT))
(WORK / "Export_Validation.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
print("EXPORT_VALIDATED " + json.dumps(report, ensure_ascii=False))

scene.render.filepath = str(WORK / "Friend_Milo_Preview.png")
bpy.ops.render.render(write_still=True)
scene.camera = front_camera
scene.render.filepath = str(WORK / "Friend_Milo_Front.png")
bpy.ops.render.render(write_still=True)
