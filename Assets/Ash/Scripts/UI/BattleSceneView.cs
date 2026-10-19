using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ash
{
    [RequireComponent(typeof(GameManager))]
    public sealed class BattleSceneView : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private EncounterConfig[] encounters = Array.Empty<EncounterConfig>();
        private readonly List<NpcEncounter> camps = new List<NpcEncounter>();
        private int selectedHero, selectedCamp;
        private bool abilities = true;
        private Font font;
        private Text header, notice, encounterButton, abilityButton;
        private CombatResultsUI results;
        private VisualBattlePlayback playback;
        private ScrollRect resultsScroll;
        private GameObject presentation;

        private void Start()
        {
            if (gameManager == null) gameManager = GetComponent<GameManager>();
            if (!gameManager.IsInitialized || gameManager.Heroes.Count == 0 || encounters.Length == 0)
            { Debug.LogError("Battle scene requires an initialized hero roster and encounter assets.", this); enabled = false; return; }
            foreach (var config in encounters)
            {
                if (config == null) { Debug.LogError("Missing PvE encounter asset.", this); enabled = false; return; }
                camps.Add(config.CreateEncounter());
            }
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            presentation = new GameObject("PvE Test Presentation");
            BuildWorld(); BuildUI();
            gameManager.HeroArmies.Changed += Refresh;
            gameManager.Resources.Changed += Refresh;
            gameManager.Fragments.Changed += Refresh;
            Refresh();
            notice.text = "Use Balanced or Specialty to prepare fighters from reserve. Preview is safe; Fight sends wounded to hospital and applies rewards.";
        }
        private void OnDestroy()
        {
            if (gameManager != null && gameManager.IsInitialized)
            {
                gameManager.HeroArmies.Changed -= Refresh; gameManager.Resources.Changed -= Refresh; gameManager.Fragments.Changed -= Refresh;
            }
            if (presentation != null) Destroy(presentation);
        }
        private void Refresh()
        {
            var hero = gameManager.Heroes[selectedHero];
            var text = new StringBuilder("ASHLANDS • PvE AUTO-COMBAT\n");
            text.Append(HeroRarityColors.Text(hero.Definition.DisplayName+" • "+hero.Definition.Rarity,hero.Definition.Rarity)).Append(" Lv").Append(hero.Level).Append(" • XP ").Append(hero.Xp).Append('/').Append(hero.XpToNextLevel)
                .Append("\nArmy ").Append(hero.Army.TotalUnits).Append('/').Append(hero.Army.Capacity).Append(" • Power ").Append(hero.Army.Power.ToString("0.##"));
            text.Append("\n");
            foreach (var fighter in gameManager.Units) text.Append(fighter.Definition.Type).Append(':').Append(hero.Army.QuantityOf(fighter.Definition.Id, fighter.Level)).Append(' ');
            text.Append("\nFood ").Append(gameManager.Resources.Get(ResourceType.Food)).Append(" • Scrap ").Append(gameManager.Resources.Get(ResourceType.Scrap))
                .Append(" • Water ").Append(gameManager.Resources.Get(ResourceType.Water)).Append(" • Fuel ").Append(gameManager.Resources.Get(ResourceType.Fuel))
                .Append("\nFragments ").Append(gameManager.Fragments.Get(hero.Definition.Id)).Append(" • Selected hero only");
            header.text = text.ToString();
            encounterButton.text = "CAMP: " + camps[selectedCamp].Name;
            abilityButton.text = "ABILITY: " + (abilities ? "AUTO ON" : "OFF (TEST)");
        }
        private void SelectHero(int delta)
        { selectedHero = (selectedHero + delta + gameManager.Heroes.Count) % gameManager.Heroes.Count; results.Clear(); Refresh(); }
        private void PrepareArmy(bool specialty)
        {
            var hero = gameManager.Heroes[selectedHero];
            // Return existing survivors first; dead fighters are never recreated by this helper.
            var existing = new List<UnitStack>(hero.Army.Units);
            foreach (var stack in existing) gameManager.HeroArmies.RemoveFighters(hero.InstanceId, stack.Unit.Definition.Id, stack.Count);
            int remaining = hero.Army.Capacity;
            for (int i = 0; i < gameManager.Units.Count; i++)
            {
                var fighter = gameManager.Units[i];
                if (specialty && fighter.Definition.Type != hero.Definition.Specialization) continue;
                int requested = specialty ? remaining : hero.Army.Capacity / gameManager.Units.Count + (i < hero.Army.Capacity % gameManager.Units.Count ? 1 : 0);
                int quantity = Math.Min(requested, gameManager.HeroArmies.Available(fighter.Definition.Id));
                if (quantity <= 0) continue;
                gameManager.HeroArmies.AssignFighters(hero.InstanceId, fighter.Definition.Id, quantity);
                remaining -= quantity;
            }
            results.Clear(); Refresh();
            notice.text = "Army prepared from available reserve. Reset Test Session replenishes the debug fixture and clears all progress.";
        }
        private void Fight(bool apply)
        {
            if(playback!=null && playback.IsOpen)return;
            try
            {
                var hero = gameManager.Heroes[selectedHero];
                var result = apply ? gameManager.Battles.RunBattle(hero.InstanceId, camps[selectedCamp], abilities)
                    : gameManager.Battles.PreviewBattle(hero.InstanceId, camps[selectedCamp], abilities);
                if(playback==null)playback=gameObject.AddComponent<VisualBattlePlayback>();
                playback.Play(result,hero.Definition.Id,hero.Definition.DisplayName,camps[selectedCamp].Name,hero.Definition.Rarity,apply,()=>
                { results.Show(result,apply);resultsScroll.verticalNormalizedPosition=1;Refresh();notice.text=apply?"Battle settled once. Wounded sent to hospital; survivors stay assigned.":"Preview only. No losses, XP, resources, or fragments applied."; });
                if (apply) foreach (var error in gameManager.Battles.NotificationErrors) Debug.LogWarning("Battle observer error: " + error, this);
            }
            catch (Exception error) { notice.text = "Battle rejected: " + error.Message; }
        }
        private void BuildWorld()
        {
            var camera = new GameObject("Battle Camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(presentation.transform); camera.tag = "MainCamera";
            camera.transform.position = new Vector3(7, 10, -9); camera.transform.LookAt(Vector3.zero);
            camera.orthographic = true; camera.orthographicSize = 8;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
            camera.cullingMask = 0; // The playback owns its isolated arena camera.
        }
        private void BuildUI()
        {
            var canvas = new GameObject("Battle Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(presentation.transform); canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 960); scaler.matchWidthOrHeight = 0.5f;
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("Battle Input", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(presentation.transform);
            var root = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>(); root.SetParent(canvas.transform, false);
            var safe = Screen.safeArea; root.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            root.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height); root.offsetMin = root.offsetMax = Vector2.zero;
            header = Label(root, new Vector2(0, 1), Vector2.one, new Vector2(12, -156), new Vector2(-12, -12), 17);
            Button(root, "PREVIOUS HERO", 0, 0.5f, -204, -162, true, () => SelectHero(-1));
            Button(root, "NEXT HERO", 0.5f, 1, -204, -162, true, () => SelectHero(1));
            encounterButton = Button(root, "CAMP", 0, 0.5f, -250, -210, true, () => { selectedCamp = (selectedCamp + 1) % camps.Count; results.Clear(); Refresh(); });
            abilityButton = Button(root, "ABILITY", 0.5f, 1, -250, -210, true, () => { abilities = !abilities; results.Clear(); Refresh(); });
            Button(root, "BALANCED ARMY", 0, 0.333f, -295, -255, true, () => PrepareArmy(false));
            Button(root, "SPECIALTY ARMY", 0.333f, 0.666f, -295, -255, true, () => PrepareArmy(true));
            Button(root, "RESET TEST SESSION", 0.666f, 1, -295, -255, true, () => SceneManager.LoadScene(gameObject.scene.name));
            var report = Label(root, Vector2.zero, Vector2.one, new Vector2(12, 174), new Vector2(-12, -305), 17);
            report.transform.parent.gameObject.AddComponent<RectMask2D>();
            var content = report.GetComponent<RectTransform>(); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1); content.offsetMin = new Vector2(10, 0); content.offsetMax = new Vector2(-10, -8);
            report.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            resultsScroll = report.transform.parent.gameObject.AddComponent<ScrollRect>(); resultsScroll.viewport = report.transform.parent.GetComponent<RectTransform>();
            resultsScroll.content = content; resultsScroll.horizontal = false; resultsScroll.movementType = ScrollRect.MovementType.Clamped;
            results = report.transform.parent.gameObject.AddComponent<CombatResultsUI>(); results.Bind(report);
            var hospital=HospitalUI.Create(root,gameManager,font);
            Button(root,"HOSPITAL",0,.5f,128,162,false,hospital.Show);
            notice = Label(root, Vector2.zero, new Vector2(1, 0), new Vector2(12, 76), new Vector2(-12, 128), 16);
            Button(root, "FIGHT — APPLY RESULT", 0, 0.5f, 16, 68, false, () => Fight(true));
            Button(root, "PREVIEW — NO CHANGES", 0.5f, 1, 16, 68, false, () => Fight(false));
        }
        private Text Label(Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, int size)
        {
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            panel.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.12f, 0.94f);
            var obj = new GameObject("Text", typeof(RectTransform), typeof(Text)); obj.transform.SetParent(panel.transform, false);
            var tr = obj.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(10, 8); tr.offsetMax = new Vector2(-10, -8);
            var text = obj.GetComponent<Text>(); text.font = font; text.fontSize = size; text.color = new Color(0.95f, 0.88f, 0.72f); text.alignment = TextAnchor.UpperLeft; text.raycastTarget = false;
            return text;
        }
        private Text Button(Transform parent, string title, float minX, float maxX, float minY, float maxY, bool top, UnityEngine.Events.UnityAction action)
        {
            var text = Label(parent, new Vector2(minX, top ? 1 : 0), new Vector2(maxX, top ? 1 : 0), new Vector2(12, minY), new Vector2(-12, maxY), 14);
            text.alignment = TextAnchor.MiddleCenter; text.text = title;
            var button = text.transform.parent.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = text.transform.parent.GetComponent<Image>(); button.onClick.AddListener(action);
            return text;
        }
    }
}
