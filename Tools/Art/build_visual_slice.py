"""Original asset source. Run: blender -b --python Tools/Art/build_visual_slice.py -- /workspace/Ash
No external models, textures or add-ons. Outputs FBX, baked albedo/AO and tangent normals.
"""
import bpy, math, sys, os, random
random.seed(214)
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else os.getcwd()
out=os.path.join(root,'Assets/Ash/Resources/Art'); os.makedirs(out,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=16
scene.cycles.use_denoising=False
scene.cycles.bake_type='DIFFUSE'; scene.render.bake.margin=8
scene.world.color=(.26,.29,.33)
parts=[]
def mat(name,color,kind='noise'):
 m=bpy.data.materials.new(name); m.use_nodes=True
 n=m.node_tree.nodes; l=m.node_tree.links; bs=n.get('Principled BSDF'); bs.inputs['Roughness'].default_value=.82
 geo=n.new('ShaderNodeNewGeometry')
 tex=n.new('ShaderNodeTexNoise'); tex.inputs['Scale'].default_value=85 if kind=='cloth' else 9; tex.inputs['Detail'].default_value=5
 l.new(geo.outputs['Position'],tex.inputs['Vector'])
 ramp=n.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].position=.22; ramp.color_ramp.elements[0].color=tuple(c*.58 for c in color)+(1,)
 ramp.color_ramp.elements[1].position=.80; ramp.color_ramp.elements[1].color=tuple(min(1,c*1.22) for c in color)+(1,)
 l.new(tex.outputs['Fac'],ramp.inputs[0]); l.new(ramp.outputs[0],bs.inputs['Base Color'])
 bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.22; bump.inputs['Distance'].default_value=.005 if kind=='cloth' else .012
 l.new(tex.outputs['Fac'],bump.inputs['Height']); l.new(bump.outputs[0],bs.inputs['Normal'])
 if kind=='brick':
  # Project masonry in real meters on both vertical wall directions, not normalized XY stripes.
  xyz=n.new('ShaderNodeSeparateXYZ');l.new(geo.outputs['Position'],xyz.inputs[0])
  normal=n.new('ShaderNodeSeparateXYZ');l.new(geo.outputs['Normal'],normal.inputs[0])
  absx=n.new('ShaderNodeMath');absx.operation='ABSOLUTE';l.new(normal.outputs['X'],absx.inputs[0])
  vectors=[]
  for horizontal,depth in [('X','Y'),('Y','X')]:
   v=n.new('ShaderNodeCombineXYZ');l.new(xyz.outputs[horizontal],v.inputs['X']);l.new(xyz.outputs['Z'],v.inputs['Y']);l.new(xyz.outputs[depth],v.inputs['Z']);vectors.append(v)
  mix=n.new('ShaderNodeMixRGB');l.new(absx.outputs[0],mix.inputs[0]);l.new(vectors[0].outputs[0],mix.inputs[1]);l.new(vectors[1].outputs[0],mix.inputs[2])
  brick=n.new('ShaderNodeTexBrick');l.new(mix.outputs[0],brick.inputs['Vector']);brick.inputs['Scale'].default_value=1
  brick.inputs['Mortar Size'].default_value=.009;brick.inputs['Brick Width'].default_value=.30;brick.inputs['Row Height'].default_value=.12
  brick.inputs['Color1'].default_value=tuple(color)+(1,);brick.inputs['Color2'].default_value=tuple(c*.55 for c in color)+(1,);brick.inputs['Mortar'].default_value=(.14,.13,.105,1)
  wear=n.new('ShaderNodeMixRGB');wear.blend_type='MULTIPLY';wear.inputs[0].default_value=.48;l.new(brick.outputs['Color'],wear.inputs[1]);l.new(ramp.outputs[0],wear.inputs[2]);l.new(wear.outputs[0],bs.inputs['Base Color'])
  l.new(brick.outputs['Fac'],bump.inputs['Height'])
 return m

brick=mat('Old red masonry',(.29,.17,.105),'brick'); plaster=mat('Chipped plaster',(.42,.39,.30)); rust=mat('Oxidized roof',(.25,.105,.05)); iron=mat('Painted steel',(.12,.155,.15)); wood=mat('Dry timber',(.27,.19,.105)); dark=mat('Dark rubber',(.035,.035,.03)); glass=mat('Dusty glass',(.08,.13,.15)); cloth=mat('Olive canvas',(.105,.13,.085),'cloth'); skin=mat('Weathered skin',(.40,.24,.15)); leather=mat('Brown leather',(.10,.065,.035)); hair=mat('Dark hair',(.045,.033,.022))
sand=mat('Compacted earth',(.25,.225,.16)); stone=mat('Rough rubble',(.29,.28,.24)); paint=mat('Faded yellow paint',(.52,.36,.08)); sack=mat('Weathered sandbag canvas',(.32,.30,.20),'cloth')
def finish(o,name,material,bone=None,bevel=0,smooth=False):
 o.name=name; o.data.materials.append(material)
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Soft manufactured edges','BEVEL'); mod.width=bevel; mod.segments=2
  bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=mod.name)
 if smooth:
  for p in o.data.polygons:p.use_smooth=True
 if bone:
  group=o.vertex_groups.new(name=bone); group.add(list(range(len(o.data.vertices))),1,'REPLACE')
 parts.append(o); return o

def box(name,p,size,m,bone=None,bevel=.025):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p); o=bpy.context.object; o.scale=size; return finish(o,name,m,bone,bevel)
def ball(name,p,size,m,bone=None):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=1,location=p); o=bpy.context.object; o.scale=size; return finish(o,name,m,bone,smooth=True)
def rod(name,a,b,r,m,bone=None):
 d=Vector(b)-Vector(a); bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2)
 o=bpy.context.object; o.rotation_euler=d.to_track_quat('Z','Y').to_euler(); return finish(o,name,m,bone,smooth=True)
def join(name,items):
 bpy.ops.object.select_all(action='DESELECT')
 for o in items:o.select_set(True)
 bpy.context.view_layer.objects.active=items[0]; bpy.ops.object.join(); obj=bpy.context.object; obj.name=name
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.uv.smart_project(angle_limit=math.radians(65),island_margin=.015); bpy.ops.object.mode_set(mode='OBJECT')
 tri=obj.modifiers.new('Export triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
 return obj
# Headquarters, matching existing 3.4 x 2.8 meter footprint.
box('Concrete footing',(0,0,.14),(3.7,3,.28),plaster)
box('Brick command building',(0,0,1.25),(3.4,2.8,2.25),brick,bevel=.045)
# Inset plaster bands, structural piers and cornice.
for x in (-1.68,1.68):
 for y in (-1.38,1.38):box('Concrete corner pier',(x,y,1.25),(.16,.16,2.3),plaster)
box('Concrete top course',(0,0,2.38),(3.55,2.94,.16),plaster)
# Corrugated roof sheet ridges follow two real pitched slopes.
angle=math.atan2(.78,1.65)
for side in (-1,1):
 o=box('Rusted roof sheet',(0,side*.825,2.83),(3.85,1.84,.055),rust,bevel=.01); o.rotation_euler.x=-side*angle
 for i in range(38):
  x=-1.83+i*.099
  rod('Corrugation rib',(x,side*.04,3.26),(x,side*1.66,2.48),.017,rust)
rod('Roof ridge cap',(-1.98,0,3.23),(1.98,0,3.23),.07,iron)
# Visible front, which faces Blender -Y.
box('Steel door surround',(0,-1.435,.99),(1.04,.1,1.84),iron)
box('Recessed reinforced door',(0,-1.50,.98),(.86,.055,1.68),wood)
for x in (-.29,.29):box('Door steel straps',(x,-1.54,.99),(.055,.02,1.56),iron,bevel=.005)
rod('Door pull',(.24,-1.57,.88),(.24,-1.57,1.13),.022,iron)
for x in (-1.12,1.12):
 box('Window reveal',(x,-1.43,1.52),(.78,.08,.87),wood)
 box('Dusty window',(x,-1.48,1.52),(.64,.025,.72),glass)
 for dx in (-.35,0,.35):box('Window mullion',(x+dx,-1.51,1.52),(.038,.025,.82),iron,bevel=.004)
 box('Window sill',(x,-1.55,1.08),(.89,.22,.08),plaster)
 box('Window lintel',(x,-1.49,1.99),(.9,.12,.10),plaster)
for y in (-.65,.65):
 box('Side window frame',(1.73,y,1.52),(.06,.72,.77),iron)
 box('Side dusty glass',(1.77,y,1.52),(.02,.61,.64),glass)
# Porch steps and braces.
for i in range(3):box('Porch step',(0,-1.7-i*.19,.28-i*.07),(1.44,.32,.12),plaster)
for x in (-.78,.78):
 rod('Porch post',(x,-1.97,.15),(x,-1.97,2.18),.055,wood)
 rod('Diagonal porch brace',(x,-1.97,1.6),(x*.6,-1.48,2.15),.035,wood)
o=box('Porch metal awning',(0,-1.79,2.22),(1.95,.91,.065),rust);o.rotation_euler.x=.12
# Drainpipe, vent chimney, radio aerial and supply details.
rod('Drain pipe',(1.81,1.20,.30),(1.81,1.20,2.45),.045,iron)
for z in (.5,1.3,2.1):rod('Pipe bracket',(1.65,1.2,z),(1.83,1.2,z),.018,rust)
rod('Chimney stack',(-.95,.68,2.78),(-.95,.68,3.62),.105,iron)
box('Chimney rain cap',(-.95,.68,3.65),(.38,.33,.055),rust)
rod('Radio mast',(.65,.2,3.1),(.65,.2,4.18),.025,iron)
rod('Aerial crossbar',(.21,.2,3.93),(1.09,.2,3.93),.018,iron)
rod('Aerial guy',(.65,.2,4.03),(1.4,1.12,2.82),.009,iron)
for x in (-1.4,-.9):box('Supply crate',(x,1.65,.28),(.40,.46,.45),wood)
# Command-post details: boarded glazing, security mesh, vents, patched roofing and signage.
for x in (-1.12,1.12):
 for z in (1.28,1.52,1.76):rod('Window security bar',(x-.32,-1.53,z),(x+.32,-1.53,z),.012,iron)
box('Air conditioning casing',(1.84,.63,.87),(.27,.70,.40),plaster)
for z in (.75,.80,.85,.90,.95):box('Cooling grille',(1.99,.63,z),(.015,.57,.02),iron,bevel=.002)
rod('AC conduit',(1.77,.37,.81),(1.77,.37,1.43),.017,iron)
box('Metal sign board',(0,-1.495,2.08),(1.1,.045,.19),iron,bevel=.008)
bpy.ops.object.text_add(location=(-.44,-1.525,2.02),rotation=(math.pi/2,0,0));letter=bpy.context.object
letter.data.body='EMBER HQ';letter.data.size=.145;letter.data.extrude=.001;letter.data.align_x='LEFT'
bpy.ops.object.convert(target='MESH');finish(bpy.context.object,'Embossed HQ lettering',paint)
for x in (-.56,.56):
 box('Door lighting hood',(x,-1.52,2.13),(.17,.20,.10),iron)
for x in (-1.7,1.7):
 rod('Gutter',(x,-1.62,2.43),(x,1.61,2.43),.036,iron)
for i in range(5):
 x=-1.4+i*.65
 box('Roof exposed fastener',(x,-1.4,2.63),(.035,.035,.035),iron,bevel=.008)
# Layered rust patch at the back slope.
patch=box('Repaired roof panel',(.8,.91,2.82),(.80,.67,.024),iron,bevel=.005);patch.rotation_euler.x=-angle
hq=join('Headquarters',parts[:]); parts.clear()
# Tailored body surfaces and smooth joint weights, rather than separate limb spheres.
def loft(name,rings,m,weights):
 verts=[];faces=[];sides=20
 for j,(cx,cy,z,rx,ry) in enumerate(rings):
  for i in range(sides):
   a=i*math.tau/sides; fold=1+.018*math.sin(a*6+j*1.4)
   verts.append((cx+rx*math.cos(a)*fold,cy+ry*math.sin(a)*fold,z))
 for j in range(len(rings)-1):
  for i in range(sides):
   a=j*sides+i;b=j*sides+(i+1)%sides;faces.append((a,b,b+sides,a+sides))
 faces.append(tuple(reversed(range(sides))));faces.append(tuple((len(rings)-1)*sides+i for i in range(sides)))
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
 finish(o,name,m,smooth=True)
 for j,assignments in enumerate(weights):
  for b,w in assignments.items():
   group=o.vertex_groups.get(b) or o.vertex_groups.new(name=b);group.add(list(range(j*sides,(j+1)*sides)),w,'REPLACE')
 return o
loft('Tailored canvas jacket',[(0,0,.87,.17,.11),(0,0,.94,.18,.115),(0,0,1.04,.175,.115),(0,0,1.18,.21,.125),(0,.005,1.32,.245,.125),(0,0,1.39,.17,.105),(0,0,1.435,.068,.065)],cloth,[{'Hips':1},{'Hips':.6,'Chest':.4},{'Chest':1},{'Chest':1},{'Chest':1},{'Chest':1},{'Chest':1}])
rod('Neck',(0,0,1.40),(0,0,1.52),.061,skin,'Head')
ball('Human head',(0,-.01,1.64),(.095,.089,.132),skin,'Head')
ball('Defined jaw',(0,-.022,1.558),(.073,.066,.049),skin,'Head')
ball('Cropped hair',(0,.008,1.73),(.098,.088,.048),hair,'Head')
ball('Nose bridge',(0,-.094,1.636),(.017,.020,.033),skin,'Head')
ball('Nose tip',(0,-.111,1.617),(.021,.017,.013),skin,'Head')
for x in (-.092,.092):ball('Ear',(x,0,1.63),(.016,.018,.032),skin,'Head')
for x in (-.038,.038):
 ball('Recessed eye',(x,-.092,1.66),(.016,.007,.006),dark,'Head')
 rod('Brow',(x-.018,-.092,1.676),(x+.017,-.092,1.677),.004,hair,'Head')
rod('Mouth',(-.024,-.091,1.575),(.024,-.091,1.575),.0035,leather,'Head')
ball('Beard chin',(0,-.061,1.548),(.058,.028,.018),hair,'Head')
ball('Scarf collar',(0,-.002,1.433),(.080,.071,.035),wood,'Chest')
rod('Jacket zipper',(0,-.124,.94),(0,-.124,1.39),.006,iron,'Chest')
for x in (-.12,.12):
 box('Chest pocket',(x,-.122,1.22),(.095,.025,.11),cloth,'Chest',.009)
 box('Pocket flap',(x,-.139,1.264),(.098,.012,.025),wood,'Chest',.003)
 rod('Pack shoulder webbing',(x,-.118,1.35),(x,-.12,.99),.015,leather,'Chest')
box('Backpack',(0,.169,1.16),(.29,.16,.38),wood,'Chest',.045)
for x in (-.10,.10):box('Pack pouch',(x,.26,1.12),(.11,.07,.16),cloth,'Chest',.020)
rod('Bedroll',(-.17,.17,1.39),(.17,.17,1.39),.048,cloth,'Chest')
ball('Trouser seat',(0,.008,.84),(.185,.115,.105),iron,'Hips')
for suffix,x in (('L',-.098),('R',.098)):
 thigh='Thigh.'+suffix;shin='Shin.'+suffix;arm='UpperArm.'+suffix;fore='Forearm.'+suffix
 loft('Continuous trouser leg',[(x,0,.88,.094,.105),(x,0,.76,.088,.10),(x,.005,.62,.078,.09),(x,-.007,.49,.070,.078),(x,-.01,.455,.068,.074),(x,.008,.37,.068,.074),(x,.012,.24,.057,.060),(x,0,.145,.05,.052)],iron,[{'Hips':1},{'Hips':.35,thigh:.65},{thigh:1},{thigh:.75,shin:.25},{thigh:.5,shin:.5},{shin:1},{shin:1},{shin:1}])
 box('Leather ankle boot',(x,-.040,.085),(.145,.245,.15),leather,shin,.035)
 box('Boot sole',(x,-.05,.022),(.15,.25,.032),dark,shin,.01)
 for i in range(3):rod('Boot lace',(x-.034,-.125,.10+i*.018),(x+.034,-.125,.10+i*.018),.004,wood,shin)
 sx=-1 if suffix=='L' else 1
 loft('Continuous jacket sleeve',[(sx*.208,0,1.37,.068,.085),(sx*.25,0,1.29,.075,.082),(sx*.278,0,1.17,.068,.071),(sx*.293,0,1.06,.062,.066),(sx*.30,0,1.03,.060,.064),(sx*.302,-.005,.93,.054,.058),(sx*.305,-.006,.83,.047,.049)],cloth,[{'Chest':1},{'Chest':.6,arm:.4},{arm:1},{arm:.7,fore:.3},{arm:.4,fore:.6},{fore:1},{fore:1}])
 ball('Work glove',(sx*.305,-.014,.77),(.043,.037,.065),leather,fore)
 ball('Glove thumb',(sx*.271,-.029,.78),(.016,.018,.032),leather,fore)
rod('Utility belt',(-.16,-.11,.90),(.16,-.11,.90),.022,leather,'Hips')
box('Belt buckle',(0,-.137,.9),(.04,.012,.035),iron,'Hips',.003)
char=join('Scavenger',parts[:]);parts.clear()
# One surrounding terrain/prop mesh and atlas, authored in HQ local coordinates.
# Ground sits above the existing slab and does not change collision or resource systems.
box('Broken concrete apron',(0,-.35,.115),(4.18,4.7,.08),plaster,bevel=.035)
box('HQ approach path',(0,-2.28,.137),(1.50,.85,.045),stone,bevel=.014)
for i in range(25):
 x=random.uniform(-2.0,2.0);y=random.uniform(-2.5,1.8)
 if abs(x)<1.84 and y> -1.6:continue
 ball('Loose stone',(x,y,.18),(random.uniform(.03,.10),random.uniform(.03,.09),random.uniform(.025,.06)),stone)
# Concrete cracks drawn as fine irregular dark seams, no alpha sorting.
for a,b in [((-1.9,-2.2),(-1.3,-1.7)),((.9,-2.4),(1.4,-1.9)),((1.8,-.8),(2.0,-.2)),((-1.95,.8),(-1.8,1.3))]:
 rod('Concrete fracture',(a[0],a[1],.158),(b[0],b[1],.158),.006,dark)
for row in range(2):
 for i in range(5-row):
  x=-1.7+i*.25+row*.12
  o=box('Stacked sandbag',(x,-2.24,.23+row*.15),(.28,.19,.15),sack,bevel=.065)
  o.rotation_euler.z=random.uniform(-.08,.08)
# Side supplies: detailed drums, pallet, slatted crates and cables.
for y in (.4,1.05):
 rod('Salvaged fuel drum',(-1.96,y,.17),(-1.96,y,.76),.17,iron)
 for z in (.23,.43,.69):
  bpy.ops.mesh.primitive_torus_add(major_radius=.17,minor_radius=.012,major_segments=20,minor_segments=6,location=(-1.96,y,z));finish(bpy.context.object,'Drum rolled seam',rust,smooth=True)
 rod('Drum bung',(-1.90,y,.755),(-1.90,y,.78),.018,dark)
for j in range(4):box('Pallet plank',(1.70,-1.00+j*.145,.21),(.60,.115,.055),wood,bevel=.006)
for x in (1.48,1.92):box('Pallet runner',(x,-.78,.17),(.08,.70,.08),wood,bevel=.006)
box('Supply crate',(1.69,-.79,.48),(.46,.49,.47),wood)
for z in (.29,.45,.62):box('Crate slat',(1.69,-1.047,z),(.48,.025,.045),iron,bevel=.003)
for i in range(10):
 a=i*.63;x=1.9+.10*math.cos(a);y=-1.73+.10*math.sin(a)
 rod('Dry grass',(x,y,.16),(x+.02*math.cos(a),y+.02*math.sin(a),.31+random.uniform(0,.12)),.006,cloth)
ground=join('HeadquartersSurroundings',parts[:]);parts.clear()
models=(hq,char,ground)

def bake(obj,label,res):
 for other in models:other.hide_render=other!=obj;other.hide_set(other!=obj)
 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
 images={}
 for kind,bake_type in (('Albedo','DIFFUSE'),('Normal','NORMAL'),('AO','AO')):
  image=bpy.data.images.new(label+kind,width=res,height=res,alpha=False)
  if kind!='Albedo':image.colorspace_settings.name='Non-Color'
  for material in obj.data.materials:
   node=material.node_tree.nodes.new('ShaderNodeTexImage');node.image=image; material.node_tree.nodes.active=node
  if bake_type=='DIFFUSE':bpy.ops.object.bake(type=bake_type,pass_filter={'COLOR'})
  else:bpy.ops.object.bake(type=bake_type)
  images[kind]=image
 # Bake AO directly into albedo so contact creases survive mobile quality changes.
 pixels=list(images['Albedo'].pixels[:]);ao=list(images['AO'].pixels[:])
 for i in range(0,len(pixels),4):
  factor=.40+.60*ao[i]
  for c in range(3):pixels[i+c]*=factor
 images['Albedo'].pixels[:]=pixels
 for kind in ('Albedo','Normal'):
  image=images[kind];image.filepath_raw=os.path.join(out,label+kind+'.png');image.file_format='PNG';image.save()
 m=bpy.data.materials.new(label+'Atlas');m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links;bs=n.get('Principled BSDF');bs.inputs['Roughness'].default_value=.8
 al=n.new('ShaderNodeTexImage');al.image=images['Albedo'];l.new(al.outputs['Color'],bs.inputs['Base Color'])
 no=n.new('ShaderNodeTexImage');no.image=images['Normal'];normal=n.new('ShaderNodeNormalMap');l.new(no.outputs['Color'],normal.inputs['Color']);l.new(normal.outputs[0],bs.inputs['Normal'])
 obj.data.materials.clear();obj.data.materials.append(m)
 for p in obj.data.polygons:p.material_index=0
 for other in models:other.hide_render=False;other.hide_set(False)
 print('BAKED',label,len(obj.data.polygons),'faces',flush=True)
bake(hq,'Headquarters',1024);bake(char,'Scavenger',1024);bake(ground,'HeadquartersSurroundings',1024)
# A real bone rig; each shaped limb carries the relevant vertex group.
bpy.ops.object.armature_add();rig=bpy.context.object;rig.name='ScavengerRig';bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones.remove(rig.data.edit_bones[0])
def bone(name,head,tail,parent=None):
 b=rig.data.edit_bones.new(name);b.head=head;b.tail=tail
 if parent:b.parent=rig.data.edit_bones[parent]
 return b
bone('Hips',(0,0,.83),(0,0,1.0));bone('Chest',(0,0,1.0),(0,0,1.43),'Hips');bone('Head',(0,0,1.43),(0,0,1.8),'Chest')
for suffix,sx in (('L',-1),('R',1)):
 bone('Thigh.'+suffix,(sx*.098,0,.86),(sx*.098,0,.455),'Hips');bone('Shin.'+suffix,(sx*.098,0,.455),(sx*.098,0,.08),'Thigh.'+suffix)
 bone('UpperArm.'+suffix,(sx*.23,0,1.34),(sx*.30,0,1.03),'Chest');bone('Forearm.'+suffix,(sx*.30,0,1.03),(sx*.31,0,.73),'UpperArm.'+suffix)
bpy.ops.object.mode_set(mode='OBJECT');char.parent=rig;mod=char.modifiers.new('Scavenger skin','ARMATURE');mod.object=rig
rig.animation_data_create();action=bpy.data.actions.new('Walk');rig.animation_data.action=action
scene.render.fps=24;scene.frame_start=1;scene.frame_end=33
for frame in range(1,34,4):
 t=(frame-1)/32*2*math.pi
 for suffix,sign in (('L',1),('R',-1)):
  for name,rotation in [('Thigh.'+suffix,.43*math.sin(t)*sign),('Shin.'+suffix,max(0,-math.sin(t)*sign)*.62),('UpperArm.'+suffix,-.32*math.sin(t)*sign),('Forearm.'+suffix,-.17-.10*math.cos(t))]:
   p=rig.pose.bones[name];p.rotation_mode='XYZ';p.rotation_euler=(rotation,0,0);p.keyframe_insert('rotation_euler',frame=frame)
 p=rig.pose.bones['Hips'];p.location=(0,0,.018*(1-math.cos(t*2)));p.keyframe_insert('location',frame=frame)
scene.frame_set(1)
# Export game-ready files with one atlas/material per model.
def export(path,objs,animated):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objs:o.select_set(True)
 bpy.context.view_layer.objects.active=objs[0]
 bpy.ops.export_scene.fbx(filepath=os.path.join(out,path),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,
  bake_anim=animated,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,
  axis_forward='-Z',axis_up='Y',use_tspace=True,path_mode='RELATIVE',apply_scale_options='FBX_SCALE_UNITS')
export('Headquarters.fbx',[hq],False);export('Scavenger.fbx',[rig,char],True);export('HeadquartersSurroundings.fbx',[ground],False)
# Keep editable source outside Resources; no Blender dependency in Unity imports.
source=os.path.join(root,'Tools/Art/VisualSlice.blend');bpy.ops.wm.save_as_mainfile(filepath=source)
# Actual asset proof render, separate from Unity/device validation.
char.parent=rig;rig.location=(1.15,-2.10,.15);scene.frame_set(9)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.04));plane=bpy.context.object;plane.data.materials.append(sand)
bpy.ops.object.light_add(type='AREA',location=(-3,-5,8));bpy.context.object.data.energy=1600;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=5
bpy.ops.object.light_add(type='AREA',location=(4,1,6));bpy.context.object.data.energy=700;bpy.context.object.data.color=(.64,.75,1);bpy.context.object.data.size=5
bpy.ops.object.camera_add(location=(8,-11,8));camera=bpy.context.object;camera.rotation_euler=(Vector((0,-.4,1.3))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=7.8
scene.camera=camera;scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.cycles.samples=64
scene.render.filepath=os.path.join(root,'Tools/Art/VisualSlice_Render.png');bpy.ops.render.render(write_still=True)
# Character close-up from the same game mesh and bone pose, at actual scale.
hq.hide_render=True;ground.hide_render=True;rig.location=(0,0,0);scene.frame_set(9)
camera.location=(2.2,-4,2.35);camera.rotation_euler=(Vector((0,0,.94))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=2.15
scene.render.resolution_x=900;scene.render.resolution_y=1200
scene.render.filepath=os.path.join(root,'Tools/Art/Survivor_Closeup.png');bpy.ops.render.render(write_still=True)
print('DONE',flush=True)
