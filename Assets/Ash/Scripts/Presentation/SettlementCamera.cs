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
        private Vector3 home;
        private const float HomeZoom = 20;
        private void Awake() { view = GetComponent<Camera>(); home = transform.position; }
        public void ResetView() { transform.position = home; view.orthographicSize = HomeZoom; }
        private bool OverUI(int finger = -1) => EventSystem.current != null && (finger < 0 ? EventSystem.current.IsPointerOverGameObject() : EventSystem.current.IsPointerOverGameObject(finger));
        private void Update()
        {
            if (Input.touchCount >= 2)
            {
                dragging = false;
                var a = Input.GetTouch(0); var b = Input.GetTouch(1);
                if (OverUI(a.fingerId) || OverUI(b.fingerId)) return;
                float before = Vector2.Distance(a.position-a.deltaPosition, b.position-b.deltaPosition);
                float now = Vector2.Distance(a.position,b.position);
                view.orthographicSize = Mathf.Clamp(view.orthographicSize + (before-now)*.025f, 10, 23); return;
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
            if (!OverUI()) view.orthographicSize = Mathf.Clamp(view.orthographicSize - Input.mouseScrollDelta.y, 10, 23);
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
                var offset = transform.position-home;
                transform.position = home + new Vector3(Mathf.Clamp(offset.x,-6,6),0,Mathf.Clamp(offset.z,-6,6));
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
