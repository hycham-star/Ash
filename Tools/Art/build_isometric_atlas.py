"""Combine original isometric landmarks and home icon, retaining a raw-pixel fallback."""
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[2]/'Assets/Ash/Resources/Map'
atlas=Image.new('RGBA',(1152,768))
for i,name in enumerate(('Food','Water','Scrap','Lumber','Camp','Settlement')):
 image=Image.open(root/(name+'.png')).convert('RGBA')
 assert image.size==(384,384)
 atlas.alpha_composite(image,((i%3)*384,(i//3)*384))
atlas.save(root/'IsometricOverworldAtlas.png')
(root/'IsometricOverworldAtlasPixels.bytes').write_bytes(atlas.transpose(Image.Transpose.FLIP_TOP_BOTTOM).tobytes())
print('PASS isometric atlas + bottom-up RGBA backup')
