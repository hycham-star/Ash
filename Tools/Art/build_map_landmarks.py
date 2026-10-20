"""Original miniature landmarks. blender -b -t 4 --python-exit-code 1 --python Tools/Art/build_map_landmarks.py -- /path/to/Ash"""
import bpy, math, os, sys, random
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1]
out=os.path.join(root,'Assets/Ash/Resources/Map');os.makedirs(out,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32
scene.cycles.use_denoising=False;scene.render.film_transparent=True
scene.render.resolution_x=384;scene.render.resolution_y=384;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
scene.world.color=(.35,.39,.44);scene.view_settings.view_transform='AgX'
def material(name,color):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(*color,1)
 m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.82
 return m
soil=material('Dry soil',(.25,.21,.12));stone=material('Limestone',(.52,.49,.36))
wood=material('Old timber',(.26,.16,.075));roof=material('Rusty roof',(.42,.19,.08))
metal=material('Steel',(.20,.25,.24));water=material('Water',(.08,.32,.38))
leaf=material('Leaves',(.16,.30,.08));leaf2=material('Sunlit leaves',(.29,.39,.11))
dark=material('Shadow',(.06,.065,.045));crop=material('Crops',(.32,.45,.10))
def finish(o,name,mat,bevel=0):
 o.name=name;o.data.materials.append(mat)
 if bevel:
  m=o.modifiers.new('Soft worn edges','BEVEL');m.width=bevel;m.segments=2
  o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
 return o
def box(name,p,s,m):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.dimensions=s
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 return finish(o,name,m,.025)
def cylinder(name,p,r,d,m):
 bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=r,depth=d,location=p)
 return finish(bpy.context.object,name,m,.015)
def sphere(name,p,s,m):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,location=p);o=bpy.context.object;o.scale=s
 for face in o.data.polygons:face.use_smooth=True
 return finish(o,name,m)
def rod(name,a,b,r,m):
 delta=Vector(b)-Vector(a);o=cylinder(name,(Vector(a)+Vector(b))/2,r,delta.length,m)
 o.rotation_euler=delta.to_track_quat('Z','Y').to_euler();return o
def base():
 cylinder('Earth island',(0,0,.03),1.65,.09,soil)
 for i in range(9):
  a=i*2.39996;sphere('Pebble',(math.cos(a)*1.45,math.sin(a)*1.45,.12),(.14,.1,.08),stone)
def well():
 base();cylinder('Dark well mouth',(0,0,.36),.59,.5,dark)
 for level in range(3):
  for i in range(12):
   a=(i+(level%2)*.5)*math.tau/12
   o=box('Masonry block',(math.cos(a)*.62,math.sin(a)*.62,.19+level*.19),(.27,.23,.18),stone);o.rotation_euler.z=a
 cylinder('Water surface',(0,0,.56),.49,.02,water)
 for x in (-.9,.9):box('Roof support',(x,0,.98),(.13,.15,1.85),wood)
 rod('Winding spindle',(-.95,0,1.3),(.95,0,1.3),.09,wood)
 rod('Bucket rope',(0,0,.6),(0,0,1.3),.018,wood)
 cylinder('Bucket',(.75,-.65,.25),.18,.35,metal)
 for side in (-1,1):
  o=box('Well roof',(0,side*.35,1.95),(2.35,.85,.09),roof);o.rotation_euler.x=side*-.42
  for x in (-.9,-.6,-.3,0,.3,.6,.9):rod('Roof seam',(x,0,2.13),(x,side*.74,1.8),.018,wood)
def farm():
 base()
 for x in (-.9,-.3,.3,.9):
  box('Planted bed',(x,0,.15),(.42,2.1,.16),dark)
  for y in (-.8,-.4,0,.4,.8):
   sphere('Vegetable',(x,y,.30),(.20,.18,.17),crop)
   sphere('Leaf',(x-.08,y,.39),(.12,.08,.15),leaf)
 for y in (-1.15,1.15):
  for x in (-1.25,0,1.25):box('Fence stake',(x,y,.42),(.08,.08,.65),wood)
  box('Fence rail',(0,y,.53),(2.65,.06,.09),wood)
 box('Supply crate',(1.1,-.95,.35),(.45,.4,.38),wood)
def scrap():
 base()
 box('Salvage shed',(-.6,.55,.58),(1.1,1,.9),metal)
 o=box('Sheet roof',(-.6,.55,1.1),(1.35,1.2,.08),roof);o.rotation_euler.y=.12
 box('Shed entrance',(-.6,-.005,.52),(.55,.035,.65),dark)
 box('Vehicle frame',(.45,-.45,.38),(1.45,.65,.36),roof)
 box('Cab',(.1,-.45,.75),(.6,.6,.45),metal)
 for x in (-.05,1):
  for y in (-.8,-.1):sphere('Tire',(x,y,.25),(.23,.12,.23),dark)
 for i in range(6):
  o=box('Reclaimed sheet',(.3+i*.12,.65,.22+i*.055),(.8,.52,.045),roof if i%2 else metal);o.rotation_euler.z=i*.23
 for i in range(3):cylinder('Drum',(-1+i*.33,-.85,.35),.17,.48,metal)
def forest():
 base()
 for i,(x,y,h) in enumerate(((-.65,.35,1.4),(.55,.55,1.8),(.15,-.7,1.15))):
  cylinder('Tree trunk',(x,y,h*.5),.10,h,wood)
  for j in range(5):
   a=j*2.39996;sphere('Leaf crown',(x+math.cos(a)*.26,y+math.sin(a)*.26,h+.15+(j%2)*.18),(.48,.42,.49),leaf if (i+j)%2 else leaf2)
 for i in range(3):rod('Stacked log',(-1.15+i*.2,-.85,.17),(-1.15+i*.2,-.05,.17),.085,wood)
bpy.ops.object.camera_add(location=(6,-6,6));camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,.8))-camera.location).to_track_quat('-Z','Y').to_euler()
# True isometric elevation, matching the game camera.
camera.location=Vector((6,-6,6.8));camera.rotation_euler=(Vector((0,0,.8))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=4.6;scene.camera=camera
for p,power,size in (((-3,-4,7),700,5),((4,2,5),450,4)):
 bpy.ops.object.light_add(type='AREA',location=p);lamp=bpy.context.object;lamp.data.energy=power;lamp.data.shape='DISK';lamp.data.size=size
 lamp.rotation_euler=(Vector((0,0,.5))-lamp.location).to_track_quat('-Z','Y').to_euler()
for name,build in (('Water',well),('Food',farm),('Scrap',scrap),('Lumber',forest)):
 build();scene.render.filepath=os.path.join(out,name+'.png');bpy.ops.render.render(write_still=True)
 for obj in list(scene.objects):
  if obj.type=='MESH':bpy.data.objects.remove(obj,do_unlink=True)
print('PASS four original isometric landmark renders')
