"""Original four-building expansion. Blender 4.3.2; no external art or add-ons.
Reuses definition nodes from the approved slice generator without rebuilding its assets.
Run: blender -b --threads 4 --python-exit-code 1 --python Tools/Art/build_settlement_buildings.py -- /path/to/Ash
"""
import ast
import bpy
import math
import os
import random
import sys
from mathutils import Vector

root=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else os.getcwd()
out=os.path.join(root,'Assets/Ash/Resources/Art');os.makedirs(out,exist_ok=True)
common=os.path.join(root,'Tools/Art/build_visual_slice.py')
tree=ast.parse(open(common).read(),filename=common)
names={'mat','finish','box','ball','rod','join','bake','export'}
definitions=[node for node in tree.body if isinstance(node,ast.FunctionDef) and node.name in names]
assert {node.name for node in definitions}==names,'Shared art helper definitions changed'
exec(compile(ast.Module(body=definitions,type_ignores=[]),common,'exec'),globals())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
random.seed(173)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=False;scene.render.bake.margin=8
scene.world.color=(.26,.29,.33)
parts=[]
concrete=mat('Reclaimed concrete',(.42,.39,.30));wood=mat('Weathered timber',(.27,.19,.105));iron=mat('Painted steel',(.12,.155,.15));rust=mat('Oxidized sheet',(.25,.105,.05))
blue=mat('Faded tank enamel',(.12,.25,.27));canvas=mat('Weathered canvas',(.27,.28,.19),'cloth');dark=mat('Rubber and cavities',(.035,.035,.03));glass=mat('Dusty glazing',(.08,.13,.15));yellow=mat('Faded hazard paint',(.52,.36,.08))
soil=mat('Compost soil',(.10,.075,.045));green=mat('Vegetable leaves',(.12,.24,.065));pale=mat('Leaf ribs',(.20,.30,.10));brick=mat('Masonry',(.29,.17,.105),'brick')

def cylinder(name,p,radius,height,m):
 bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=radius,depth=height,location=p)
 return finish(bpy.context.object,name,m,bevel=.012,smooth=True)

def ring(name,p,radius,m,axis='Z'):
 bpy.ops.mesh.primitive_torus_add(major_radius=radius,minor_radius=.013,major_segments=32,minor_segments=6,location=p)
 o=bpy.context.object
 if axis=='Y':o.rotation_euler.x=math.pi/2
 return finish(o,name,m,smooth=True)

def label(text,p,width):
 box(text+' sign panel',p,(width,.035,.19),iron,bevel=.008)
 bpy.ops.object.text_add(location=(p[0]-width*.44,p[1]-.025,p[2]-.052),rotation=(math.pi/2,0,0))
 o=bpy.context.object;o.data.body=text;o.data.size=.125;o.data.extrude=.001
 bpy.ops.object.convert(target='MESH');finish(bpy.context.object,text+' lettering',yellow)

def foundation(w,d):
 box('Concrete footing',(0,0,.13),(w+.22,d+.22,.16),concrete)

def pitched_roof(w,d,z,rise,m):
 angle=math.atan2(rise,d/2)
 for side in (-1,1):
  o=box('Pitched sheet',(0,side*d/4,z+rise/2),(w,math.sqrt((d/2)**2+rise**2),.04),m,bevel=.008);o.rotation_euler.x=-side*angle
  for i in range(int(w/.13)):
   x=-w/2+.07+i*.13
   rod('Roof corrugation',(x,side*.02,z+rise+.035),(x,side*d/2,z+.035),.012,m)
 rod('Ridge cap',(-w/2,0,z+rise+.04),(w/2,0,z+rise+.04),.045,iron)

def window(x,y,z,w=.55,h=.5):
 box('Window surround',(x,y,z),(w+.1,.06,h+.1),wood)
 box('Glazing',(x,y-.04,z),(w,.018,h),glass)
 rod('Window mullion',(x,y-.06,z-h/2),(x,y-.06,z+h/2),.014,iron)

def crate(p):
 x,y,z=p;box('Slatted supply crate',(x,y,z),(.42,.38,.40),wood)
 for h in (-.14,0,.14):box('Crate straps',(x,y-.20,z+h),(.44,.025,.025),iron,bevel=.003)

# Workshop: industrial garage, rolled shutter, corrugated roof and external work area.
foundation(3,2.8)
box('Workshop masonry shell',(0,0,1.15),(3,2.65,2.0),brick)
for x in (-1.48,1.48):box('Corner frame',(x,0,1.15),(.1,2.7,2.02),iron)
pitched_roof(3.4,3.0,2.15,.52,iron)
box('Roller door recess',(-.27,-1.36,1.02),(1.73,.06,1.65),dark)
for i in range(15):box('Shutter slat',(-.27,-1.405,.25+i*.105),(1.66,.045,.09),iron,bevel=.008)
for x in (-1.17,.63):box('Garage rail',(x,-1.43,1.03),(.07,.10,1.77),rust)
window(1.10,-1.37,1.27,.45,.62)
label('WORKSHOP',(-.18,-1.40,2.02),1.34)
box('Workbench top',(1.18,-1.66,.78),(.67,.5,.075),wood)
for x in (.92,1.43):
 for y in (-1.84,-1.49):rod('Bench legs',(x,y,.055),(x,y,.75),.022,iron)
box('Bench vise',(1.24,-1.71,.89),(.18,.14,.15),iron)
rod('Vise handle',(1.24,-1.85,.84),(1.24,-1.85,1.02),.012,iron)
for y in (-.65,.03,.70):
 rod('Wall pipework',(1.55,y,.35),(1.55,y,1.8),.025,rust)
crate((-1.32,-1.69,.36))
for z in (.28,.48):ring('Discarded tire',(-1.38,1.52,z),.20,dark)
cylinder('Vent stack',(.85,.55,2.65),.11,.6,iron);box('Vent hood',(.85,.55,2.99),(.34,.33,.05),rust)
workshop=join('Workshop',parts[:]);parts.clear()

# Barracks: practical timber quarters under a rounded canvas roof.
foundation(3,2.6)
box('Quarters timber shell',(0,0,.83),(2.85,2.45,1.35),wood)
for x in (-1.42,1.42):
 for y in (-1.21,1.21):rod('Quarters posts',(x,y,.15),(x,y,1.61),.048,iron)
# Closed curved roof with thickness and support ribs, not a triangular low-poly wedge.
verts=[];faces=[]
for x in (-1.65,1.65):
 for i in range(17):
  t=i/16*math.pi;verts.append((x,1.44*math.cos(t),1.45+.85*math.sin(t)))
for i in range(16):faces.append((i,i+1,18+i,17+i))
mesh=bpy.data.meshes.new('Canvas vault');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Canvas roof',mesh);bpy.context.collection.objects.link(o)
solid=o.modifiers.new('Canvas thickness','SOLIDIFY');solid.thickness=.028;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=solid.name);finish(o,'Canvas roof',canvas,smooth=True)
for x in (-1.5,-.75,0,.75,1.5):
 for i in range(12):
  a=i/12*math.pi;b=(i+1)/12*math.pi
  rod('Canvas tension rib',(x,1.45*math.cos(a),1.45+.86*math.sin(a)),(x,1.45*math.cos(b),1.45+.86*math.sin(b)),.012,iron)
box('Quarters door',(0,-1.26,.82),(.72,.045,1.27),iron)
rod('Door handle',(.23,-1.30,.77),(.23,-1.30,.92),.017,wood)
for x in (-.96,.96):window(x,-1.26,1.01,.49,.42)
label('QUARTERS',(0,-1.29,1.58),1.25)
box('Entry step',(0,-1.46,.18),(1.02,.38,.12),concrete)
for x in (-1.5,1.5):
 rod('Canvas guy line',(x,-1.35,1.5),(x,-1.68,.055),.008,iron)
box('Bedroll storage rack',(-.68,1.48,.23),(1.05,.32,.34),wood)
for i in range(3):rod('Rolled bedroll',(-1.0+i*.32,1.48,.40),(-1.0+i*.32,1.48,.80),.12,canvas)
crate((1.2,-1.52,.35))
barracks=join('Barracks',parts[:]);parts.clear()

# Water Station: round enamel tanks, welded tower, ladder, valves and pump.
foundation(3,2.6)
for x in (-.72,.72):
 for dx in (-.31,.31):
  for y in (-.33,.33):rod('Tank support legs',(x+dx,y,.17),(x+dx,y,1.27),.034,iron)
 for y in (-.34,.34):
  rod('Tower cross brace',(x-.32,y,.20),(x+.32,y,1.18),.017,rust)
  rod('Tower cross brace',(x+.32,y,.20),(x-.32,y,1.18),.017,rust)
 cylinder('Enamel tank',(x,0,1.77),.45,1.04,blue)
 for z in (1.28,1.52,2.04,2.28):ring('Rolled tank seam',(x,0,z),.45,iron)
 ball('Tank crown',(x,0,2.29),(.45,.45,.095),blue)
 cylinder('Inspection cap',(x,0,2.40),.12,.07,iron)
 rod('Outlet',(x,-.10,1.27),(x,-.10,.68),.035,blue)
 rod('Outlet plumbing',(x,-.10,.68),(x,-.93,.68),.035,blue)
 rod('Pump inlet connection',(x,-.93,.68),(math.copysign(.20,x),-.93,.68),.035,blue)
 rod('Valve spindle',(x,-.90,.68),(x,-.90,.83),.012,iron)
 ring('Valve wheel',(x,-.90,.83),.11,rust)
 for i in range(4):
  a=i*math.pi/2;rod('Valve spokes',(x,-.90,.83),(x+.10*math.cos(a),-.90+.10*math.sin(a),.83),.008,iron)
box('Pump housing',(0,-1.0,.43),(.55,.43,.53),iron)
cylinder('Pump motor',(.25,-1.0,.43),.15,.35,rust)
for z in (.25,.34,.43,.52):box('Pump ventilation',(-.286,-1,z),(.013,.30,.018),dark,bevel=.002)
for x in (-.20,.20):rod('Access ladder',(x,.59,.18),(x,.59,2.43),.019,iron)
for i in range(9):rod('Ladder rung',(-.20,.59,.29+i*.24),(.20,.59,.29+i*.24),.014,iron)
rod('Tank bridge',(-.7,0,.83),(.7,0,.83),.04,blue)
label('WATER',(0,-1.25,.57),.73)
water=join('WaterStation',parts[:]);parts.clear()

# Food Garden: timber raised beds, shaped leaves, irrigation and tool storage.
box('Garden earth base',(0,0,.11),(3.02,2.60,.13),soil)
for row in range(3):
 y=-.82+row*.82
 box('Raised compost bed',(0,y,.23),(2.62,.53,.20),soil)
 for side in (-1,1):box('Bed retaining plank',(0,y+side*.29,.26),(2.78,.045,.25),wood,bevel=.006)
 for x in (-1.37,1.37):box('Bed end board',(x,y,.26),(.045,.62,.25),wood,bevel=.006)
 rod('Drip irrigation line',(-1.23,y,.35),(1.23,y,.35),.009,dark)
 for plant in range(5):
  x=-1.0+plant*.50;z=.34
  rod('Vegetable stem',(x,y,z),(x,y,z+.22),.009,pale)
  for i in range(5):
   a=i*math.tau/5+row*.6;length=.18+random.uniform(0,.05)
   # Shaped leaf with a curved center and a thin underside.
   coords=[(0,0,0),(-.04,.07,.025),(-.04,length*.72,.05),(0,length,.035),(.04,length*.72,.05),(.04,.07,.025),(0,length*.5,.06)]
   vv=[(x+vx*math.cos(a)-vy*math.sin(a),y+vx*math.sin(a)+vy*math.cos(a),z+.08+vz) for vx,vy,vz in coords]
   mm=bpy.data.meshes.new('Curved leaf');mm.from_pydata(vv,[],[(6,j,(j+1)%6) for j in range(6)]);mm.update();oo=bpy.data.objects.new('Vegetable leaf',mm);bpy.context.collection.objects.link(oo)
   solid=oo.modifiers.new('Leaf underside','SOLIDIFY');solid.thickness=.003;bpy.context.view_layer.objects.active=oo;bpy.ops.object.modifier_apply(modifier=solid.name);finish(oo,'Vegetable leaf',green,smooth=True)
   rod('Leaf midrib',(x,y,z+.12),(x-length*.75*math.sin(a),y+length*.75*math.cos(a),z+.13),.003,pale)
for x in (-1.48,1.48):
 for y in (-1.23,1.23):rod('Garden corner stake',(x,y,.12),(x,y,.72),.025,wood)
 for z in (.40,.65):rod('Garden wire',(x,-1.23,z),(x,1.23,z),.004,iron)
cylinder('Irrigation barrel',(1.07,1.09,.54),.17,.64,blue)
for z in (.27,.76):ring('Barrel band',(1.07,1.09,z),.17,iron)
rod('Watering hose',(1.07,1.09,.25),(1.07,.82,.25),.012,dark)
for i in range(3):
 x=-.8+i*.30
 rod('Garden tool handle',(x,1.20,.15),(x+.08,1.20,.88),.012,wood)
 box('Tool head',(x+.08,1.20,.88),(.10,.035,.16),iron,bevel=.014)
label('GARDEN',(0,-1.158,.29),.82)
garden=join('FoodGarden',parts[:]);parts.clear()

models=(workshop,barracks,water,garden)
for model in models:
 bake(model,model.name,1024)
 export(model.name+'.fbx',[model],False)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(root,'Tools/Art/SettlementBuildings.blend'))
# Separate actual asset renders, with identical lighting and camera across the set.
previewmat=mat('Preview ground',(.25,.225,.16))
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,.045));bpy.context.object.data.materials.append(previewmat)
for loc,energy,color in [((-3,-5,8),1600,(1,1,1)),((4,1,6),700,(.64,.75,1))]:
 bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object;light.data.energy=energy;light.data.color=color;light.data.size=5
bpy.ops.object.camera_add(location=(6,-8,6));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,1.05))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=5.3;scene.camera=camera
scene.render.resolution_x=800;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.cycles.samples=24
for model in models:
 for other in models:other.hide_render=other!=model
 scene.render.filepath=os.path.join(root,'Tools/Art/'+model.name+'_Preview.png');bpy.ops.render.render(write_still=True)
print('DONE FOUR BUILDINGS',flush=True)
