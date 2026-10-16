"""Original character expansion using the approved skinned body and procedural art helpers.
Blender 4.3.2. Run with --python-exit-code 1, then -- /path/to/Ash.
Shares a body/skeleton; distinct outfits, hair, colors and equipment are authored below.
"""
import ast
import bpy
import math
import os
import sys
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else os.getcwd()
out=os.path.join(root,'Assets/Ash/Resources/Art');os.makedirs(out,exist_ok=True)
sourcepath=os.path.join(root,'Tools/Art/build_visual_slice.py')
source=open(sourcepath).read();tree=ast.parse(source,filename=sourcepath)
helpers={'mat','finish','box','ball','rod','join','loft','bake','bone'}
nodes=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in helpers]
assert {n.name for n in nodes}==helpers
exec(compile(ast.Module(body=nodes,type_ignores=[]),sourcepath,'exec'),globals())
body=source[source.index("loft('Tailored canvas jacket'"):source.index("char=join('Scavenger'")]
body='\n'.join(line for line in body.splitlines() if not line.startswith("ball('Beard chin'") and not line.startswith("ball('Cropped hair'"))
# Name, role, coat, skin, hair, feminine body proportions, hair style.
characters=[
 ('Engineer','engineer',(.28,.12,.065),(.43,.28,.18),(.055,.038,.022),False,'short'),
 ('Sentry','sentry',(.13,.16,.15),(.24,.14,.085),(.025,.022,.018),False,'short'),
 ('MaraFlint','salvage',(.12,.25,.27),(.40,.25,.16),(.11,.055,.025),True,'tied'),
 ('JonasReed','surveyor',(.28,.22,.11),(.43,.30,.21),(.055,.04,.025),False,'short'),
 ('SeraVale','pathfinder',(.25,.13,.075),(.34,.20,.12),(.035,.025,.02),True,'tied'),
 ('BramHolt','builder',(.18,.23,.13),(.32,.20,.13),(.10,.065,.035),False,'beard'),
 ('IlyraVoss','radio',(.18,.15,.29),(.46,.32,.23),(.04,.033,.025),True,'bob'),
 ('KadeRook','courier',(.10,.19,.26),(.25,.14,.08),(.028,.023,.02),False,'short'),
 ('AshaSol','founder',(.38,.27,.09),(.29,.17,.10),(.035,.025,.02),True,'braid'),
 ('OrinAshcroft','strategist',(.13,.17,.20),(.44,.31,.22),(.26,.25,.23),False,'beard'),
]
sourceout=os.path.join(root,'Tools/Art/Characters');os.makedirs(sourceout,exist_ok=True)

def animate(mode):
 action=bpy.data.actions.new(mode);rig.animation_data.action=action
 end=33 if mode=='Walk' else 49
 scene.frame_start=1;scene.frame_end=end
 for frame in range(1,end+1,4):
  t=(frame-1)/(end-1)*math.tau
  for suffix,sign in (('L',1),('R',-1)):
   values=[('Thigh.'+suffix,.43*math.sin(t)*sign),('Shin.'+suffix,max(0,-math.sin(t)*sign)*.62),('UpperArm.'+suffix,-.32*math.sin(t)*sign),('Forearm.'+suffix,-.17-.10*math.cos(t))] if mode=='Walk' else [('Thigh.'+suffix,0),('Shin.'+suffix,0),('UpperArm.'+suffix,.015*math.sin(t)*sign),('Forearm.'+suffix,-.10+.012*math.sin(t))]
   for name,angle in values:
    p=rig.pose.bones[name];p.rotation_mode='XYZ';p.rotation_euler=(angle,0,0);p.keyframe_insert('rotation_euler',frame=frame)
  p=rig.pose.bones['Hips'];p.location=(0,0,.018*(1-math.cos(t*2)) if mode=='Walk' else .003*(1-math.cos(t)));p.keyframe_insert('location',frame=frame)
 return action

def hat(brim=False):
 ball('Cap crown',(0,0,1.76),(.113,.10,.055),cloth,'Head')
 if brim:
  ball('Broad brim',(0,-.012,1.735),(.178,.15,.013),leather,'Head')
 else:box('Cap visor',(0,-.108,1.739),(.16,.11,.015),leather,'Head',.01)

def goggles(z=1.71):
 rod('Goggle strap',(-.089,-.045,z),(.089,-.045,z),.018,leather,'Head')
 for x in (-.043,.043):
  ball('Goggle rim',(x,-.091,z),(.034,.020,.025),iron,'Head')
  ball('Goggle lens',(x,-.109,z),(.026,.006,.018),glass,'Head')

def pouch(x,z=.96):
 box('Utility pouch',(x,-.13,z),(.11,.075,.14),wood,'Hips',.018)
 box('Pouch clasp',(x,-.172,z),(.018,.012,.025),iron,'Hips',.003)

def facing_markers():
 markers=[]
 for marker,position in [('FaceAimOrigin',(0,0,1)),('FaceAimForward',(0,-1,1))]:
  obj=bpy.data.objects.new(marker,None);bpy.context.collection.objects.link(obj);obj.parent=rig;obj.location=position;markers.append(obj)
 return markers

for index,(name,role,coat,tone,haircolor,feminine,style) in enumerate(characters):
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 for action in list(bpy.data.actions):bpy.data.actions.remove(action)
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=False;scene.render.bake.margin=8;scene.world.color=(.26,.29,.33)
 parts=[]
 cloth=mat(name+' jacket',coat,'cloth');skin=mat(name+' skin',tone);hair=mat(name+' hair',haircolor)
 iron=mat(name+' steel',(.11,.14,.145));wood=mat(name+' canvas',(.25,.19,.11),'cloth');leather=mat(name+' leather',(.105,.065,.035));dark=mat(name+' dark',(.035,.035,.03));glass=mat(name+' glass',(.08,.13,.15));gold=mat(name+' ochre',(.50,.34,.09));silver=mat(name+' pale metal',(.36,.37,.34))
 thisbody=body
 if feminine:
  thisbody=thisbody.replace('.245,.125','.215,.125').replace('.175,.115','.16,.115').replace('(.095,.089,.132)','(.089,.085,.128)').replace('(.073,.066,.049)','(.069,.064,.045)')
 exec(compile(thisbody,sourcepath+' [shared character body]','exec'),globals())
 ball('Hair crown',(0,.008,1.73),(.100,.090,.05),hair,'Head')
 if style in ('tied','braid','bob'):
  for side in (-1,1):ball('Side hair',(side*.077,.017,1.67),(.032,.063,.073),hair,'Head')
  if style=='tied':
   for i in range(4):ball('Tied ponytail',(0,.11+i*.009,1.67-i*.035),(.035,.04,.045),hair,'Head')
  elif style=='braid':
   for i in range(6):ball('Woven braid',(.075,.085,1.64-i*.027),(.024,.023,.027),hair,'Head')
 if style=='beard':
  ball('Full beard',(0,-.048,1.565),(.074,.054,.045),hair,'Head')
  for x in (-.055,.055):ball('Beard side',(x,-.033,1.60),(.026,.051,.05),hair,'Head')
 # Individual equipment is bound to the relevant bone, not left as unweighted props.
 if role=='engineer':
  ball('Safety helmet',(0,.003,1.765),(.119,.108,.069),gold,'Head')
  box('Helmet rim',(0,-.015,1.742),(.26,.23,.018),gold,'Head',.01)
  goggles(1.70)
  for x in (-.13,.13):pouch(x,.88)
  rod('Wrench handle',(.16,-.15,.89),(.16,-.15,1.10),.013,silver,'Hips')
  box('Wrench jaw',(.16,-.15,1.10),(.055,.025,.045),silver,'Hips',.008)
 elif role=='sentry':
  ball('Guard helmet',(0,.01,1.753),(.117,.103,.069),iron,'Head')
  box('Guard vest',(0,-.127,1.17),(.34,.055,.30),wood,'Chest',.025)
  for x in (-.10,.10):box('Vest panel',(x,-.16,1.17),(.15,.020,.24),iron,'Chest',.014)
  rod('Slung staff',(.27,.15,.75),(.27,.15,1.62),.016,wood,'Chest')
 elif role=='salvage':
  goggles(1.71)
  for x in (-.12,.12):rod('Safety vest tape',(x,-.145,1.03),(x,-.145,1.37),.013,gold,'Chest')
  for x in (-.14,.14):pouch(x,.88)
 elif role=='surveyor':
  hat(True)
  for x in (-.045,.045):rod('Survey binocular',(x,-.165,1.18),(x,-.165,1.30),.028,iron,'Chest')
  box('Survey map case',(.19,0,.91),(.075,.20,.23),wood,'Hips',.018)
 elif role=='pathfinder':
  goggles(1.70)
  ball('Dust scarf',(0,-.015,1.445),(.10,.081,.045),gold,'Chest')
  box('Route folio',(-.20,.015,.95),(.085,.18,.28),leather,'Hips',.015)
 elif role=='builder':
  box('Builder chest guard',(0,-.133,1.20),(.32,.040,.27),iron,'Chest',.025)
  rod('Builder harness',(-.14,-.16,1.30),(.14,-.16,1.00),.018,wood,'Chest')
  for x in (-.14,.14):pouch(x,.88)
 elif role=='radio':
  for x in (-.107,.107):ball('Headset earcup',(x,.0,1.65),(.024,.033,.039),iron,'Head')
  rod('Headset boom',(-.11,-.01,1.63),(-.035,-.13,1.59),.006,iron,'Head')
  box('Field radio',(-.17,-.145,1.11),(.095,.065,.17),iron,'Chest',.012)
  rod('Radio antenna',(-.17,-.13,1.18),(-.17,-.13,1.43),.005,silver,'Chest')
  box('Map folio',(.19,.005,.95),(.08,.18,.26),wood,'Hips',.014)
 elif role=='courier':
  goggles(1.70)
  ball('Courier scarf',(0,-.006,1.44),(.095,.08,.042),wood,'Chest')
  box('Medicine satchel',(.19,0,.93),(.09,.20,.25),wood,'Hips',.020)
  box('Satchel light stripe',(.24,-.02,.94),(.012,.12,.018),silver,'Hips',.003)
  for x in (-.13,.13):box('Riding shoulder patch',(x,-.05,1.36),(.14,.16,.035),leather,'Chest',.014)
 elif role=='founder':
  ball('Founders mantle',(0,.01,1.385),(.25,.145,.060),gold,'Chest')
  o=box('Ember diamond',(.09,-.159,1.25),(.045,.016,.045),gold,'Chest',.008);o.rotation_euler.y=math.pi/4
  box('Council document case',(-.19,.01,.94),(.07,.19,.24),wood,'Hips',.018)
 elif role=='strategist':
  hat(False)
  ball('High coat collar',(0,.018,1.44),(.092,.079,.043),cloth,'Chest')
  box('Treaty folio',(.19,.01,.94),(.08,.21,.26),wood,'Hips',.015)
  rod('Chest pen',(-.13,-.145,1.18),(-.13,-.145,1.31),.006,silver,'Chest')
 char=join(name,parts[:]);parts.clear();models=(char,)
 bake(char,name,1024)
 bpy.ops.object.armature_add();rig=bpy.context.object;rig.name=name+'Rig';bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones.remove(rig.data.edit_bones[0])
 bone('Hips',(0,0,.83),(0,0,1.0));bone('Chest',(0,0,1.0),(0,0,1.43),'Hips');bone('Head',(0,0,1.43),(0,0,1.8),'Chest')
 for suffix,sx in (('L',-1),('R',1)):
  bone('Thigh.'+suffix,(sx*.098,0,.86),(sx*.098,0,.455),'Hips');bone('Shin.'+suffix,(sx*.098,0,.455),(sx*.098,0,.08),'Thigh.'+suffix)
  bone('UpperArm.'+suffix,(sx*.23,0,1.34),(sx*.30,0,1.03),'Chest');bone('Forearm.'+suffix,(sx*.30,0,1.03),(sx*.31,0,.73),'UpperArm.'+suffix)
 bpy.ops.object.mode_set(mode='OBJECT');char.parent=rig;mod=char.modifiers.new('Character skin','ARMATURE');mod.object=rig
 rig.animation_data_create();scene.render.fps=24
 walk=animate('Walk');walk.use_fake_user=True;idle=animate('Idle');idle.use_fake_user=True;rig.animation_data.action=idle;scene.frame_start=1;scene.frame_end=49;scene.frame_set(1)
 markers=facing_markers()
 bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);char.select_set(True);bpy.context.view_layer.objects.active=rig
 for marker in markers:marker.select_set(True)
 bpy.ops.export_scene.fbx(filepath=os.path.join(out,name+'.fbx'),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',use_tspace=True,path_mode='RELATIVE',apply_scale_options='FBX_SCALE_UNITS')
 bpy.ops.wm.save_as_mainfile(filepath=os.path.join(sourceout,name+'.blend'))
 ground=mat('Preview ground',(.25,.225,.16));bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.025));bpy.context.object.data.materials.append(ground)
 for loc,energy,color in [((-3,-5,8),1600,(1,1,1)),((4,1,6),700,(.64,.75,1))]:
  bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object;light.data.energy=energy;light.data.color=color;light.data.size=5
 bpy.ops.object.camera_add(location=(2.1,-4,2.2));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.96))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=2.13;scene.camera=camera
 scene.render.resolution_x=600;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.cycles.samples=16
 scene.render.filepath=os.path.join(sourceout,name+'_Preview.png');bpy.ops.render.render(write_still=True)
 print('CHARACTER DONE',name,flush=True)
print('DONE ALL CHARACTERS',flush=True)
