import bpy
from pathlib import Path
W=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(W/'Delivery'/'Wait_Friend_ReferenceV2.blend'))
s=bpy.context.scene
s.frame_set(1); s.camera=bpy.data.objects['CAM_02 • Front']; s.render.resolution_percentage=70
detail=bpy.data.collections['02_Face_and_Stitch_Details']
for o in bpy.data.objects:
    if o.name.startswith('Leg skin.'):
        for c in list(o.users_collection): c.objects.unlink(o)
        detail.objects.link(o)
s.render.filepath=str(W/'Test_LegOutlinesOff.png')
bpy.ops.render.render(write_still=True)
print('TEST_DONE',flush=True)
