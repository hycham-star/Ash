"""Render actual HUD art/layout over the existing default town asset scene in Blender.
This is an asset/layout preview, not a Unity screenshot. Run Blender -b -t 4 --python this_file -- repo.
"""
import bpy,sys,os
root=sys.argv[sys.argv.index('--')+1]
sourcefile=os.path.join(root,'Tools/Art/render_outpost_district_preview.py');source=open(sourcefile).read();exec(compile(source[:source.index('bpy.ops.render.render(write_still=True)')],sourcefile,'exec'),globals())
scene.render.resolution_x=540;scene.render.resolution_y=960;scene.render.resolution_percentage=100;camera.data.ortho_scale=36
atlas=bpy.data.images.load(os.path.join(root,'Assets/Ash/Resources/HUD/WastelandHUD.png'));title=bpy.data.fonts.load(os.path.join(root,'Assets/Ash/Resources/HUD/OutpostTitle.ttf'))
unit=36/960

def unlit(name,color=None,image=None):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links;n.clear();out=n.new('ShaderNodeOutputMaterial');emit=n.new('ShaderNodeEmission')
 if image:
  tex=n.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Linear';l.new(tex.outputs['Color'],emit.inputs['Color']);transparent=n.new('ShaderNodeBsdfTransparent');mix=n.new('ShaderNodeMixShader');l.new(tex.outputs['Alpha'],mix.inputs[0]);l.new(transparent.outputs[0],mix.inputs[1]);l.new(emit.outputs[0],mix.inputs[2]);l.new(mix.outputs[0],out.inputs['Surface'])
 else:emit.inputs['Color'].default_value=(*color,1);l.new(emit.outputs[0],out.inputs['Surface'])
 return m
skin=unlit('Actual HUD atlas',image=atlas);cream=unlit('Ivory UI lettering',(.88,.80,.65));muted=unlit('Muted lettering',(.65,.54,.36));gold=unlit('Hero XP gold',(.50,.29,.075))

def mesh(name,quads,material,z):
 vertices=[];faces=[];uvs=[]
 for x0,y0,x1,y1,u0,v0,u1,v1 in quads:
  start=len(vertices);vertices.extend([((x0-270)*unit,(y0-480)*unit,z),((x0-270)*unit,(y1-480)*unit,z),((x1-270)*unit,(y1-480)*unit,z),((x1-270)*unit,(y0-480)*unit,z)]);faces.append((start,start+1,start+2,start+3));uvs.extend([(u0,v0),(u0,v1),(u1,v1),(u1,v0)])
 data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.update();layer=data.uv_layers.new(name='HUD UV')
 for polygon in data.polygons:
  for loop in polygon.loop_indices:layer.data[loop].uv=uvs[data.loops[loop].vertex_index]
 obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj);obj.parent=camera;obj.visible_shadow=False;obj.data.materials.append(material);return obj

def frame(rect,b=9):
 x0,y0,x1,y1=rect;xx=[x0,x0+b,x1-b,x1];yy=[y0,y0+b,y1-b,y1];uu=[16/1280,80/1280,240/1280,304/1280];vv=[976/1280,1040/1280,1200/1280,1264/1280]
 return mesh('Riveted nine-slice panel',[(xx[x],yy[y],xx[x+1],yy[y+1],uu[x],vv[y],uu[x+1],vv[y+1]) for y in range(3) for x in range(3)],skin,-1.12)
def icon(cell,cx,cy,w,h=None):
 h=h or w;x0=(cell%4*320+4)/1280;y0=((3-cell//4)*320+4)/1280
 return mesh('Illustration '+str(cell),[(cx-w/2,cy-h/2,cx+w/2,cy+h/2,x0,y0,x0+312/1280,y0+312/1280)],skin,-1.09)
def text(body,x,y,size=16,material=cream):
 curve=bpy.data.curves.new('HUD label','FONT');curve.body=body;curve.font=title;curve.align_x='CENTER';curve.align_y='CENTER';curve.size=size*unit
 obj=bpy.data.objects.new('Text '+body,curve);bpy.context.collection.objects.link(obj);obj.parent=camera;obj.location=((x-270)*unit,(y-480)*unit,-1.06);obj.data.materials.append(material);obj.visible_shadow=False;return obj
frame((4,854,536,960),10);frame((8,840,79,956),9);icon(14,44,914,54,73);text('LEVEL 1',44,860,15)
resources=[('SCRAP','100',1),('LUMBER','0',2),('WATER','50',3),('FOOD','100',5),('FUEL','20',6),('ARMY','0/30',7)]
for i,(name,value,cell) in enumerate(resources):
 col=i%3;row=i//3;x0=(.16+col*.245)*540+4;x1=(.16+(col+1)*.245)*540-4;y1=955-row*47;y0=911-row*47
 frame((x0,y0,x1,y1),7);icon(cell,x0+(x1-x0)*.17,(y0+y1)/2,31);text(name,x0+(x1-x0)*.64,y1-10,9,muted);text(value,x0+(x1-x0)*.62,y0+16,19)
for cell,y in [(13,933),(15,886)]:frame((.898*540+4,y-22,536,y+22),7);icon(cell,513,y,32)
frame((4,0,536,99),10)
for i,(name,cell) in enumerate([('Heroes',7),('Barracks',9),('Build',8),('Hospital',10),('Raid',12)]):
 x0=i*108+4;x1=(i+1)*108-4;frame((x0,5,x1,95),9);icon(cell,(x0+x1)/2,61,57);text(name,(x0+x1)/2,20,16)
frame((4,105,.27*540-4,242),10);icon(11,74,183,98,96);text('WORLD MAP',73,123,16)
frame((.28*540+4,151,536,187),7);text('MARA FLINT  •  Lv 1  •  0/30',348,169,13)
mesh('XP track',[(160,144,533,148,0,0,1,1)],muted,-1.10)
frame((.28*540+4,100,536,140),7);text('Welcome to Ember Outpost',347,120,11,muted)
scene.render.filepath=os.path.join(root,'Tools/Art/WastelandHUD_Preview.png');bpy.ops.render.render(write_still=True)
print('PASS HUD asset/layout Blender preview',flush=True)
