using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace Ash
{
    [RequireComponent(typeof(Camera))]
    public sealed class SettlementCamera : MonoBehaviour
    {
        public event Action<SettlementBuilding> BuildingSelected, MoveStarted;
        public event Action<SettlementBuilding,Vector3> MoveRequested;
        public event Action MoveCancelled, WorldMapRequested;
        public Func<bool> InputAllowed;
        public Func<SettlementBuilding,Vector3,bool> PlacementAllowed;
        public const float HoldSeconds=1.5f;
        private Camera view;
        private Vector2 previous, origin, pointer;
        private bool dragging, pinching, buildingDragged;
        private Vector3 home, panOrigin, moveOrigin, grabOffset;
        private float homeZoom, maximumZoom, pressedAt, inputDelay;
        private int width,height;
        private Rect safeArea;
        private SettlementBuilding candidate,moving;
        private LineRenderer footprint;
        private Material footprintMaterial;
        public Rect OverviewArea {get;private set;}
        public float OverviewZoom=>homeZoom;
        public bool IsMoving(SettlementBuilding building)=>moving==building;
        private float DragThreshold=>10*Mathf.Max(1,Screen.height/960f);
        private void Awake(){view=GetComponent<Camera>();ResetView();}
        public void ResetView()
        {
            CancelGesture();view=GetComponent<Camera>();IsometricView.Configure(view);
            width=Screen.width;height=Screen.height;safeArea=Screen.safeArea;
            float scale=Mathf.Sqrt(width/540f*height/960f);
            var area=new Rect((safeArea.xMin+12*scale)/width,(safeArea.yMin+247*scale)/height,
                Mathf.Max(1,safeArea.width-24*scale)/width,Mathf.Max(1,safeArea.height-405*scale)/height);
            OverviewArea=area;
            var bounds=new Bounds(new Vector3(0,1.5f,0),new Vector3(SettlementLayout.HalfWidth*2+4,6,SettlementLayout.HalfDepth*2+4));
            homeZoom=Mathf.Max(10,IsometricView.FitSize(view,bounds,area));maximumZoom=Mathf.Max(80,homeZoom*3.5f);
            home=IsometricView.Position(view,bounds.center,area,homeZoom);
            panOrigin=home;transform.position=home;view.orthographicSize=homeZoom;inputDelay=0;pinching=false;
        }
        public void SuspendInput(float seconds=.35f){CancelGesture();inputDelay=Time.unscaledTime+seconds;pinching=true;}
        public void FocusOn(Vector3 target,float zoom)
        {
            CancelGesture();view.orthographicSize=Mathf.Clamp(zoom,4,maximumZoom);
            panOrigin=IsometricView.Position(view,target,OverviewArea,view.orthographicSize);transform.position=panOrigin;
        }
        public void SetZoom(float requested)
        {
            if(float.IsNaN(requested)||float.IsInfinity(requested)||Time.unscaledTime<inputDelay)return;
            if(WorldMapRequested!=null && requested>view.orthographicSize && requested>=homeZoom*1.2f)
            {
                view.orthographicSize=homeZoom*1.2f;SuspendInput();WorldMapRequested.Invoke();return;
            }
            view.orthographicSize=Mathf.Clamp(requested,4,maximumZoom);
        }
        private bool OverUI(int finger=-1)=>EventSystem.current!=null && (finger<0?EventSystem.current.IsPointerOverGameObject():EventSystem.current.IsPointerOverGameObject(finger));
        private void Update()
        {
            if(Screen.width!=width || Screen.height!=height || Screen.safeArea!=safeArea)ResetView();
            if(InputAllowed?.Invoke()==false){CancelGesture();return;}
            if(Time.unscaledTime<inputDelay)return;
            if(Input.GetKeyDown(KeyCode.Escape)){CancelGesture();return;}
            if(Input.touchCount>=2)
            {
                CancelGesture();pinching=true;
                var a=Input.GetTouch(0);var b=Input.GetTouch(1);
                if(OverUI(a.fingerId)||OverUI(b.fingerId))return;
                float before=Vector2.Distance(a.position-a.deltaPosition,b.position-b.deltaPosition);
                SetZoom(view.orthographicSize*(before/Mathf.Max(1,Vector2.Distance(a.position,b.position))));return;
            }
            // A remaining pinch finger must lift before it can start a tap, pan or hold.
            if(pinching){if(Input.touchCount>0 || Input.GetMouseButton(0))return;pinching=false;return;}
            if(Input.touchCount==1)
            {
                var touch=Input.GetTouch(0);
                if(touch.phase==TouchPhase.Began)Begin(touch.position,OverUI(touch.fingerId));
                else if(touch.phase==TouchPhase.Moved || touch.phase==TouchPhase.Stationary)Move(touch.position);
                else if(touch.phase==TouchPhase.Ended)End(touch.position,OverUI(touch.fingerId));
                else if(touch.phase==TouchPhase.Canceled)CancelGesture();
                CheckHold();return;
            }
            float wheel=Input.mouseScrollDelta.y;
            if(wheel!=0 && !OverUI()){CancelGesture();SetZoom(view.orthographicSize*Mathf.Pow(1.12f,-wheel));return;}
            if(Input.GetMouseButtonDown(0))Begin(Input.mousePosition,OverUI());
            if(Input.GetMouseButton(0))Move(Input.mousePosition);
            if(Input.GetMouseButtonUp(0))End(Input.mousePosition,OverUI());
            CheckHold();
        }
        private SettlementBuilding Pick(Vector2 point)
        {
            return Physics.Raycast(view.ScreenPointToRay(point),out var hit,300)?hit.collider.GetComponentInParent<SettlementBuilding>():null;
        }
        private bool Ground(Vector2 point,out Vector3 position)
        {
            var ray=view.ScreenPointToRay(point);var plane=new Plane(Vector3.up,Vector3.zero);
            if(plane.Raycast(ray,out float distance)){position=ray.GetPoint(distance);return true;}
            position=Vector3.zero;return false;
        }
        private void Begin(Vector2 point,bool overUI)
        {
            CancelGesture();dragging=!overUI;origin=previous=pointer=point;pressedAt=Time.unscaledTime;
            if(dragging){candidate=Pick(point);if(candidate!=null && candidate.PlacementId==null)candidate=null;}
        }
        private void CheckHold()
        {
            if(!dragging || moving!=null || candidate==null || Time.unscaledTime-pressedAt<HoldSeconds)return;
            if(!Ground(pointer,out var ground))return;
            moving=candidate;candidate=null;moveOrigin=moving.transform.position;grabOffset=moveOrigin-ground;
            buildingDragged=false;
            MoveStarted?.Invoke(moving);UpdatePlacement(pointer);
        }
        private void Move(Vector2 point)
        {
            if(!dragging)return;pointer=point;
            if(moving!=null){UpdatePlacement(point);return;}
            if(candidate!=null && Vector2.Distance(origin,point)<=DragThreshold){previous=point;return;}
            candidate=null;
            if(Ground(previous,out var before) && Ground(point,out var after))
            {
                transform.position+=before-after;var offset=transform.position-panOrigin;
                transform.position=panOrigin+new Vector3(Mathf.Clamp(offset.x,-SettlementLayout.HalfWidth-6,SettlementLayout.HalfWidth+6),0,Mathf.Clamp(offset.z,-SettlementLayout.HalfDepth-6,SettlementLayout.HalfDepth+6));
            }
            previous=point;
        }
        private void UpdatePlacement(Vector2 point)
        {
            if(moving==null || !Ground(point,out var ground))return;
            buildingDragged|=Vector2.Distance(origin,point)>DragThreshold/3;
            var target=ground+grabOffset;target.x=SettlementPlacementCatalog.Snap(target.x);target.z=SettlementPlacementCatalog.Snap(target.z);target.y=moveOrigin.y;
            if(!buildingDragged)target=moveOrigin;
            moving.transform.position=target;bool valid=PlacementAllowed?.Invoke(moving,target)??false;
            if(footprint==null)
            {
                var root=new GameObject("Placement preview footprint",typeof(LineRenderer));footprint=root.GetComponent<LineRenderer>();
                footprintMaterial=new Material(Resources.Load<Shader>("AshSettlement"));footprint.sharedMaterial=footprintMaterial;
                footprint.positionCount=4;footprint.loop=true;footprint.useWorldSpace=true;footprint.widthMultiplier=.10f;
                footprint.generateLightingData=true;
                footprint.shadowCastingMode=ShadowCastingMode.Off;footprint.receiveShadows=false;
            }
            footprintMaterial.color=valid?new Color(.2f,1,.4f):new Color(1,.2f,.15f);
            var def=SettlementPlacementCatalog.Find(moving.PlacementId);float x=def.Width/2,z=def.Depth/2;
            footprint.SetPositions(new[]{target+new Vector3(-x,.22f,-z),target+new Vector3(x,.22f,-z),target+new Vector3(x,.22f,z),target+new Vector3(-x,.22f,z)});
        }
        private void End(Vector2 point,bool overUI)
        {
            if(!dragging)return;
            if(moving!=null)
            {
                if(overUI || !OverviewArea.Contains(new Vector2(point.x/Screen.width,point.y/Screen.height))){CancelGesture();return;}
                UpdatePlacement(point);if(!buildingDragged){CancelGesture();return;}
                var building=moving;var target=moving.transform.position;moving=null;candidate=null;dragging=false;ClearFootprint();
                if(MoveRequested!=null)MoveRequested.Invoke(building,target);else building.transform.position=moveOrigin;
                return;
            }
            dragging=false;candidate=null;
            if(overUI || Vector2.Distance(origin,point)>DragThreshold)return;
            var selected=Pick(point);if(selected!=null)BuildingSelected?.Invoke(selected);
        }
        private void ClearFootprint()
        {
            if(footprint!=null)SettlementArt.Release(footprint.gameObject);
            if(footprintMaterial!=null)SettlementArt.Release(footprintMaterial);footprint=null;footprintMaterial=null;
        }
        private void CancelGesture()
        {
            bool wasMoving=moving!=null;if(wasMoving)moving.transform.position=moveOrigin;
            moving=null;candidate=null;dragging=false;ClearFootprint();if(wasMoving)MoveCancelled?.Invoke();
        }
        private void OnDisable(){CancelGesture();}
        private void OnApplicationPause(bool paused){if(paused)CancelGesture();}
        private void OnApplicationFocus(bool focused){if(!focused)CancelGesture();}
        private void OnDestroy(){CancelGesture();}
    }
}
