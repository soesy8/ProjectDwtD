import bpy
import json
from pathlib import Path
from mathutils import Vector

ROOT=Path(r'D:\project\ProjectWait')
WORK=ROOT/'Assets/M/CodexTemp/FriendIntegration~'
MODEL=ROOT/'Assets/M/Arts/Models/Friend'
SOURCE=MODEL/'Source~/Wait_Friend_Edited.blend'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
char=bpy.data.collections.get('01_CHARACTER • 부위별 편집')
objects=[o for o in (char.all_objects if char else rig.children_recursive) if o.type=='MESH' and not o.hide_render]
if not objects: raise RuntimeError('Character meshes could not be identified')

def srgb(v): return 12.92*v if v<=.0031308 else 1.055*max(v,0)**(1/2.4)-.055
records=[]
seen=set()
for ob in objects:
    for mat in ob.data.materials:
        if mat is None or mat.name in seen: continue
        seen.add(mat.name)
        color=tuple(mat.diffuse_color); flat=False
        if mat.use_nodes:
            nodes=mat.node_tree.nodes
            em=next((n for n in nodes if n.type=='EMISSION'),None)
            ramp=next((n for n in nodes if n.type=='VALTORGB'),None)
            principled=next((n for n in nodes if n.type=='BSDF_PRINCIPLED'),None)
            if em and not em.inputs['Color'].is_linked:
                color=tuple(em.inputs['Color'].default_value); flat=True
            elif ramp: color=tuple(max(ramp.color_ramp.elements,key=lambda e:e.position).color)
            elif principled: color=tuple(principled.inputs['Base Color'].default_value)
        records.append({'name':mat.name,'linear':color,'srgb':[srgb(c) for c in color[:3]]+[color[3]],'flat':flat})
        # FBX carries simple diffuse materials; Unity replaces them with M-owned toon materials.
        mat.use_nodes=True; n=mat.node_tree.nodes; n.clear()
        out=n.new('ShaderNodeOutputMaterial'); p=n.new('ShaderNodeBsdfPrincipled')
        p.inputs['Base Color'].default_value=color; p.inputs['Roughness'].default_value=1
        p.inputs['Specular IOR Level'].default_value=0
        mat.node_tree.links.new(p.outputs['BSDF'],out.inputs['Surface']); mat.diffuse_color=color

bpy.context.scene.frame_set(1)
rig.animation_data_clear()
for pb in rig.pose.bones: pb.matrix_basis.identity()
rig.data.pose_position='REST'
for ob in objects:
    for mod in ob.modifiers:
        if mod.type=='SUBSURF': mod.render_levels=mod.levels
bpy.context.view_layer.update()
deps=bpy.context.evaluated_depsgraph_get(); vertex_count=0; points=[]
for ob in objects:
    ev=ob.evaluated_get(deps); me=ev.to_mesh(); vertex_count+=len(me.vertices)
    points.extend(ev.matrix_world@v.co for v in me.vertices); ev.to_mesh_clear()
lo=[min(p[i] for p in points) for i in range(3)]; hi=[max(p[i] for p in points) for i in range(3)]

# Preserve all user-authored mesh edits; exclude studio cameras, lights and packed reference.
bpy.ops.object.select_all(action='DESELECT')
for col in rig.users_collection: col.hide_viewport=False
for ob in objects+[rig]:
    ob.hide_set(False); ob.hide_viewport=False; ob.select_set(True)
bpy.context.view_layer.objects.active=rig
path=MODEL/'Wait_Friend_Edited.fbx'
bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'ARMATURE','MESH'},
    global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
    use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,primary_bone_axis='Y',
    secondary_bone_axis='X',armature_nodetype='NULL',use_armature_deform_only=False,
    bake_anim=False,path_mode='AUTO',embed_textures=False)
report={'source':str(SOURCE),'fbx':str(path),'meshes':len(objects),'vertices_evaluated':vertex_count,
    'bounds_blender':{'min':lo,'max':hi},'materials':records,
    'bones':[{'name':b.name,'head':list(b.head_local),'tail':list(b.tail_local),'parent':b.parent.name if b.parent else None} for b in rig.data.bones]}
(MODEL/'Friend_MaterialManifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
(WORK/'Export_Validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('FBX_EXPORT_OK '+json.dumps({k:report[k] for k in ('meshes','vertices_evaluated','bounds_blender')},ensure_ascii=True),flush=True)
