"""Pack Blender-rendered source sprites; no painting or reference-image edits."""
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[2];sprites=root/'Tools/Art/ComicSprites';maps=root/'Assets/Ash/Resources/Map'
names=['Headquarters','Workshop','Barracks','Hospital','WaterStation','FoodGarden','OutpostHut','Market','OutpostWatchtower']
contact=Image.new('RGBA',(1536,1536),(0,0,0,0))
for i,name in enumerate(names):
 image=Image.open(sprites/(name+'.png')).convert('RGBA');assert image.size==(512,512)
 contact.alpha_composite(image,((i%3)*512,(i//3)*512))
contact.save(root/'Tools/Art/ComicBuildings_Preview.png')
for target,source in [('Food','FoodGarden'),('Water','WaterStation'),('Scrap','Workshop'),('Lumber','Lumber'),('Camp','Barracks'),('Settlement','Headquarters')]:
 image=Image.open(sprites/(source+'.png')).convert('RGBA').resize((384,384),Image.Resampling.LANCZOS);image.save(maps/(target+'.png'))
exec(compile((root/'Tools/Art/build_isometric_atlas.py').read_text(),str(root/'Tools/Art/build_isometric_atlas.py'),'exec'),{'__file__':str(root/'Tools/Art/build_isometric_atlas.py')})
print('PASS nine-sprite contact sheet and matching six-cell miniature map atlas')
