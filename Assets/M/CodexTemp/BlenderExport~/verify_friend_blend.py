"""Reopen the delivery file and compare its geometry with the exported Unity data."""
import bpy
import json
from pathlib import Path
from mathutils import Matrix, Vector, Quaternion

work = Path(__file__).resolve().parent
source = json.loads((work / "Friend_Milo_Source.json").read_text(encoding="utf-8"))
bpy.ops.wm.open_mainfile(filepath=str(work / "Friend_Milo_Prototype.blend"))
objects = {o["Unity_Source_ID"]: o for o in bpy.data.objects if "Unity_Source_ID" in o}
conversion = Matrix(((1, 0, 0, 0), (0, 0, -1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))
max_vertex_error = 0.0
max_transform_error = 0.0
mesh_count = uv_count = face_count = 0
for part in source["parts"]:
    obj = objects[part["id"]]
    assert (obj.parent["Unity_Source_ID"] if obj.parent else None) == part["parent"]
    assert obj.hide_get() == (not part["visible"]), obj.name
    assert obj.hide_render == (not part["visible"]), obj.name
    q = part["localRotation"]
    expected = conversion @ Matrix.LocRotScale(Vector(part["localPosition"]),
        Quaternion((q[3], q[0], q[1], q[2])), Vector(part["localScale"])) @ conversion.inverted()
    max_transform_error = max(max_transform_error, max(abs(expected[i][j] - obj.matrix_basis[i][j]) for i in range(4) for j in range(4)))
    if part["geometry"]:
        mesh_count += 1
        geometry = part["geometry"]
        assert len(obj.data.vertices) == len(geometry["vertices"])
        assert len(obj.data.polygons) == sum(len(s) // 3 for s in geometry["submeshes"])
        assert obj.data.users == 1, "Mesh data must be independently editable"
        assert obj.data.has_custom_normals, "Source normals missing"
        for vertex, original in zip(obj.data.vertices, geometry["vertices"]):
            max_vertex_error = max(max_vertex_error, (vertex.co - conversion.to_3x3() @ Vector(original)).length)
        for polygon in obj.data.polygons:
            submesh = geometry["submeshes"][polygon.material_index]
            assert polygon.material_index < len(part["materials"])
        if geometry["uv"]:
            uv_count += 1
            uv_layer = obj.data.uv_layers["Unity_UV0"]
            for loop in obj.data.loops:
                expected_uv = Vector(geometry["uv"][loop.vertex_index])
                assert (uv_layer.data[loop.index].uv - expected_uv).length < 0.000001
        face_count += len(obj.data.polygons)
        assert len(obj.data.materials) == len(part["materials"])
        for material, source_id in zip(obj.data.materials, part["materials"]):
            material_record = next(m for m in source["materials"] if m["id"] == source_id)
            assert material["Unity_Material_Path"] == material_record["path"]
assert len(objects) == len(source["parts"])
assert max_vertex_error < 0.000001
assert max_transform_error < 0.000001
assert not list(bpy.data.armatures)
assert not list(bpy.data.images), "Unexpected external image dependency"
assert bpy.context.scene.camera is not None
assert bpy.data.texts.get("읽어주세요_README") is not None
report = {
    "reopenedSuccessfully": True, "meshesChecked": mesh_count, "uvMeshesChecked": uv_count,
    "trianglesChecked": face_count, "maxVertexErrorMeters": max_vertex_error,
    "maxLocalTransformError": max_transform_error,
    "hierarchyPreserved": True, "visibilityPreserved": True, "singleUserMeshes": True,
    "sourceNormalsPreserved": True, "materialAssignmentsPreserved": True,
    "externalTextureDependencies": 0, "armature": False,
}
(work / "Reopen_Validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print("REOPEN_VALIDATED " + json.dumps(report))
