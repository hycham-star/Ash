"""Blender preview of the default settlement assets/layout, not a Unity screenshot.
blender -b -t 4 --python-exit-code 1 --python this_file -- repo
"""
import bpy,sys,os,math,random
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1];art=os.path.join(root,'Assets/Ash/Resources/Art')
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=False;scene.world.color=(.25,.28,.31);scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
def material(name,color):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.9;return m
def model(name,position,scale=1,heading=0):
 before=set(bpy.context.scene.objects);bpy.ops.import_scene.fbx(filepath=os.path.join(art,name+'.fbx'));objects=set(scene.objects)-before
 m=bpy.data.materials.new(name+' preview');m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links;p=n.get('Principled BSDF');p.inputs['Roughness'].default_value=.85
 tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(os.path.join(art,name+'Albedo.png'),check_existing=True);l.new(tex.outputs['Color'],p.inputs['Base Color'])
 normal=n.new('ShaderNodeTexImage');normal.image=bpy.data.images.load(os.path.join(art,name+'Normal.png'),check_existing=True);normal.image.colorspace_settings.name='Non-Color';bump=n.new('ShaderNodeNormalMap');l.new(normal.outputs['Color'],bump.inputs['Color']);l.new(bump.outputs[0],p.inputs['Normal'])
 for o in objects:
  if o.type!='MESH':continue
  o.data.materials.clear();o.data.materials.append(m)
  for f in o.data.polygons:f.material_index=0
  o.location+=Vector(position);o.scale*=scale;o.rotation_euler.z+=math.radians(heading)
 return objects
soil=material('Warm worn earth',(.31,.22,.135));n=soil.node_tree.nodes;l=soil.node_tree.links;p=n.get('Principled BSDF');uv=n.new('ShaderNodeTexCoord');mapping=n.new('ShaderNodeVectorMath');mapping.operation='SCALE';mapping.inputs[3].default_value=7.68;l.new(uv.outputs['UV'],mapping.inputs[0]);tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(os.path.join(art,'OutpostGroundAlbedo.png'));l.new(mapping.outputs[0],tex.inputs['Vector']);l.new(tex.outputs[0],p.inputs['Base Color'])
asphalt=material('Dusty asphalt',(.075,.07,.056));n=asphalt.node_tree.nodes;l=asphalt.node_tree.links;geo=n.new('ShaderNodeNewGeometry');noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=7;l.new(geo.outputs['Position'],noise.inputs['Vector']);ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(.045,.043,.036,1);ramp.color_ramp.elements[1].color=(.13,.115,.087,1);l.new(noise.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs[0],n.get('Principled BSDF').inputs['Base Color'])
shoulder=material('Road verge',(.28,.22,.15));mark=material('Quiet lot corner',(.36,.30,.20))
def box(name,p,s,m):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=name;o.dimensions=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m);return o
box('Continuous dirt surface',(0,0,-.2),(96,96,.4),soil)
cores=[('Headquarters',-3,3,4.6,4.2),('Workshop',3,2.5,3.4,3.2),('Barracks',-3.3,-4,3,2.6),('WaterStation',3.4,-4,3,2.6),('FoodGarden',3.5,6,3,2.5),('Barracks',-3,6.5,3,2.5)]
plots=[(x,z) for z in (-14,14) for x in (-12,0,12)]+[(x,z) for x in (-12,12) for z in (-6,2,8)]+[(x,z) for z in (-25,25) for x in (-21,-7,7,21)]+[(x,z) for x in (-23,23) for z in (-12,12)]
blocked=[(x,z,6,6) for x,z in plots]+[(x,z,w,d) for _,x,z,w,d in cores]
roads=[(False,x,1.8,20) for x in (-18,-8,8,18)]+[(False,0,1.2,10)]+[(True,z,1.2 if z==-1.9 else 1.8,27) for z in (-20,-10,-1.9,20)]+[(False,x,1.2,30) for x in (-26.8,26.8)]+[(True,z,1.2,27) for z in (-30,30)]
for across,fixed,width,extent in roads:
  cuts=[]
  for x,z,w,d in blocked:
   side,half=(z,d/2) if across else (x,w/2)
   if abs(side-fixed)>=half+width/2+.03:continue
   along,span=(x,w/2) if across else (z,d/2);cuts.append((along-span-.25,along+span+.25))
  def segment(a,b):
   if b-a<.2:return
   mid=(a+b)/2;p=(mid,fixed,.035) if across else (fixed,mid,.03);s=(b-a,width,.018) if across else (width,b-a,.018)
   box('Road verge',(p[0],p[1],p[2]-.012),(s[0]+(0 if across else .65),s[1]+(.65 if across else 0),s[2]),shoulder);box('Worn street',p,s,asphalt)
  start=-extent
  for a,b in sorted(cuts):
   if a>extent:break
   segment(start,min(extent,a));start=max(start,b)
  segment(start,extent)
for i,(name,x,z,w,d) in enumerate(cores):model(name,(x,z,0),.8 if i==5 else 1)
model('SalvageTruck',(6.7,.2,.06),.75,-35)
for i,(x,z) in enumerate(((-5.8,8),(5.8,8),(-5.8,-7),(5.8,-7))):model('OutpostTree',(x,z,.02),.52,i*137.5)
for x,z in plots:
 for side in (-1,1):
  for other in (-1,1):box('Lot corner',(x+side*2.85,z+other*2.85,.022),(.32,.055,.018),mark)
for loc,power,color,size in [((-10,-16,25),7000,(1,.88,.72),9),((14,9,18),2600,(.68,.78,1),12)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.data.color=color;o.rotation_euler=(Vector((0,0,0))-o.location).to_track_quat('-Z','Y').to_euler()
target=Vector((0,1,0));bpy.ops.object.camera_add(location=target+Vector((20,-20,20)));camera=bpy.context.object;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=24;scene.camera=camera
scene.render.resolution_x=800;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.filepath=os.path.join(root,'Tools/Art/OutpostDistrict_Preview.png');bpy.ops.render.render(write_still=True)
print('PASS default layout district preview; Blender, not Unity',flush=True)
