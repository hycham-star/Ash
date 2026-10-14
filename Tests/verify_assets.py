"""Check tracked asset metadata and references without claiming Unity import validation."""
from pathlib import Path
import json
import re

root = Path(__file__).resolve().parents[1]
assets = root / "Assets"
index = {}
for meta in assets.rglob("*.meta"):
    match = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(), re.MULTILINE)
    assert match, f"Invalid GUID in {meta}"
    guid = match.group(1)
    assert guid not in index, f"Duplicate GUID: {guid}"
    target = Path(str(meta)[:-5])
    assert target.exists(), f"Orphan metadata: {meta}"
    index[guid] = target

for target in assets.rglob("*"):
    if target.name.startswith(".") or target.suffix == ".meta":
        continue
    assert Path(str(target) + ".meta").exists(), f"Missing metadata: {target}"

references = 0
for target in list(assets.rglob("*.asset")) + list(assets.rglob("*.unity")):
    text = target.read_text()
    for guid in re.findall(r"guid: ([0-9a-f]{32})", text):
        assert guid in index, f"Broken GUID reference in {target}: {guid}"
        references += 1
    for script_guid in re.findall(r"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32}), type: 3\}", text):
        script = index[script_guid]
        assert script.suffix == ".cs", f"Script reference points to a non-script: {script}"
        assert re.search(r"class\s+" + re.escape(script.stem) + r"\b", script.read_text()), f"Script class and filename mismatch: {script}"

scene = (assets / "Ash/Scenes/Phase1Test.unity").read_text()
ids = re.findall(r"^--- !u!\d+ &(\d+)$", scene, re.MULTILINE)
assert len(ids) == len(set(ids)), "Duplicate scene object IDs"
for object_id in re.findall(r"\{fileID: (\d+)\}", scene):
    assert object_id == "0" or object_id in ids, f"Missing local scene object: {object_id}"
assert "gameManager: {fileID: 1002}" in scene, "Missing explicit manager reference"
assert "RuntimeInitializeOnLoadMethod" not in "\n".join(p.read_text() for p in assets.rglob("*.cs")), "Global automatic launcher remains"
assert not (assets / "Ash/Scripts/Settlement.cs").exists(), "Duplicate legacy resource/combat system remains"
manifest = json.loads((root / "Packages/manifest.json").read_text())
assert manifest["dependencies"]["com.unity.ugui"] == "2.0.0", "Unexpected UI dependency"
print(f"PASS asset metadata: {len(index)} unique GUIDs, {references} resolved references, scene object links, no legacy launcher.")
print("Unity compilation, import, and Play mode require the Unity Editor and are not covered by this check.")
