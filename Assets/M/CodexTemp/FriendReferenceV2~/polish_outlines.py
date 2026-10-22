import bpy
from pathlib import Path
WORK=Path(__file__).resolve().parent
OUT=WORK/'Delivery'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Wait_Friend_ReferenceV2.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
detail=bpy.data.collections['02_Face_and_Stitch_Details']
for ob in bpy.data.objects:
    if ob.name.startswith('Leg skin.'):
        for col in list(ob.users_collection): col.objects.unlink(ob)
        detail.objects.link(ob)
for lines in bpy.context.view_layer.freestyle_settings.linesets:
    lines.linestyle.use_length_min=True
    lines.linestyle.length_min=16
scene.frame_set(24)
scene.camera=bpy.data.objects['CAM_01 • Three-quarter']
scene.render.filepath='//Wait_Friend_Hero.png'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Wait_Friend_ReferenceV2.blend'))
for name,camera,frame in [('Hero','CAM_01 • Three-quarter',24),('Front','CAM_02 • Front',1),('Side','CAM_03 • Right profile',1),('Back','CAM_04 • Back',1),('Back_ThreeQuarter','CAM_05 • Backpack',24)]:
    scene.frame_set(frame); scene.camera=bpy.data.objects[camera]
    scene.render.filepath=str(OUT/('Wait_Friend_'+name+'.png'))
    bpy.ops.render.render(write_still=True)
    print('POLISHED '+name,flush=True)
print('POLISH_ALL_DONE',flush=True)
