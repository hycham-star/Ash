"""Create original warm desert earth and a painted overworld terrain from Blender materials/geometry.
No reference pixels are copied. Output includes a tileable town texture and macro terrain PNG.
"""
import bpy,sys,os,math,random
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1];out=os.path.join(root,'Assets/Ash/Resources');random.seed(84)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=False;scene.render.resolution_x=1024;scene.render.resolution_y=1024;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=.55;scene.world.color=(.35,.25,.15)
m=bpy.data.materials.new('Reference sun baked orange sand');m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links
uv=n.new('ShaderNodeTexCoord');separate=n.new('ShaderNodeSeparateXYZ');l.new(uv.outputs['UV'],separate.inputs[0])
def mathnode(op,a,b=None):
 q=n.new('ShaderNodeMath');q.operation=op
 for i,value in enumerate((a,b)):
  if value is None:continue
  if isinstance(value,(int,float)):q.inputs[i].default_value=value
  else:l.new(value,q.inputs[i])
 return q.outputs[0]
u=mathnode('MULTIPLY',separate.outputs['X'],math.tau);v=mathnode('MULTIPLY',separate.outputs['Y'],math.tau)
coord=n.new('ShaderNodeCombineXYZ');l.new(mathnode('SINE',u),coord.inputs['X']);l.new(mathnode('COSINE',u),coord.inputs['Y']);l.new(mathnode('SINE',v),coord.inputs['Z'])
def noise(scale):
 q=n.new('ShaderNodeTexNoise');q.noise_dimensions='4D';q.inputs['Scale'].default_value=scale;q.inputs['Detail'].default_value=3;l.new(coord.outputs[0],q.inputs['Vector']);l.new(mathnode('COSINE',v),q.inputs['W']);return q.outputs['Fac']
broad=noise(2.8);fine=noise(160);dust=mathnode('ADD',mathnode('MULTIPLY',broad,.85),mathnode('MULTIPLY',fine,.15))
ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.1;ramp.color_ramp.elements[0].color=(.36,.16,.055,1);ramp.color_ramp.elements[1].position=.9;ramp.color_ramp.elements[1].color=(.78,.43,.18,1);l.new(dust,ramp.inputs[0])
# Independent soft pigment variation avoids a flat single-ramp palette after mobile downsampling.
tint=n.new('ShaderNodeTexNoise');tint.noise_dimensions='4D';tint.inputs['Scale'].default_value=3.9;tint.inputs['Detail'].default_value=2;l.new(coord.outputs[0],tint.inputs['Vector']);l.new(mathnode('COSINE',v),tint.inputs['W'])
painted=n.new('ShaderNodeMixRGB');painted.blend_type='MULTIPLY';painted.inputs[0].default_value=.4;l.new(ramp.outputs[0],painted.inputs[1]);l.new(tint.outputs['Color'],painted.inputs[2])
emit=n.new('ShaderNodeEmission');l.new(painted.outputs[0],emit.inputs['Color']);l.new(emit.outputs[0],n.get('Material Output').inputs['Surface'])
bpy.ops.mesh.primitive_plane_add(size=100);ground=bpy.context.object;ground.name='Tileable desert earth';ground.data.materials.append(m)
bpy.ops.object.camera_add(location=(0,0,100));cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=100;scene.camera=cam
scene.render.filepath=os.path.join(out,'Art/OutpostGroundAlbedo.png');bpy.ops.render.render(write_still=True)
# World terrain uses the same ground, with drawn tracks and sunlit rock formations.
bs=n.get('Principled BSDF');l.new(painted.outputs[0],bs.inputs['Base Color']);bs.inputs['Roughness'].default_value=1;l.new(bs.outputs[0],n.get('Material Output').inputs['Surface'])
def flat(name,color):
 q=bpy.data.materials.new(name);q.diffuse_color=(*color,1);q.use_nodes=True;p=q.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=1;return q
rock=flat('Sunlit umber rock',(.47,.25,.115));pathmat=flat('Dry worn desert path',(.56,.30,.12))
for cx,cy in ((39,-29),(37,24),(-39,-29),(-33,29),(10,-40)):
 for j in range(7):
  x=cx+random.uniform(-8,8);y=cy+random.uniform(-7,7)
  bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=(x,y,random.uniform(.2,1)))
  o=bpy.context.object;o.scale=(random.uniform(2,5),random.uniform(2,6),random.uniform(2,5));o.rotation_euler.z=random.uniform(0,6.28);o.data.materials.append(rock)
for points in [((-45,12),(-25,8),(-8,-4),(12,6),(44,20)),((-20,-49),(-15,-25),(-1,-7),(7,14),(10,49))]:
 curve=bpy.data.curves.new('Winding worn track','CURVE');curve.dimensions='3D';curve.bevel_depth=.22;curve.bevel_resolution=1;sp=curve.splines.new('BEZIER');sp.bezier_points.add(len(points)-1)
 for p,(x,y) in zip(sp.bezier_points,points):p.co=(x,y,.015);p.handle_left_type=p.handle_right_type='AUTO'
 o=bpy.data.objects.new('Desert route',curve);scene.collection.objects.link(o);o.data.materials.append(pathmat)
bpy.ops.object.light_add(type='SUN',location=(-20,-25,50));sun=bpy.context.object;sun.data.energy=1.6;sun.data.angle=.08;sun.data.color=(1,.88,.68);sun.rotation_euler=(Vector((0,0,0))-sun.location).to_track_quat('-Z','Y').to_euler()
scene.render.use_freestyle=True;style=scene.view_layers[0].freestyle_settings.linesets[0].linestyle;style.color=(.15,.085,.04);style.thickness=.6
scene.render.filepath=os.path.join(out,'Map/WastelandTerrain.png');bpy.ops.render.render(write_still=True)
print('PASS warm desert town earth and painted world terrain',flush=True)
