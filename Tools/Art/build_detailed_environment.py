"""Original concrete outpost buildings and salvage truck inspired by miniature wasteland dioramas.
Blender 4.3.2, CPU: blender -b -t 4 --python-exit-code 1 --python this_file -- repo
One baked 1024px albedo/AO + normal atlas per model, original gameplay IDs unchanged.
"""
import ast,bpy,math,os,random,sys
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1];out=os.path.join(root,'Assets/Ash/Resources/Art')
common=os.path.join(root,'Tools/Art/build_visual_slice.py');tree=ast.parse(open(common).read())
names={'mat','finish','box','ball','rod','join','bake','export'}
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in names],type_ignores=[]),common,'exec'),globals())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);random.seed(933)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=False;scene.render.bake.margin=8
scene.world.color=(.22,.25,.29);scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast';parts=[]
concrete=mat('Eroded sand concrete',(.36,.33,.24));edge=mat('Exposed concrete edges',(.43,.40,.31));brick=mat('Exposed red brick',(.28,.12,.065),'brick')
wood=mat('Sun bleached boards',(.23,.16,.085));iron=mat('Old military steel',(.095,.13,.115));rust=mat('Oxidised metal',(.25,.105,.045));canvas=mat('Faded canvas',(.31,.28,.17),'cloth')
dark=mat('Recesses and rubber',(.022,.027,.025));glass=mat('Dusty blue glass',(.065,.12,.13));sand=mat('Dirt and rubble',(.24,.205,.135));sack=mat('Old hessian',(.32,.28,.18),'cloth');green=mat('Dry weeds',(.13,.18,.055))
paint=mat('Faded hazard ochre',(.44,.32,.13));enamel=mat('Weathered truck enamel',(.23,.26,.18));pale=mat('Dusty headlights',(.52,.49,.32))

def cylinder(name,p,r,h,m,vertices=16):
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=h,location=p);return finish(bpy.context.object,name,m,bevel=.008,smooth=True)
def drum(x,y,z=.35):
 cylinder('Fuel drum',(x,y,z),.14,.43,iron)
 for h in (-.14,.14):cylinder('Drum rolled seam',(x,y,z+h),.146,.025,rust)
 cylinder('Drum bung',(x+.06,y,z+.225),.025,.018,dark,8)
def crate(x,y,z=.29,scale=1):
 box('Supply crate',(x,y,z),(.4*scale,.34*scale,.36*scale),wood)
 for dx in (-.13,.13):box('Crate binding',(x+dx*scale,y-.18*scale,z),(.035*scale,.018,.36*scale),iron,bevel=.002)
def dish(x,y,z,r=.32):
 rod('Dish support',(x,y,z-.3),(x,y,z),.026,iron)
 verts=[(0,0,0)];faces=[]
 for ring in (1,2):
  for i in range(16):
   a=i*math.tau/16;rr=r*ring/2;verts.append((math.cos(a)*rr,math.sin(a)*rr,.11*(ring/2)**2))
 for i in range(16):
  j=(i+1)%16;faces.append((0,1+i,1+j));faces.append((1+i,17+i,17+j,1+j))
 mesh=bpy.data.meshes.new('Concave satellite dish');mesh.from_pydata(verts,[],faces);mesh.update();obj=bpy.data.objects.new('Satellite dish',mesh);bpy.context.collection.objects.link(obj);obj.location=(x,y,z);obj.rotation_euler=(.75,.15,0);finish(obj,'Satellite receiver',iron,smooth=True)
 rod('Receiver arm',(x,y,z),(x+.13,y-.25,z+.25),.014,rust)
def window(x,y,z,width=.56,height=.58):
 box('Deep window shadow',(x,y,z),(width+.12,.04,height+.13),dark,bevel=.01)
 box('Dusty inset glazing',(x,y-.025,z),(width,.018,height),glass,bevel=.005)
 for dx in (-width/2,0,width/2):box('Bent window bars',(x+dx,y-.05,z),(.025,.02,height+.04),iron,bevel=.003)
 box('Cracked sill',(x,y-.09,z-height/2-.05),(width+.18,.20,.075),edge,bevel=.012)
def shell(width,depth,height,parapet=True):
 box('Broken concrete plinth',(0,0,.12),(width+.27,depth+.26,.17),edge,bevel=.04)
 box('Weathered building shell',(0,0,height/2+.18),(width,depth,height),concrete,bevel=.055)
 for x in (-width/2,width/2):
  for y in (-depth/2,depth/2):box('Exposed structural pier',(x,y,height/2+.18),(.12,.14,height+.04),edge,bevel=.018)
 roof=height+.21;box('Rooftop concrete slab',(0,0,roof),(width+.18,depth+.18,.14),edge)
 if parapet:
  for y in (-depth/2,depth/2):box('Low chipped parapet',(0,y,roof+.17),(width+.1,.12,.28),concrete,bevel=.035)
  for x in (-width/2,width/2):box('Roof parapet',(x,0,roof+.17),(.12,depth+.1,.28),concrete,bevel=.035)
  for i in range(10):
   x=random.uniform(-width*.43,width*.43);y=random.uniform(-depth*.4,depth*.4)
   ball('Rooftop rubble',(x,y,roof+.11),(random.uniform(.025,.07),.04,.03),sand)
 # Patches of exposed masonry and dark weather streaks sit just above the wall surface.
 for i in range(13):
  x=random.uniform(-width*.43,width*.43);z=random.uniform(.28,height)
  if abs(x)<.45 and z<1.5:continue
  box('Spalled plaster',(x,-depth/2-.008,z),(random.uniform(.08,.22),.018,random.uniform(.05,.19)),brick,bevel=.007)
 # Side windows and a downpipe break up the large visible wall.
 for y in (-depth*.25,depth*.25):
  box('Side window recess',(width/2+.008,y,height*.60),(.025,.59,.63),dark,bevel=.008)
  box('Side window glass',(width/2+.025,y,height*.60),(.018,.47,.51),glass,bevel=.005)
  for yy in (-.24,0,.24):box('Side window mullion',(width/2+.047,y+yy,height*.60),(.025,.025,.55),iron,bevel=.003)
  box('Side window sill',(width/2+.07,y,height*.60-.35),(.21,.7,.07),edge)
 rod('Exposed rainwater pipe',(width/2+.09,depth*.37,.2),(width/2+.09,depth*.37,roof),.026,rust)
 return roof

def doorway(depth,height=1.3):
 y=-depth/2-.035
 box('Dark recessed doorway',(0,y,height/2+.19),(.78,.055,height),dark)
 for x in (-.43,.43):box('Door jamb',(x,y-.02,height/2+.19),(.10,.12,height+.12),edge)
 box('Salvaged door',(0,y-.045,height/2+.16),(.61,.018,height-.07),wood)
 for z in (.5,1.1):box('Door metal strap',(0,y-.066,z),(.61,.02,.045),iron,bevel=.005)
 rod('Door pull',(.19,y-.09,.65),(.19,y-.09,.88),.014,iron)
 box('Door lintel',(0,y,height+.25),(1.0,.17,.12),edge)
 for i in range(2):box('Entry step',(0,y-.16-i*.16,.17-i*.055),(1.0,.29,.10),concrete)
def awning(width,depth,height):
 y=-depth/2-.30
 for x in (-width*.35,width*.35):rod('Awning post',(x,y-.2,.1),(x,y-.2,height-.15),.025,iron)
 for i in range(6):
  x=(i-2.5)*width*.12;o=box('Patched canvas awning',(x,y,height),(width*.12,.64,.027),canvas if i%2 else paint,bevel=.005);o.rotation_euler.x=.15
 rod('Awning brace',(-width*.38,y-.2,height-.12),(width*.38,y-.2,height-.12),.022,wood)
def rooftop(x,y,z):
 cylinder('Water reservoir',(x,y,z+.27),.28,.45,iron)
 for h in (.07,.45):cylinder('Reservoir seam',(x,y,z+h),.285,.025,rust)
 box('Reservoir concrete base',(x,y,z+.03),(.65,.65,.08),edge)
 rod('Rooftop plumbing',(x+.28,y,z+.2),(x+.52,y,z+.2),.025,iron)
def clutter(width,depth):
 for x,y in [(-width*.40,-depth*.50),(width*.40,depth*.27)]:
  crate(x,y);drum(x,y+.40)
 for i in range(6):
  x=-width*.38+i*.17;ball('Doorway sandbag',(x,-depth*.58,.15),(.12,.085,.065),sack)
 for i in range(8):
  x=random.uniform(-width*.47,width*.47);y=random.choice([-1,1])*depth*.52
  ball('Scattered brick rubble',(x,y,.13),(.035,.025,.03),brick)
 for i in range(4):
  x=width*.46;y=-depth*.25+i*.17
  for j in range(3):rod('Dry weeds',(x,y,.09),(x+.035*math.cos(j*2),y+.035*math.sin(j*2),.24+random.random()*.1),.005,green)

def headquarters():
 w,d=3.25,2.65;z=shell(w,d,2.4);doorway(d,1.38)
 for x in (-1.03,1.03):window(x,-d/2-.03,1.34,.59,.70)
 # Small upper communications room, stepped back to reveal the accessible roof.
 box('Roof communications room',(-.62,.37,z+.50),(1.1,1.05,.85),concrete,bevel=.04)
 box('Upper room roof',(-.62,.37,z+.98),(1.22,1.17,.12),edge)
 window(-.62,-.18,z+.53,.52,.42)
 dish(.80,.62,z+.52,.36);rooftop(.70,-.54,z+.05)
 rod('Radio aerial',(-1.28,.87,z),(-1.28,.87,z+1.12),.018,iron)
 for i in range(3):rod('Aerial crossbar',(-1.50,.87,z+.72+i*.12),(-1.06,.87,z+.72+i*.12),.008,iron)
 # Ladder on the visible side and a low entry canopy.
 for x in (1.05,1.34):rod('Roof access ladder',(x,-d/2-.16,.19),(x,-d/2-.16,z+.28),.018,wood)
 for i in range(10):rod('Ladder rung',(1.05,-d/2-.18,.36+i*.23),(1.34,-d/2-.18,.36+i*.23),.015,wood)
 awning(1.8,d,1.80);clutter(w,d)
 return join('Headquarters',parts[:])
def barracks():
 w,d=2.85,2.4;z=shell(w,d,1.85);doorway(d,1.25)
 for x in (-.96,.96):window(x,-d/2-.03,1.07,.48,.5)
 rooftop(-.70,.43,z+.06);dish(.77,.56,z+.42,.26)
 box('Ventilation unit',(.54,-.46,z+.23),(.58,.44,.35),iron)
 for i in range(6):box('Vent slat',(.54,-.69,z+.10+i*.045),(.45,.025,.018),dark,bevel=.002)
 awning(w*.9,d,1.55);clutter(w,d)
 for i in range(3):
  x=-.35+i*.3;rod('Rooftop bedroll',(x,.55,z+.17),(x,.95,z+.17),.09,canvas)
 return join('Barracks',parts[:])
def workshop():
 w,d=2.85,2.5;z=shell(w,d,1.95)
 box('Garage door recess',(-.23,-d/2-.03,1.0),(1.70,.05,1.62),dark)
 for i in range(14):box('Rusty roller shutter',(-.23,-d/2-.065,.26+i*.105),(1.57,.03,.09),iron,bevel=.008)
 for x in (-1.1,.65):box('Shutter rail',(x,-d/2-.07,1),(.065,.10,1.72),rust)
 window(1.05,-d/2-.03,1.15,.36,.58)
 rooftop(-.72,.48,z+.04)
 box('Rooftop service hatch',(.63,.48,z+.10),(.7,.65,.10),rust)
 rod('Vent stack',(.90,-.50,z),(.90,-.50,z+.64),.07,iron);box('Vent rain cap',(.9,-.5,z+.67),(.26,.26,.04),rust)
 awning(w*.95,d,1.90);clutter(w,d)
 box('Tool workbench',(1.12,-1.47,.65),(.60,.44,.07),wood)
 for x in (.9,1.34):rod('Workbench leg',(x,-1.47,.12),(x,-1.47,.62),.022,iron)
 box('Bench vise',(1.12,-1.47,.74),(.13,.14,.16),iron)
 for i in range(3):
  o=box('Leaning salvage sheet',(1.46,.35+i*.23,.52),(.035,.40,.75),rust if i%2 else iron);o.rotation_euler.y=-.22
 return join('Workshop',parts[:])

def truck():
 # Longitudinal axis +Y: individual chassis, cab, engine hood and an open cargo bed.
 box('Truck chassis',(0,0,.44),(1.25,2.8,.17),iron)
 box('Cab body',(0,.38,.95),(1.24,1.03,.72),enamel,bevel=.075)
 box('Cab roof',(0,.37,1.45),(1.33,1.14,.11),iron,bevel=.04)
 box('Engine hood',(0,1.13,.85),(1.19,.66,.30),enamel,bevel=.045)
 box('Front windshield',(0,.91,1.24),(1.03,.025,.31),glass,bevel=.012)
 for x in (-.56,0,.56):rod('Windshield frame',(x,.935,1.05),(x,.935,1.43),.018,iron)
 for side in (-1,1):
  box('Cab side window',(side*.63,.42,1.24),(.025,.75,.29),glass,bevel=.01)
  box('Door panel',(side*.645,.40,.94),(.027,.79,.23),enamel,bevel=.015)
  box('Door handle',(side*.67,.1,1.02),(.025,.14,.025),iron,bevel=.003)
  rod('Wing mirror arm',(side*.62,.83,1.17),(side*.83,.83,1.22),.018,iron);box('Mirror',(side*.84,.83,1.25),(.07,.13,.15),dark)
  box('Cargo side',(side*.62,-.95,.94),(.09,1.22,.53),wood)
  for y in (-1.47,-.95,-.40):box('Cargo rack upright',(side*.65,y,1.08),(.065,.065,.85),iron)
  rod('Cargo top rail',(side*.65,-1.5,1.48),(side*.65,-.38,1.48),.024,iron)
  for y in (-1.05,1.03):
   o=cylinder('All terrain tire',(side*.70,y,.43),.37,.24,dark,20);o.rotation_euler.y=math.pi/2
   o=cylinder('Wheel rim',(side*.835,y,.43),.22,.035,iron,16);o.rotation_euler.y=math.pi/2
   o=cylinder('Wheel hub',(side*.86,y,.43),.085,.04,rust,12);o.rotation_euler.y=math.pi/2
   for lug in range(6):
    a=lug*math.tau/6;ball('Wheel lug',(side*.89,y+math.cos(a)*.13,.43+math.sin(a)*.13),(.017,.017,.017),edge)
   for tread in range(14):
    a=tread*math.tau/14;o=box('Tire tread',(side*.70,y+math.cos(a)*.36,.43+math.sin(a)*.36),(.26,.10,.07),dark,bevel=0);o.rotation_euler.x=a-math.pi/2
 box('Cargo bed',(0,-.96,.65),(1.16,1.20,.11),wood)
 box('Tailgate',(0,-1.56,.92),(1.22,.07,.48),enamel)
 box('Front armored bumper',(0,1.52,.54),(1.49,.17,.18),iron)
 box('Radiator dark grille',(0,1.47,.80),(.67,.035,.21),dark)
 for x in (-.25,-.125,0,.125,.25):rod('Grille bars',(x,1.50,.70),(x,1.50,.91),.013,iron)
 for x in (-.47,.47):box('Headlamp',(x,1.48,.85),(.22,.045,.14),pale,bevel=.03)
 for y in (-1.25,-.75):
  crate(-.20,y,.94,.9);ball('Cargo sack',(.32,y,1.01),(.19,.23,.25),sack)
 o=cylinder('Spare tire',(0,-1.62,1.04),.30,.13,dark,20);o.rotation_euler.x=math.pi/2
 for side in (-1,1):
  for y in (-1.05,1.03):box('Wheel arch armor',(side*.65,y,.85),(.30,.88,.08),rust)
 return join('SalvageTruck',parts[:])

models=[]
for build in (headquarters,barracks,workshop,truck):
 model=build()
 # Visual extents must fit the existing movable-building footprint, with no save/placement change.
 limits={'Headquarters':(2.28,2.08),'Barracks':(1.48,1.28),'Workshop':(1.68,1.58)}
 if model.name in limits:
  xlim,ylim=limits[model.name];sx=min(1,xlim/max(abs(v.co.x) for v in model.data.vertices));sy=min(1,ylim/max(abs(v.co.y) for v in model.data.vertices))
  for v in model.data.vertices:v.co.x*=sx;v.co.y*=sy
  model.data.update()
 models.append(model);parts.clear()
 triangles=[len(model.data.polygons) for model in models];assert max(triangles)<20000,triangles
for model in models:
 bake(model,model.name,1024);export(model.name+'.fbx',[model],False)
# Keep a focused editable source and previews of actual exported meshes.
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(root,'Tools/Art/DetailedEnvironment.blend'))
for location,power,color in [((-4,-5,8),1000,(1,.88,.73)),((4,2,5),450,(.68,.78,1))]:
 bpy.ops.object.light_add(type='AREA',location=location);o=bpy.context.object;o.data.energy=power;o.data.size=5;o.data.color=color;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(6,-6,6.8));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.8))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';scene.camera=camera
scene.render.film_transparent=True;scene.render.image_settings.color_mode='RGBA';scene.render.resolution_x=700;scene.render.resolution_y=700;scene.render.resolution_percentage=100;scene.cycles.samples=32
for model in models:
 for other in models:other.hide_render=other!=model
 target=Vector((0,0,1.70 if model.name=='Headquarters' else .95));camera.location=target+Vector((6,-6,6));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
 camera.data.ortho_scale=6.0 if model.name=='Headquarters' else 4.7
 scene.render.filepath=os.path.join(root,'Tools/Art/'+model.name+'_DetailedPreview.png');bpy.ops.render.render(write_still=True)
# Do not retain Blender's automatic previous-save copy as another shipped source.
backup=os.path.join(root,'Tools/Art/DetailedEnvironment.blend1')
if os.path.exists(backup):os.remove(backup)
print('PASS detailed environment',[(m.name,len(m.data.polygons)) for m in models],flush=True)
