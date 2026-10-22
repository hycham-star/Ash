"""Proof render of actual shipped HQ/house/watchtower/Engineer meshes and town texture.
A staged art sample in Blender, not a Unity screenshot or a player save.
"""
import bpy,sys,os,math,random
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1]
path=os.path.join(root,'Tools/Art/render_outpost_district_preview.py');code=open(path).read()
exec(compile(code[:code.index('soil=material(')],path,'exec'),globals())
random.seed(723)
soil=material('Shipped town earth',(1,1,1));n=soil.node_tree.nodes;l=soil.node_tree.links;geo=n.new('ShaderNodeNewGeometry');mapping=n.new('ShaderNodeVectorMath');mapping.operation='SCALE';mapping.inputs[3].default_value=.08;l.new(geo.outputs['Position'],mapping.inputs[0]);tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(os.path.join(art,'OutpostGroundAlbedo.png'));l.new(mapping.outputs[0],tex.inputs[0]);l.new(tex.outputs[0],n.get('Principled BSDF').inputs['Base Color'])
wood=material('Sun dried timber',(.29,.19,.10));metal=material('Dusty iron',(.24,.26,.23));dry=material('Dry straw',(.45,.31,.11));stone=material('Ochre rubble',(.40,.28,.16))
def box(name,position,size,m):
 bpy.ops.mesh.primitive_cube_add(size=1,location=position);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m);return o
box('Continuous town earth',(0,0,-.08),(80,80,.15),soil)
model('Headquarters',(0,.8,0));model('OutpostHut',(-3,-1.0,0));model('OutpostWatchtower',(3.15,2.4,0));model('Engineer',(.15,-1.55,.01),.6,190)
# The real runtime combines dry nature and rubble; this staged view uses the same small scale.
for i in range(44):
 x=random.uniform(-5,5);y=random.uniform(-4,4)
 if (abs(x)<2.4 and abs(y-.8)<2.25) or (abs(x+3)<1.4 and abs(y+1)<1.3) or (abs(x-3.15)<.8 and abs(y-2.4)<.8):continue
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=(x,y,.02));o=bpy.context.object;o.name='Small noninteractive rubble';o.scale=(.06+random.random()*.09,.04+random.random()*.06,.025+random.random()*.05);o.data.materials.append(stone)
 if i%2==0:
  for j in range(4):
   a=j*1.5;v=[(x-.007,y,.01),(x+.007,y,.01),(x+math.cos(a)*.04,y+math.sin(a)*.04,.13+random.random()*.09)];mesh=bpy.data.meshes.new('Bent dry grass');mesh.from_pydata(v,[],[(0,1,2),(2,1,0)]);o=bpy.data.objects.new('Miniature dry grass',mesh);scene.collection.objects.link(o);o.data.materials.append(dry)
for i in range(10):
 x=-5+i*1.1;box('Patched rear wall',(x,4.4,.58),(1.04,.075,1.12),wood if i%3==0 else metal);box('Perimeter post',(x-.51,4.4,.70),(.09,.12,1.4),wood)
for location,power,color in [((-6,-7,10),2.2,(1,.91,.76))]:
 bpy.ops.object.light_add(type='SUN',location=location);sun=bpy.context.object;sun.data.energy=power;sun.data.angle=.065;sun.data.color=color;sun.rotation_euler=(Vector((0,0,0))-sun.location).to_track_quat('-Z','Y').to_euler()
scene.world.use_nodes=True;background=scene.world.node_tree.nodes.get('Background');background.inputs['Color'].default_value=(.64,.58,.47,1);background.inputs['Strength'].default_value=.55
scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0
focus=Vector((0,.15,1.3));bpy.ops.object.camera_add(location=focus+Vector((10,-10,10)));camera=bpy.context.object;camera.rotation_euler=(focus-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=12.2;scene.camera=camera
scene.render.resolution_x=1280;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.cycles.samples=32;scene.cycles.use_denoising=False
scene.render.use_freestyle=True;style=scene.view_layers[0].freestyle_settings.linesets[0].linestyle;style.color=(.065,.041,.023);style.thickness=1.1
scene.render.image_settings.file_format='PNG';scene.render.filepath=os.path.join(root,'Tools/Art/PolishedSample_Preview.png');bpy.ops.render.render(write_still=True)
print('PASS staged polished sample of shipped assets; Blender, not Unity',flush=True)
