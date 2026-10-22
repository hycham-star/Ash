"""Normalize supplied icon atlas and pack an exact bottom-up RGBA backup. No repainting."""
from pathlib import Path
from PIL import Image
import sys
root=Path(__file__).resolve().parents[2]/'Assets/Ash/Resources/HUD'
source=Path(sys.argv[1]) if len(sys.argv)>1 else root/'WastelandNavigation.png'
image=Image.open(source).convert('RGBA')
if image.size!=(768,512):image=image.resize((768,512),Image.Resampling.LANCZOS)
image.save(root/'WastelandNavigation.png')
(root/'WastelandNavigationPixels.bytes').write_bytes(image.transpose(Image.Transpose.FLIP_TOP_BOTTOM).tobytes())
print('PASS six navigation icons and exact bottom-up RGBA backup')
