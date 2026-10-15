"""Original asset source. Run: blender -b --python Tools/Art/build_visual_slice.py -- /workspace/Ash
No external models, textures or add-ons. Outputs FBX, baked albedo/AO and tangent normals.
"""
import bpy, math, sys, os
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else os.getcwd()
out=os.path.join(root,'Assets/Ash/Resources/Art'); os.makedirs(out,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=16
scene.cycles.use_denoising=False
scene.cycles.bake_type='DIFFUSE'; scene.render.bake.margin=8
scene.world.color=(.18,.18,.18)
parts=[]
def mat(name,color,kind='noise'):
 m=bpy.data.materials.new(name); m.use_nodes=True
 n=m.node_tree.nodes; l=m.node_tree.links; bs=n.get('Principled BSDF'); bs.inputs['Roughness'].default_value=.8
 tex=n.new('ShaderNodeTexNoise'); tex.inputs['Scale'].default_value=36 if kind=='cloth' else 12; tex.inputs['Detail'].default_value=4
 ramp=n.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].position=.2; ramp.color_ramp.elements[0].color=tuple(c*.5 for c in color)+(1,)
 ramp.color_ramp.elements[1].position=.8; ramp.color_ramp.elements[1].color=tuple(min(1,c*1.25) for c in color)+(1,)
 l.new(tex.outputs['Fac'],ramp.inputs[0]); l.new(ramp.outputs[0],bs.inputs['Base Color'])
 bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.25; bump.inputs['Distance'].default_value=.025
 l.new(tex.outputs['Fac'],bump.inputs['Height']); l.new(bump.outputs[0],bs.inputs['Normal'])
 if kind=='brick':
  brick=n.new('ShaderNodeTexBrick'); brick.inputs['Scale'].default_value=7; brick.inputs['Mortar Size'].default_value=.035
  brick.inputs['Color1'].default_value=tuple(color)+(1,); brick.inputs['Color2'].default_value=tuple(c*.65 for c in color)+(1,)
  brick.inputs['Mortar'].default_value=(.13,.12,.105,1); brick.inputs['Brick Width'].default_value=.55; brick.inputs['Row Height'].default_value=.23
  l.new(brick.outputs['Color'],bs.inputs['Base Color']); l.new(brick.outputs['Fac'],bump.inputs['Height'])
 return m
brick=mat('Old red masonry',(.29,.17,.105),'brick'); plaster=mat('Chipped plaster',(.42,.39,.30)); rust=mat('Oxidized roof',(.25,.105,.05)); iron=mat('Painted steel',(.12,.155,.15)); wood=mat('Dry timber',(.27,.19,.105)); dark=mat('Dark rubber',(.035,.035,.03)); glass=mat('Dusty glass',(.08,.13,.15)); cloth=mat('Olive canvas',(.105,.13,.085),'cloth'); skin=mat('Weathered skin',(.40,.24,.15)); leather=mat('Brown leather',(.10,.065,.035)); hair=mat('Dark hair',(.045,.033,.022))
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
  rod('Corrugation rib',(x,side*.04,3.22),(x,side*1.66,2.44),.017,rust)
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
hq=join('Headquarters',parts[:]); parts.clear()
# Anatomically proportioned scavenger with smooth surfaces and practical gear.
ball('Pelvis',(0,0,.87),(.18,.13,.14),cloth,'Hips')
ball('Jacket torso',(0,.005,1.15),(.235,.135,.30),cloth,'Chest')
ball('Shoulder left',(-.23,0,1.34),(.105,.12,.105),cloth,'UpperArm.L');ball('Shoulder right',(.23,0,1.34),(.105,.12,.105),cloth,'UpperArm.R')
rod('Neck',(0,0,1.38),(0,0,1.52),.064,skin,'Chest')
ball('Human head',(0,-.015,1.64),(.108,.100,.145),skin,'Head')
ball('Jaw',(0,-.029,1.565),(.082,.081,.069),skin,'Head')
ball('Hair',(0,.005,1.725),(.113,.104,.073),hair,'Head')
ball('Nose',(0,-.112,1.63),(.023,.035,.036),skin,'Head')
for x in (-.103,.103):ball('Ear',(x,-.003,1.63),(.023,.023,.043),skin,'Head')
for x in (-.042,.042):
 ball('Eye socket',(x,-.099,1.66),(.025,.014,.014),hair,'Head')
 ball('Eye',(x,-.11,1.66),(.014,.007,.009),glass,'Head')
rod('Mouth',(-.029,-.102,1.583),(.029,-.102,1.583),.006,leather,'Head')
# Scarf collar / zipper / webbing.
ball('Scarf',(0,-.008,1.425),(.095,.09,.055),wood,'Chest')
rod('Jacket zipper',(0,-.133,.96),(0,-.133,1.38),.009,iron,'Chest')
for x in (-.13,.13):
 box('Chest pocket',(x,-.125,1.21),(.105,.035,.12),cloth,'Chest',.012)
 rod('Backpack strap',(x,-.12,1.35),(x,-.13,.99),.022,leather,'Chest')
box('Backpack',(0,.185,1.16),(.30,.18,.40),wood,'Chest',.065)
for x in (-.11,.11):box('Pack pocket',(x,.291,1.13),(.12,.09,.18),cloth,'Chest',.025)
rod('Bedroll',(-.20,.19,1.40),(.20,.19,1.40),.055,cloth,'Chest')
for suffix,x in (('L',-.115),('R',.115)):
 ball('Trouser thigh',(x,0,.675),(.098,.105,.235),iron,'Thigh.'+suffix)
 ball('Knee',(x,-.012,.455),(.088,.096,.082),leather,'Shin.'+suffix)
 ball('Trouser calf',(x,.005,.29),(.076,.08,.18),iron,'Shin.'+suffix)
 box('Ankle boot',(x,-.045,.095),(.17,.28,.17),dark,'Shin.'+suffix,.055)
 for i in range(3):rod('Boot lace',(x-.042,-.145,.10+i*.025),(x+.042,-.145,.10+i*.025),.006,wood,'Shin.'+suffix)
 sx=-1 if suffix=='L' else 1
 ball('Upper sleeve',(sx*.275,0,1.19),(.083,.092,.18),cloth,'UpperArm.'+suffix)
 ball('Elbow',(sx*.30,0,1.03),(.067,.075,.07),cloth,'Forearm.'+suffix)
 ball('Lower sleeve',(sx*.305,-.005,.92),(.063,.07,.135),cloth,'Forearm.'+suffix)
 ball('Gloved hand',(sx*.31,-.012,.77),(.055,.045,.08),leather,'Forearm.'+suffix)
 ball('Thumb',(sx*.266,-.032,.78),(.024,.024,.045),leather,'Forearm.'+suffix)
char=join('Scavenger',parts[:]);parts.clear()

def bake(obj,label,res):
 for other in (hq,char):other.hide_render=other!=obj;other.hide_set(other!=obj)
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
 for other in (hq,char):other.hide_render=False;other.hide_set(False)
 print('BAKED',label,len(obj.data.polygons),'faces',flush=True)
bake(hq,'Headquarters',1024);bake(char,'Scavenger',1024)
# A real bone rig; each shaped limb carries the relevant vertex group.
bpy.ops.object.armature_add();rig=bpy.context.object;rig.name='ScavengerRig';bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones.remove(rig.data.edit_bones[0])
def bone(name,head,tail,parent=None):
 b=rig.data.edit_bones.new(name);b.head=head;b.tail=tail
 if parent:b.parent=rig.data.edit_bones[parent]
 return b
bone('Hips',(0,0,.83),(0,0,1.0));bone('Chest',(0,0,1.0),(0,0,1.43),'Hips');bone('Head',(0,0,1.43),(0,0,1.8),'Chest')
for suffix,sx in (('L',-1),('R',1)):
 bone('Thigh.'+suffix,(sx*.115,0,.86),(sx*.115,0,.455),'Hips');bone('Shin.'+suffix,(sx*.115,0,.455),(sx*.115,0,.08),'Thigh.'+suffix)
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
export('Headquarters.fbx',[hq],False);export('Scavenger.fbx',[rig,char],True)
# Keep editable source outside Resources; no Blender dependency in Unity imports.
source=os.path.join(root,'Tools/Art/VisualSlice.blend');bpy.ops.wm.save_as_mainfile(filepath=source)
# Actual asset proof render, separate from Unity/device validation.
char.parent=rig;rig.location=(2.8,-.1,0);rig.scale=(1.4,1.4,1.4)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.04));plane=bpy.context.object;plane.data.materials.append(plaster)
bpy.ops.object.light_add(type='AREA',location=(-3,-5,8));bpy.context.object.data.energy=1100;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=5
bpy.ops.object.camera_add(location=(8,-11,7));camera=bpy.context.object;camera.rotation_euler=(Vector((.5,0,1.4))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=9
scene.camera=camera;scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.cycles.samples=32
scene.render.filepath=os.path.join(root,'Tools/Art/VisualSlice_Render.png');bpy.ops.render.render(write_still=True)
print('DONE',flush=True)
