"""Original lightweight district scenery and seamless ground material. Blender 4.3.2 CPU.
blender -b -t 4 --python-exit-code 1 --python this_file -- repo
"""
import bpy,sys,os,ast,math,random
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1];out=os.path.join(root,'Assets/Ash/Resources/Art');parts=[];random.seed(641)
common=os.path.join(root,'Tools/Art/build_visual_slice.py');tree=ast.parse(open(common).read());names={'mat','finish','box','ball','rod','join','bake','export'}
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in names],type_ignores=[]),common,'exec'),globals())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=False;scene.render.bake.margin=8;scene.world.color=(.22,.25,.28)
plaster=mat('Old earth plaster',(.30,.245,.17));timber=mat('Dry timber',(.21,.14,.075));rust=mat('Patched tin roofing',(.26,.13,.065));steel=mat('Dull metal',(.13,.16,.14));dark=mat('Deep openings',(.025,.03,.027));cloth=mat('Salvage canvas',(.28,.27,.18),'cloth');leaf=mat('Dusty olive foliage',(.14,.20,.07));sand=mat('Compacted earth',(.29,.22,.14));stone=mat('Loose concrete',(.36,.32,.25))
def rawbox(n,p,s,m):return box(n,p,s,m,bevel=0)
# One tiny residential shelter, rather than reusing a large military barracks for every home.
box('Hut foundation',(0,0,.09),(1.9,1.75,.12),stone,bevel=.025)
box('Patched shelter walls',(0,0,.68),(1.7,1.45,1.10),plaster,bevel=.025)
for x in (-.84,.84):rawbox('Corner timber',(x,0,.69),(.075,1.48,1.16),timber)
for side in (-1,1):
 o=rawbox('Salvage roof',(0,side*.42,1.33),(2.02,.91,.045),rust if side==1 else steel);o.rotation_euler.x=-side*.32
 for i in range(9):
  x=-.9+i*.225;rod('Corrugated seam',(x,0,1.48),(x,side*.86,1.20),.009,steel)
rawbox('Door shadow',(-.25,-.735,.62),(.50,.025,.88),dark)
rawbox('Wooden door',(-.25,-.755,.60),(.39,.015,.81),timber)
for z in (.38,.83):rawbox('Door brace',(-.25,-.77,z),(.40,.02,.025),steel)
rawbox('Hut window',(.48,-.735,.83),(.37,.025,.34),dark)
for x in (.28,.48,.68):rawbox('Window shutter frame',(x,-.76,.83),(.025,.04,.38),timber)
rawbox('Window sill',(.48,-.80,.64),(.48,.17,.06),stone)
rawbox('Door step',(-.25,-.90,.12),(.67,.30,.09),stone)
rod('Stove chimney',(.53,.35,1.3),(.53,.35,1.95),.045,steel)
rawbox('Chimney cap',(.53,.35,1.97),(.18,.18,.035),rust)
for x in (-.7,.65):rod('Shade post',(x,-1,.1),(x,-1,1.12),.019,timber)
o=rawbox('Torn shade canopy',(-.1,-.86,1.18),(1.45,.50,.023),cloth);o.rotation_euler.x=.14
rawbox('Supplies',(.80,.5,.24),(.30,.32,.31),timber)
hut=join('OutpostHut',parts[:]);parts.clear()
# Thin dry trees; foliage has an irregular outline instead of a stack of round balls.
rod('Tree trunk',(0,0,0),(.06,0,2.55),.055,timber)
for i in range(12):
 a=i*2.39996;z=1.1+(i%5)*.27;end=Vector((math.cos(a)*(.24+(i%3)*.05),math.sin(a)*.28,z+.28))
 rod('Thin branch',(.02,0,z-.18),end,.015,timber)
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=end);o=bpy.context.object;o.scale=(.27,.23,.40)
 finish(o,'Dry leaf cluster',leaf,smooth=True)
tree=join('OutpostTree',parts[:]);parts.clear()
# Light timber-and-scrap perimeter tower.
for x in (-.45,.45):
 for y in (-.45,.45):rod('Tower support',(x,y,.05),(x*.85,y*.85,2.5),.045,timber)
for side in (-1,1):
 rod('Cross brace',(side*.44,-.43,.7),(side*.38,.40,2.30),.02,steel)
 rod('Cross brace',(side*.44,.43,.7),(side*.38,-.40,2.30),.02,steel)
rawbox('Lookout floor',(0,0,2.42),(1.14,1.10,.10),timber)
for x in (-.48,.48):
 for y in (-.47,.47):rod('Shelter post',(x,y,2.42),(x,y,3.24),.025,timber)
for y in (-.49,.49):rawbox('Lookout railing',(0,y,2.73),(1.05,.05,.08),steel)
for side in (-1,1):
 o=rawbox('Watchtower roof',(0,side*.32,3.28),(1.35,.72,.04),rust);o.rotation_euler.x=-side*.32
for x in (-.18,.18):rod('Tower ladder',(x,-.56,.10),(x,-.56,2.46),.015,timber)
for i in range(10):rod('Tower ladder rung',(-.18,-.57,.20+i*.23),(.18,-.57,.20+i*.23),.012,timber)
tower=join('OutpostWatchtower',parts[:]);parts.clear();models=[hut,tree,tower]
for model in models:
 assert len(model.data.polygons)<6000,(model.name,len(model.data.polygons))
 bake(model,model.name,1024);export(model.name+'.fbx',[model],False)
 print('PASS district mesh',model.name,len(model.data.polygons),flush=True)
# A seamless, original ground albedo: periodic 4D noise, not a photograph or edited reference image.
for obj in list(scene.objects):bpy.data.objects.remove(obj,do_unlink=True)
bpy.ops.mesh.primitive_plane_add(size=1);plane=bpy.context.object;ground=bpy.data.materials.new('Seamless outpost soil');ground.use_nodes=True;n=ground.node_tree.nodes;l=ground.node_tree.links;p=n.get('Principled BSDF')
uv=n.new('ShaderNodeTexCoord');separate=n.new('ShaderNodeSeparateXYZ');l.new(uv.outputs['UV'],separate.inputs[0]);periodic=[]
for axis in ('X','Y'):
 mul=n.new('ShaderNodeMath');mul.operation='MULTIPLY';mul.inputs[1].default_value=math.tau;l.new(separate.outputs[axis],mul.inputs[0])
 for fn in ('SINE','COSINE'):
  op=n.new('ShaderNodeMath');op.operation=fn;l.new(mul.outputs[0],op.inputs[0]);periodic.append(op.outputs[0])
vec=n.new('ShaderNodeCombineXYZ')
for i,key in enumerate(('X','Y','Z')):l.new(periodic[i],vec.inputs[key])
def noise(scale):
 t=n.new('ShaderNodeTexNoise');t.noise_dimensions='4D';t.inputs['Scale'].default_value=scale;t.inputs['Detail'].default_value=4;l.new(vec.outputs[0],t.inputs['Vector']);l.new(periodic[3],t.inputs['W']);return t
broad=noise(1.4);fine=noise(48)
ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.23;ramp.color_ramp.elements[0].color=(.20,.135,.075,1);ramp.color_ramp.elements[1].position=.76;ramp.color_ramp.elements[1].color=(.42,.31,.18,1);l.new(broad.outputs['Fac'],ramp.inputs[0])
fineramp=n.new('ShaderNodeValToRGB');fineramp.color_ramp.elements[0].color=(.56,.50,.40,1);fineramp.color_ramp.elements[1].color=(1,1,1,1);l.new(fine.outputs['Fac'],fineramp.inputs[0]);mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.5;l.new(ramp.outputs[0],mix.inputs[1]);l.new(fineramp.outputs[0],mix.inputs[2]);l.new(mix.outputs[0],p.inputs['Base Color'])
image=bpy.data.images.new('OutpostGroundAlbedo',width=1024,height=1024,alpha=False);node=n.new('ShaderNodeTexImage');node.image=image;n.active=node;plane.data.materials.append(ground)
bpy.context.view_layer.objects.active=plane;plane.select_set(True);bpy.ops.object.bake(type='DIFFUSE',pass_filter={'COLOR'});image.filepath_raw=os.path.join(out,'OutpostGroundAlbedo.png');image.file_format='PNG';image.save()
print('PASS seamless outpost soil texture',flush=True)
