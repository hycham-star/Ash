"""Normalize production atlas dimensions and rebuild its import-independent RGBA backup.
Optional positional source PNG; no artwork is repainted by this packing step.
"""
from pathlib import Path
from PIL import Image
import sys
root=Path(__file__).resolve().parents[2]/'Assets/Ash/Resources/HUD'
source=Path(sys.argv[1]) if len(sys.argv)>1 else root/'WastelandHUD.png'
image=Image.open(source).convert('RGBA')
if image.size!=(1280,1280):image=image.resize((1280,1280),Image.Resampling.LANCZOS)
image.save(root/'WastelandHUD.png')
(root/'WastelandHUDPixels.bytes').write_bytes(image.transpose(Image.Transpose.FLIP_TOP_BOTTOM).tobytes())
print('PASS 16-cell HUD atlas and exact bottom-up RGBA backup')
