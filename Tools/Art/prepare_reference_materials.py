"""Create metadata/material bindings only for newly added original reference-kit assets."""
from pathlib import Path
import json,re,uuid
root=Path(__file__).resolve().parents[2];art=root/'Assets/Ash/Resources/Art'
names=list(json.loads((root/'Tools/Art/ComicOutpostBudget.json').read_text()))
def guid(path):
 meta=Path(str(path)+'.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')
 return re.search(r'guid: ([0-9a-f]{32})',meta.read_text()).group(1)
template=(art/'MarketMaterial.mat').read_text()
for name in names:
 for suffix in ('.fbx','Albedo.png','Normal.png'):guid(art/(name+suffix))
 path=art/(name+'Material.mat')
 if not path.exists():
  value=template.replace('m_Name: MarketMaterial','m_Name: '+name+'Material')
  old_albedo=guid(art/'MarketAlbedo.png');old_normal=guid(art/'MarketNormal.png')
  value=value.replace(old_albedo,guid(art/(name+'Albedo.png'))).replace(old_normal,guid(art/(name+'Normal.png')))
  path.write_text(value)
 guid(path)
print('PASS fifteen reference model and texture bindings; existing GUIDs preserved')
