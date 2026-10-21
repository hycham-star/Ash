"""Render the actual concrete building assets into the existing home/camp map cells.
Run after build_detailed_environment.py; then run build_isometric_atlas.py.
"""
import bpy,os,sys,ast,math,random
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1];out=os.path.join(root,'Assets/Ash/Resources/Map')
common=os.path.join(root,'Tools/Art/build_map_landmarks.py');tree=ast.parse(open(common).read())
names={'material','finish','box','cylinder','sphere','rod'}
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in names],type_ignores=[]),common,'exec'),globals())
for name,asset,scale in [('Camp','Barracks',.85),('Settlement','Headquarters',.65)]:
 bpy.ops.wm.open_mainfile(filepath=os.path.join(root,'Tools/Art/DetailedEnvironment.blend'));scene=bpy.context.scene
 for obj in list(scene.objects):
  if obj.name!=asset:bpy.data.objects.remove(obj,do_unlink=True)
 model=bpy.data.objects[asset];model.hide_render=False;model.hide_set(False);model.scale=(scale,)*3
 soil=material('Dry compound earth',(.23,.19,.12));wood=material('Dry fence timber',(.20,.13,.07));metal=material('Oxidised perimeter steel',(.12,.16,.14));rust=material('Rust sheet',(.26,.105,.04));stone=material('Debris',(.32,.30,.24))
 cylinder('Earth patch',(0,0,.035),1.82,.08,soil)
 random.seed(712)
 for x in (-1.53,1.53):
  for y in (-1.35,-.8,-.25,.3,.85,1.35):box('Fence support',(x,y,.38),(.055,.055,.65),wood)
  for z in (.25,.50):rod('Fence wire',(x,-1.37,z),(x,1.37,z),.009,metal)
 for y in (-1.37,1.37):
  for x in (-1.3,-.8,.8,1.3):box('Salvage perimeter',(x,y,.28),(.44,.035,.40),wood if name=='Settlement' else rust)
 for i in range(14):
  a=random.uniform(0,math.tau);r=random.uniform(1.45,1.75);sphere('Loose rubble',(math.cos(a)*r,math.sin(a)*r,.09),(.035,.026,.023),stone)
 bpy.ops.object.camera_add();camera=bpy.context.object;target=Vector((0,0,1.0));camera.location=target+Vector((6,-6,6));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=5.25;scene.camera=camera
 scene.world.color=(.20,.23,.28)
 for loc,power,color in [((-4,-5,8),900,(1,.88,.74)),((4,2,5),350,(.68,.79,1))]:
  bpy.ops.object.light_add(type='AREA',location=loc);lamp=bpy.context.object;lamp.data.energy=power;lamp.data.size=4;lamp.data.color=color;lamp.rotation_euler=(target-lamp.location).to_track_quat('-Z','Y').to_euler()
 scene.render.resolution_x=384;scene.render.resolution_y=384;scene.render.resolution_percentage=100;scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.cycles.samples=32;scene.cycles.use_denoising=False
 scene.render.filepath=os.path.join(out,name+'.png');bpy.ops.render.render(write_still=True)
 print('PASS detailed compound',name,flush=True)
