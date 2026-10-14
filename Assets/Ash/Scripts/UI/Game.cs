using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ash
{
    /// <summary>Phase 1 smoke-test presentation. Owns no resource, hero, or army state.</summary>
    [RequireComponent(typeof(GameManager))]
    public sealed class Game : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        private Text status;
        private Text notice;
        private Font font;
        private GameObject presentation;
        private Material groundMaterial, headquartersMaterial;

        private void Start()
        {
            if (gameManager == null) gameManager = GetComponent<GameManager>();
            if (!gameManager.IsInitialized)
            {
                Debug.LogError("Phase 1 view requires an initialized GameManager.", this);
                enabled = false;
                return;
            }
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            presentation = new GameObject("Phase 1 Presentation");
            BuildWorld();
            BuildUI();
            gameManager.Resources.Changed += Refresh;
            Refresh();
            notice.text = "Phase 1 data checks. Buttons manipulate sample data only. No combat or training simulation.";
        }

        private void OnDestroy()
        {
            if (gameManager != null && gameManager.Resources != null) gameManager.Resources.Changed -= Refresh;
            if (presentation != null) Destroy(presentation);
            if (groundMaterial != null) Destroy(groundMaterial);
            if (headquartersMaterial != null) Destroy(headquartersMaterial);
        }

        private void Refresh()
        {
            var text = new StringBuilder("ASHLANDS: RISE OF THE REMNANTS\nPHASE 1 • ARCHITECTURE TEST\n\n");
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
                text.Append(type).Append(": ").Append(gameManager.Resources.Get(type)).Append("   ");
            text.Append("\n\nHERO ROSTER\n");
            foreach (var hero in gameManager.Heroes)
                text.Append(hero.Definition.DisplayName).Append(" • ").Append(hero.Definition.Rarity)
                    .Append(" • ").Append(hero.Definition.Specialization).Append(" • Lv ").Append(hero.Level).Append('\n');
            text.Append("\nUNIT CATALOGUE\n");
            foreach (var unit in gameManager.Units)
                text.Append(unit.Definition.DisplayName).Append(" • ").Append(unit.Definition.Type)
                    .Append(" • HP ").Append(unit.Definition.Health).Append(" • Lv ").Append(unit.Level).Append('\n');
            text.Append("\nARMY: ").Append(gameManager.Army.TotalUnits).Append(" units\nCommander: ")
                .Append(gameManager.Army.Commander == null ? "Unassigned" : gameManager.Army.Commander.Definition.DisplayName);
            foreach (var stack in gameManager.Army.Units)
                text.Append("\n").Append(stack.Unit.Definition.DisplayName).Append(" x ").Append(stack.Count);
            status.text = text.ToString();
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
            status = Label(root, new Vector2(0, 0.37f), Vector2.one, new Vector2(12, 0), new Vector2(-12, -12), 20);
            notice = Label(root, new Vector2(0, 0), new Vector2(1, 0), new Vector2(12, 148), new Vector2(-12, 225), 18);
            AddButton(root, "ADD 10 SCRAP", 0, 80, () => { gameManager.Resources.Add(ResourceType.Scrap, 10); notice.text = "Added 10 scrap."; });
            AddButton(root, "SPEND 5 WATER + 2 FUEL", 1, 80, () =>
            {
                bool spent = gameManager.Resources.TrySpend(new[] { new ResourceAmount(ResourceType.Water, 5), new ResourceAmount(ResourceType.Fuel, 2) });
                notice.text = spent ? "Atomic spend succeeded." : "Insufficient resources. Balances unchanged.";
            });
            AddButton(root, "ASSIGN SAMPLE HERO", 0, 12, () =>
            {
                if (gameManager.Heroes.Count == 0) { notice.text = "No hero definitions configured."; return; }
                gameManager.Army.AssignCommander(gameManager.Heroes[0]); Refresh(); notice.text = "Commander assigned.";
            });
            AddButton(root, "ADD SAMPLE UNIT", 1, 12, () =>
            {
                if (gameManager.Units.Count == 0) { notice.text = "No unit definitions configured."; return; }
                gameManager.Army.AddUnits(gameManager.Units[0], 1); Refresh(); notice.text = "One sample unit added. No training costs or timers.";
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

        private void AddButton(Transform parent, string title, int column, float y, UnityEngine.Events.UnityAction action)
        {
            var text = Label(parent, new Vector2(column * 0.5f, 0), new Vector2((column + 1) * 0.5f, 0), new Vector2(12, y), new Vector2(-12, y + 60), 17);
            text.alignment = TextAnchor.MiddleCenter; text.text = title;
            var button = text.transform.parent.gameObject.AddComponent<Button>();
            button.targetGraphic = text.transform.parent.GetComponent<Image>();
            button.onClick.AddListener(action);
        }
    }
}
