import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

WORK=Path(__file__).resolve().parent
OUT=WORK/'Delivery'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Wait_Friend_ReferenceV2.blend'))
scene=bpy.context.scene
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
model=[o for o in bpy.data.objects if o.type=='MESH' and 'BindBone' in o]
fail=[]
stats={'meshes':len(model),'bones':len(rig.data.bones),'base_vertices':sum(len(o.data.vertices) for o in model),'base_faces':sum(len(o.data.polygons) for o in model)}
bad_weights=[]
for o in model:
    for v in o.data.vertices:
        if not all(math.isfinite(f) for f in v.co): fail.append('Nonfinite vertex: '+o.name); break
        total=sum(g.weight for g in v.groups)
        if abs(total-1)>1e-4: bad_weights.append((o.name,v.index,total))
    if not o.data.materials: fail.append('No material: '+o.name)
    for group in o.vertex_groups:
        if group.name not in rig.data.bones: fail.append('Missing bone: '+group.name)
    ar=[m for m in o.modifiers if m.type=='ARMATURE']
    if not ar or ar[0].object!=rig: fail.append('Missing armature link: '+o.name)
if bad_weights: fail.append('Invalid vertex weight sums: '+str(bad_weights[:10]))
for img in bpy.data.images:
    if img.source=='FILE' and not img.packed_file: fail.append('Unpacked external image: '+img.name)
for m in bpy.data.materials:
    if m.use_nodes:
        out=next((n for n in m.node_tree.nodes if n.type=='OUTPUT_MATERIAL' and n.is_active_output),None)
        if out is None or not out.inputs['Surface'].is_linked: fail.append('Unconnected material: '+m.name)

bounds={}
hands={}
for frame in (1,24):
    scene.frame_set(frame); bpy.context.view_layer.update()
    dg=bpy.context.evaluated_depsgraph_get(); coords=[]
    for o in model:
        eo=o.evaluated_get(dg); me=eo.to_mesh()
        for v in me.vertices:
            p=eo.matrix_world @ v.co
            if not all(math.isfinite(f) for f in p): fail.append('Nonfinite evaluated position: '+o.name)
            coords.append(p.copy())
        if o.name.startswith('Hand • soft mitten.'):
            hands[(frame,o.name)]=sum((eo.matrix_world@v.co for v in me.vertices),Vector())/len(me.vertices)
        eo.to_mesh_clear()
    lo=Vector(tuple(min(p[i] for p in coords) for i in range(3)))
    hi=Vector(tuple(max(p[i] for p in coords) for i in range(3)))
    bounds[str(frame)]={'min':list(lo),'max':list(hi),'dimensions':list(hi-lo)}
for name in [o.name for o in model if o.name.startswith('Hand • soft mitten.')]:
    moved=(hands[(24,name)]-hands[(1,name)]).length
    if moved<.08: fail.append('Hand pose does not move sufficiently: '+name)
stats['bounds']=bounds
stats['hand_centres']={str(k):list(v) for k,v in hands.items()}
stats['packed_images']=[i.name for i in bpy.data.images if i.packed_file]
stats['default_pose_frame']=24
stats['failures']=fail
stats['pass']=not fail
(WORK/'Reopen_Validation.json').write_text(json.dumps(stats,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(stats,ensure_ascii=True,indent=2),flush=True)
if fail: raise RuntimeError('Character verification failed')
