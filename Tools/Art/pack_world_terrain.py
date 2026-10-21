"""Pack source terrain illustration as 1024px PNG and exact bottom-up RGBA data.
Optional positional source PNG. Packing only: does not paint or modify the design.
"""
from pathlib import Path
from PIL import Image
import sys
root=Path(__file__).resolve().parents[2]/'Assets/Ash/Resources/Map'
source=Path(sys.argv[1]) if len(sys.argv)>1 else root/'WastelandTerrain.png'
image=Image.open(source).convert('RGBA')
if image.size!=(1024,1024):image=image.resize((1024,1024),Image.Resampling.LANCZOS)
image.save(root/'WastelandTerrain.png')
(root/'WastelandTerrainPixels.bytes').write_bytes(image.transpose(Image.Transpose.FLIP_TOP_BOTTOM).tobytes())
print('PASS wasteland terrain source and exact bottom-up RGBA backup')
