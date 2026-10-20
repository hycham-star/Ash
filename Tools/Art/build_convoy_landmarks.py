"""Original armored wasteland bus, hero motorcycle and rusty camp; eight travel headings.
Run blender -b -t 4 --python-exit-code 1 --python Tools/Art/build_convoy_landmarks.py -- /path/to/Ash
"""
import bpy,sys,os,math
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1];out=os.path.join(root,'Assets/Ash/Resources/Map')
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=False
scene.render.film_transparent=True;scene.render.resolution_x=384;scene.render.resolution_y=384;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.world.color=(.35,.39,.44)
def mat(n,c):
 m=bpy.data.materials.new(n);m.diffuse_color=(*c,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=.85;return m
rust=mat('Oxidized armor',(.32,.13,.055));ochre=mat('Worn ochre paint',(.48,.32,.10));steel=mat('Steel',(.19,.23,.22));dark=mat('Tires',(.035,.035,.03));glass=mat('Dusty glass',(.11,.23,.25));wood=mat('Timber',(.23,.14,.07));soil=mat('Ground',(.27,.23,.15));cloth=mat('Rider coat',(.25,.30,.26));skin=mat('Skin',(.52,.34,.21));dust=mat('Dust',(.56,.43,.27))
def finish(o,n,m):
 o.name=n;o.data.materials.append(m);return o
def box(n,p,s,m):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.dimensions=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 b=o.modifiers.new('Worn edges','BEVEL');b.width=.035;b.segments=2;o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');return finish(o,n,m)
def cyl(n,p,r,h,m):
 bpy.ops.mesh.primitive_cylinder_add(vertices=20,radius=r,depth=h,location=p);return finish(bpy.context.object,n,m)
def rod(n,a,b,r,m):
 d=Vector(b)-Vector(a);o=cyl(n,(Vector(a)+Vector(b))/2,r,d.length,m);o.rotation_euler=d.to_track_quat('Z','Y').to_euler();return o
def ball(n,p,s,m):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,location=p);o=bpy.context.object;o.scale=s
 for f in o.data.polygons:f.use_smooth=True
 return finish(o,n,m)
def wheel(x,y,z,r=.35):
 o=cyl('Heavy tire',(x,y,z),r,.22,dark);o.rotation_euler.y=math.pi/2
 o=cyl('Wheel hub',(x+(.12 if x>0 else -.12),y,z),r*.45,.025,steel);o.rotation_euler.y=math.pi/2
def bus():
 box('Armored bus',(0,0,.95),(1.2,3.4,1.1),ochre);box('Roof',(0,0,1.53),(1.25,3.4,.16),rust)
 for side in (-1,1):
  for y in (-1.2,-.6,0,.6,1.2):
   box('Window',(side*.61,y,1.16),(.025,.41,.42),glass)
   panel=box('Salvaged armor',(side*.65,y,.72),(.08,.53,.40),rust);panel.rotation_euler.z=side*.08
  for y in (-1.13,1.15):wheel(side*.68,y,.38)
 box('Front windshield',(0,1.715,1.15),(1.02,.03,.45),glass)
 for x in (-.38,0,.38):box('Windshield bars',(x,1.75,1.17),(.035,.05,.49),steel)
 box('Ram bumper',(0,1.86,.51),(1.5,.25,.32),steel)
 for x in (-.55,-.28,0,.28,.55):
  bpy.ops.mesh.primitive_cone_add(vertices=8,radius1=.08,radius2=0,depth=.4,location=(x,2.08,.55));o=bpy.context.object;o.rotation_euler.x=-math.pi/2;finish(o,'Ram spike',steel)
 for x in (-.38,.38):box('Headlight',(x,1.75,.81),(.20,.035,.13),ochre)
 for x in (-.5,.5):rod('Roof rack',(x,-1.4,1.76),(x,1.25,1.76),.035,steel)
 for y in (-1.4,-.8,.1,1.25):rod('Rack brace',(-.5,y,1.76),(.5,y,1.76),.025,steel)
 box('Roof supplies',(0,-.6,1.83),(.7,1,.35),wood)
 rod('Tall exhaust',(-.73,-1.5,.6),(-.73,-1.5,1.8),.07,steel)
def bike():
 for y in (-.7,.7):wheel(0,y,.29,.32)
 rod('Motorbike frame',(0,-.65,.35),(0,.4,.7),.07,steel);rod('Front fork',(0,.7,.3),(0,.4,.9),.045,steel)
 box('Fuel tank',(0,.13,.78),(.42,.55,.30),rust);box('Seat',(0,-.35,.84),(.35,.50,.11),dark)
 rod('Handlebar',(-.35,.42,1.02),(.35,.42,1.02),.035,steel)
 box('Engine',(0,-.05,.43),(.35,.38,.30),steel)
 box('Rider torso',(0,-.2,1.15),(.46,.32,.57),cloth)
 ball('Helmet',(0,-.16,1.60),(.23,.22,.25),dark)
 box('Visor',(0,.035,1.59),(.32,.035,.10),glass)
 for s in (-1,1):
  rod('Arm',(s*.22,-.13,1.32),(s*.30,.4,1.02),.075,cloth)
  rod('Leg',(s*.17,-.30,.96),(s*.30,.05,.48),.08,cloth)
  box('Boot',(s*.30,.10,.43),(.16,.31,.16),dark)
def camp():
 cyl('Camp soil',(0,0,.03),2.1,.1,soil)
 box('Rusty barracks',(-.3,.1,.57),(1.5,1.4,1),steel)
 for side in (-1,1):
  r=box('Rust roof',(-.3,side*.4+.1,1.16),(1.8,.9,.08),rust);r.rotation_euler.x=side*-.38
 box('Gate',(-.3,-.62,.46),(.53,.04,.70),dark)
 for x in (-1.65,1.65):
  for y in (-1.5,-.9,-.3,.3,.9,1.5):box('Steel perimeter',(x,y,.45),(.1,.1,.8),steel)
  box('Scrap wall',(x,0,.38),(.07,3,.48),rust)
 for y in (-1.5,1.5):
  for x in (-1.65,-1.1,-.55,0,.55,1.1,1.65):box('Fence pole',(x,y,.45),(.09,.09,.8),steel)
  box('Fence rail',(0,y,.62),(3.4,.05,.07),wood)
 for x in (-1.1,-.75):cyl('Fuel drum',(x,-1.1,.29),.16,.45,rust)
 for x in (1.05,1.55):box('Watchtower leg',(x,.9,.9),(.1,.1,1.8),wood)
 box('Watch platform',(1.3,.9,1.6),(.8,.8,.12),steel);box('Watch roof',(1.3,.9,2.1),(1,1,.1),rust)
 rod('Flag mast',(-1.3,.9,0),(-1.3,.9,2.4),.035,steel);box('Raider flag',(-1.02,.9,2.15),(.55,.03,.30),rust)
def cloud():
 for i in range(9):
  a=i*2.39996;ball('Dust cloud',(math.cos(a)*.7,math.sin(a)*.7,.45+(i%3)*.25),(.65,.6,.65),dust)
bpy.ops.object.camera_add(location=(6,-6,6.7));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.7))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';scene.camera=camera
for p,power,size in (((-3,-4,7),650,5),((4,2,5),400,4)):
 bpy.ops.object.light_add(type='AREA',location=p);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((0,0,.5))-o.location).to_track_quat('-Z','Y').to_euler()
for name,build,scale in (('Camp',camp,5.6),('Bus',bus,4.6),('Bike',bike,3.1),('Dust',cloud,3.8)):
 build();meshes=[o for o in scene.objects if o.type=='MESH']
 for heading in range(8 if name in ('Bus','Bike') else 1):
  angle=heading*math.tau/8
  for o in meshes:
   if heading==0:o['base_location']=list(o.location);o['base_rotation']=list(o.rotation_euler)
   from mathutils import Matrix
   rotation=Matrix.Rotation(angle,4,'Z');o.location=rotation@Vector(o['base_location'])
   # Rotate around the world origin while preserving each part's original local rotation.
   from mathutils import Euler
   o.rotation_euler=(rotation.to_3x3()@Euler(o['base_rotation']).to_matrix()).to_euler()
  camera.data.ortho_scale=scale;scene.render.filepath=os.path.join(out,name+(str(heading) if name in ('Bus','Bike') else '')+'.png');bpy.ops.render.render(write_still=True)
 for o in meshes:bpy.data.objects.remove(o,do_unlink=True)
print('PASS original camp, dust and eight bus/bike headings')
