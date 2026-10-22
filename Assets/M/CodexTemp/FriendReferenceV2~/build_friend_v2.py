"""Reference-based Blender character. All build/QA output stays beside this script."""
import bpy
import bmesh
import math
import json
import random
from pathlib import Path
from mathutils import Vector, Matrix
from math import sin, cos, pi, sqrt

WORK = Path(__file__).resolve().parent
OUT = WORK / 'Delivery'
OUT.mkdir(parents=True, exist_ok=True)
S = 0.30
random.seed(12)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.name = 'Wait! | Friend • Reference V2'
scene.render.engine = 'BLENDER_EEVEE'
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'None'
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.render.resolution_x = 1100
scene.render.resolution_y = 1280
scene.render.resolution_percentage = 100
scene.render.fps = 24

def collection(name, parent=None):
    c = bpy.data.collections.new(name)
    (parent or scene.collection).children.link(c)
    return c

character = collection('01_CHARACTER • 부위별 편집')
ink = collection('01_Outlined_Forms', character)
details = collection('02_Face_and_Stitch_Details', character)
rigcol = collection('02_RIG • 기본 FK 포즈')
studio = collection('03_STUDIO • 카메라와 조명')
reference = collection('04_REFERENCE • 원본 이미지 (숨김)')
reference.hide_render = True
parts = []

def linear(v):
    return v / 12.92 if v <= .04045 else ((v + .055) / 1.055) ** 2.4

def rgba(hexcode):
    h = hexcode.lstrip('#')
    return tuple(linear(int(h[i:i+2], 16) / 255) for i in (0, 2, 4)) + (1,)

materials = {}
def toon(name, hexcode, flat=False):
    m = bpy.data.materials.new(name)
    m.diffuse_color = rgba(hexcode)
    m.use_nodes = True
    m['BaseColor_sRGB'] = hexcode
    m['Shading'] = 'Three bands; geometric normals; narrow transitions; Blender preview'
    n, l = m.node_tree.nodes, m.node_tree.links
    n.clear()
    output = n.new('ShaderNodeOutputMaterial'); output.location = (610, 0)
    emission = n.new('ShaderNodeEmission'); emission.location = (400, 0)
    l.new(emission.outputs[0], output.inputs['Surface'])
    if flat:
        emission.inputs[0].default_value = rgba(hexcode)
    else:
        geom = n.new('ShaderNodeNewGeometry'); geom.location = (-700, 0)
        dot = n.new('ShaderNodeVectorMath'); dot.operation = 'DOT_PRODUCT'
        dot.location = (-490, 0)
        dot.inputs[1].default_value = Vector((-.40, -1.0, .75)).normalized()
        l.new(geom.outputs['Normal'], dot.inputs[0])
        mapped = n.new('ShaderNodeMath'); mapped.operation = 'MULTIPLY_ADD'
        mapped.location = (-290, 0)
        mapped.inputs[1].default_value = .5; mapped.inputs[2].default_value = .5
        l.new(dot.outputs['Value'], mapped.inputs[0])
        ramp = n.new('ShaderNodeValToRGB'); ramp.location = (-60, 0)
        ramp.label = 'Shadow / midtone / sunlight · 3단계 셀 명암'
        ramp.color_ramp.interpolation = 'LINEAR'
        rgb = [int(hexcode.lstrip('#')[i:i+2], 16) / 255 for i in (0,2,4)]
        def shade(f): return tuple(linear(rgb[i] * f[i]) for i in range(3)) + (1,)
        low, mid, high = shade((.79,.79,.76)), shade((.92,.91,.89)), rgba(hexcode)
        stops = [(0,low),(.504,low),(.516,mid),(.804,mid),(.816,high),(1,high)]
        for i, (p, c) in enumerate(stops):
            el = ramp.color_ramp.elements[i] if i < 2 else ramp.color_ramp.elements.new(p)
            el.position = p; el.color = c
        l.new(mapped.outputs[0], ramp.inputs[0]); l.new(ramp.outputs[0], emission.inputs[0])
    materials[name] = m
    return m

skin = toon('Skin • #FFC08A', '#FFC08A')
inner_ear = toon('Ear warm coral', '#F29A6C')
cheek = toon('Cheeks • soft coral', '#FA9670', True)
hair = [toon('Hair chestnut %02d' % i, h) for i,h in enumerate(('#603B29','#68412E','#583322','#704832','#633D2B'))]
orange = toon('Hoodie • #FF8C2D', '#FF8C2D')
cuffmat = toon('Cuff & hem • #E87927', '#E87927')
orange_seam = toon('Orange stitch', '#BB6323')
hood_inner = toon('Hood lining', '#DA7026')
shortmat = toon('Shorts • #3B3E48', '#3B3E48')
shortseam = toon('Shorts seam', '#30313B')
teal = toon('Backpack teal', '#38958C')
strapmat = toon('Shoulder straps', '#277D76')
tealdark = toon('Teal piping', '#205F59')
teallight = toon('Teal stitch', '#5CADA0')
paper = toon('Shoes & bag label • warm cream', '#F6EFD9')
solemat = toon('Soles • warm grey', '#D3D1C4')
sockmat = toon('Socks • ivory', '#FFF6DF')
inkmat = toon('Ink • #493327', '#493327', True)
eye = toon('Eyes • espresso', '#28231D', True)
white = toon('Eye glints / teeth', '#FFF8E8', True)
mouthmat = toon('Mouth inside', '#73382D', True)
tongue = toon('Tongue', '#EC7B6C', True)

def link_obj(obj, col):
    for old in list(obj.users_collection): old.objects.unlink(obj)
    col.objects.link(obj)

def finish(obj, name, material, bone='spine', outlined=True, smooth=True):
    obj.name = name
    link_obj(obj, ink if outlined else details)
    if material: obj.data.materials.append(material)
    if obj.type == 'MESH':
        for p in obj.data.polygons: p.use_smooth = smooth
    obj['Part'] = name
    obj['BindBone'] = bone
    parts.append(obj)
    return obj

def mesh(name, verts, faces, material, bone='spine', outlined=True, sub=0):
    me = bpy.data.meshes.new(name + '_Mesh')
    me.from_pydata(verts, [], faces); me.update()
    ob = bpy.data.objects.new(name, me); ink.objects.link(ob)
    bm = bmesh.new(); bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces)); bm.to_mesh(me); bm.free()
    finish(ob,name,material,bone,outlined)
    if sub:
        mod=ob.modifiers.new('Soft quad surface','SUBSURF'); mod.levels=sub; mod.render_levels=sub
    return ob

def ellipsoid(name, loc, scale, mat, bone='spine', rot=(0,0,0), outlined=True, seg=40, rings=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, location=loc)
    ob=bpy.context.object; ob.scale=scale; ob.rotation_euler=rot
    return finish(ob,name,mat,bone,outlined)

def roundbox(name, loc, dims, bevel, mat, bone='spine', outlined=True, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    ob=bpy.context.object; ob.dimensions=dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.rotation_euler=rot
    finish(ob,name,mat,bone,outlined)
    bev=ob.modifiers.new('Rounded tailored edges','BEVEL'); bev.width=bevel; bev.segments=5
    nor=ob.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL'); nor.keep_sharp=True
    return ob

def curve(name, coords, radius, mat, bone='spine', cyclic=False, outlined=False):
    cu=bpy.data.curves.new(name+'_Curve','CURVE'); cu.dimensions='3D'
    cu.resolution_u=16; cu.bevel_depth=radius; cu.bevel_resolution=3; cu.use_fill_caps=True
    sp=cu.splines.new('BEZIER'); sp.bezier_points.add(len(coords)-1)
    for b,p in zip(sp.bezier_points,coords):
        b.co=p; b.handle_left_type='AUTO'; b.handle_right_type='AUTO'
    sp.use_cyclic_u=cyclic
    ob=bpy.data.objects.new(name,cu); details.objects.link(ob)
    return finish(ob,name,mat,bone,outlined)

def loft(name, rings, mat, bone='spine', n=48, sub=1):
    # (z, center_x, center_y, x_radius, y_radius), ordered bottom to top.
    vs=[]; fs=[]
    for z,x,y,rx,ry in rings:
        for j in range(n):
            a=2*pi*j/n; vs.append((x+rx*cos(a),y+ry*sin(a),z))
    for k in range(len(rings)-1):
        for j in range(n): fs.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
    fs.append(tuple(reversed(range(n))))
    fs.append(tuple((len(rings)-1)*n+j for j in range(n)))
    return mesh(name,vs,fs,mat,bone,True,sub)

def tube(name, centers, radii, mat, bone, n=24, sub=1):
    vs=[]; fs=[]
    pts=[Vector(p) for p in centers]
    for i,(p,r) in enumerate(zip(pts,radii)):
        t=(pts[min(i+1,len(pts)-1)]-pts[max(0,i-1)]).normalized()
        u=t.cross(Vector((0,1,0))).normalized(); v=t.cross(u).normalized()
        rx,ry = r if isinstance(r,tuple) else (r,r)
        for j in range(n):
            a=j*2*pi/n; vs.append(p+u*(rx*cos(a))+v*(ry*sin(a)))
    for k in range(len(pts)-1):
        for j in range(n): fs.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
    fs.extend([tuple(reversed(range(n))),tuple((len(pts)-1)*n+j for j in range(n))])
    return mesh(name,vs,fs,mat,bone,True,sub)

def catmull(points, steps=6):
    p=[Vector(points[0])]+[Vector(q) for q in points]+[Vector(points[-1])]
    out=[]
    for i in range(1,len(p)-2):
        for j in range(steps):
            t=j/steps; a,b,c,d=p[i-1:i+3]
            out.append(.5*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t))
    out.append(Vector(points[-1])); return out

def ribbon(name, points, width, thick, mat):
    pts=catmull(points,7); vs=[]; fs=[]; n=12
    for i,p in enumerate(pts):
        tangent=(pts[min(i+1,len(pts)-1)]-pts[max(i-1,0)]).normalized()
        u=Vector((1,0,0)); v=tangent.cross(u).normalized()
        for j in range(n):
            a=2*pi*j/n; vs.append(p+u*(width*.5*cos(a))+v*(thick*.5*sin(a)))
    for i in range(len(pts)-1):
        for j in range(n): fs.append((i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j))
    fs.extend([tuple(reversed(range(n))),tuple((len(pts)-1)*n+j for j in range(n))])
    return mesh(name,vs,fs,mat,'spine')

# Oversized, gently pear-shaped head; broad cheeks and rounded jaw.
head=ellipsoid('Head • 둥근 얼굴', (0,0,3.34),(1.025,.735,.83),skin,'head',seg=64,rings=40)
for v in head.data.vertices:
    z=v.co.z
    v.co.x *= 1.0 + .055*math.exp(-((z+.23)/.42)**2)

def face_y(x,z,offset=0):
    q=max(.04,1-(x/1.055)**2-((z-3.34)/.83)**2)
    return -.735*sqrt(q)-offset

ellipsoid('Neck', (0,.01,2.61),(.245,.23,.30),skin,'neck')
for sign,side in [(-1,'R'),(1,'L')]:
    ellipsoid('Ear.'+side,(sign*1.005,.01,3.24),(.205,.15,.272),skin,'head')
    ellipsoid('Ear inner.'+side,(sign*1.045,-.131,3.25),(.112,.018,.17),inner_ear,'head',outlined=False)
    x=sign*.355; z=3.385; y=face_y(x,z,.023)
    # Align the eyes to the curved face rather than placing them on a flat plane.
    eyeob=ellipsoid('Eye.'+side,(x,y,z),(.088,.038,.159),eye,'head',rot=(0,0,sign*.22))
    ellipsoid('Eye highlight.'+side,(x-.023,y-.038,z+.066),(.025,.013,.039),white,'head',outlined=False,seg=24,rings=16)
    ellipsoid('Eye small glint.'+side,(x+.016,y-.040,z-.052),(.010,.007,.014),white,'head',outlined=False,seg=16,rings=12)
    x=sign*.646; z=3.105
    ellipsoid('Cheek blush.'+side,(x,face_y(x,z,.014),z),(.139,.015,.103),cheek,'head',rot=(0,0,sign*.53),outlined=False)

ellipsoid('Nose • 작은 둥근 코',(0,face_y(0,3.16,.04),3.16),(.112,.088,.085),skin,'head',seg=32,rings=20)

def face_patch(name, points, mat, offset):
    cx=sum(p[0] for p in points)/len(points); cz=sum(p[1] for p in points)/len(points)
    verts=[(cx,face_y(cx,cz,offset),cz)]+[(x,face_y(x,z,offset),z) for x,z in points]
    faces=[(0,i+1,(i+1)%len(points)+1) for i in range(len(points))]
    return mesh(name,verts,faces,mat,'head',False)

mouth_pts=[(-.225,3.025),(-.21,2.951),(-.175,2.882),(-.105,2.833),(0,2.813),(.105,2.833),(.175,2.882),(.21,2.951),(.225,3.025),(.12,3.018),(0,3.014),(-.12,3.018)]
face_patch('Smile • 열린 웃는 입',mouth_pts,mouthmat,.025)
curve('Smile brown rim',[(x,face_y(x,z,.033),z) for x,z in mouth_pts],.012,inkmat,'head',True)
face_patch('Upper milk teeth',[(-.145,3.014),(-.14,2.975),(-.11,2.965),(.11,2.965),(.14,2.975),(.145,3.014)],white,.041)
face_patch('Tongue smile',[(-.12,2.854),(-.10,2.884),(-.05,2.901),(.04,2.903),(.10,2.884),(.123,2.858),(.07,2.834),(0,2.825),(-.07,2.835)],tongue,.045)

# A continuous scalp under overlapping sculptural locks prevents holes in profile/back views.
vs=[]; fs=[]; nr=20; ns=64
for i in range(nr+1):
    t=max(.001,i/nr)
    for j in range(ns):
        a=2*pi*j/ns
        end=1.75+.75*sin(a) # front (-Y) higher, rear down to nape
        p=t*end
        wave=1+.017*cos(7*a+1.2)*t*t
        vs.append((1.063*sin(p)*cos(a)*wave,.775*sin(p)*sin(a)*wave,3.39+.825*cos(p)))
for i in range(nr):
    for j in range(ns): fs.append((i*ns+j,i*ns+(j+1)%ns,(i+1)*ns+(j+1)%ns,(i+1)*ns+j))
fs.append(tuple(reversed(range(ns)))); fs.append(tuple(nr*ns+j for j in range(ns)))
mesh('Hair • continuous scalloped cap',vs,fs,hair[0],'head')

# Layered back locks, broad enough to read as a designed hairstyle rather than fine strands.
for row in range(3):
    for i in range(7):
        a=.12+(pi-.24)*i/6
        xx=(.86,.90,.73)[row]*cos(a)
        yy=(.59,.64,.51)[row]*sin(a)+.09
        zz=(3.75,3.39,3.015)[row]+.035*cos(i*1.9+row)
        ob=ellipsoid('Hair back row%d lock%02d'%(row+1,i+1),(xx,yy,zz),(.30,.25,(.36,.35,.34)[row]),hair[(i+row)%len(hair)],'head',rot=(.16*cos(a),.22*cos(a)+.08*sin(i*2),a-pi/2))

for i in range(5):
    x=(i-2)*.35
    ellipsoid('Hair crown lock%02d'%i,(x,.02,4.035-abs(x)*.09),(.34,.37,.24),hair[(i+1)%5],'head',rot=(.03,.15,0))

for i,x in enumerate((-.91,-.60,-.23,.16,.54,.88)):
    z=3.86+.13*(1-abs(x))
    y=-.51-.18*(1-abs(x))
    ob=ellipsoid('Hair fringe • leaf %02d'%(i+1),(x,y,z),(.295,.245,.355),hair[(i+1)%5],'head',rot=(.08,.42,.08*x))
    # Tighten the lower end slightly, preserving an editable quad mesh.
    for v in ob.data.vertices:
        if v.co.z < 0: v.co.x *= 1+.16*v.co.z

for i,(x,a) in enumerate(((-.115,-.38),(.105,.42))):
    ellipsoid('Hair tuft %d'%(i+1),(x,.03,4.265),(.102,.105,.205),hair[i+1],'head',rot=(.05,a,0),seg=32,rings=20)

# Hoodie, a single quad loft with a broad soft body and fitted hem.
body=loft('Hoodie • main body and tailored hem',[(1.245,0,0,.59,.35),(1.255,0,0,.64,.385),(1.285,0,0,.66,.397),(1.375,0,0,.675,.403),(1.41,0,0,.68,.405),(1.80,0,0,.65,.43),(2.10,0,0,.61,.39),(2.30,0,.01,.56,.34),(2.40,0,.02,.44,.28),(2.48,0,.01,.27,.22)],orange,n=48,sub=2)
body.data.materials.append(cuffmat)
for p in body.data.polygons:
    if p.center.z < 1.375: p.material_index=1

# Lowered hood, full teardrop volume at the back of the neck.
hood=ellipsoid('Hood • lowered orange hood',(0,.40,2.46),(.69,.46,.27),orange,'spine',seg=48,rings=28)
for v in hood.data.vertices:
    if v.co.z<0: v.co.x *= 1+.32*v.co.z
curve('Hood rear centre seam',[(0,.76,2.62),(0,.867,2.49),(0,.83,2.36),(0,.65,2.23)],.010,orange_seam)
curve('Hood folded rim',[(-.60,.22,2.49),(-.46,-.13,2.51),(0,-.255,2.47),(.46,-.13,2.51),(.60,.22,2.49)],.047,cuffmat)

# Soft raised kangaroo pocket, curved with the front of the torso.
pocket=[(-.39,1.49),(-.43,1.53),(-.37,1.81),(-.32,1.91),(.32,1.91),(.37,1.81),(.43,1.53),(.39,1.49)]
pv=[]
for depth in (0,.043):
    for x,z in pocket: pv.append((x,-.438-depth+.075*(abs(x)/.43)**2,z))
pf=[tuple(reversed(range(8))),tuple(8+i for i in range(8))]
for i in range(8): pf.append((i,(i+1)%8,(i+1)%8+8,i+8))
po=mesh('Hoodie • kangaroo pocket',pv,pf,orange)
bev=po.modifiers.new('Soft pocket corners','BEVEL'); bev.width=.045; bev.segments=4
curve('Pocket U-shaped stitch',[(-.34,-.462,1.86),(-.405,-.465,1.59),(-.35,-.48,1.52),(0,-.485,1.51),(.35,-.48,1.52),(.405,-.465,1.59),(.34,-.462,1.86)],.010,orange_seam)

for sign,side in [(-1,'R'),(1,'L')]:
    def mir(p): return (p[0]*sign,p[1],p[2])
    sleeve=tube('Hoodie sleeve.'+side,[mir(p) for p in ((.52,.0,2.29),(.62,0,2.24),(.78,-.015,2.07),(.89,-.045,1.91),(.93,-.08,1.72),(.93,-.09,1.64))],[(.22,.24),(.245,.245),(.24,.23),(.235,.215),(.195,.18),(.17,.17)],orange,'sleeve.'+side,n=32,sub=2)
    tube('Cuff.'+side,[mir(p) for p in ((.929,-.088,1.71),(.937,-.096,1.67),(.942,-.102,1.58),(.94,-.106,1.565))],[.179,.182,.173,.159],cuffmat,'forearm.'+side,n=32,sub=1)
    ellipsoid('Hand palm.'+side,mir((.946,-.10,1.442)),(.167,.135,.186),skin,'hand.'+side)
    for k in range(4):
        ellipsoid('Finger.%s.%d'%(side,k+1),mir((.836+k*.069,-.116,1.355+(.012 if k in(0,3) else -.008))),(.044,.105,.074),skin,'hand.'+side,outlined=False,seg=24,rings=16)
    ellipsoid('Thumb.'+side,mir((.819,-.183,1.46)),(.085,.084,.12),skin,'hand.'+side,rot=(0,sign*.3,0))
    for k in range(2):
        x=sign*(.91+k*.066)
        curve('Finger crease.%s.%d'%(side,k),[(x,-.222,1.342),(x,-.239,1.374),(x,-.233,1.409)],.0045,inner_ear,'hand.'+side)
    curve('Sleeve soft elbow fold.'+side,[mir(p) for p in ((.78,-.21,1.85),(.87,-.252,1.88),(.96,-.23,1.855))],.009,cuffmat,'forearm.'+side)

# Dark shorts: shared rise and two distinct short legs.
loft('Shorts • waist and rise',[(1.035,0,0,.54,.32),(1.12,0,0,.59,.345),(1.31,0,0,.60,.345),(1.37,0,0,.56,.32)],shortmat,'pelvis',sub=2)
for sign,side in [(-1,'R'),(1,'L')]:
    x=sign*.318
    loft('Shorts leg.'+side,[(.94,x,-.003,.293,.305),(.955,x,0,.318,.322),(1.04,x,0,.32,.328),(1.19,x,0,.30,.325),(1.29,x,0,.27,.30)],shortmat,'thigh.'+side,sub=2)
    curve('Shorts hem stitch.'+side,[(x-.26,-.18,.994),(x,-.315,.982),(x+.26,-.18,.994)],.007,shortseam,'thigh.'+side)
    loft('Leg skin.'+side,[(.41,sign*.34,0,.139,.14),(.48,sign*.345,0,.147,.15),(.77,sign*.35,0,.155,.16),(1.02,sign*.34,0,.16,.17)],skin,'shin.'+side,n=32,sub=1)
    loft('Sock.'+side,[(.30,sign*.35,-.005,.172,.175),(.35,sign*.35,-.005,.18,.177),(.54,sign*.35,0,.174,.17),(.565,sign*.35,0,.169,.164)],sockmat,'foot.'+side,n=32,sub=1)
    loft('Sock folded cuff.'+side,[(.485,sign*.35,0,.174,.17),(.50,sign*.35,0,.183,.178),(.565,sign*.35,0,.181,.178),(.577,sign*.35,0,.17,.165)],sockmat,'foot.'+side,n=32,sub=1)
    # A superellipse footprint gives a wide round toe and soft square heel.
    vs=[]; fs=[]; nn=48
    rows=[(.035,.27,.37),(.045,.32,.426),(.13,.328,.435),(.17,.32,.426),(.25,.304,.405),(.355,.258,.337),(.405,.19,.25)]
    for z,rx,ry in rows:
        for j in range(nn):
            a=j*2*pi/nn
            px=math.copysign(abs(cos(a))**.80,cos(a))*rx
            py=math.copysign(abs(sin(a))**.80,sin(a))*ry
            vs.append((sign*.37+px,-.115+py,z))
    for i in range(len(rows)-1):
        for j in range(nn): fs.append((i*nn+j,i*nn+(j+1)%nn,(i+1)*nn+(j+1)%nn,(i+1)*nn+j))
    fs.extend([tuple(reversed(range(nn))),tuple((len(rows)-1)*nn+j for j in range(nn))])
    shoe=mesh('Sneaker • rounded upper.'+side,vs,fs,paper,'foot.'+side,True,2)
    shoe.data.materials.append(solemat)
    for p in shoe.data.polygons:
        if p.center.z < .15: p.material_index=1

curve('Shorts front centre seam',[(0,-.345,1.255),(0,-.357,1.14),(0,-.33,1.07)],.007,shortseam,'pelvis')

# A fully modeled, separate backpack with front pocket, side gussets and woven straps.
roundbox('Backpack • main padded shell',(0,.625,1.84),(1.13,.56,1.08),.225,teal)
roundbox('Backpack • rear pocket',(0,.936,1.685),(.86,.18,.76),.145,teal)
roundbox('Backpack • cream name label',(0,1.035,1.71),(.354,.023,.23),.027,paper,outlined=False)
curve('Backpack rear pocket piping',[(-.35,1.018,1.405),(-.40,1.013,1.52),(-.39,1.012,1.88),(-.28,1.01,2.001),(0,1.01,2.026),(.28,1.01,2.001),(.39,1.012,1.88),(.40,1.013,1.52),(.35,1.018,1.405),(0,1.023,1.374)],.013,tealdark,cyclic=True)
curve('Backpack main zipper seam',[(-.51,.84,1.44),(-.515,.84,2.04),(-.40,.84,2.30),(0,.84,2.374),(.40,.84,2.30),(.515,.84,2.04),(.51,.84,1.44)],.009,tealdark)
curve('Backpack handle',[(-.17,.59,2.36),(-.155,.61,2.52),(0,.61,2.566),(.155,.61,2.52),(.17,.59,2.36)],.035,strapmat)
for sign,side in [(-1,'R'),(1,'L')]:
    roundbox('Backpack side pouch.'+side,(sign*.553,.60,1.61),(.18,.36,.46),.075,strapmat)
    curve('Side pouch lip.'+side,[(sign*.64,.47,1.80),(sign*.66,.60,1.83),(sign*.64,.73,1.80)],.012,tealdark)
    points=[(sign*x,y,z) for x,y,z in ((.39,.65,2.31),(.46,.37,2.49),(.50,.02,2.45),(.50,-.31,2.29),(.52,-.45,2.08),(.55,-.46,1.82),(.57,-.405,1.60),(.61,-.10,1.48),(.56,.40,1.45))]
    ribbon('Backpack shoulder strap.'+side,points,.178,.064,strapmat)
    curve('Strap subtle edge stitch.'+side,[(sign*.55,-.344,2.28),(sign*.58,-.49,2.08),(sign*.615,-.486,1.82),(sign*.62,-.424,1.66)],.006,teallight)
    roundbox('Strap adjuster.'+side,(sign*.565,-.486,1.68),(.158,.073,.12),.025,tealdark)
    roundbox('Strap adjuster inner.'+side,(sign*.565,-.529,1.68),(.082,.012,.067),.009,strapmat,outlined=False)
roundbox('Zipper pull',(0.36,1.047,1.976),(.046,.034,.094),.014,paper,outlined=False,rot=(0,.14,0))

# Unite intersecting forms where an actual tailored/skin surface should be continuous.
def unite_forms(objects, name, bind, voxel):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects:
        bpy.context.view_layer.objects.active=ob; ob.select_set(True)
        for mod in list(ob.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        ob.select_set(False)
    for ob in objects: ob.select_set(True)
    active=objects[0]; bpy.context.view_layer.objects.active=active
    bpy.ops.object.join()
    for ob in objects:
        if ob in parts: parts.remove(ob)
    active.name=name; active['Part']=name; active['BindBone']=bind
    rem=active.modifiers.new('Continuous surface','REMESH'); rem.mode='VOXEL'; rem.voxel_size=voxel; rem.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=rem.name)
    smo=active.modifiers.new('Gentle smoothing','SMOOTH'); smo.factor=.6; smo.iterations=5
    bpy.ops.object.modifier_apply(modifier=smo.name)
    for p in active.data.polygons: p.use_smooth=True
    su=active.modifiers.new('Soft editable surface','SUBSURF'); su.levels=1; su.render_levels=1
    parts.append(active); active.select_set(False)
    return active

unite_forms([o for o in parts if o.name.startswith(('Shorts •','Shorts leg.'))], 'Shorts • unified tailored shape','shorts',.022)
for side in ('L','R'):
    unite_forms([o for o in parts if o.name=='Hand palm.'+side or o.name=='Thumb.'+side or o.name.startswith('Finger.'+side+'.')], 'Hand • soft mitten.'+side,'hand.'+side,.022)

# Apply small non-destructive shape modifiers, convert seam curves, bake to meters.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    bpy.context.view_layer.objects.active=ob; ob.select_set(True)
    if ob.type=='CURVE': bpy.ops.object.convert(target='MESH')
    for mod in list(ob.modifiers):
        if mod.type in {'BEVEL','WEIGHTED_NORMAL'}: bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    for v in ob.data.vertices: v.co*=S
    ob.select_set(False)

# Lightweight FK rig. Rigid head/facial attachments; blended sleeves and shins.
arm=bpy.data.armatures.new('Wait_Friend_FK_Rig')
rig=bpy.data.objects.new('Wait_Friend_Rig • Pose Mode',arm); rigcol.objects.link(rig)
bpy.context.view_layer.objects.active=rig; rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
    b=arm.edit_bones.new(name); b.head=Vector(head)*S; b.tail=Vector(tail)*S
    if parent: b.parent=arm.edit_bones[parent]
    return b
bone('root',(0,0,0),(0,0,.35))
bone('pelvis',(0,0,1.03),(0,0,1.40),'root')
bone('spine',(0,0,1.40),(0,0,2.39),'pelvis')
bone('neck',(0,0,2.39),(0,0,2.70),'spine')
bone('head',(0,0,2.70),(0,0,4.10),'neck')
for sign,side in [(-1,'R'),(1,'L')]:
    bone('upper_arm.'+side,(sign*.56,0,2.30),(sign*.885,-.04,1.94),'spine')
    bone('forearm.'+side,(sign*.885,-.04,1.94),(sign*.942,-.10,1.58),'upper_arm.'+side)
    bone('hand.'+side,(sign*.942,-.10,1.58),(sign*.946,-.10,1.34),'forearm.'+side)
    bone('thigh.'+side,(sign*.34,0,1.28),(sign*.35,0,.78),'pelvis')
    bone('shin.'+side,(sign*.35,0,.78),(sign*.35,0,.40),'thigh.'+side)
    bone('foot.'+side,(sign*.35,0,.40),(sign*.35,-.43,.16),'shin.'+side)
bpy.ops.object.mode_set(mode='OBJECT')
rig.show_in_front=True; arm.display_type='STICK'
rig['Rig_notes']='Basic FK. Head/hair/face rigidly attached; sleeves and legs blended. No facial controls, IK or production skinning.'
for ob in parts:
    bind=ob['BindBone']
    if bind.startswith('sleeve.'):
        side=bind.split('.')[-1]
        a=ob.vertex_groups.new(name='upper_arm.'+side); b=ob.vertex_groups.new(name='forearm.'+side)
        for v in ob.data.vertices:
            z=v.co.z/S; w=max(0,min(1,(z-1.83)/.24))
            if w: a.add([v.index],w,'REPLACE')
            if w<1: b.add([v.index],1-w,'REPLACE')
    elif bind=='shorts':
        groups={n:ob.vertex_groups.new(name=n) for n in ('pelvis','thigh.L','thigh.R')}
        for v in ob.data.vertices:
            w=max(0,min(1,(v.co.z/S-1.04)/.20)); side='L' if v.co.x>0 else 'R'
            if w: groups['pelvis'].add([v.index],w,'REPLACE')
            if w<1: groups['thigh.'+side].add([v.index],1-w,'REPLACE')
    elif bind.startswith('shin.'):
        side=bind.split('.')[-1]
        a=ob.vertex_groups.new(name='thigh.'+side); b=ob.vertex_groups.new(name='shin.'+side)
        for v in ob.data.vertices:
            w=max(0,min(1,(v.co.z/S-.68)/.23))
            if w: a.add([v.index],w,'REPLACE')
            if w<1: b.add([v.index],1-w,'REPLACE')
    else:
        vg=ob.vertex_groups.new(name=bind); vg.add(list(range(len(ob.data.vertices))),1,'REPLACE')
    mod=ob.modifiers.new('FK skin • basic deformation','ARMATURE'); mod.object=rig
    # Deform the quad cage before subdivision.
    while list(ob.modifiers).index(mod)>0:
        bpy.context.view_layer.objects.active=ob; bpy.ops.object.modifier_move_up(modifier=mod.name)
    ob.parent=rig
rig.select_set(False)

# Packed reference for convenient comparison inside Blender; never rendered.
ref_path=Path(r'E:\ProjectWait\ConceptArt\Wait_친구캐릭터_프로젝트기준_구조레퍼런스_v2.png')
img=bpy.data.images.load(str(ref_path)); img.pack()
ref=bpy.data.objects.new('Original reference • 정면/측면/후면',None); reference.objects.link(ref)
ref.empty_display_type='IMAGE'; ref.data=img; ref.empty_display_size=1.5
ref.location=(1.5,.55,.75); ref.rotation_euler=(pi/2,0,0); ref.hide_set(True)

# Brown Freestyle strokes keep linework independent of editable geometry and export.
scene.render.use_freestyle=True
freestyle=bpy.context.view_layer.freestyle_settings
freestyle.crease_angle=math.radians(140)
ls=freestyle.linesets[0] if freestyle.linesets else freestyle.linesets.new('Warm brown ink')
if ls.linestyle is None: ls.linestyle=bpy.data.linestyles.new('Warm brown ink')
ls.linestyle.color=rgba('#493327')[:3]; ls.linestyle.thickness=2.35
ls.select_silhouette=True; ls.select_border=True; ls.select_contour=True; ls.select_crease=False
ls.select_by_collection=True; ls.collection=ink; ls.collection_negation='INCLUSIVE'

# Warm paper studio and soft contact shadows.
world=bpy.data.worlds.new('Warm cream studio'); world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=rgba('#FFF8E8')
world.node_tree.nodes['Background'].inputs['Strength'].default_value=1; scene.world=world
floor=bpy.data.materials.new('Studio floor • cream'); floor.diffuse_color=rgba('#F7ECCD'); floor.use_nodes=True
pbs=floor.node_tree.nodes.get('Principled BSDF'); pbs.inputs['Base Color'].default_value=rgba('#F7ECCD'); pbs.inputs['Roughness'].default_value=1
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.001))
plane=bpy.context.object; plane.name='Studio ground'; link_obj(plane,studio); plane.data.materials.append(floor)
plane.hide_render=True
# A restrained illustrated contact shadow, separate from the model.
shadow_mat=toon('Studio illustrated ground shadow','#E2DCCB',True)
bpy.ops.mesh.primitive_uv_sphere_add(segments=64,ring_count=16,location=(0,0,.001))
shadow=bpy.context.object; shadow.name='Illustrated oval contact shadow'; shadow.scale=(.91*S,.53*S,.006*S)
link_obj(shadow,studio); shadow.data.materials.append(shadow_mat)
def area(name,loc,energy,size):
    ld=bpy.data.lights.new(name,'AREA'); ld.energy=energy; ld.shape='DISK'; ld.size=size*S
    ob=bpy.data.objects.new(name,ld); studio.objects.link(ob); ob.location=Vector(loc)*S
    ob.rotation_euler=(Vector((0,0,2))*S-ob.location).to_track_quat('-Z','Y').to_euler()
area('Key • broad warm sunlight',(-3,-4,7),500,4)
area('Fill',(4,-1,5),120,5)
def camera(name,loc,target=(0,0,2.19),ortho=5.12):
    cd=bpy.data.cameras.new(name); cd.type='ORTHO'; cd.ortho_scale=ortho*S; cd.lens=55
    ob=bpy.data.objects.new(name,cd); studio.objects.link(ob); ob.location=Vector(loc)*S
    ob.rotation_euler=(Vector(target)*S-ob.location).to_track_quat('-Z','Y').to_euler()
    cd.clip_start=.01; cd.clip_end=100
    return ob
cams={
    'Hero':camera('CAM_01 • Three-quarter',(6,-12,5.1)),
    'Front':camera('CAM_02 • Front',(0,-12,2.20)),
    'Side':camera('CAM_03 • Right profile',(12,0,2.20)),
    'Back':camera('CAM_04 • Back',(0,12,2.20)),
    'Back_ThreeQuarter':camera('CAM_05 • Backpack',(6,12,4.7)),
}
scene.camera=cams['Hero']

readme='''Wait! 친구 캐릭터 — 구조 레퍼런스 V2 기반 신규 모델

첨부된 구조 레퍼런스를 기준으로 새로 모델링했습니다.
큰 머리, 겹쳐 내려오는 갈색 앞머리와 뒷머리, 두 가닥의 정수리 머리,
주황 후드/캥거루 주머니, 청록 가방과 어깨끈, 짙은 반바지,
짧은 팔다리, 크림색 운동화, 웃는 얼굴을 분리된 메시로 구성했습니다.
기존 친구_프로토타입 파일은 유지합니다.

Wait_Friend_ReferenceV2.blend: 편집용 원본. Blender 5.1.1에서 작성.
01_CHARACTER: 부위별 이름이 붙은 메시, 수정 가능한 일부 Subdivision.
02_RIG: 기본 FK 뼈대. Pose Mode에서 머리/몸/팔/다리 포즈 조정 가능.
03_STUDIO: 정면/측면/후면/앞뒤 사선 카메라, 조명, 바닥.
04_REFERENCE: 첨부 레퍼런스 이미지를 파일 안에 포함. 기본 숨김.
F12: 사선 미리보기. 카메라를 전환하면 다른 방향을 렌더할 수 있습니다.

Blender Z축 위, -Y 정면, 미터 단위, 발바닥은 원점 부근.
메시와 아트 부품은 좌우 독립 편집 가능합니다.
프로젝트 팔레트: 후드 #FF8C2D, 소매 끝 #E87927, 피부 #FFC08A,
반바지 #3B3E48, 외곽선 #493327. 넓은 무광 색면과 3단계 셀 명암.
셀 명암은 노멀 기반 Blender 미리보기 재질이며, 외곽선은 Freestyle입니다.
Unity의 URP/XR 셰이더를 이 파일에서 실행하거나 검증한 것은 아닙니다.
별도 UV 텍스처가 필요 없는 색상 재질이며, 외부 이미지 의존성은 없습니다.

FK 뼈대는 간단한 포즈 조정용입니다. 표정 컨트롤, IK, 손가락 뼈,
상용 애니메이션용 연속 토폴로지/스키닝 검수는 포함하지 않습니다.
Unity 씬/원본 자산은 변경하지 않았습니다.
'''
bpy.data.texts.new('읽어주세요 • README').write(readme)
(OUT/'README_친구캐릭터.txt').write_text(readme,encoding='utf-8-sig')

# Comfortable first-open modeling view.
bpy.ops.object.select_all(action='DESELECT'); head.select_set(True); bpy.context.view_layer.objects.active=head
for screen in bpy.data.screens:
    for a in screen.areas:
        if a.type=='VIEW_3D':
            a.spaces.active.region_3d.view_distance=2.5
            a.spaces.active.region_3d.view_location=(0,0,.68)
            a.spaces.active.region_3d.view_rotation=cams['Hero'].rotation_euler.to_quaternion()
            a.spaces.active.clip_end=100
            a.spaces.active.shading.type='MATERIAL'
            a.spaces.active.overlay.show_extras=False
            a.spaces.active.overlay.show_floor=False

scene['Source']='Wait_친구캐릭터_프로젝트기준_구조레퍼런스_v2.png'
scene['Character_height_m']=round(4.45*S,3)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Wait_Friend_ReferenceV2.blend'))
summary={'blend':str(OUT/'Wait_Friend_ReferenceV2.blend'),'mesh_count':len(parts),'vertices':sum(len(o.data.vertices) for o in parts),'polygons':sum(len(o.data.polygons) for o in parts),'bones':len(arm.bones),'materials':len(materials),'packed_reference':bool(img.packed_file),'warnings':[]}
(WORK/'Build_Validation.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print('BUILD_COMPLETE '+json.dumps(summary,ensure_ascii=True),flush=True)

# Render initial review at moderate resolution; final previews rendered after QA.
scene.render.resolution_percentage=70
for key in ('Hero','Front','Side','Back','Back_ThreeQuarter'):
    scene.camera=cams[key]; scene.render.filepath=str(OUT/('Wait_Friend_'+key+'.png'))
    bpy.ops.render.render(write_still=True)
    print('RENDER_COMPLETE '+key,flush=True)
print('ALL_DONE',flush=True)
