"""Pack source 3D sprite renders without painting or editing reference-image pixels."""
from pathlib import Path
from PIL import Image
import json
root=Path(__file__).resolve().parents[2];sprites=root/'Tools/Art/ComicSprites';maps=root/'Assets/Ash/Resources/Map'
names=list(json.loads((root/'Tools/Art/ComicOutpostBudget.json').read_text()))
contact=Image.new('RGBA',(2048,2048),(0,0,0,0))
for i,name in enumerate(names):
 image=Image.open(sprites/(name+'.png')).convert('RGBA');assert image.size==(512,512)
 contact.alpha_composite(image,((i%4)*512,(i//4)*512))
contact.save(root/'Tools/Art/ComicBuildings_Preview.png')
for target,source in [('Food','FoodGarden'),('Water','WaterStation'),('Scrap','Scrapyard'),('Lumber','LumberYard'),('Camp','EnemyCamp'),('Settlement','Headquarters')]:
 image=Image.open(sprites/(source+'.png')).convert('RGBA').resize((384,384),Image.Resampling.LANCZOS);image.save(maps/(target+'.png'))
exec(compile((root/'Tools/Art/build_isometric_atlas.py').read_text(),str(root/'Tools/Art/build_isometric_atlas.py'),'exec'),{'__file__':str(root/'Tools/Art/build_isometric_atlas.py')})
print('PASS fifteen-sprite contact sheet and six reference map cells/raw backup')
