"""Original weathered 2.5D map models, rendered on CPU without external art.
blender -b -t 4 --python-exit-code 1 --python Tools/Art/build_weathered_landmarks.py -- /path/to/Ash
Rebuild the isometric atlas afterwards with build_isometric_atlas.py.
"""
import bpy,sys,os,math,random
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1]
common=os.path.join(root,'Tools/Art/build_map_landmarks.py')
source=open(common).read();exec(compile(source[:source.index('bpy.ops.object.camera_add')],common,'exec'),globals())
random.seed(6821)
scene.cycles.samples=48
scene.cycles.max_bounces=5
scene.cycles.use_denoising=False
scene.view_settings.look='AgX - Medium High Contrast'

def weathered(name,color,kind='stone'):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links;p=n.get('Principled BSDF')
 geo=n.new('ShaderNodeTexCoord');tex=n.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=7 if kind!='wood' else 3;tex.inputs['Detail'].default_value=4;tex.inputs['Roughness'].default_value=.75
 mapping=n.new('ShaderNodeVectorMath');mapping.operation='MULTIPLY';mapping.inputs[1].default_value=(5,5,.6) if kind=='wood' else (1,1,1)
 l.new(geo.outputs['Generated'],mapping.inputs[0]);l.new(mapping.outputs[0],tex.inputs['Vector'])
 ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.25;ramp.color_ramp.elements[0].color=tuple(c*.28 for c in color)+(1,)
 ramp.color_ramp.elements[1].position=.77;ramp.color_ramp.elements[1].color=tuple(min(1,c*1.4) for c in color)+(1,)
 if kind=='metal':
  middle=ramp.color_ramp.elements.new(.48);middle.color=(.18,.065,.025,1)
  p.inputs['Metallic'].default_value=.5
 l.new(tex.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs[0],p.inputs['Base Color'])
 fine=n.new('ShaderNodeTexNoise');fine.inputs['Scale'].default_value=95;fine.inputs['Detail'].default_value=2;l.new(geo.outputs['Generated'],fine.inputs['Vector'])
 bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.38;bump.inputs['Distance'].default_value=.025 if kind=='stone' else .012;l.new(fine.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs[0],p.inputs['Normal'])
 p.inputs['Roughness'].default_value=.85 if kind!='metal' else .65
 return m
soil=weathered('Uneven compacted earth',(.20,.145,.075));stone=weathered('Eroded limestone',(.36,.33,.24))
wood=weathered('Splintered dry timber',(.23,.13,.055),'wood');roof=weathered('Flaking rusted sheet',(.29,.105,.035),'metal')
metal=weathered('Chipped military enamel',(.095,.16,.16),'metal');water=material('Deep shaded water',(.025,.14,.16))
water.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.19
leaf=weathered('Dusty olive foliage',(.09,.17,.035),'leaf');leaf2=weathered('Dry sunlit leaves',(.20,.25,.065),'leaf')
dark=weathered('Black rubber and crevices',(.028,.027,.021));crop=weathered('Vegetable leaf variation',(.10,.24,.035),'leaf')
wire=material('Galvanized edges',(.24,.27,.23));pale=weathered('Faded cloth',(.30,.27,.16),'wood')

def base():
 # Uneven boundary and a thin earth skirt rather than a perfect pedestal.
 vertices=[];segments=48
 for z,radius in ((-.035,1.62),(.07,1.68)):
  for i in range(segments):
   a=i*math.tau/segments;r=radius*(1+.035*math.sin(i*2.31));vertices.append((math.cos(a)*r,math.sin(a)*r,z+random.uniform(-.015,.015)))
 vertices.append((0,0,.075));faces=[]
 for i in range(segments):
  j=(i+1)%segments;faces.extend(((i,j,segments+j,segments+i),(segments+i,segments+j,segments*2)))
 mesh=bpy.data.meshes.new('Ragged soil');mesh.from_pydata(vertices,[],faces);mesh.update();o=bpy.data.objects.new('Ragged soil',mesh);bpy.context.collection.objects.link(o);finish(o,'Ragged earth',soil)
 for i in range(38):
  a=random.random()*math.tau;r=random.uniform(.9,1.6);size=random.uniform(.018,.06)
  sphere('Scattered gravel',(math.cos(a)*r,math.sin(a)*r,.09),(size,size*.7,size*.45),stone)

def corrugated(cx,cy,z,width,depth):
 for side in (-1,1):
  for i in range(int(width/.11)+1):
   x=cx-width/2+i*.11;rod('Corrugation',(x,cy,z+.18),(x,cy+side*depth/2,z-.13),.012,roof)

def water_site():
 well()
 # Individual blocks no longer form an unnaturally exact toy ring.
 for obj in list(scene.objects):
  if obj.type=='MESH' and obj.name.startswith('Masonry block'):
   obj.rotation_euler.z+=random.uniform(-.045,.045);obj.scale*=random.uniform(.95,1.04)
 corrugated(0,0,1.95,2.3,1.48)
 rod('Crank shaft',(.95,0,1.3),(1.15,0,1.3),.035,wire);rod('Crank arm',(1.15,0,1.3),(1.15,0,1.05),.035,wire)
 cylinder('Bucket rim',(.75,-.65,.43),.19,.025,wire)
 for i in range(8):
  a=i*math.tau/8;rod('Bucket seam',(.75+math.cos(a)*.17,-.65+math.sin(a)*.17,.1),(.75+math.cos(a)*.17,-.65+math.sin(a)*.17,.42),.008,wire)
 box('Water trough',(-.9,-.65,.17),(.5,.6,.14),wood)

def leaf_blade(center,angle,size,mat):
 x,y,z=center;verts=[(0,0,0),(-size*.24,size*.42,size*.24),(0,size,size*.19),(size*.24,size*.42,size*.24),(0,size*.45,size*.30)]
 mesh=bpy.data.meshes.new('Curved leaf');mesh.from_pydata(verts,[],[(0,1,4),(1,2,4),(2,3,4),(3,0,4)]);mesh.update();obj=bpy.data.objects.new('Curved leaf',mesh);bpy.context.collection.objects.link(obj);obj.location=center;obj.rotation_euler.z=angle;finish(obj,'Curved vegetable leaf',mat)

def food_site():
 base()
 for x in (-1.0,-.34,.34,1.0):
  box('Compost bed',(x,0,.13),(.50,2.0,.10),dark)
  for dx in (-.26,.26):box('Warped raised bed',(x+dx,0,.2),(.055,2.05,.16),wood)
  for y in (-.78,-.26,.26,.78):
   for i in range(7):leaf_blade((x,y,.21),i*math.tau/7+random.uniform(0,.4),random.uniform(.23,.38),crop if i%2 else leaf2)
 for y in (-1.17,1.17):
  for x in (-1.3,0,1.3):box('Uneven fence stake',(x,y,.42),(.07,.07,.65),wood)
  for h in (.28,.55):rod('Irrigation boundary wire',(-1.32,y,h),(1.32,y,h),.012,wire)
 box('Tool crate',(1.25,-1.15,.28),(.45,.4,.30),wood)
 for x in (-1.0,-.34,.34,1.0):rod('Drip irrigation',(x-.15,-.98,.25),(x-.15,.98,.25),.012,dark)
 cylinder('Water butt',(-1.26,.91,.42),.18,.60,metal)
 rod('Leaning garden tool',(-1.36,-1,.11),(-1.15,-.9,1),.022,wood)

def scrap_site():
 scrap()
 for i in range(13):
  x=-1.23+i*.105;z=1.155-(x+.6)*.12
  rod('Shed roof corrugation',(x,-.03,z),(x,1.13,z),.012,roof)
 box('Cracked windshield',(.10,-.765,.81),(.43,.018,.23),water)
 box('Dark door panel',(.55,-.785,.42),(.55,.015,.20),metal)
 for x in (-.05,1):
  o=cylinder('Wheel hub',(x,-.925,.25),.10,.025,wire);o.rotation_euler.x=math.pi/2
 for i in range(3):
  x=-1+i*.33
  for z in (.18,.48):cylinder('Drum rolled seam',(x,-.85,z),.176,.025,roof)
 for i in range(12):
  x=random.uniform(-1.3,1.2);y=random.uniform(-1.2,1.15)
  o=box('Discarded timber',(x,y,.12),(.45,.055,.04),wood);o.rotation_euler.z=random.random()*math.tau
 for i in range(5):rod('Salvaged pipe',(.10+i*.13,.8,.15),(.10+i*.13,1.4,.15),.035,metal)


def forest_site():
 base()
 for index,(x,y,h) in enumerate(((-.67,.5,1.55),(.57,.59,1.8),(.1,-.65,1.25))):
  rod('Rough tree trunk',(x,y,.06),(x+.06,y,h),.075,wood)
  for branch in range(7):
   a=branch*2.39996+index;z=h*.58+(branch%3)*.18;end=Vector((x+math.cos(a)*.48,y+math.sin(a)*.48,z+.25))
   rod('Exposed branch',(x,y,z-.2),end,.024,wood)
   for cluster in range(4):
    delta=Vector((random.uniform(-.22,.22),random.uniform(-.22,.22),random.uniform(-.02,.20)))
    sphere('Irregular leaf cluster',end+delta,(random.uniform(.19,.32),random.uniform(.16,.27),random.uniform(.17,.28)),leaf if (branch+cluster)%3 else leaf2)
 for i in range(4):
  x=-1.1+i*.18;rod('Cut timber',(x,-.85,.16),(x,-.1,.16),.073,wood)
  o=cylinder('Cut log end',(x,-.86,.16),.067,.015,stone);o.rotation_euler.x=math.pi/2
 for i in range(10):
  a=random.random()*math.tau;r=random.uniform(.6,1.4);x=math.cos(a)*r;y=math.sin(a)*r
  rod('Dry brush',(x,y,.08),(x+.06,y+.03,.30),.008,wood)


def compound(home=False):
 base()
 box('Brick quarters',(-.25,.24,.59),(1.65,1.3,1),stone if home else metal)
 for side in (-1,1):
  o=box('Patched sheet roof',(-.25,.24+side*.37,1.18),(1.95,.85,.065),roof);o.rotation_euler.x=-side*.38
 corrugated(-.25,.24,1.18,1.9,1.5)
 box('Recessed door',(-.25,-.425,.45),(.42,.04,.68),dark)
 for x in (-.80,.35):
  box('Window frame',(x,-.425,.77),(.35,.035,.32),wood);box('Shaded glass',(x,-.45,.77),(.26,.015,.23),water)
 for x in (-1.43,1.43):
  for y in (-1.15,-.58,0,.58,1.15):box('Perimeter post',(x,y,.36),(.06,.06,.6),wood)
  for h in (.25,.49):rod('Perimeter wire',(x,-1.18,h),(x,1.18,h),.011,wire)
 for y in (-1.18,1.18):
  for x in (-1.43,-.95,-.48,0,.48,.95,1.43):
   if y<0 and abs(x)<.3:continue
   box('Scrap palisade',(x,y,.28),(.42,.04,.38),wood if home else roof)
 for x in (1.05,1.35):
  for y in (.5,.95):rod('Lookout brace',(x,y,.1),(x,y,1.4),.03,metal)
 box('Lookout platform',(1.2,.72,1.3),(.65,.7,.09),wood);box('Watch roof',(1.2,.72,1.78),(.78,.8,.055),roof)
 for x in (-1.1,-.78):cylinder('Supplies', (x,-.85,.3),.14,.42,metal if home else roof)
 rod('Flag mast',(-1.12,.80,.1),(-1.12,.80,1.97),.02,wire)
 box('Faded pennant',(-.90,.80,1.77),(.43,.025,.23),pale if home else roof)
 for i in range(10):sphere('Perimeter sandbags',(-1.2+(i%5)*.23,-1.05+(i//5)*.15,.13),(.14,.09,.07),pale)
 if home:
  for i in range(3):
   x=.25+i*.28;box('Home raised bed',(x,-.7,.12),(.2,.65,.08),dark)
   for j in range(3):
    for k in range(4):leaf_blade((x,-.95+j*.23,.16),k*math.pi/2,.16,crop)
 else:
  box('Wrecked supply container',(.7,-.60,.27),(.6,.6,.31),roof)
  for i in range(5):box('Container ribs',(.40+i*.14,-.91,.27),(.02,.02,.25),metal)

bpy.ops.object.camera_add(location=(6,-6,6.8));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.8))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=4.6;scene.camera=camera
# Warm directional key and a cool, restrained sky fill; contact shadows are baked into alpha sprites.
scene.world.color=(.15,.18,.23)
for location,power,size,color in (((-3,-4,7),650,3.5,(1,.86,.68)),((4,2,5),180,5,(.66,.78,1))):
 bpy.ops.object.light_add(type='AREA',location=location);lamp=bpy.context.object;lamp.data.energy=power;lamp.data.size=size;lamp.data.color=color;lamp.rotation_euler=(Vector((0,0,.5))-lamp.location).to_track_quat('-Z','Y').to_euler()
for name,build in [('Water',water_site),('Food',food_site),('Scrap',scrap_site),('Lumber',forest_site),('Camp',lambda:compound(False)),('Settlement',lambda:compound(True))]:
 if os.environ.get('ASH_LANDMARK_ONLY') and name!=os.environ['ASH_LANDMARK_ONLY']:continue
 build();scene.render.filepath=os.path.join(out,name+'.png');bpy.ops.render.render(write_still=True)
 for obj in list(scene.objects):
  if obj.type=='MESH':bpy.data.objects.remove(obj,do_unlink=True)
 print('PASS weathered landmark',name,flush=True)
