"""Original comic wasteland kit: low-poly models, painted atlases and fixed-view sprite renders.
Blender -b -t 4 --python-exit-code 1 --python this_file -- repo
Preserves existing asset paths/GUIDs and gameplay footprints. No reference-image pixels used.
"""
import bpy,sys,os,ast,math,random,json
from mathutils import Vector
root=sys.argv[sys.argv.index('--')+1];out=os.path.join(root,'Assets/Ash/Resources/Art');parts=[];models=[]
common=os.path.join(root,'Tools/Art/build_visual_slice.py');tree=ast.parse(open(common).read());names={'finish','join','bake','export'}
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in names],type_ignores=[]),common,'exec'),globals())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8;scene.cycles.use_denoising=False;scene.render.bake.margin=8;scene.world.color=(.26,.25,.22)
scene.view_settings.view_transform='Standard';scene.view_settings.look='Medium High Contrast' if 'Medium High Contrast' in [i.identifier for i in scene.bl_rna.properties['view_settings'].fixed_type.properties['look'].enum_items] else 'None'
random.seed(227)
def paint(name,color,grain=False):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links;p=n.get('Principled BSDF');p.inputs['Roughness'].default_value=1;p.inputs['Specular IOR Level'].default_value=0
 geo=n.new('ShaderNodeNewGeometry');scale=n.new('ShaderNodeVectorMath');scale.operation='MULTIPLY';scale.inputs[1].default_value=(3,35,3) if grain else (9,9,9);l.new(geo.outputs['Position'],scale.inputs[0]);noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=1;noise.inputs['Detail'].default_value=2;l.new(scale.outputs[0],noise.inputs['Vector'])
 ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.interpolation='CONSTANT'
 for i,(at,factor) in enumerate([(0,.72),(.38,.88),(.58,1.02),(.76,1.12)]):
  e=ramp.color_ramp.elements[i] if i<2 else ramp.color_ramp.elements.new(at);e.position=at;e.color=tuple(min(1,c*factor) for c in color)+(1,)
 l.new(noise.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs[0],p.inputs['Base Color']);return m
ink=paint('Ink seams',(.055,.047,.037));wood=paint('Dry painted timber',(.32,.245,.165),True);pale=paint('Bleached plank highlights',(.43,.35,.235),True);rust=paint('Oxide repair sheet',(.39,.23,.15));steel=paint('Dull blue grey tin',(.27,.30,.29));olive=paint('Faded olive tin',(.29,.31,.23));cloth=paint('Old khaki canvas',(.49,.45,.33));patch=paint('Canvas repair',(.37,.345,.265));dirt=paint('Packed dirt footing',(.30,.25,.17));green=paint('Desaturated crop leaves',(.23,.31,.13));red=paint('Faded red trade and medical paint',(.42,.13,.09));white=paint('Old ivory paint',(.63,.59,.45))
def box(name,p,size,m):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.scale=size;return finish(o,name,m)
def rod(name,a,b,r,m,verts=6):
 d=Vector(b)-Vector(a);bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2);o=bpy.context.object;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();return finish(o,name,m)
def sphere(name,p,size,m):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=p);o=bpy.context.object;o.scale=size;return finish(o,name,m)
def crate(x,y,z=.25):
 box('Supply crate',(x,y,z),(.32,.30,.36),wood)
 for xx in (-.11,.11):box('Crate ink strap',(x+xx,y-.153,z),(.025,.012,.37),ink)
def barrel(x,y,z=.27):
 rod('Salvaged drum',(x,y,z-.2),(x,y,z+.2),.13,olive,8)
 for zz in (-.13,.13):rod('Dark drum hoop',(x,y,z+zz-.013),(x,y,z+zz+.013),.139,ink,8)
def roof(w,d,eave,rise):
 # Different sheets overlap; geometric seams survive miniature rendering.
 for side in (-1,1):
  cols=max(3,round(w/.55))
  for col in range(cols):
   for row in range(2):
    x0=-w/2+col*w/cols;x1=x0+w/cols+.015;t0=max(0,row/2-.015);t1=min(1,(row+1)/2+.015)
    verts=[]
    for t in (t0,t1):
     for k in range(9):
      x=x0+(x1-x0)*k/8;rib=.011*(k%2);verts.append((x,side*(.02+t*d/2),eave+rise*(1-t)+rib+.014*((col+row)%2)))
    faces=[(k,k+1,10+k,9+k) if side==1 else (9+k,10+k,k+1,k) for k in range(8)]
    mesh=bpy.data.meshes.new('Corrugated tin patch');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Overlapping corrugated repair sheet',mesh);bpy.context.collection.objects.link(o);finish(o,o.name,[rust,steel,olive,patch][random.randrange(4)])
    # Dark panel borders read like comic linework, not shiny metal ribs.
    for x in (x0,x1):rod('Roof panel ink seam',(x,side*(.02+t0*d/2),eave+rise*(1-t0)+.026),(x,side*(.02+t1*d/2),eave+rise*(1-t1)+.026),.009,ink,4)
 for side in (-1,1):rod('Roof eave dark trim',(-w/2,side*d/2,eave),(w/2,side*d/2,eave),.027,ink,4)
 rod('Roof ridge cap',(-w/2,0,eave+rise+.027),(w/2,0,eave+rise+.027),.035,steel,6)
def shell(w,d,h,stories=1):
 box('Thin dirt footing',(0,0,.065),(w+.14,d+.14,.08),dirt)
 box('Dark interior and plank gaps',(0,0,.12+h/2),(w,d,h),ink)
 for story in range(stories):
  for side in (-1,1):
   count=max(5,round(w/.27))
   for i in range(count):
    x=-w/2+(i+.5)*w/count;box('Individual salvaged wall board',(x,side*(d/2+.017),.12+(story+.5)*h/stories),(w/count-.025,.055,h/stories-.035),pale if i%5==0 else wood)
   for yy in range(max(4,round(d/.28))):
    count=max(4,round(d/.28));y=-d/2+(yy+.5)*d/count;box('Side wall board',(side*(w/2+.017),y,.12+(story+.5)*h/stories),(.055,d/count-.025,h/stories-.035),wood if yy%3 else pale)
 for z in (.16,h*.51+.12,h+.12):
  for side in (-1,1):
   box('Visible timber wall rail',(0,side*(d/2+.06),z),(w+.14,.09,.095),pale)
   box('Side timber wall rail',(side*(w/2+.06),0,z),(.09,d+.08,.095),wood)
 for x in (-w/2,0,w/2):
  for y in (-d/2,d/2):box('Structural post',(x,y,.12+h/2),(.12,.12,h+.10),wood)
 # Small boarded side windows keep every facade readable in the fixed view.
 for side in (-1,1):
  for story in range(stories):
   for y in (-d*.26,d*.26):
    z=.12+(story+.62)*h/stories;x=side*(w/2+.09)
    box('Side window ink recess',(x,y,z),(.035,.42,.38),ink)
    for dz in (-.21,.21):box('Side window timber frame',(x+side*.025,y,z+dz),(.045,.49,.045),pale)
    rod('Boarded side window',(x+side*.05,y-.2,z-.14),(x+side*.05,y+.2,z+.14),.025,wood,4)
 # Closed gables keep roof volumes solid from the fixed isometric camera.
 for side in (-1,1):
  verts=[(side*w/2,-d/2,h+.12),(side*w/2,d/2,h+.12),(side*w/2,0,h+.12+d*.25)]
  mesh=bpy.data.meshes.new('Timber gable');mesh.from_pydata(verts,[],[(0,1,2)] if side==1 else [(2,1,0)]);mesh.update();o=bpy.data.objects.new('Timber gable',mesh);bpy.context.collection.objects.link(o);finish(o,o.name,wood)
 return h+.12

def opening(w,d,h,double=False):
 width=.68 if not double else .96;y=-d/2-.092
 box('Door opening',(0,y,.69),(width,.035,1.12),ink)
 for side in (-1,1):box('Door frame',(side*(width/2+.035),y-.022,.69),(.07,.075,1.18),pale)
 box('Door lintel',(0,y-.02,1.30),(width+.17,.08,.10),wood)
 box('Half open salvaged door',(-width*.25,y-.07,.68),(width*.45,.045,1.03),wood)
 for x in (-w*.30,w*.30):
  if w<1.8 and x<0:continue
  z=min(h*.67,1.6);box('Dark window recess',(x,y,z),(.47,.04,.44),ink)
  for zz in (-.16,.16):box('Window frame',(x,y-.04,z+zz),(.52,.065,.045),pale)
  rod('Diagonal boarded window',(x-.22,y-.08,z-.15),(x+.22,y-.08,z+.15),.035,wood,4)
 for i in range(2):box('Uneven wooden step',(0,-d/2-.18-i*.10,.13-i*.035),(width+.25,.24,.09),wood)
def canopy(w,d,z):
 for x in (-w*.36,w*.36):rod('Rope tied awning support',(x,-d/2-.36,.08),(x,-d/2-.36,z-.15),.025,wood)
 o=box('Sagging shade canvas',(0,-d/2-.18,z),(w*.82,.40,.028),cloth);o.rotation_euler.x=.19
 for x in (-w*.3,w*.3):rod('Awning ink edge',(x,-d/2+.02,z+.05),(x,-d/2-.39,z-.04),.012,ink,4)
 box('Sewn canvas patch',(-w*.18,-d/2-.20,z+.02),(.28,.24,.018),patch)
def complete(name):
 obj=join(name,parts[:]);parts.clear();models.append(obj);assert len(obj.data.polygons)<4000,(name,len(obj.data.polygons));return obj
# Main two-storey command hall: the silhouette and patchwork roof carry the identity.
w,d,h=3.50,2.65,2.50;e=shell(w,d,h,2);opening(w,d,h,True);roof(w+.30,d+.28,e,.78);canopy(2.3,d,1.48)
for x in (-1.25,1.25):
 box('Command banner',(x,-d/2-.12,2.04),(.31,.02,.65),patch)
 for z in (1.7,2.5):rod('Diagonal frame brace',(x-.32,-d/2-.10,z-.18),(x+.32,-d/2-.10,z+.18),.032,wood,4)
for x,y in [(-1.98,.7),(1.98,-.4)]:barrel(x,y)
for i in range(5):sphere('Hand stacked sandbag',(-1.1+i*.40,-1.92,.18),(.23,.11,.12),cloth)
complete('Headquarters')
for name,w,d,h in [('Workshop',2.55,2.03,1.60),('Barracks',2.25,1.75,1.48),('OutpostHut',1.63,1.35,1.15)]:
 e=shell(w,d,h);opening(w,d,h);roof(w+.24,d+.20,e,d*.25);canopy(w,d,min(h,1.5))
 for i in range(2):barrel(-w*.39+i*.28,d*.35)
 if name=='Workshop':
  for i in range(3):o=box('Leaning salvage sheet',(w*.43,-.12+i*.16,.65),(.035,.35,.8),[rust,steel,olive][i]);o.rotation_euler.y=-.15
 if name=='Barracks':box('Barracks faded banner',(.76,-d/2-.12,1.0),(.28,.025,.50),red)
 complete(name)
# Clinic: patched canvas tent with a painted medical sign, not an urban concrete hospital.
w,d=2.15,1.80
box('Tent plinth',(0,0,.06),(2.65,2.35,.06),dirt)
for x in (-w/2,w/2):
 for y in (-d/2,d/2):rod('Tent timber pole',(x,y,.1),(x,y,1.30),.025,wood)
box('Tent interior',(0,0,.65),(w,d,1.1),ink)
for x in (-w/2,w/2):box('Stitched side canvas',(x,0,.70),(.035,d,1.16),cloth)
box('Rear canvas',(0,d/2,.70),(w,.035,1.16),patch)
for side in (-1,1):
 o=box('Tent pitched canvas',(side*w*.25,0,1.47),(w*.63,d+.16,.035),cloth);o.rotation_euler.y=side*.49
 box('Front hanging canvas',(side*.78,-d/2-.035,.68),(.55,.035,1.08),cloth)
 for y in (-d*.28,d*.3):rod('Tent stitched roof edge',(side*w*.53,y,1.18),(0,y,1.78),.009,ink,4)
box('Medical sign',(0,-d/2-.06,1.39),(.38,.035,.33),white);box('Medical cross horizontal',(0,-d/2-.085,1.39),(.26,.016,.065),red);box('Medical cross vertical',(0,-d/2-.086,1.39),(.065,.016,.26),red)
crate(-1.08,-.95);barrel(1.12,.50);complete('Hospital')
# Well: masonry ring, recycled timber roof and a working-looking hand pump.
box('Well dirt',(0,0,.045),(2.65,2.3,.06),dirt)
for i in range(10):
 a=i*math.tau/10;o=box('Well ring block',(math.cos(a)*.50,math.sin(a)*.50,.42),(.32,.19,.34),pale);o.rotation_euler.z=a+math.pi/2
rod('Dark well opening',(0,0,.37),(0,0,.39),.40,ink,12)
for x in (-.76,.76):rod('Well timber post',(x,0,.10),(x,0,1.55),.065,wood)
roof(1.94,1.55,1.5,.46);rod('Well windlass',(-.8,0,1.0),(.9,0,1.0),.065,steel);rod('Well rope',(0,0,.4),(0,0,1.0),.016,ink);rod('Pump crank',(.91,0,1.0),(.91,-.27,1.0),.025,wood)
barrel(.97,.75);complete('WaterStation')
# Farm: small seed shed and planted earth rows with sticks instead of a bright plastic greenhouse.
e=shell(1.1,.85,.85);opening(1.1,.85,.85);roof(1.32,1.07,e,.27)
for x in (-1.1,1.1):
 for y in (-1.0,1.0):rod('Crop fence post',(x,y,.08),(x,y,.53),.025,wood)
 rod('Farm fence',(x,-1,.39),(x,1,.39),.016,wood)
for row in range(3):
 y=-.65+row*.44;box('Cultivated crop bed',(0,y,.10),(1.65,.24,.12),dirt)
 for i in range(6):
  x=-.73+i*.28;rod('Crop seedling stem',(x,y,.15),(x,y,.35),.009,green,4)
  verts=[];faces=[]
  for leaf in range(4):
   a=leaf*math.pi/2+.3;dx=math.cos(a)*.12;dy=math.sin(a)*.12;j=len(verts)
   verts.extend([(x,y,.24),(x+dx*.5-dy*.27,y+dy*.5+dx*.27,.30),(x+dx,y+dy,.38),(x+dx*.5+dy*.27,y+dy*.5-dx*.27,.30)]);faces.extend([(j,j+2,j+1),(j,j+3,j+2)])
  mesh=bpy.data.meshes.new('Painted crop leaves');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Crop seedling leaves',mesh);bpy.context.collection.objects.link(o);finish(o,o.name,green)
# Put the seed shed behind the garden rows.
for o in parts:
 if 'Crop' not in o.name and 'Farm' not in o.name and 'Cultivated' not in o.name and 'seedling' not in o.name:o.location.y+=.55
complete('FoodGarden')
# Trading stall is a dedicated asset, while retaining the existing Market plot ID.
e=shell(1.8,1.0,1.1);roof(2.02,1.2,e,.3)
for x in (-1.1,1.1):rod('Trading canopy post',(x,-.95,.05),(x,-.95,1.55),.035,wood)
o=box('Faded red market awning',(0,-.68,1.58),(2.5,1.05,.035),red);o.rotation_euler.x=.12
box('Market counter',(0,-1,.46),(2.25,.42,.72),wood)
for i in range(4):crate(-.8+i*.53,-.95,.97)
barrel(1.10,.35);complete('Market')
# The perimeter tower has the same timber/patchwork style.
for x in (-.43,.43):
 for y in (-.43,.43):rod('Timber lookout leg',(x,y,.05),(x*.8,y*.8,2.4),.055,wood)
for side in (-1,1):
 rod('Lookout diagonal brace',(side*.43,-.43,.6),(side*.35,.35,2.25),.026,wood)
 rod('Lookout crossing brace',(side*.43,.43,.6),(side*.35,-.35,2.25),.026,wood)
box('Lookout platform',(0,0,2.37),(1.12,1.12,.10),wood)
for x in (-.48,.48):
 for y in (-.48,.48):rod('Roof support',(x,y,2.4),(x,y,3.12),.026,wood)
roof(1.45,1.45,3.05,.36)
for y in (-.5,.5):box('Lookout railing',(0,y,2.65),(1.12,.06,.10),pale)
for x in (-.18,.18):rod('Tower ladder',(x,-.57,.08),(x,-.57,2.4),.016,wood)
for i in range(10):rod('Ladder rung',(-.18,-.57,.2+i*.22),(.18,-.57,.2+i*.22),.014,wood)
complete('OutpostWatchtower')
# Existing source exports preserve filenames and existing meta GUIDs.
report={}
for model in models:
 bake(model,model.name,1024);export(model.name+'.fbx',[model],False);report[model.name]={'triangles':len(model.data.polygons),'materials':1,'texture':1024}
 print('PASS comic kit export',model.name,report[model.name],flush=True)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(root,'Tools/Art/ComicOutpost.blend'))
open(os.path.join(root,'Tools/Art/ComicOutpostBudget.json'),'w').write(json.dumps(report,indent=2)+'\n')
print('PASS comic outpost kit complete',flush=True)
