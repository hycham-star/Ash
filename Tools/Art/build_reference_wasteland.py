"""Rebuild the original low-poly kit to the supplied orange-desert/container-fort reference.
Editable 3D geometry and painted atlases; preserves old asset paths and gameplay IDs.
blender -b -t 4 --python-exit-code 1 --python this_file -- repo
"""
import bpy,sys,os,math,random,json
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1]
base=os.path.join(root,'Tools/Art/build_comic_outpost.py');src=open(base).read();exec(compile(src[:src.index('# Main two-storey command hall')],base,'exec'),globals())
random.seed(9106)
# Muted painted metal, canvas and sun-bleached timber, with dark comic seams.
concrete=paint('Warm chipped concrete',(.46,.35,.25));blue=paint('Faded slate panel',(.32,.36,.35));sand=paint('Orange desert footing',(.60,.37,.18));rubber=paint('Old rubber',(.055,.047,.037));water=paint('Oasis water',(.15,.42,.39));dirt=sand

def tire(x,y,z=.22,r=.20,flat=False):
 bpy.ops.mesh.primitive_torus_add(major_segments=10,minor_segments=4,location=(x,y,z),major_radius=r*.72,minor_radius=r*.28)
 o=bpy.context.object
 if not flat:o.rotation_euler.x=math.pi/2
 finish(o,'Worn salvage tire',rubber)

def panel(name,p,size,m,ribs=0):
 box(name,p,size,m)
 for i in range(ribs):
  x=p[0]-size[0]*.45+i*size[0]*.9/max(1,ribs-1)
  box('Pressed sheet rib',(x,p[1]-size[1]/2-.012,p[2]),(.018,.022,size[2]*.9),m)

def container(p,size,m):
 x,y,z=p;w,d,h=size;panel('Shipping container',(x,y,z),(w,d,h),m,max(5,round(w/.16)))
 for side in (-1,1):
  for end in (-1,1):box('Container corner frame',(x+side*w*.48,y+end*d*.48,z),(.06,.06,h+.06),steel)
  box('Container edge rail',(x,y-d*.51,z+side*h*.47),(w+.06,.045,.05),ink)
 for dx in (-.16,.16):rod('Container locking bar',(x+dx,y-d*.52,z-h*.42),(x+dx,y-d*.52,z+h*.42),.014,steel,4)

def tower(x=0,y=0,height=3.2,size=.94):
 for sx in (-1,1):
  for sy in (-1,1):rod('Lookout timber leg',(x+sx*size*.45,y+sy*size*.45,.04),(x+sx*size*.37,y+sy*size*.37,height*.72),.045,wood)
 for side in (-1,1):
  rod('Crossed tower brace',(x+side*size*.43,y-size*.43,.3),(x+side*size*.37,y+size*.37,height*.69),.025,wood)
  rod('Crossed tower brace',(x+side*size*.43,y+size*.43,.3),(x+side*size*.37,y-size*.37,height*.69),.025,wood)
 for side in (-1,1):
  rod('Front lookout diagonal',(x-size*.43,y+side*size*.43,.30),(x+size*.37,y+side*size*.37,height*.69),.025,wood)
  rod('Front lookout crossing',(x+size*.43,y+side*size*.43,.30),(x-size*.37,y+side*size*.37,height*.69),.025,wood)
 box('Lookout floor',(x,y,height*.72),(size*1.2,size*1.2,.10),wood)
 for sy in (-1,1):box('Lookout half wall',(x,y+sy*size*.53,height*.79),(size*1.1,.055,height*.14),wood)
 for sx in (-1,1):
  box('Lookout side wall',(x+sx*size*.53,y,height*.79),(.055,size*1.1,height*.14),rust)
  for sy in (-1,1):rod('Lookout roof pole',(x+sx*size*.52,y+sy*size*.52,height*.73),(x+sx*size*.52,y+sy*size*.52,height*.97),.025,wood)
 roofparts=len(parts);o=box('Single sloping tin lookout roof',(x,y,height),(size*1.46,size*1.48,.055),rust);o.rotation_euler.x=.10
 for i in range(5):box('Lookout roof seam',(x-size*.6+i*size*.3,y,height+.035),(.014,size*1.48,.016),ink)
 for sx in (-1,1):rod('Lookout ladder',(x+sx*.14,y-size*.65,.05),(x+sx*.14,y-size*.65,height*.72),.015,wood)
 for i in range(9):rod('Lookout ladder rung',(x-.14,y-size*.65,.15+i*height*.065),(x+.14,y-size*.65,.15+i*height*.065),.012,wood,4)

def flag(x,y,z):
 rod('Faction flag mast',(x,y,z-.7),(x,y,z+.65),.018,steel)
 verts=[(x,y,z+.62),(x+.50,y+.025,z+.57),(x+.45,y-.02,z+.30),(x+.2,y+.02,z+.35),(x,y,z+.32)]
 mesh=bpy.data.meshes.new('Torn red flag');mesh.from_pydata(verts,[],[(0,1,2,3,4),(4,3,2,1,0)]);mesh.update();o=bpy.data.objects.new('Wind bent red banner',mesh);bpy.context.collection.objects.link(o);finish(o,o.name,red)

def sandbags(x,y,count=5):
 for i in range(count):sphere('Hand stacked sandbag',(x+i*.28,y,.15),(.17,.12,.12),cloth)

def bike(x,y,heading=0):
 start=len(parts)
 for yy in (-.36,.36):
  tire(0,yy,.22,.22);rod('Motorcycle wheel hub',(-.08,yy,.22),(.08,yy,.22),.085,steel,8)
 rod('Motorbike frame',(0,-.36,.22),(0,.02,.61),.035,steel);rod('Motorbike frame',(0,.02,.61),(0,.36,.22),.035,steel)
 box('Bike fuel tank',(0,-.10,.56),(.23,.30,.15),rust);box('Bike leather seat',(0,.18,.61),(.24,.30,.07),rubber)
 rod('Front forks',(-.07,-.36,.22),(-.07,-.29,.72),.02,steel);rod('Handlebar',(-.24,-.29,.74),(.24,-.29,.74),.018,steel)
 box('Engine',(0,.05,.32),(.22,.24,.22),steel);rod('Exhaust',(.13,-.08,.25),(.13,.49,.25),.028,steel)
 a=math.radians(heading)
 for o in parts[start:]:
  p=o.location.copy();o.location=(x+p.x*math.cos(a)-p.y*math.sin(a),y+p.x*math.sin(a)+p.y*math.cos(a),p.z);o.rotation_euler.z+=a

def shack(w=1.8,d=1.4,h=1.25):
 box('Dark shack interior',(0,0,h*.5),(w,d,h),ink)
 for side in (-1,1):
  for i in range(4):
   x=-w/2+(i+.5)*w/4
   if side==-1 and abs(x)<w*.22:continue
   panel('Salvaged shack wall',(x,side*d*.5,h*.5),(w/4-.018,.045,h),[cloth,blue,rust,wood][(i+(side+1))%4],2)
  for y in (-d*.3,d*.3):box('Shack side panel',(side*w*.5,y,h*.5),(.045,d*.52,h),wood)
 for x in (-w*.5,w*.5):
  for y in (-d*.5,d*.5):rod('Shack timber edge',(x,y,0),(x,y,h+.05),.025,wood,4)
 roof(w+.16,d+.16,h+.03,.25)
 # Recessed window, timber strips and a door brace add readable comic details at miniature scale.
 box('Shack window recess',(-w*.30,-d*.5-.035,h*.65),(.34,.03,.26),ink)
 for zz in (-.14,.14):box('Shack window trim',(-w*.30,-d*.5-.060,h*.65+zz),(.40,.035,.035),pale)
 for xx in (-w*.48,w*.48):box('Patched shack facade frame',(xx,-d*.5-.055,h*.5),(.055,.035,h),pale)
 box('Dark open doorway',(0,-d*.5-.025,.5),(.52,.025,1),ink)
 o=box('Hanging canvas doorway',(.3,-d*.5-.05,.55),(.20,.025,1.04),cloth);o.rotation_euler.z=.1
 crate(-w*.39,-d*.5-.25);tire(w*.40,-d*.5-.18,.19,.18)

def tent(x,y,w=1.2,d=1.4):
 start=len(parts);box('Tent interior',(0,0,.5),(w,d,.95),ink)
 for side in (-1,1):
  o=box('Olive canvas tent roof',(side*w*.26,0,.96),(w*.64,d+.12,.025),cloth);o.rotation_euler.y=side*.58
  box('Tent side canvas',(side*w*.52,0,.39),(.03,d,.78),olive)
  box('Half open tent flap',(side*w*.37,-d*.51,.44),(w*.27,.025,.84),patch)
 for yy in (-d*.5,d*.5):rod('Tent ridge pole',(0,yy,0),(0,yy,1.20),.018,wood)
 for o in parts[start:]:o.location+=Vector((x,y,0))

def fence(w,d,height=.8,metal=False):
 for x in (-w/2,w/2):
  for y in (-d/2,d/2):rod('Perimeter fence post',(x,y,0),(x,y,height+.12),.028,wood)
 for z in (height*.35,height*.8):
  for x in (-w/2,w/2):rod('Fence side rail',(x,-d/2,z),(x,d/2,z),.014,steel if metal else wood,4)
  rod('Fence rear rail',(-w/2,d/2,z),(w/2,d/2,z),.014,wood,4)
 if metal:
  for side in (-1,1):
   for i in range(12):
    y=-d/2+i*d/12;rod('Chain-link diagonal',(side*w/2,y,.12),(side*w/2,min(d/2,y+.4),height),.006,steel,3)

def wreck(x,y,angle=0):
 start=len(parts);box('Abandoned car body',(0,0,.35),(.68,1.25,.35),rust);o=box('Crushed car roof',(0,.12,.66),(.59,.60,.20),blue);o.rotation_euler.y=.08
 box('Broken windshield',(0,-.20,.64),(.48,.03,.17),ink)
 for sx in (-1,1):
  for yy in (-.40,.40):tire(sx*.36,yy,.23,.17)
 a=math.radians(angle)
 for o in parts[start:]:
  p=o.location.copy();o.location=(x+p.x*math.cos(a)-p.y*math.sin(a),y+p.x*math.sin(a)+p.y*math.cos(a),p.z);o.rotation_euler.z+=a

# Headquarters: fortified concrete base and irregular stacked shipping containers.
box('Fortified command foundation',(0,0,.70),(3.45,2.8,1.4),concrete)
for x in (-1.65,1.65):
 for y in (-1.33,1.33):box('Reinforced corner pier',(x,y,.85),(.28,.28,1.7),concrete)
for x in (-1.10,0,1.10):panel('Front repair plate',(x,-1.43,.96),(.91,.07,.63),[rust,steel,olive][round(x+1)],3)
box('Fortified dark gateway',(0,-1.48,.5),(.68,.05,1),ink)
for x in (-.45,.45):box('Entrance concrete jamb',(x,-1.48,.62),(.20,.16,1.25),concrete)
container((-.85,0,1.90),(1.55,2.45,.9),rust);container((.85,.10,1.90),(1.55,2.25,.9),olive)
container((.35,.15,2.82),(1.45,1.95,.88),rust)
# Narrow central communications tower and flag.
box('Command lookout lower',(0,.4,3.5),(.83,.85,.60),concrete)
for side in (-1,1):box('Lookout window',(side*.42,.4,3.55),(.025,.55,.20),ink)
box('Command lookout window',(0,-.03,3.55),(.55,.025,.20),ink)
box('Lookout tin top',(0,.4,3.84),(1.02,1.06,.06),rust);flag(.1,.4,4.17)
for x,y in [(-1.95,-.7),(1.95,.65)]:barrel(x,y);tire(x,y+.35,.20,.18)
sandbags(-1.18,-1.82,4)
for x in (-1.96,1.96):
 for i in range(3):panel('Perimeter HQ shield',(x,-.7+i*.65,.47),(.065,.6,.82),steel)
complete('Headquarters')

# Homes and hero barracks retain their individual saved identity, but receive patched metal skins.
shack(1.65,1.32,1.20);complete('OutpostHut')
shack(2.1,1.7,1.50);flag(.7,.5,1.75);barrel(-1.15,.45);complete('Barracks')
# General workshop: salvage garage and a roll-up entry, deliberately distinct from the biker center.
box('Workshop shell',(0,0,.72),(2.5,1.8,1.44),concrete)
box('Workshop open door',(0,-.915,.60),(1.45,.04,1.2),ink)
for x in (-1.12,1.12):panel('Workshop patched face',(x,-.94,.82),(.24,.05,1.05),rust,2)
box('Workshop sloped roof',(0,0,1.5),(2.75,2.02,.09),steel)
for i in range(7):box('Workshop roof seam',(-1.2+i*.4,0,1.56),(.018,2.03,.02),ink)
for x in (-.7,.7):tire(x,0,1.70,.22,True)
barrel(-1.20,-1.10);crate(1.15,-1.1);complete('Workshop')

# Dedicated Melee center: an open rope ring and hanging punching bag.
box('Melee ring compacted dirt',(0,0,.05),(3.25,3.15,.06),sand)
for x in (-1.4,1.4):
 for y in (-1.35,1.35):rod('Ring corner post',(x,y,.05),(x,y,1.1),.06,wood)
for z in (.35,.66,.96):
 for x in (-1.4,1.4):rod('Boxing ring side rope',(x,-1.35,z),(x,1.35,z),.018,cloth)
 for y in (-1.35,1.35):rod('Boxing ring end rope',(-1.4,y,z),(1.4,y,z),.018,cloth)
for y in (-1.50,1.5):sandbags(-1.1,y,8)
rod('Punching frame',(.75,.5,0),(.75,.5,1.65),.04,wood);rod('Punching crossbar',(.75,.5,1.65),(0,.5,1.65),.035,wood)
rod('Hanging chain',(0,.5,1.65),(0,.5,1.38),.012,steel);rod('Worn punching bag',(0,.5,.65),(0,.5,1.35),.15,patch,8)
complete('InfantryCenter')

# Biker station: squat roadside garage, flat sheet roof, tire stacks and parked bikes.
box('Biker garage',(0,.3,.85),(2.65,1.8,1.7),concrete);box('Biker open garage',(0,-.62,.7),(1.55,.04,1.40),ink)
box('Biker garage roof',(0,.3,1.75),(2.9,2.1,.09),rust)
for i in range(8):box('Garage corrugated seam',(-1.3+i*.37,.3,1.81),(.025,2.12,.02),steel)
box('Garage faded sign',(0,-.8,1.6),(1.7,.05,.25),white)
for x in (-.9,.9):tire(x,.3,1.95,.23,True)
for n in range(3):tire(-1.5,-.45,.15+n*.19,.19,True)
bike(-.65,-1.45,-35);bike(.8,-1.45,-20);barrel(1.48,.5);complete('BikerCenter')

# Sharpshooter military: two canvas tents, target boards and wire range fencing.
tent(-.85,.65,1.2,1.45);tent(.85,.65,1.2,1.45);fence(3.7,3.5,.9,True)
for x in (-1.12,0,1.12):
 rod('Target stand',(x,-1.15,.03),(x,-1.15,1.10),.025,wood)
 for layer,(radius,m) in enumerate(((.27,white),(.19,red),(.11,white),(.05,red))):
  rod('Painted target ring',(x,-1.20-layer*.02,.93),(x,-1.218-layer*.02,.93),radius,m,12)
crate(1.45,-.55);complete('ArmyCenter')

# Oasis pump and water tower: distinct from a roofed medieval well.
rod('Oasis shallow water',(0,0,.035),(0,0,.055),.86,water,16)
for i in range(12):
 a=i*math.tau/12;sphere('Oasis stone edge',(math.cos(a)*.91,math.sin(a)*.91,.10),(.19,.13,.12),concrete)
for x in (.68,1.12):
 for y in (.45,.89):rod('Water tank tower leg',(x,y,.03),(x,y,1.35),.032,wood)
rod('Reclaimed water tank',(.90,.67,1.32),(.90,.67,1.95),.30,steel,10)
for z in (1.4,1.85):rod('Tank band',(.9,.67,z-.018),(.9,.67,z+.018),.312,ink,10)
rod('Oasis pump riser',(-.78,-.52,.05),(-.78,-.52,.60),.04,steel);rod('Hand pump handle',(-.98,-.52,.67),(-.50,-.52,.67),.022,steel)
barrel(-.9,.7);complete('WaterStation')

# Dry farm: separate rows of greens, a tiny lean-to, wood fencing.
fence(2.8,2.6,.52)
for row in range(5):
 y=-1.02+row*.40;box('Dry cultivated furrow',(.28,y,.04),(1.72,.23,.045),concrete)
 for col in range(5):
  x=-.38+col*.32;sphere('Crop seedling',(x,y,.12),(.09,.10,.13),green)
start=len(parts);shack(.72,.70,.75)
for o in parts[start:]:o.location+=Vector((-.95,.70,0))
complete('FoodGarden')

# Scrap resource center: wrecked cars, bent plates, loose tires, storage lean-to.
wreck(-.65,-.35,-35);wreck(.70,.2,25)
for i in range(5):
 o=box('Twisted scrap plate',(-1.2+i*.54,.95,.16+(i%2)*.11),(.45,.55,.045),[rust,blue,olive][i%3]);o.rotation_euler=(.1*i,.2,-i*.27)
for i in range(4):tire(-1.05+i*.6,-1.1,.18,.18,True)
crate(-1.2,.1);complete('Scrapyard')
# Dry timber node with stacked logs and a few leafless tree silhouettes.
for x,y in [(-.85,.35),(.6,.6),(0,-.8)]:
 rod('Dead tree trunk',(x,y,0),(x+.12,y,1.55),.055,wood)
 rod('Dead branch',(x+.05,y,.60),(x-.38,y+.10,1.1),.025,wood)
 rod('Dead branch',(x+.08,y,1.0),(x+.42,y-.12,1.45),.02,wood)
for j in range(6):
 yy=-.45+(j%3)*.19;zz=.12+(j//3)*.17;rod('Stacked timber log',(-.60,yy,zz),(.35,yy,zz),.085,wood,8)
complete('LumberYard')

# Enemy camp: rusty enclosed clan base with shacks and gate shields.
for side in (-1,1):
 for i in range(6):
  p=-1.65+i*.66;panel('Rusty camp side',(side*1.85,p,.48),(.065,.62,.90),[rust,steel,wood][i%3])
  panel('Camp rear or gate',(p,side*1.85,.48),(.62,.065,.90),rust if i%2 else wood)
  rod('Camp pointed post',(side*1.85,p,0),(side*1.85,p,1.15),.035,wood)
for x,y in [(-.8,.65),(.8,.65)]:
 start=len(parts);shack(1.20,1.0,1.1)
 for o in parts[start:]:o.location+=Vector((x,y,0))
box('Camp fortified gate',(0,-1.90,.55),(.90,.09,1.10),steel)
barrel(-1.3,-1.0);crate(1.3,-1.05);crate(1.3,-.65);flag(1.3,.5,1.4)
for i in range(12):
 x=-1.72+i*.31;rod('Camp sharpened rear stake',(x,1.88,.74),(x+.025,1.88,1.22),.025,wood,4)
complete('EnemyCamp')
# Retain the market and clinic designs, recolored into the same reference palette.
tent(0,0,2.15,1.8)
box('Clinic medical sign',(0,-.98,1.05),(.42,.035,.36),white);box('Medical cross',(0,-1.005,1.05),(.28,.018,.07),red);box('Medical cross',(0,-1.007,1.05),(.07,.018,.28),red)
barrel(1.15,.55);crate(-1.12,-.75);complete('Hospital')
shack(1.7,.95,1.1)
for x in (-1.15,1.15):rod('Market awning pole',(x,-.95,0),(x,-.95,1.65),.025,wood)
box('Market red canvas',(0,-.55,1.62),(2.6,1.12,.04),red);box('Market counter',(0,-1.02,.45),(2.3,.40,.70),wood)
for i in range(4):crate(-.82+i*.55,-1.0,.95)
complete('Market')
tower();complete('OutpostWatchtower')

report={}
for model in models:
 bake(model,model.name,1024);export(model.name+'.fbx',[model],False)
 report[model.name]={'triangles':len(model.data.polygons),'materials':1,'texture':1024}
 print('PASS reference kit export',model.name,report[model.name],flush=True)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(root,'Tools/Art/ComicOutpost.blend'))
bpy.ops.file.make_paths_relative()
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(root,'Tools/Art/ComicOutpost.blend'))
open(os.path.join(root,'Tools/Art/ComicOutpostBudget.json'),'w').write(json.dumps(report,indent=2)+'\n')
print('PASS reference wasteland kit complete',flush=True)
