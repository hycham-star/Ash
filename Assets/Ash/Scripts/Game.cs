using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    public sealed class Game : MonoBehaviour
    {
        private Settlement state = new Settlement();
        private Text resources, message, territory;
        private Transform buildings;
        private GameObject depotFlag;
        private float timer;
        private bool paused;
        private const float DayLength = 20f;
        private readonly Color amber = new Color(0.94f, 0.65f, 0.28f);
        private Font font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (FindFirstObjectByType<Game>() == null)
                new GameObject("Ash Prototype").AddComponent<Game>();
        }

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildWorld();
            BuildUI();
            Refresh();
            message.text = "The old world is gone. Build Ash, strengthen your squad, and claim the Iron Pact's water depot.";
        }

        private void Update()
        {
            if (paused) return;
            timer += Time.deltaTime;
            if (timer >= DayLength)
            {
                timer -= DayLength;
                state.Produce();
                Refresh();
            }
            territory.text = (state.OwnsDepot ? "WATER DEPOT • ASH CONTROL" : "WATER DEPOT • IRON PACT • STRENGTH 7")
                + "\nNext production: " + Mathf.CeilToInt(DayLength - timer) + "s";
        }

        private void Act(System.Func<string> action)
        {
            message.text = action();
            Refresh();
        }

        private void Refresh()
        {
            resources.text = "ASH  /  DAY " + state.Day + "\nSUPPLIES " + state.Supplies + "     SALVAGE " + state.Salvage
                + "\nSQUAD " + state.Squad + "     STRENGTH " + (state.Squad + state.Watchtowers * 2);
            foreach (Transform child in buildings)
            {
                Destroy(child.GetComponent<Renderer>().sharedMaterial);
                Destroy(child.gameObject);
            }
            for (int i = 0; i < state.Farms; i++)
                Block("Farm", new Vector3(-4 + (i % 4) * 1.8f, 0.3f, -2 - (i / 4) * 1.4f), new Vector3(1.5f, 0.6f, 1), new Color(0.36f, 0.49f, 0.26f), buildings);
            for (int i = 0; i < state.Workshops; i++)
                Block("Workshop", new Vector3(-4 + (i % 4) * 1.8f, 0.7f, 0 + (i / 4) * 1.6f), new Vector3(1.3f, 1.4f, 1.2f), new Color(0.58f, 0.46f, 0.33f), buildings);
            for (int i = 0; i < state.Watchtowers; i++)
                Block("Watchtower", new Vector3(-5 + (i % 6) * 1.8f, 1.3f, 3 + (i / 6) * 1.2f), new Vector3(0.7f, 2.6f, 0.7f), amber, buildings);
            depotFlag.GetComponent<Renderer>().material.color = state.OwnsDepot ? amber : new Color(0.7f, 0.2f, 0.16f);
        }

        private GameObject Block(string label, Vector3 position, Vector3 scale, Color color, Transform parent = null)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = label;
            block.transform.SetParent(parent == null ? transform : parent);
            block.transform.position = position;
            block.transform.localScale = scale;
            var material = new Material(Shader.Find("Standard"));
            material.color = color;
            block.GetComponent<Renderer>().material = material;
            return block;
        }

        private void BuildWorld()
        {
            var cameraObject = new GameObject("Settlement Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(12, 19, -17);
            camera.transform.LookAt(new Vector3(0, 0, 2));
            camera.orthographic = true;
            camera.orthographicSize = 15;
            camera.backgroundColor = new Color(0.17f, 0.20f, 0.22f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -35, 0);
            light.intensity = 1.2f;
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.60f);
            Block("Wasteland", new Vector3(0, -0.3f, 2), new Vector3(22, 0.5f, 25), new Color(0.37f, 0.34f, 0.28f));
            Block("Headquarters", new Vector3(0, 1.1f, 1), new Vector3(2.5f, 2.2f, 2), new Color(0.30f, 0.40f, 0.46f));
            Block("Ash Banner", new Vector3(0, 3, 1), new Vector3(1, 0.7f, 0.12f), amber);
            for (int i = 0; i < 9; i++)
                Block("Scrap Wall", new Vector3(-6 + i * 1.5f, 0.5f, -4), new Vector3(1.3f, 1, 0.35f), new Color(0.32f, 0.31f, 0.29f));
            Block("Rival Water Depot", new Vector3(2, 0.8f, 8), new Vector3(3, 1.6f, 2), new Color(0.38f, 0.40f, 0.42f));
            depotFlag = Block("Depot Banner", new Vector3(2, 2.2f, 8), new Vector3(1.2f, 0.8f, 0.12f), Color.red);
            buildings = new GameObject("Settlement Buildings").transform;
            buildings.SetParent(transform);
        }

        private void BuildUI()
        {
            var canvasObject = new GameObject("Portrait HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 960);
            scaler.matchWidthOrHeight = 0.5f;
            new GameObject("Event System", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            var root = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            Rect safe = Screen.safeArea;
            root.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            root.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            root.offsetMin = root.offsetMax = Vector2.zero;
            resources = Label(root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -118), new Vector2(-12, -12), 25);
            territory = Label(root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -185), new Vector2(-12, -125), 18);
            message = Label(root, Vector2.zero, new Vector2(1, 0), new Vector2(12, 218), new Vector2(-12, 318), 20);
            Button(root, "FARM\n25 salvage", 0, 146, () => Act(state.BuildFarm));
            Button(root, "WORKSHOP\n25 supplies / 20 salvage", 1, 146, () => Act(state.BuildWorkshop));
            Button(root, "RECRUIT\n20 supplies", 0, 78, () => Act(state.Recruit));
            Button(root, "WATCHTOWER\n30 salvage", 1, 78, () => Act(state.BuildWatchtower));
            Button(root, "RAID WATER DEPOT\n15 supplies", 0, 10, () => Act(state.AttackDepot));
            Button(root, "PAUSE / RESUME", 1, 10, () => { paused = !paused; message.text = paused ? "Paused." : "Settlement resumed."; });
        }

        private Text Label(Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, int size)
        {
            var obj = new GameObject("Label", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            obj.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.12f, 0.92f);
            var child = new GameObject("Text", typeof(RectTransform), typeof(Text));
            child.transform.SetParent(obj.transform, false);
            var tr = child.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(10, 4); tr.offsetMax = new Vector2(-10, -4);
            var text = child.GetComponent<Text>();
            text.font = font; text.fontSize = size; text.color = new Color(0.95f, 0.91f, 0.81f);
            text.alignment = TextAnchor.MiddleLeft;
            return text;
        }

        private void Button(Transform parent, string title, int column, float y, UnityEngine.Events.UnityAction action)
        {
            var text = Label(parent, new Vector2(column * 0.5f, 0), new Vector2((column + 1) * 0.5f, 0), new Vector2(12, y), new Vector2(-12, y + 60), 17);
            text.alignment = TextAnchor.MiddleCenter;
            text.text = title;
            var button = text.transform.parent.gameObject.AddComponent<Button>();
            button.targetGraphic = text.transform.parent.GetComponent<Image>();
            button.onClick.AddListener(action);
        }
    }
}
