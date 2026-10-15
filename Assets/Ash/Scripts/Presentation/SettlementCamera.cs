using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Ash
{
    [RequireComponent(typeof(Camera))]
    public sealed class SettlementCamera : MonoBehaviour
    {
        public event Action<SettlementBuilding> BuildingSelected;
        private Camera view;
        private Vector2 previous, origin;
        private bool dragging;
        private Vector3 home, panOrigin;
        private float homeZoom, maximumZoom;
        private int width, height;
        private Rect safeArea;
        public Rect OverviewArea { get; private set; }
        private void Awake() { view = GetComponent<Camera>(); ResetView(); }
        public void ResetView()
        {
            view = GetComponent<Camera>();
            width = Screen.width; height = Screen.height; safeArea = Screen.safeArea;
            // Match the HUD CanvasScaler (540x960, equal width/height match).
            float scale = Mathf.Sqrt(width / 540f * height / 960f);
            var area = new Rect((safeArea.xMin + 12 * scale) / width,
                (safeArea.yMin + 247 * scale) / height,
                Mathf.Max(1, safeArea.width - 24 * scale) / width,
                Mathf.Max(1, safeArea.height - 405 * scale) / height);
            OverviewArea = area;
            // Enclose the settlement walls, roofs and towers, excluding distant scenery.
            var bounds = new Bounds(new Vector3(0, 1.5f, 0), new Vector3(16, 6, 19));
            float horizontal = 0, vertical = 0;
            for (int corner = 0; corner < 8; corner++)
            {
                var offset = Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1 : 1,
                    (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                horizontal = Mathf.Max(horizontal, Mathf.Abs(Vector3.Dot(offset, transform.right)));
                vertical = Mathf.Max(vertical, Mathf.Abs(Vector3.Dot(offset, transform.up)));
            }
            homeZoom = Mathf.Max(10, 1.08f * Mathf.Max(horizontal / (view.aspect * area.width), vertical / area.height));
            maximumZoom = Mathf.Max(80, homeZoom * 3.5f);
            home = bounds.center - transform.forward * 45
                - transform.right * ((area.center.x - .5f) * 2 * homeZoom * view.aspect)
                - transform.up * ((area.center.y - .5f) * 2 * homeZoom);
            panOrigin=home; transform.position = home; view.orthographicSize = homeZoom; dragging = false;
        }
        public void FocusOn(Vector3 target,float zoom)
        {
            view.orthographicSize=Mathf.Clamp(zoom,4,maximumZoom);
            float size=view.orthographicSize;
            panOrigin=target-transform.forward*45
                -transform.right*((OverviewArea.center.x-.5f)*2*size*view.aspect)
                -transform.up*((OverviewArea.center.y-.5f)*2*size);
            transform.position=panOrigin; dragging=false;
        }
        private bool OverUI(int finger = -1) => EventSystem.current != null && (finger < 0 ? EventSystem.current.IsPointerOverGameObject() : EventSystem.current.IsPointerOverGameObject(finger));
        private void Update()
        {
            if (Screen.width != width || Screen.height != height || Screen.safeArea != safeArea) ResetView();
            if (Input.touchCount >= 2)
            {
                dragging = false;
                var a = Input.GetTouch(0); var b = Input.GetTouch(1);
                if (OverUI(a.fingerId) || OverUI(b.fingerId)) return;
                float before = Vector2.Distance(a.position-a.deltaPosition, b.position-b.deltaPosition);
                float now = Vector2.Distance(a.position,b.position);
                view.orthographicSize = Mathf.Clamp(view.orthographicSize * (before / Mathf.Max(1, now)), 4, maximumZoom); return;
            }
            if (Input.touchCount == 1)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began) Begin(touch.position, OverUI(touch.fingerId));
                else if (touch.phase == TouchPhase.Moved) Move(touch.position);
                else if (touch.phase == TouchPhase.Ended) End(touch.position);
                else if (touch.phase == TouchPhase.Canceled) dragging = false;
                return;
            }
            if (!OverUI()) view.orthographicSize = Mathf.Clamp(view.orthographicSize - Input.mouseScrollDelta.y, 4, maximumZoom);
            if (Input.GetMouseButtonDown(0)) Begin(Input.mousePosition, OverUI());
            if (Input.GetMouseButton(0)) Move(Input.mousePosition);
            if (Input.GetMouseButtonUp(0)) End(Input.mousePosition);
        }
        private void Begin(Vector2 point, bool overUI) { dragging = !overUI; origin = previous = point; }
        private void Move(Vector2 point)
        {
            if (!dragging) return;
            var ground = new Plane(Vector3.up, Vector3.zero);
            var before = view.ScreenPointToRay(previous); var after = view.ScreenPointToRay(point);
            if (ground.Raycast(before,out float first) && ground.Raycast(after,out float second))
            {
                var delta = before.GetPoint(first)-after.GetPoint(second);
                transform.position += delta;
                var offset = transform.position-panOrigin;
                transform.position = panOrigin + new Vector3(Mathf.Clamp(offset.x,-6,6),0,Mathf.Clamp(offset.z,-6,6));
            }
            previous = point;
        }
        private void End(Vector2 point)
        {
            if (!dragging) return; dragging = false;
            if (Vector2.Distance(origin,point) > 10) return;
            if (Physics.Raycast(view.ScreenPointToRay(point), out var hit, 100))
            {
                var building = hit.collider.GetComponent<SettlementBuilding>();
                if (building != null) BuildingSelected?.Invoke(building);
            }
        }
    }
}
