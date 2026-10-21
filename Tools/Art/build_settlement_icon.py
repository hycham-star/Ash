"""Original 2.5D miniature home icon. Run Blender -b -t 4 --python this_file -- repo."""
import bpy,sys,os,math
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1]
common=os.path.join(root,'Tools/Art/build_map_landmarks.py')
source=open(common).read();exec(compile(source[:source.index('bpy.ops.object.camera_add')],common,'exec'),globals())
scene.cycles.samples=24
box('Settlement earth',(0,0,.04),(5,5,.15),soil)
for x,y,w,d,h in [(-.65,.45,1.5,1.4,1.25),(1,-.65,1.1,1,.8)]:
 box('Reclaimed building',(x,y,h/2+.12),(w,d,h),stone)
 box('Door',(x,y-d/2-.02,.43),(.30,.04,.58),wood)
 for side in (-1,1):
  o=box('Rusted roof',(x,y+side*d*.27,h+.20),(w+.2,d*.65,.07),roof);o.rotation_euler.x=-side*.40
 for xx in (-.38,.38):box('Window',(x+xx,y-d/2-.03,h*.65),(.22,.03,.24),metal)
for x in (-2.3,2.3):
 for j in range(8):
  y=-2.25+j*.65;box('Fence post',(x,y,.47),(.10,.10,.8),wood)
  if j<7:box('Fence rail',(x,y+.325,.55),(.06,.65,.07),metal)
for y in (-2.3,2.3):
 for j in range(8):
  x=-2.25+j*.65
  if y<0 and abs(x)<.7:continue
  box('Fence post',(x,y,.47),(.1,.1,.8),wood)
  if j<7 and not(y<0 and abs(x+.325)<.7):box('Fence rail',(x+.325,y,.55),(.65,.06,.07),metal)
for x in (-2.15,2.15):
 for dx in (-.2,.2):rod('Tower frame',(x+dx,1.9,.15),(x+dx,1.9,1.4),.04,wood)
 box('Lookout',(x,1.9,1.4),(.6,.6,.12),wood);box('Tower roof',(x,1.9,1.9),(.7,.7,.09),roof)
for x in (-1.65,-1.15):
 box('Garden bed',(x,-1.2,.20),(.35,1.0,.12),dark)
 for y in (-1.5,-1.15,-.8):sphere('Food plant',(x,y,.35),(.17,.15,.13),crop)
for x in (1.1,1.6):cylinder('Water tank',(x,1.25,.58),.23,.8,metal)
rod('Home pennant post',(.1,-.7,.1),(.1,-.7,2),.025,wood)
box('Gold pennant',(.33,-.7,1.78),(.42,.035,.27),material('Home gold',(.65,.43,.13)))
bpy.ops.object.camera_add(location=(7,-7,7.65));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.65))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=7.4;scene.camera=camera
for location,power in [((-3,-4,8),900),((4,2,5),450)]:
 bpy.ops.object.light_add(type='AREA',location=location);lamp=bpy.context.object;lamp.data.energy=power;lamp.data.size=5;lamp.rotation_euler=(Vector((0,0,.5))-lamp.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=os.path.join(out,'Settlement.png');bpy.ops.render.render(write_still=True)
print('PASS original isometric settlement icon')
