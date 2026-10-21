"""Actual bundled UI/terrain/landmark asset preview, not a Unity screenshot.
Seed-42 positions are exported from ScavengingService.Generate and WorldMapService.GenerateCamps.
Blender -b -t 4 --python-exit-code 1 --python this_file -- repo
"""
import bpy,sys,os,json
root=sys.argv[sys.argv.index('--')+1]
# Reuse established UI mesh/UV/font helpers without executing its settlement scene or render.
sourcefile=os.path.join(root,'Tools/Art/render_wasteland_hud_preview.py');source=open(sourcefile).read()
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8;scene.cycles.use_denoising=False
scene.view_settings.view_transform='Standard';scene.world.color=(.02,.015,.01)
bpy.ops.object.camera_add(location=(0,0,0));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=36
scene.render.resolution_x=540;scene.render.resolution_y=960;scene.render.resolution_percentage=100
start=source.index('atlas=bpy.data.images.load');end=source.index('frame((4,854')
exec(compile(source[start:end],sourcefile,'exec'),globals())
# Reference the real sixteen-cell HUD at its actual portrait screen positions.
header=source[end:source.index('frame((4,105')];exec(compile(header,sourcefile,'exec'),globals())
terrain_image=bpy.data.images.load(os.path.join(root,'Assets/Ash/Resources/Map/WastelandTerrain.png'));terrain_mat=unlit('Bundled painted desert',image=terrain_image)
landmarks=bpy.data.images.load(os.path.join(root,'Assets/Ash/Resources/Map/IsometricOverworldAtlas.png'));landmark_mat=unlit('Actual isometric landmarks',image=landmarks)
black=unlit('Map interface background',(.075,.052,.035))
mesh('World map background',[(8,104,532,851,0,0,1,1)],black,-1.18)
frame((8,104,532,851),10);frame((14,803,466,845),7);text('WASTELAND WORLD MAP',240,824,20);frame((474,803,526,845),7);text('X',500,824,18)
frame((14,771,526,800),7);text('Heartland  •  Lv 1  •  X 0 / Y 0',270,786,13)
# Fit view: the map's square area follows the viewport's smaller dimension.
viewport=(16,304,524,765);side=min(viewport[2]-viewport[0],viewport[3]-viewport[1]);span=side-32;cx=270;cy=(viewport[1]+viewport[3])/2
mesh('World terrain painting',[(cx-span/2,cy-span/2,cx+span/2,cy+span/2,0,0,1,1)],terrain_mat,-1.115)
fixture=json.load(open(os.path.join(root,'Tools/Art/WorldMapPreviewFixture.json')))
def landmark(cell,x,y,w,h):
 u=(cell%3*384+2)/1152;v=((1-cell//3)*384+2)/768
 return mesh('Interactive landmark asset',[(x-w/2,y-h/2,x+w/2,y+h/2,u,v,u+380/1152,v+380/768)],landmark_mat,-1.09)
for site in fixture['Sites']:
 x=cx+site['X']*span/201;y=cy+site['Z']*span/201;cell={'Food':0,'Water':1,'Scrap':2,'Lumber':3}[site['Resource']];landmark(cell,x,y+4,36,34)
for camp in fixture['Camps']:landmark(4,cx+camp['X']*span/201,cy+camp['Z']*span/201+3,35,31)
landmark(5,cx,cy+6,42,42)
# Overview references the same source terrain. Its viewfinder is full extent in Fit mode.
frame((414,665,520,761),7);mesh('Live overview artwork',[(422,677,512,753,0,0,1,1)],terrain_mat,-1.105);icon(15,467,715,16)
for rect in [(422,677,512,678),(422,752,512,753),(422,677,423,753),(511,677,512,753)]:mesh('Overview viewfinder',[(*rect,0,0,1,1)],cream,-1.07)
def button(label,x0,y0,x1,y1,size=14):frame((x0,y0,x1,y1),7);text(label,(x0+x1)/2,(y0+y1)/2,size)
for label,x0,x1 in [('−',16,110),('+',116,214),('FIT',220,369),('LOCATE',375,524)]:button(label,x0,260,x1,298)
button('‹',16,222,66,256);button('Mara Flint • 0 fighters • HOME',72,222,468,256,13);button('›',474,222,524,256)
button('−',16,178,86,214);button('Farm [−86,48] • 1 / 10 civilians',92,178,448,214,12);button('+',454,178,524,214)
text('Drag to explore • Tap a resource site',270,160,12,muted)
for i,label in enumerate(['COLLECT','GATHER','RECALL','AUTO']):button(label,16+i*128,109,140+i*128,145,13)
scene.render.filepath=os.path.join(root,'Tools/Art/WorldMapHUD_Preview.png');bpy.ops.render.render(write_still=True)
print('PASS world map asset/layout Blender preview',flush=True)
