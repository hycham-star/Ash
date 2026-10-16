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
for target in list(assets.rglob("*.asset")) + list(assets.rglob("*.unity")) + list(assets.rglob("*.mat")):
    text = target.read_text()
    for guid in re.findall(r"guid: ([0-9a-f]{32})", text):
        assert guid in index, f"Broken GUID reference in {target}: {guid}"
        references += 1
    for script_guid in re.findall(r"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32}), type: 3\}", text):
        script = index[script_guid]
        assert script.suffix == ".cs", f"Script reference points to a non-script: {script}"
        assert re.search(r"class\s+" + re.escape(script.stem) + r"\b", script.read_text()), f"Script class and filename mismatch: {script}"

for scene_path in assets.rglob("*.unity"):
    scene = scene_path.read_text()
    ids = re.findall(r"^--- !u!\d+ &(\d+)$", scene, re.MULTILINE)
    assert len(ids) == len(set(ids)), f"Duplicate scene object IDs: {scene_path}"
    for object_id in re.findall(r"\{fileID: (\d+)\}", scene):
        assert object_id == "0" or object_id in ids, f"Missing local scene object: {object_id} in {scene_path}"
    assert "gameManager: {fileID: 1002}" in scene, f"Missing explicit manager reference: {scene_path}"
    roster = scene.split("  heroDefinitions:\n", 1)[1].split("  unitDefinitions:", 1)[0]
    roster_guids = re.findall(r"guid: ([0-9a-f]{32})", roster)
    assert len(roster_guids) == 8 and len(set(roster_guids)) == 8, f"Expected eight distinct hero assets: {scene_path}"
assert (assets / "Ash/Scenes/HeroArmyTest.unity").exists(), "Missing Hero Army test scene"
battle = (assets / "Ash/Scenes/BattleScene.unity").read_text()
encounters = battle.split("  encounters:\n", 1)[1].split("---", 1)[0]
assert len(set(re.findall(r"guid: ([0-9a-f]{32})", encounters))) == 3, "Battle scene needs three distinct encounter references"
settlement_path = assets / "Ash/Scenes/SettlementScene.unity"
assert settlement_path.exists(), "Missing original settlement scene"
settlement = settlement_path.read_text()
settlement_encounters = settlement.split("  encounters:\n", 1)[1].split("---", 1)[0]
assert len(set(re.findall(r"guid: ([0-9a-f]{32})", settlement_encounters))) == 3, "Settlement must reuse three PvE encounters"
assert "  enableLocalPersistence: 1" in settlement, "Settlement persistence must be enabled"
for scene_name in ("BattleScene", "HeroArmyTest", "Phase1Test"):
    assert "  enableLocalPersistence: 1" not in (assets / f"Ash/Scenes/{scene_name}.unity").read_text(), "Debug scenes must not overwrite real progress"
assert "RuntimeInitializeOnLoadMethod" not in "\n".join(p.read_text() for p in assets.rglob("*.cs")), "Global automatic launcher remains"
assert not (assets / "Ash/Scripts/Settlement.cs").exists(), "Duplicate legacy resource/combat system remains"
assert (assets / "Ash/Resources/AshSettlement.shader").exists(), "Missing bundled settlement shader for player builds"
manifest = json.loads((root / "Packages/manifest.json").read_text())
assert manifest["dependencies"]["com.unity.ugui"] == "2.0.0", "Unexpected UI dependency"
assert manifest["dependencies"]["com.unity.modules.jsonserialize"] == "1.0.0", "Missing native JSON serialization module"
catalog = (assets / "Ash/Scripts/Presentation/HeroVisualCatalog.cs").read_text()
bindings = re.findall(r'\{"([a-z-]+)","([A-Za-z]+)"\}', catalog)
hero_ids = {re.search(r'^  id: (.+)$', p.read_text(), re.MULTILINE).group(1) for p in (assets / "Ash/Data/Heroes").glob("*.asset")}
assert len(bindings) == 8 and {key for key, _ in bindings} == hero_ids, "Hero visual bindings differ from authored IDs"
assert len({name for _, name in bindings}) == 8, "Hero visual assets must be distinct"
for name in [value for _, value in bindings] + ["Engineer", "Sentry"]:
    for suffix in (".fbx", "Albedo.png", "Normal.png", "Material.mat"):
        assert (assets / ("Ash/Resources/Art/" + name + suffix)).exists(), f"Missing character art: {name + suffix}"
assert "SurvivorView" not in (assets / "Ash/Scripts/Presentation/SettlementView.cs").read_text(), "Settlement still creates legacy cubic characters"
print(f"PASS asset metadata: {len(index)} unique GUIDs, {references} resolved references, scene object links, no legacy launcher.")
print("Unity compilation, import, and Play mode require the Unity Editor and are not covered by this check.")
