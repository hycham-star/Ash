"""Add a movable lumber mill and replace pale clinic canvas with painted weathered surfaces.
blender -b -t 4 --python-exit-code 1 --python this_file -- repo
"""
import bpy,sys,os,json,math
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1]
path=os.path.join(root,'Tools/Art/build_polished_sample.py');s=open(path).read();exec(compile(s[:s.index('# Retain the reference HQ silhouette')],path,'exec'),globals())
# Medical canvas has deliberately muted, darker paint and a visible red cross.
tent(0,0,2.15,1.8)
box('Clinic medical sign',(0,-.98,1.05),(.42,.035,.36),white)
box('Medical cross horizontal',(0,-1.005,1.05),(.28,.018,.07),red);box('Medical cross vertical',(0,-1.007,1.05),(.07,.018,.28),red)
for x in (-1.03,1.03):
 rod('Clinic awning pole',(x,-1.18,0),(x,-1.18,.85),.025,wood)
 rod('Clinic porch rail',(x,-1.18,.05),(x,-.8,.05),.035,wood)
barrel(1.18,.6);crate(-1.17,-.75);complete('Hospital')
# Saw shed, stacked cut timber and a sawbench are distinct from the world-map dead-tree site.
start=len(parts);shack(1.35,1.14,1.1)
for obj in parts[start:]:obj.location+=Vector((-.75,.55,0))
for row in range(3):
 for col in range(3-row):
  x=.45+col*.27+row*.135;z=.17+row*.23
  rod('Stacked rough cut log',(x,-1.08,z),(x,.76,z),.14,wood,8)
  rod('Pale sawn log end',(x,-1.088,z),(x,-1.103,z),.126,pale,8)
  rod('Dark heartwood ring',(x,-1.105,z),(x,-1.110,z),.055,wood,8)
# Timber stack slats and iron saw disk on a heavy plank workbench.
for i in range(4):box('Fresh timber board',(-.52+i*.16,-.9,.31),(.14,1.05,.07),pale)
for x in (-.61,.02):box('Saw bench leg',(x,-.65,.24),(.09,.60,.48),wood)
box('Saw bench top',(-.30,-.65,.52),(1,.68,.09),pale)
rod('Salvaged circular saw blade',(-.25,-.65,.52),(-.22,-.65,.52),.23,steel,16)
box('Saw motor casing',(.03,-.5,.32),(.24,.26,.22),rust)
barrel(1.36,.72);crate(-1.4,-.25);complete('LumberMill')
report=json.load(open(os.path.join(root,'Tools/Art/ComicOutpostBudget.json')))
for model in models:
 assert len(model.data.polygons)<4000
 bake(model,model.name,1024);export(model.name+'.fbx',[model],False)
 report[model.name]={'triangles':len(model.data.polygons),'materials':1,'texture':1024}
 print('PASS mill/clinic',model.name,report[model.name],flush=True)
bpy.context.preferences.filepaths.save_version=0
for im in bpy.data.images:
 if im.filepath:im.filepath=bpy.path.relpath(im.filepath,start=os.path.join(root,'Tools/Art'))
sample=os.path.join(root,'Tools/Art/LumberMillAndClinic.blend');bpy.ops.wm.save_as_mainfile(filepath=sample)
bpy.ops.wm.open_mainfile(filepath=os.path.join(root,'Tools/Art/ComicOutpost.blend'))
old=bpy.data.objects.get('Hospital');bpy.data.objects.remove(old,do_unlink=True)
with bpy.data.libraries.load(sample,link=False) as (available,loaded):loaded.objects=['Hospital','LumberMill']
for obj in loaded.objects:bpy.context.collection.objects.link(obj)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(root,'Tools/Art/ComicOutpost.blend'))
open(os.path.join(root,'Tools/Art/ComicOutpostBudget.json'),'w').write(json.dumps(report,indent=2)+'\n')
