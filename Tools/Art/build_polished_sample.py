"""Author the HQ/house/watchtower reference sample, using a generated painted material source.
Run after the base reference kit: blender -b -t 4 --python-exit-code 1 --python this_file -- repo
Keeps gameplay footprints, resource names, one material per model and existing GUIDs.
"""
import bpy,sys,os,math,random,json
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1]
source=os.path.join(root,'Tools/Art/build_reference_wasteland.py');code=open(source).read()
exec(compile(code[:code.index('# Headquarters: fortified')],source,'exec'),globals())
random.seed(20261023)
image=bpy.data.images.load(os.path.join(root,'Tools/Art/PaintedSurfaceSource.png'),check_existing=True)
# Project each face into a selected quadrant before baking. No runtime extra samplers.
def surface(material,cell,tint=(1,1,1)):
 n=material.node_tree.nodes;l=material.node_tree.links;p=n.get('Principled BSDF')
 geo=n.new('ShaderNodeNewGeometry');pos=n.new('ShaderNodeSeparateXYZ');normal=n.new('ShaderNodeSeparateXYZ');l.new(geo.outputs['Position'],pos.inputs[0]);l.new(geo.outputs['Normal'],normal.inputs[0])
 def op(kind,a,b=None):
  q=n.new('ShaderNodeMath');q.operation=kind
  for i,v in enumerate((a,b)):
   if v is None:continue
   if isinstance(v,(int,float)):q.inputs[i].default_value=v
   else:l.new(v,q.inputs[i])
  return q.outputs[0]
 xface=op('GREATER_THAN',op('ABSOLUTE',normal.outputs['X']),.6);top=op('GREATER_THAN',op('ABSOLUTE',normal.outputs['Z']),.6)
 def blend(a,b,t):return op('ADD',op('MULTIPLY',a,op('SUBTRACT',1,t)),op('MULTIPLY',b,t))
 u=blend(pos.outputs['X'],pos.outputs['Y'],xface);v=blend(pos.outputs['Z'],pos.outputs['Y'],top)
 vec=n.new('ShaderNodeCombineXYZ')
 for axis,value,offset in [('X',u,.515 if cell%2 else .015),('Y',v,.515 if cell<2 else .015)]:
  sample=op('ADD',op('MULTIPLY',op('FRACT',op('MULTIPLY',value,.41)),.47),offset);l.new(sample,vec.inputs[axis])
 tex=n.new('ShaderNodeTexImage');tex.image=image;tex.extension='EXTEND';l.new(vec.outputs[0],tex.inputs[0])
 color=n.new('ShaderNodeMixRGB');color.blend_type='MULTIPLY';color.inputs[0].default_value=1;color.inputs[2].default_value=(*tint,1);l.new(tex.outputs['Color'],color.inputs[1]);l.new(color.outputs[0],p.inputs['Base Color'])
 bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.18;bump.inputs['Distance'].default_value=.018;l.new(tex.outputs['Color'],bump.inputs['Height']);l.new(bump.outputs[0],p.inputs['Normal'])
for m,cell,tint in [(steel,0,(1.3,1.25,1.15)),(rust,0,(1.5,.94,.65)),(blue,0,(1.15,1.2,1.2)),(olive,0,(1.15,1.22,.78)),(wood,1,(1.25,1.15,1.02)),(pale,1,(1.6,1.5,1.3)),(concrete,2,(1.25,1.15,.96)),(cloth,3,(1.8,1.65,1.30)),(patch,3,(1.3,1.26,1.02))]:surface(m,cell,tint)

def bolts(x,y,z,w,count=4):
 for i in range(count):rod('Dark bolt socket',(x-w/2+i*w/(count-1),y,z),(x-w/2+i*w/(count-1),y-.022,z),.025,ink,6)
def window(x,y,z,w=.55,h=.42):
 box('Deep shaded window',(x,y,z),(w,.035,h),ink)
 for xx in (-w/2,w/2):box('Window vertical trim',(x+xx,y-.025,z),(.045,.045,h+.07),pale)
 for zz in (-h/2,h/2):box('Window sill',(x,y-.035,z+zz),(w+.08,.10,.045),wood)
 for dx in (-w*.24,w*.24):rod('Salvaged window bars',(x+dx,y-.065,z-h*.4),(x+dx,y-.065,z+h*.4),.012,steel,4)
def tarp(x,y,z,w,d):
 verts=[(x-w/2,y,z),(x,y,z-.035),(x+w/2,y,z),(x-w/2,y-d,z-.16),(x,y-d,z-.24),(x+w/2,y-d,z-.16)]
 mesh=bpy.data.meshes.new('Sagging stitched canvas');mesh.from_pydata(verts,[],[(0,1,4,3),(1,2,5,4),(3,4,1,0),(4,5,2,1)]);mesh.update();o=bpy.data.objects.new('Torn shade awning',mesh);bpy.context.collection.objects.link(o);finish(o,o.name,cloth)
 for side in (-1,1):rod('Awning support',(x+side*w*.47,y-d,.04),(x+side*w*.47,y-d,z-.14),.022,wood)
 for dx in (-w/2,0,w/2):rod('Sewn canvas edge',(x+dx,y,z+.005),(x+dx,y-d,z-(.24 if dx==0 else .16)),.009,ink,4)
# Retain the reference HQ silhouette, then add a lived-in human scale around it.
block=code[code.index('# Headquarters: fortified'):code.index("complete('Headquarters')")]
exec(compile(block,source,'exec'),globals())
window(-1.10,-1.485,.94,.53,.38);window(1.10,-1.485,.94,.53,.38)
# A balcony has real boards, dark gaps, pale ends and a rope handrail.
for i in range(12):box('Weathered balcony board',(-1.58+i*.285,-1.44,1.48),(.267,.59,.055),pale if i%4==0 else wood)
for x in (-1.58,-.52,.52,1.58):
 rod('Balcony upright',(x,-1.70,1.47),(x,-1.70,1.98),.024,wood)
 rod('Balcony diagonal bracket',(x,-1.16,1.02),(x,-1.69,1.45),.027,steel,4)
for z in (1.72,1.98):rod('Balcony rope rail',(-1.58,-1.70,z),(1.58,-1.70,z),.014,cloth,6)
for i in range(8):box('Access stair tread',(1.86,-.96+i*.25,.13+i*.17),(.42,.28,.055),wood)
rod('Stair handrail',(2.07,-1.06,.6),(2.07,.90,1.85),.022,steel)
for z in (.35,.75,1.15):bolts(0,-1.535,z,3.1,5)
tarp(-.08,-1.53,1.28,1.05,.45)
for z in (2.5,2.8,3.1):box('Tower repair rivet strip',(.35,-.85,z),(1.42,.035,.025),ink)
# Repaired communications gear and a small cable silhouette.
rod('Aerial mast',(-.76,.82,2.37),(-.76,.82,3.88),.018,steel)
for z,w in ((3.7,.5),(3.52,.34)):rod('Radio antenna crosspiece',(-.76-w,.82,z),(-.76+w,.82,z),.011,steel,4)
for i in range(4):box('Gateway repair patch',(-1.4+i*.79,-1.47,.26),(.31,.027,.16),steel if i%2 else rust)
# Selective single-segment edge bevels; no subdivision or new materials at runtime.
for o in parts:
 if o.name.startswith(('Fortified command foundation','Reinforced corner pier')):
  bpy.context.view_layer.objects.active=o;mod=o.modifiers.new('Worn concrete edges','BEVEL');mod.width=.018;mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
complete('Headquarters')
# House: overlapping timber repairs and a sagging canvas porch, not a plain box.
shack(1.65,1.32,1.20)
tarp(0,-.71,.98,1.24,.52)
for side in (-1,1):
 for i in range(5):
  box('House side horizontal repair',(side*.856,-.48+i*.24,.40+(i%2)*.11),(.045,.225,.42),pale if i%3==0 else wood)
for x in (-.57,.57):bolts(x,-.718,1.12,.24,2)
box('Doorstep stone',(0,-1.06,.055),(.56,.29,.10),concrete)
barrel(.85,-.78);crate(-.81,-.86,.20)
complete('OutpostHut')
# Tower: lighter but sharper silhouette, patched roof, open slatted guard cabin.
tower(height=3.2,size=.94)
for i in range(7):box('Lookout deck board',(-.51+i*.17,0,2.36),(.155,1.32,.05),pale if i%3==0 else wood)
for side in (-1,1):
 for i in range(5):box('Lookout vertical planking',(-.46+i*.23,side*.51,2.54),(.20,.036,.38),wood if i%2 else pale)
 for y in (-.52,.52):rod('Lookout bright corner',(side*.52,y,2.34),(side*.52,y,3.12),.022,pale,4)
patchpiece=box('Lookout replacement tin',(.30,.10,3.26),(.53,.73,.025),steel);patchpiece.rotation_euler.x=.10
for x in (-.4,.4):bolts(x,-.55,2.66,.18,2)
box('Lookout supply box',(0,0,2.46),(.28,.30,.19),wood)
complete('OutpostWatchtower')
report=json.load(open(os.path.join(root,'Tools/Art/ComicOutpostBudget.json')))
for model in models:
 assert len(model.data.polygons)<4000,(model.name,len(model.data.polygons))
 bake(model,model.name,1024);export(model.name+'.fbx',[model],False)
 report[model.name]={'triangles':len(model.data.polygons),'materials':1,'texture':1024}
 print('PASS polished sample export',model.name,report[model.name],flush=True)
# Retain this three-model source, then update the complete sprite-rendering source.
bpy.context.preferences.filepaths.save_version=0
for im in bpy.data.images:
 if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=os.path.join(root,'Tools/Art'))
sample=os.path.join(root,'Tools/Art/PolishedOutpost.blend');bpy.ops.wm.save_as_mainfile(filepath=sample)
bpy.ops.wm.open_mainfile(filepath=os.path.join(root,'Tools/Art/ComicOutpost.blend'))
for name in ('Headquarters','OutpostHut','OutpostWatchtower'):
 old=bpy.data.objects.get(name)
 if old:bpy.data.objects.remove(old,do_unlink=True)
with bpy.data.libraries.load(sample,link=False) as (available,loaded):loaded.objects=['Headquarters','OutpostHut','OutpostWatchtower']
for obj in loaded.objects:bpy.context.collection.objects.link(obj)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(root,'Tools/Art/ComicOutpost.blend'))
open(os.path.join(root,'Tools/Art/ComicOutpostBudget.json'),'w').write(json.dumps(report,indent=2)+'\n')
