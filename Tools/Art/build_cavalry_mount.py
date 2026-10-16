"""Static mounted combat representative, using the original courier mesh and original horse geometry.
Blender 4.3.2: --python-exit-code 1 --python Tools/Art/build_cavalry_mount.py -- /path/to/Ash
"""
import bpy,ast,math,os,sys
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else os.getcwd();out=root+'/Assets/Ash/Resources/Art'
source=root+'/Tools/Art/build_visual_slice.py';tree=ast.parse(open(source).read())
nodes=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in {'mat','finish','box','ball','rod','join','bake'}]
exec(compile(ast.Module(body=nodes,type_ignores=[]),source,'exec'),globals())
bpy.ops.wm.open_mainfile(filepath=root+'/Tools/Art/Characters/KadeRook.blend')
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=False;scene.render.bake.margin=8
rig=bpy.data.objects['KadeRookRig'];rider=bpy.data.objects['KadeRook'];rig.animation_data_clear();scene.frame_set(1)
for side,sign in [('L',-1),('R',1)]:
 p=rig.pose.bones['Thigh.'+side];p.rotation_mode='XYZ';p.rotation_euler=(.75,0,sign*.65)
 p=rig.pose.bones['Shin.'+side];p.rotation_mode='XYZ';p.rotation_euler=(-.95,0,0)
 p=rig.pose.bones['Forearm.'+side];p.rotation_mode='XYZ';p.rotation_euler=(-.65,0,0)
bpy.context.view_layer.update()
mesh=bpy.data.meshes.new_from_object(rider.evaluated_get(bpy.context.evaluated_depsgraph_get()),preserve_all_data_layers=True,depsgraph=bpy.context.evaluated_depsgraph_get())
body=bpy.data.objects.new('Mounted courier',mesh);bpy.context.collection.objects.link(body);body.location.z=.57
# Preserve the original courier atlas while laying out a new combined horse/rider atlas.
mesh.uv_layers.active.name='SourceUV';mesh.uv_layers.new(name='BakeUV');mesh.uv_layers.active_index=1
for material in mesh.materials:
 uv=material.node_tree.nodes.new('ShaderNodeUVMap');uv.uv_map='SourceUV'
 for node in list(material.node_tree.nodes):
  if node.type=='TEX_IMAGE':material.node_tree.links.new(uv.outputs[0],node.inputs['Vector'])
  if node.type=='NORMAL_MAP':node.uv_map='SourceUV'
for obj in list(bpy.context.scene.objects):
 if obj!=body:bpy.data.objects.remove(obj,do_unlink=True)
parts=[body]
fur=mat('Bay horse coat',(.17,.085,.035));dark=mat('Horse mane and hooves',(.035,.025,.02));leather=mat('Worn saddle leather',(.15,.085,.045));steel=mat('Tack buckles',(.28,.29,.27));eye=mat('Horse eyes',(.02,.022,.02))
ball('Horse barrel',(0,0,1.05),(.28,.74,.32),fur)
ball('Horse shoulders',(0,-.48,1.09),(.27,.33,.34),fur)
ball('Horse quarters',(0,.52,1.06),(.27,.33,.32),fur)
o=ball('Arched neck',(0,-.65,1.43),(.16,.20,.47),fur);o.rotation_euler.x=-.32
ball('Horse head',(0,-.94,1.72),(.17,.29,.19),fur)
ball('Horse muzzle',(0,-1.19,1.61),(.14,.21,.12),fur)
for x in (-.1,.1):
 ball('Horse ear',(x,-.88,1.94),(.045,.055,.15),fur)
 ball('Horse eye',(x*1.61,-1.035,1.76),(.014,.022,.018),eye)
for side in (-1,1):
 for end in (-1,1):
  x=side*.20;y=end*.50
  rod('Horse upper leg',(x,y,.99),(x,y+end*.055,.56),.062,fur)
  ball('Horse knee',(x,y+end*.055,.55),(.064,.063,.071),fur)
  rod('Horse lower leg',(x,y+end*.055,.53),(x,y-.035,.16),.036,fur)
  box('Horse hoof',(x,y-.07,.10),(.12,.16,.12),dark,bevel=.025)
for i in range(8):ball('Mane',(0,-.56-i*.035,1.35+i*.063),(.07,.10,.085),dark)
rod('Tail',(0,.73,1.13),(0,.98,.58),.055,dark)
box('Saddle blanket',(0,.13,1.36),(.53,.53,.06),leather,bevel=.03)
ball('Saddle seat',(0,.14,1.40),(.22,.24,.07),leather)
for x in (-.28,.28):
 rod('Stirrup leather',(x,.10,1.35),(x,.10,.91),.012,leather)
 rod('Stirrup tread',(x-.045,.08,.91),(x+.045,.08,.91),.012,steel)
 rod('Bridle side',(x*.60,-1.20,1.61),(x*.60,-.83,1.85),.009,leather)
 rod('Rein',(x*.60,-1.18,1.65),(x*.62,-.18,1.62),.006,leather)
model=join('CavalryMount',parts[:]);model.data.uv_layers['BakeUV'].active_render=True;models=(model,);bake(model,'CavalryMount',1024)
# Unity samples TEXCOORD0: export only the baked atlas UV channel.
for uv_name in [uv.name for uv in model.data.uv_layers]:
 if uv_name != 'BakeUV':model.data.uv_layers.remove(model.data.uv_layers[uv_name])
model.data.uv_layers[0].name='UVMap';model.data.uv_layers[0].active_render=True
markers=[]
for name,p in [('FaceAimOrigin',(0,0,1)),('FaceAimForward',(0,-1,1))]:
 obj=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(obj);obj.location=p;markers.append(obj)
bpy.ops.object.select_all(action='DESELECT')
for obj in [model]+markers:obj.select_set(True)
bpy.context.view_layer.objects.active=model
bpy.ops.export_scene.fbx(filepath=out+'/CavalryMount.fbx',use_selection=True,object_types={'MESH','EMPTY'},bake_anim=False,axis_forward='-Z',axis_up='Y',use_tspace=True,path_mode='RELATIVE',apply_scale_options='FBX_SCALE_UNITS')
bpy.ops.wm.save_as_mainfile(filepath=root+'/Tools/Art/CavalryMount.blend')
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,.03));bpy.context.object.data.materials.append(mat('Preview floor',(.25,.225,.16)))
for loc,energy in [((-3,-5,8),1600),((4,1,6),700)]:
 bpy.ops.object.light_add(type='AREA',location=loc);bpy.context.object.data.energy=energy;bpy.context.object.data.size=5
bpy.ops.object.camera_add(location=(5,-7,4));camera=bpy.context.object;camera.rotation_euler=(Vector((0,-.15,1.25))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=3.4;scene.camera=camera
scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.cycles.samples=24;scene.render.filepath=root+'/Tools/Art/CavalryMount_Preview.png';bpy.ops.render.render(write_still=True)
print('DONE CAVALRY',flush=True)
