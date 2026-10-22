"""Render the actual fifteen original reference-kit models to fixed-view transparent sprites."""
import bpy,sys,os,json
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1];source=os.path.join(root,'Tools/Art/ComicOutpost.blend');sprites=os.path.join(root,'Tools/Art/ComicSprites');os.makedirs(sprites,exist_ok=True)
names=list(json.load(open(os.path.join(root,'Tools/Art/ComicOutpostBudget.json'))))
requested=sys.argv[sys.argv.index('--')+2:]
if requested:
 assert set(requested)<=set(names),'Unknown sprite model'
 names=requested
for name in names:
 bpy.ops.wm.open_mainfile(filepath=source);scene=bpy.context.scene
 for obj in list(scene.objects):
  if obj.name!=name:bpy.data.objects.remove(obj,do_unlink=True)
 model=bpy.data.objects[name];model.hide_render=False;model.hide_set(False)
 corners=[model.matrix_world@Vector(p) for p in model.bound_box];target=sum(corners,Vector())/8
 bpy.ops.object.camera_add();camera=bpy.context.object;camera.location=target+Vector((6,-6,6));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';scene.camera=camera;bpy.context.view_layer.update()
 projected=[camera.matrix_world.inverted()@p for p in corners];camera.data.ortho_scale=max(max(p.x for p in projected)-min(p.x for p in projected),max(p.y for p in projected)-min(p.y for p in projected))*1.18
 for location,power,color in [((-4,-5,8),700,(1,.89,.72)),((4,2,6),180,(.82,.85,.87))]:
  bpy.ops.object.light_add(type='AREA',location=location);light=bpy.context.object;light.data.energy=power;light.data.size=4;light.data.color=color;light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
 scene.world.color=(.23,.21,.18);scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.cycles.samples=12;scene.cycles.use_denoising=False
 scene.render.use_freestyle=True;style=scene.view_layers[0].freestyle_settings.linesets[0].linestyle;style.color=(.055,.035,.022);style.thickness=1.35
 scene.render.resolution_x=512;scene.render.resolution_y=512;scene.render.resolution_percentage=100;scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
 scene.render.filepath=os.path.join(sprites,name+'.png');bpy.ops.render.render(write_still=True)
 print('PASS reference miniature',name,flush=True)
