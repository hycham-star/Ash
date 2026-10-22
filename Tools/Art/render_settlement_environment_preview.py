"""Render the real outpost assets with the runtime nature placement and boundary-haze formula.
Asset/layout illustration in Blender, not a Unity screenshot or device performance measurement.
blender -b -t 4 --python-exit-code 1 --python this_file -- repo
"""
import bpy,sys,os,math
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1]
sourcefile=os.path.join(root,'Tools/Art/render_outpost_district_preview.py')
source=open(sourcefile).read();exec(compile(source[:source.index('bpy.ops.render.render(write_still=True)')],sourcefile,'exec'),globals())
def hash_value(seed,x,y):
 v=(seed^((x*0x9E3779B9)&0xffffffff)^((y*0x85EBCA6B)&0xffffffff))&0xffffffff
 v^=v>>16;v=(v*0x7FEB352D)&0xffffffff;v^=v>>15;return v

def rnd(i,salt):return (hash_value(4761,i,salt)&65535)/65535
road_bounds=[(o.location.x,o.location.y,o.dimensions.x+.7,o.dimensions.y+.7) for o in scene.objects if o.name.startswith('Worn street')]
nature_blocked=[(x,z,w,d) for _,x,z,w,d in cores]
def clear(x,y,r):return all(abs(x-a)>=w/2+r or abs(y-b)>=d/2+r for a,b,w,d in nature_blocked+road_bounds)
grass_mats=[material('Dry straw '+str(i),tuple(c/255 for c in rgb)) for i,rgb in enumerate(((151,126,72),(127,111,70),(168,142,86)))]
wood=material('Bare weathered timber',(112/255,82/255,55/255));metal=material('Salvaged wall metal',(.24,.30,.30));concrete=material('Concrete wall',(.52,.52,.46))
vertices=[];faces=[]
for i in range(6):
 a=math.radians(i*67);h=.25+(i%3)*.05
 def rot(p):return (p[0]*math.cos(a)-p[1]*math.sin(a),p[0]*math.sin(a)+p[1]*math.cos(a),p[2])
 points=[rot(p) for p in ((-.017,0,0),(.017,0,0),(.055,.012,h*.6),(.11,.025,h))];start=len(vertices);vertices+=points;faces.extend([(start,start+1,start+2),(start,start+2,start+3)])
mesh=bpy.data.meshes.new('Six dry bent blades');mesh.from_pydata(vertices,[],faces);mesh.update()
for m in grass_mats:mesh.materials.append(m)
n=0
for i in range(2400):
 x=-26+52*rnd(i,1);y=-30+60*rnd(i,2)
 if not clear(x,y,.2):continue
 o=bpy.data.objects.new('Dry grass tuft',mesh);scene.collection.objects.link(o);o.location=(x,y,.04);o.rotation_euler.z=math.radians(rnd(i,4)*360);o.scale=(.42+.63*rnd(i,3),)*3
 # Each linked mesh uses its material slot zero; object-linked overrides keep shared geometry.
 o.material_slots[0].link='OBJECT';o.material_slots[0].material=grass_mats[i%3]
 n+=1
 if n==420:break
branches=[((0,0,0),(.12,.04,1.85),.085),((.04,0,.68),(-.62,.18,1.27),.045),((-.34,.1,1.05),(-.5,.08,1.60),.020),((.08,0,1),(.58,-.22,1.50),.040),((.38,-.16,1.34),(.76,-.18,1.36),.017),((.11,.03,1.42),(-.2,-.2,1.88),.028)]
n=0
for i in range(600):
 x=-25+50*rnd(i,11);y=-29+58*rnd(i,12)
 if (abs(x)<7 and abs(y)<10) or not clear(x,y,1.1):continue
 scale=.7+.45*rnd(i,13);angle=math.radians(rnd(i,14)*360)
 def point(p):return Vector((x+(p[0]*math.cos(angle)-p[1]*math.sin(angle))*scale,y+(p[0]*math.sin(angle)+p[1]*math.cos(angle))*scale,.025+p[2]*scale))
 for a,b,radius in branches:
  a=point(a);b=point(b);bpy.ops.mesh.primitive_cylinder_add(vertices=6,radius=radius*scale,depth=(b-a).length,location=(a+b)/2);o=bpy.context.object;o.name='Dead tree branch';o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();o.data.materials.append(wood)
 n+=1
 if n==20:break
for side in (-1,1):
 x=side*28
 for j in range(46):
  y=-32+j*1.4;box('Fence post',(x,y,.85),(.14,.14,1.7),wood)
  if y+1.4<=32:box('Salvage palisade panel',(x,y+.7,.62),(.07,1.15,1.1),metal)
 model('OutpostWatchtower',(x,31,0))
for j in range(41):
 x=-28+j*1.4;box('Rear barrier',(x,32,.75),(1.35,.15,1.5),wood)
 if abs(x)>2.4:box('Gate wall',(x,-32,.55),(1.4,.25,1.1),concrete)
box('Gate sign',(0,-32,2.4),(3.5,.18,.55),metal)
for x in (-1.8,1.8):box('Gate pillar',(x,-32,1.25),(.25,.3,2.5),wood)
# Miniatures use the existing authored animated model, shown in a still pose here.
for i,(x,y) in enumerate(((-5,-2),(1,-3),(5,2),(-6,4),(1,8))):model('Scavenger',(x,y,.10),.6,i*72)
# A directional key and broad cool fill express the same lighting intent as the runtime shader.
for o in list(scene.objects):
 if o.type=='LIGHT':bpy.data.objects.remove(o,do_unlink=True)
bpy.ops.object.light_add(type='SUN',location=(-20,-28,35));sun=bpy.context.object;sun.data.energy=2.2;sun.data.angle=.06;sun.data.color=(1,.93,.81);sun.rotation_euler=(Vector((0,0,0))-sun.location).to_track_quat('-Z','Y').to_euler()
scene.world.use_nodes=True
scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.52,.49,.43,1)
scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.35
for o in scene.objects:
 if o.name=='Continuous dirt surface':o.dimensions=(128,128,.4)
# World-space rectangle distance, smoothstep(.8,16.8) outside the 28x32 walls.
for m in list(bpy.data.materials):
 if not m.use_nodes:continue
 n=m.node_tree.nodes;l=m.node_tree.links;output=n.get('Material Output')
 if not output or not output.inputs['Surface'].is_linked:continue
 original=output.inputs['Surface'].links[0].from_socket
 geo=n.new('ShaderNodeNewGeometry');sep=n.new('ShaderNodeSeparateXYZ');l.new(geo.outputs['Position'],sep.inputs[0])
 def mathnode(op,a,b=None):
  q=n.new('ShaderNodeMath');q.operation=op
  if isinstance(a,(int,float)):q.inputs[0].default_value=a
  else:l.new(a,q.inputs[0])
  if b is not None:
   if isinstance(b,(int,float)):q.inputs[1].default_value=b
   else:l.new(b,q.inputs[1])
  return q.outputs[0]
 x=mathnode('SUBTRACT',mathnode('ABSOLUTE',sep.outputs['X']),28);y=mathnode('SUBTRACT',mathnode('ABSOLUTE',sep.outputs['Y']),32)
 outside=mathnode('MAXIMUM',x,y);r=n.new('ShaderNodeMapRange');r.interpolation_type='SMOOTHSTEP';r.clamp=True;r.inputs['From Min'].default_value=.8;r.inputs['From Max'].default_value=16.8;l.new(outside,r.inputs['Value'])
 emission=n.new('ShaderNodeEmission');emission.inputs['Color'].default_value=(.52,.49,.43,1)
 mix=n.new('ShaderNodeMixShader');l.new(r.outputs[0],mix.inputs[0]);l.new(original,mix.inputs[1]);l.new(emission.outputs[0],mix.inputs[2]);l.new(mix.outputs[0],output.inputs['Surface'])
background=scene.world.node_tree.nodes.get('Background');nodes=scene.world.node_tree.nodes;links=scene.world.node_tree.links;path=nodes.new('ShaderNodeLightPath');visible=nodes.new('ShaderNodeBackground');visible.inputs['Color'].default_value=(.52,.49,.43,1);visible.inputs['Strength'].default_value=1;mix=nodes.new('ShaderNodeMixShader');links.new(path.outputs['Is Camera Ray'],mix.inputs[0]);links.new(background.outputs[0],mix.inputs[1]);links.new(visible.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],nodes.get('World Output').inputs['Surface'])
scene.render.use_freestyle=False;scene.cycles.samples=24;camera.data.ortho_scale=82
scene.render.resolution_x=1000;scene.render.resolution_y=900
scene.render.filepath=os.path.join(root,'Tools/Art/SettlementEnvironment_Preview.png');bpy.ops.render.render(write_still=True)
camera.data.ortho_scale=27;scene.render.resolution_x=800;scene.render.resolution_y=1000
scene.render.filepath=os.path.join(root,'Tools/Art/SettlementNatureDetail_Preview.png');bpy.ops.render.render(write_still=True)
print('PASS settlement environment asset illustration; Blender, not Unity',flush=True)
