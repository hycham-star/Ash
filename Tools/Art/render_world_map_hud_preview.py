"""Blender asset/layout preview of full-screen map and shared HUD, not a Unity screenshot.
Uses actual bundled textures and seed-42 generator coordinates. No painted UI mock screenshot.
Blender -b -t 4 --python-exit-code 1 --python this_file -- repo
"""
import bpy,sys,os,json
root=sys.argv[sys.argv.index('--')+1]
sourcefile=os.path.join(root,'Tools/Art/render_wasteland_hud_preview.py');source=open(sourcefile).read()
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8;scene.cycles.use_denoising=False
scene.view_settings.view_transform='Standard';scene.world.color=(.02,.015,.01)
bpy.ops.object.camera_add(location=(0,0,0));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=36
scene.render.resolution_x=540;scene.render.resolution_y=960;scene.render.resolution_percentage=100
start=source.index('atlas=bpy.data.images.load');end=source.index('frame((4,854')
exec(compile(source[start:end],sourcefile,'exec'),globals())
# Shared resource ribbon and exact five-button navigation.
exec(compile(source[end:source.index('frame((4,105')],sourcefile,'exec'),globals())
terrain_image=bpy.data.images.load(os.path.join(root,'Assets/Ash/Resources/Map/WastelandTerrain.png'));terrain_mat=unlit('Bundled painted desert',image=terrain_image)
landmarks=bpy.data.images.load(os.path.join(root,'Assets/Ash/Resources/Map/IsometricOverworldAtlas.png'));landmark_mat=unlit('Actual isometric landmarks',image=landmarks)
black=unlit('World background',(.18,.11,.055))
mesh('Opaque world playfield',[(0,0,540,960,0,0,1,1)],black,-1.18)
# Actual portrait viewport below the 106px ribbon and above the 99px footer.
span=755-32;cx=270;cy=(99+854)/2;left=cx-span/2;bottom=cy-span/2
mesh('Full-screen world terrain',[(0,bottom,540,bottom+span,(0-left)/span,0,(540-left)/span,1)],terrain_mat,-1.115)
fixture=json.load(open(os.path.join(root,'Tools/Art/WorldMapPreviewFixture.json')))
def landmark(cell,x,y,w,h):
 x0=max(0,x-w/2);x1=min(540,x+w/2);y0=max(99,y-h/2);y1=min(854,y+h/2)
 if x1<=x0 or y1<=y0:return
 u=(cell%3*384+2)/1152;v=((1-cell//3)*384+2)/768;du=380/1152;dv=380/768
 return mesh('Interactive landmark',[(x0,y0,x1,y1,u+(x0-x+w/2)/w*du,v+(y0-y+h/2)/h*dv,u+(x1-x+w/2)/w*du,v+(y1-y+h/2)/h*dv)],landmark_mat,-1.09)
for site in fixture['Sites']:
 x=cx+site['X']*span/201;y=cy+site['Z']*span/201;cell={'Food':0,'Water':1,'Scrap':2,'Lumber':3}[site['Resource']];landmark(cell,x,y+4,36,34)
for camp in fixture['Camps']:landmark(4,cx+camp['X']*span/201,cy+camp['Z']*span/201+3,35,31)
landmark(5,cx,cy+6,42,42)
base_frame=frame
def frame(rect,b=9):
 obj=base_frame(rect,b);obj.location.z=.015;return obj
text('Heartland • Lv 1 • X 0 / Y 0',204,835,12)
frame((422,744,528,840),7);mesh('Live overview',[(430,756,520,832,0,0,1,1)],terrain_mat,-1.095);icon(15,475,794,16)
for rect in [(441,756,509,757),(441,831,509,832),(441,756,442,832),(508,756,509,832)]:mesh('Viewfinder',[(*rect,0,0,1,1)],cream,-1.07)
frame((4,105,142,153),10);icon(15,34,129,32);text('HOME',99,129,17)
scene.render.filepath=os.path.join(root,'Tools/Art/WorldMapHUD_Preview.png');bpy.ops.render.render(write_still=True)
# Selected-site context preview: the card exists only after a site tap in the real UI.
# Put modal frames above landmark sprites, with their labels above both.
def frame(rect,b=9):
 obj=base_frame(rect,b);obj.location.z=.045;return obj
frame((8,161,532,421),10);text('WOODLAND  [48, 14]',237,392,19);text('Deposit 1000 • Workers 0 • Cargo 0',237,365,14);text('X',505,397,18)
for label,x0,x1 in [('‹',16,68),('Mara Flint • 0 fighters • HOME',74,466),('›',472,524)]:frame((x0,279,x1,313),7);text(label,(x0+x1)/2,296,13)
for label,x0,x1 in [('−',16,88),('1 / 10 civilians • Woodland',94,446),('+',452,524)]:frame((x0,235,x1,271),7);text(label,(x0+x1)/2,253,13)
text('Select team size, then COLLECT or GATHER',270,216,12)
for i,label in enumerate(['COLLECT','GATHER','RECALL','AUTO']):
 x0=16+i*128;frame((x0,166,x0+124,202),7);text(label,x0+62,184,13)
scene.render.filepath=os.path.join(root,'Tools/Art/WorldMapSelection_Preview.png');bpy.ops.render.render(write_still=True)
print('PASS full-screen world and context selection asset/layout previews',flush=True)
