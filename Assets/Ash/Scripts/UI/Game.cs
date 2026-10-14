using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ash
{
    /// <summary>Hero selection and army test view. All mutations go through the local domain service.</summary>
    [RequireComponent(typeof(GameManager))]
    public sealed class Game : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        private Text status;
        private Text notice;
        private InputField quantityInput;
        private int selectedHero;
        private readonly List<Image> heroButtons = new List<Image>();
        private readonly List<Text> fighterRows = new List<Text>();
        private ScrollRect detailsScroll;
        private Font font;
        private GameObject presentation;
        private Material groundMaterial, headquartersMaterial;

        private void Start()
        {
            if (gameManager == null) gameManager = GetComponent<GameManager>();
            if (!gameManager.IsInitialized)
            {
                Debug.LogError("Hero army view requires an initialized GameManager.", this);
                enabled = false;
                return;
            }
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            presentation = new GameObject("Hero Army Presentation");
            BuildWorld();
            BuildUI();
            gameManager.HeroArmies.Changed += Refresh;
            Refresh();
            notice.text = "Select a hero, enter a quantity, then assign or remove fighters. Armies are independent.";
        }

        private void OnDestroy()
        {
            if (gameManager != null && gameManager.HeroArmies != null) gameManager.HeroArmies.Changed -= Refresh;
            if (presentation != null) Destroy(presentation);
            if (groundMaterial != null) Destroy(groundMaterial);
            if (headquartersMaterial != null) Destroy(headquartersMaterial);
        }

        private void Refresh()
        {
            if (gameManager.Heroes.Count == 0) { status.text = "No heroes configured."; return; }
            var hero = gameManager.Heroes[selectedHero];
            var definition = hero.Definition;
            var ability = definition.Ability;
            var text = new StringBuilder();
            text.Append(definition.DisplayName).Append(" • ").Append(definition.Rarity)
                .Append("\nLevel ").Append(hero.Level).Append(" / ").Append(definition.MaxLevel)
                .Append("    XP ").Append(hero.Xp).Append(" / ").Append(hero.XpToNextLevel == 0 ? "MAX" : hero.XpToNextLevel.ToString())
                .Append("\n\n").Append(definition.Biography)
                .Append("\n\nARMY  ").Append(hero.Army.TotalUnits).Append(" / ").Append(hero.Army.Capacity)
                .Append("\nPOWER  ").Append(hero.Army.Power.ToString("0.##"))
                .Append("\nPASSIVE  +").Append((definition.BonusAt(hero.Level) / 100m).ToString("0.##"))
                .Append("% ").Append(definition.Specialization).Append(" power")
                .Append("\n\nABILITY DATA  ").Append(ability.Name)
                .Append("\n").Append(ability.Description)
                .Append("\nTarget: ").Append(ability.TargetType).Append("  Bonus: +").Append(ability.PowerBonusBasisPoints / 100m).Append('%')
                .Append("\nCooldown: ").Append(ability.CooldownSeconds).Append("s  Duration: ").Append(ability.DurationSeconds).Append("s")
                .Append("\nData only; no ability activation or combat.\n\nFIGHTERS\n");
            foreach (var fighter in gameManager.Units)
                text.Append(fighter.Definition.Type).Append(": ").Append(hero.Army.QuantityOf(fighter.Definition.Id, fighter.Level))
                    .Append("  • Base power ").Append(fighter.Power).Append('\n');
            status.text = text.ToString();
            for (int i = 0; i < heroButtons.Count; i++)
                heroButtons[i].color = i == selectedHero ? new Color(0.5f, 0.31f, 0.12f) : new Color(0.08f, 0.1f, 0.12f);
            for (int i = 0; i < fighterRows.Count; i++)
            {
                var fighter = gameManager.Units[i];
                fighterRows[i].text = fighter.Definition.Type + "  " + hero.Army.QuantityOf(fighter.Definition.Id, fighter.Level)
                    + "/" + gameManager.HeroArmies.Available(fighter.Definition.Id) + " reserve";
            }
        }

        private void Transfer(UnitData fighter, bool assign)
        {
            if (gameManager.Heroes.Count == 0) { notice.text = "No heroes configured."; return; }
            if (!int.TryParse(quantityInput.text, out int quantity) || quantity <= 0)
            { notice.text = "Enter a positive whole-number quantity."; return; }
            var hero = gameManager.Heroes[selectedHero];
            var result = assign
                ? gameManager.HeroArmies.AssignFighters(hero.InstanceId, fighter.Definition.Id, quantity)
                : gameManager.HeroArmies.RemoveFighters(hero.InstanceId, fighter.Definition.Id, quantity);
            switch (result)
            {
                case ArmyCommandResult.Success: notice.text = assign ? "Fighters assigned to " + hero.Definition.DisplayName + "." : "Fighters returned to the reserve."; break;
                case ArmyCommandResult.CapacityExceeded: notice.text = "Army capacity exceeded. Earn XP to increase capacity."; break;
                case ArmyCommandResult.InsufficientReserve: notice.text = "Not enough fighters in the shared reserve."; break;
                case ArmyCommandResult.InsufficientAssigned: notice.text = "This hero does not have that many fighters."; break;
                default: notice.text = "Assignment rejected: " + result; break;
            }
        }

        private void BuildWorld()
        {
            var camera = new GameObject("Test Camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(presentation.transform);
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(8, 12, -10);
            camera.transform.LookAt(Vector3.zero);
            camera.orthographic = true;
            camera.orthographicSize = 8;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.17f, 0.19f, 0.2f);
            var sun = new GameObject("Test Sun", typeof(Light)).GetComponent<Light>();
            sun.transform.SetParent(presentation.transform);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            groundMaterial = CreateBlock("Ground", new Vector3(0, -0.3f, 0), new Vector3(12, 0.5f, 12), new Color(0.38f, 0.34f, 0.28f));
            headquartersMaterial = CreateBlock("HQ Placeholder", new Vector3(0, 1, 0), new Vector3(2, 2, 2), new Color(0.58f, 0.35f, 0.22f));
        }

        private Material CreateBlock(string name, Vector3 position, Vector3 scale, Color color)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(presentation.transform);
            block.transform.position = position;
            block.transform.localScale = scale;
            var material = new Material(Shader.Find("Standard")) { color = color };
            block.GetComponent<Renderer>().sharedMaterial = material;
            return material;
        }

        private void BuildUI()
        {
            var canvas = new GameObject("Test Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(presentation.transform);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 960);
            scaler.matchWidthOrHeight = 0.5f;
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Test Input", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetParent(presentation.transform);
            }
            var root = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            var safe = Screen.safeArea;
            root.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            root.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            root.offsetMin = root.offsetMax = Vector2.zero;
            var title = Label(root, new Vector2(0, 1), Vector2.one, new Vector2(12, -80), new Vector2(-12, -12), 23);
            title.text = "ASHLANDS\nHERO → ARMY → FIGHTERS";
            for (int i = 0; i < gameManager.Heroes.Count; i++)
            {
                int index = i;
                var hero = gameManager.Heroes[i];
                float minX = (i % 2) * 0.5f;
                float top = -90 - (i / 2) * 45;
                var label = Label(root, new Vector2(minX, 1), new Vector2(minX + 0.5f, 1), new Vector2(12, top - 40), new Vector2(-12, top), 17);
                label.alignment = TextAnchor.MiddleCenter;
                label.text = hero.Definition.DisplayName + " • " + hero.Definition.Rarity;
                var button = label.transform.parent.gameObject.AddComponent<Button>();
                button.targetGraphic = label.transform.parent.GetComponent<Image>();
                heroButtons.Add((Image)button.targetGraphic);
                button.onClick.AddListener(() => { selectedHero = index; Refresh(); detailsScroll.verticalNormalizedPosition = 1; });
            }
            status = Label(root, Vector2.zero, Vector2.one, new Vector2(12, 290), new Vector2(-12, -280), 19);
            status.transform.parent.gameObject.AddComponent<RectMask2D>();
            var textRect = status.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 1); textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 1); textRect.offsetMin = new Vector2(12, 0); textRect.offsetMax = new Vector2(-12, -8);
            status.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            detailsScroll = status.transform.parent.gameObject.AddComponent<ScrollRect>();
            detailsScroll.viewport = status.transform.parent.GetComponent<RectTransform>();
            detailsScroll.content = textRect; detailsScroll.horizontal = false;
            detailsScroll.movementType = ScrollRect.MovementType.Clamped;
            notice = Label(root, Vector2.zero, new Vector2(1, 0), new Vector2(12, 218), new Vector2(-12, 278), 18);
            for (int i = 0; i < gameManager.Units.Count; i++)
            {
                var fighter = gameManager.Units[i];
                float y = 170 - i * 44;
                fighterRows.Add(Label(root, Vector2.zero, new Vector2(0.55f, 0), new Vector2(12, y), new Vector2(-4, y + 38), 17));
                AddButton(root, "+ Assign", 0.55f, 0.775f, y, 38, () => Transfer(fighter, true));
                AddButton(root, "− Remove", 0.775f, 1, y, 38, () => Transfer(fighter, false));
            }
            var quantityLabel = Label(root, Vector2.zero, new Vector2(0.55f, 0), new Vector2(12, 12), new Vector2(-4, 66), 20);
            quantityLabel.alignment = TextAnchor.MiddleCenter;
            quantityInput = quantityLabel.transform.parent.gameObject.AddComponent<InputField>();
            quantityInput.textComponent = quantityLabel;
            quantityInput.targetGraphic = quantityLabel.transform.parent.GetComponent<Image>();
            quantityInput.contentType = InputField.ContentType.IntegerNumber;
            quantityInput.text = "5";
            var hint = Label(root, Vector2.zero, new Vector2(0.55f, 0), new Vector2(12, 66), new Vector2(-4, 82), 12);
            hint.text = "FIGHTER QUANTITY";
            AddButton(root, "+125 HERO XP (TEST)", 0.55f, 1, 12, 54, () =>
            {
                if (gameManager.Heroes.Count == 0) return;
                gameManager.HeroArmies.GrantHeroXp(gameManager.Heroes[selectedHero].InstanceId, 125);
                notice.text = "Test XP granted to the selected hero.";
            });
        }

        private Text Label(Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, int size)
        {
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            panel.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.12f, 0.94f);
            var child = new GameObject("Text", typeof(RectTransform), typeof(Text));
            child.transform.SetParent(panel.transform, false);
            var tr = child.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(12, 8); tr.offsetMax = new Vector2(-12, -8);
            var text = child.GetComponent<Text>();
            text.font = font; text.fontSize = size; text.color = new Color(0.95f, 0.88f, 0.72f);
            text.alignment = TextAnchor.UpperLeft;
            text.raycastTarget = false;
            return text;
        }

        private void AddButton(Transform parent, string title, float minX, float maxX, float y, float height, UnityEngine.Events.UnityAction action)
        {
            var text = Label(parent, new Vector2(minX, 0), new Vector2(maxX, 0), new Vector2(4, y), new Vector2(-12, y + height), 16);
            text.alignment = TextAnchor.MiddleCenter; text.text = title;
            var button = text.transform.parent.gameObject.AddComponent<Button>();
            button.targetGraphic = text.transform.parent.GetComponent<Image>();
            button.onClick.AddListener(action);
        }
    }
}
