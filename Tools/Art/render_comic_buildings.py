"""Render real low-poly source models to fixed-angle transparent 2.5D sprites and map cells.
Blender -b -t 4 --python-exit-code 1 --python this_file -- repo
Sprite renders use Freestyle ink edges; Unity uses the bundled outline/paint shader.
"""
import bpy,sys,os,math
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1];source=os.path.join(root,'Tools/Art/ComicOutpost.blend');sprites=os.path.join(root,'Tools/Art/ComicSprites');os.makedirs(sprites,exist_ok=True)
names=['Headquarters','Workshop','Barracks','Hospital','WaterStation','FoodGarden','OutpostHut','Market','OutpostWatchtower']
for name in names+['Lumber']:
 bpy.ops.wm.open_mainfile(filepath=source);scene=bpy.context.scene
 for obj in list(scene.objects):
  if obj.name!=name:bpy.data.objects.remove(obj,do_unlink=True)
 if name=='Lumber':
  bpy.ops.import_scene.fbx(filepath=os.path.join(root,'Assets/Ash/Resources/Art/OutpostTree.fbx'));model=next(o for o in scene.objects if o.type=='MESH');model.hide_render=False;model.hide_set(False)
  # Imported FBX uses original Blender Z-up on round trip.
  material=bpy.data.materials.new('Painted woodland');material.use_nodes=True;n=material.node_tree.nodes;l=material.node_tree.links;tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(os.path.join(root,'Assets/Ash/Resources/Art/OutpostTreeAlbedo.png'));l.new(tex.outputs['Color'],n.get('Principled BSDF').inputs['Base Color'])
  model.data.materials.clear();model.data.materials.append(material)
  for i,(x,y,s) in enumerate([(-.65,0,.85),(.55,.4,.75),(0,-.60,.70)]):
   obj=model if i==0 else model.copy()
   if i:obj.data=model.data;bpy.context.collection.objects.link(obj)
   obj.location=(x,y,0);obj.scale=(s,)*3
 else:
  model=bpy.data.objects[name];model.hide_render=False;model.hide_set(False)
 target=Vector((0,0,1.25 if name=='Headquarters' else .95));height=6.1 if name=='Headquarters' else 4.5 if name=='OutpostWatchtower' else 4.25
 bpy.ops.object.camera_add();camera=bpy.context.object;camera.location=target+Vector((6,-6,6));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=height;scene.camera=camera
 for location,power,color in [((-4,-5,8),650,(1,.94,.81)),((4,2,6),200,(.82,.86,.80))]:
  bpy.ops.object.light_add(type='AREA',location=location);light=bpy.context.object;light.data.energy=power;light.data.size=5;light.data.color=color;light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
 scene.world.color=(.23,.24,.21);scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.cycles.samples=16;scene.cycles.use_denoising=False
 scene.render.use_freestyle=True;style=scene.view_layers[0].freestyle_settings.linesets[0].linestyle;style.color=(.055,.045,.032);style.thickness=1.4
 scene.render.resolution_x=512;scene.render.resolution_y=512;scene.render.resolution_percentage=100;scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
 scene.render.filepath=os.path.join(sprites,name+'.png');bpy.ops.render.render(write_still=True)
 print('PASS fixed-view comic sprite',name,flush=True)
