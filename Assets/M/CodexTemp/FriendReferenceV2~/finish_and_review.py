import bpy
import json
import math
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion

WORK=Path(__file__).resolve().parent
OUT=WORK/'Delivery'
BLEND=OUT/'Wait_Friend_ReferenceV2.blend'
S=.3
bpy.ops.wm.open_mainfile(filepath=str(BLEND))
scene=bpy.context.scene
rig=bpy.data.objects.get('Wait_Friend_Rig • Pose Mode')
scene.render.resolution_percentage=100
bpy.context.preferences.filepaths.save_version=0

# Hollow leg openings: fabric no longer intersects the solid caps of the skin legs.
shorts=bpy.data.objects['Shorts • unified tailored shape']
bpy.ops.object.select_all(action='DESELECT')
for sign in (-1,1):
    bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=.195*S,depth=.42*S,location=(sign*.34*S,0,.90*S))
    cutter=bpy.context.object; cutter.name='TEMP_LegOpening'
    bpy.context.view_layer.objects.active=shorts
    mod=shorts.modifiers.new('Tailored leg opening','BOOLEAN'); mod.operation='DIFFERENCE'; mod.solver='EXACT'; mod.object=cutter
    while list(shorts.modifiers).index(mod)>0: bpy.ops.object.modifier_move_up(modifier=mod.name)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter,do_unlink=True)
shorts.vertex_groups.clear()
groups={n:shorts.vertex_groups.new(name=n) for n in ('pelvis','thigh.L','thigh.R')}
for v in shorts.data.vertices:
    w=max(0,min(1,(v.co.z/S-1.04)/.20)); side='L' if v.co.x>0 else 'R'
    if w: groups['pelvis'].add([v.index],w,'REPLACE')
    if w<1: groups['thigh.'+side].add([v.index],1-w,'REPLACE')

# Bring the bangs slightly closer to the forehead for the profile view.
for ob in bpy.data.objects:
    if ob.type=='MESH' and ob.name.startswith('Hair fringe'):
        cy=sum(v.co.y for v in ob.data.vertices)/len(ob.data.vertices)
        for v in ob.data.vertices: v.co.y=cy+(v.co.y-cy)*.9+.04*S

# Two keyed poses, neutral at frame 1 and holding the backpack straps at frame 24.
pose_bones=[rig.pose.bones[n+'.'+s] for n in ('upper_arm','forearm') for s in ('L','R')]
scene.frame_start=1; scene.frame_end=24
scene.frame_set(1)
for pb in pose_bones:
    pb.rotation_mode='QUATERNION'; pb.rotation_quaternion=Quaternion()
    pb.keyframe_insert('rotation_quaternion',frame=1,group=pb.name)

scene.frame_set(24)
for sign,side in [(-1,'R'),(1,'L')]:
    upper=rig.pose.bones['upper_arm.'+side]; fore=rig.pose.bones['forearm.'+side]
    shoulder=upper.bone.head_local.copy()
    wrist=Vector((sign*.68,-.52,2.07))*S
    line=wrist-shoulder; d=line.length; direction=line.normalized()
    l1=upper.bone.length; l2=fore.bone.length
    a=(l1*l1-l2*l2+d*d)/(2*d)
    height=math.sqrt(max(0,l1*l1-a*a))
    pole=Vector((sign,0,0)); pole=(pole-direction*pole.dot(direction)).normalized()
    elbow=shoulder+direction*a+pole*height
    for pb,head,tail in ((upper,shoulder,elbow),(fore,elbow,wrist)):
        original=(pb.bone.tail_local-pb.bone.head_local).normalized()
        q=original.rotation_difference((tail-head).normalized())
        desired=q.to_matrix() @ pb.bone.matrix_local.to_3x3()
        matrix=desired.to_4x4(); matrix.translation=head
        pb.matrix=matrix
        bpy.context.view_layer.update()
        pb.keyframe_insert('rotation_quaternion',frame=24,group=pb.name)
if rig.animation_data and rig.animation_data.action:
    rig.animation_data.action.name='POSES • 01 Relaxed → 24 Backpack hold'
scene.timeline_markers.new('01 • 편안한 기본 자세',frame=1)
scene.timeline_markers.new('24 • 가방끈 잡는 자세',frame=24)

readme=bpy.data.texts['읽어주세요 • README']
extra='\n추가 포즈: 타임라인 1프레임은 기본 자세, 24프레임은 가방끈 잡기입니다.\n사선 렌더는 24프레임, 정면/측면/후면 렌더는 1프레임입니다.\n기본 FK 뼈대로 만든 시작 포즈이며, 큰 각도 변형은 추가 스키닝 작업이 필요합니다.\n'
readme.write(extra)
(OUT/'README_친구캐릭터.txt').write_text(readme.as_string(),encoding='utf-8-sig')

hero=bpy.data.objects['CAM_01 • Three-quarter']
scene.camera=hero
scene.frame_set(24)
scene.render.filepath='//Wait_Friend_Hero.png'
# Prefer the rendered viewport's deterministic geometric toon materials.
for screen in bpy.data.screens:
    for a in screen.areas:
        if a.type=='VIEW_3D':
            a.spaces.active.shading.type='MATERIAL'

bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
for name,cam,frame in [('Hero',hero,24),('Front',bpy.data.objects['CAM_02 • Front'],1),('Side',bpy.data.objects['CAM_03 • Right profile'],1),('Back',bpy.data.objects['CAM_04 • Back'],1),('Back_ThreeQuarter',bpy.data.objects['CAM_05 • Backpack'],24)]:
    scene.frame_set(frame); scene.camera=cam
    scene.render.filepath=str(OUT/('Wait_Friend_'+name+'.png'))
    bpy.ops.render.render(write_still=True)
    print('FINAL_RENDER '+name,flush=True)
print('FINAL_RENDER_ALL_DONE',flush=True)
