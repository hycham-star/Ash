using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ash
{
    [RequireComponent(typeof(GameManager))]
    public sealed partial class SettlementView : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private EncounterConfig[] encounters = Array.Empty<EncounterConfig>();
        private readonly List<NpcEncounter> camps = new List<NpcEncounter>();
        private SettlementArt art;
        private GameObject presentation, heroPanel, raidPanel, buildingPanel;
        private Transform world, leader;
        private RectTransform ui;
        private SettlementCamera controls;
        private RawImage heroIllustration;
        private Font font;
        private Text resources, selection, heroStats, raidHeader, campButton, toast, leaderLabel, saveStatus, resetButton;
        private float resetDeadline;
        private BuildingKind selectedBuilding;
        private Text buildingStats, upgradeBuilding, collectBuilding;
        private CombatResultsUI results;
        private VisualBattlePlayback playback;
        private ScavengingMapUI scavengingMap;
        private HospitalUI hospitalUI;
        private DevelopmentUI developmentUI;
        private GameObject developmentWorld;
        private SettlementArt developmentArt;
        private string plotVisualSignature;
        private Text heroHeading;
        private int selectedHero, selectedCamp, selectedFighter, renderedHero = -1;
        private Image heroPortraitFrame;
        private Slider fighterQuantity;
        private Text fighterQuantityLabel, addFightersLabel, fillFightersLabel;
        private readonly List<Text> fighterTypeLabels=new List<Text>();
        private Color oldAmbient;
        private UnityEngine.Rendering.AmbientMode oldAmbientMode;
        private float navigationReadyAt;
        private CanvasGroup navigationCover;
        private float oldShadowDistance;
        private const int PortraitLayer = 30;
        private readonly Color ink = new Color32(24,32,35,246);

        private void Start()
        {
            if (gameManager == null) gameManager = GetComponent<GameManager>();
            if (!gameManager.IsInitialized || gameManager.Heroes.Count != 8 || gameManager.Units.Count != 3)
            { Debug.LogError("Settlement scene requires the existing eight-hero and three-fighter catalogue.",this); enabled = false; return; }
            foreach (var config in encounters)
            {
                if (config == null) { Debug.LogError("Missing settlement raid encounter.",this); enabled = false; return; }
                camps.Add(config.CreateEncounter());
            }
            gameManager.WorldMap.ConfigureEncounters(camps);
            art = new SettlementArt(); font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            presentation = new GameObject("Original Ashlands Settlement");
            world = new GameObject("Settlement World").transform; world.SetParent(presentation.transform,false);
            oldAmbientMode=RenderSettings.ambientMode; oldAmbient = RenderSettings.ambientLight; RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat; oldShadowDistance = QualitySettings.shadowDistance; QualitySettings.shadowDistance = Mathf.Max(oldShadowDistance,160); RenderSettings.ambientLight = new Color(.40f,.43f,.47f);
            BuildWorld(); BuildUI(); BuildPortrait();
            gameManager.Resources.Changed += Refresh; gameManager.HeroArmies.Changed += Refresh; gameManager.Fragments.Changed += Refresh; gameManager.Buildings.Changed += Refresh;
            gameManager.SaveStatusChanged += UpdateSaveStatus; gameManager.OfflineProgressChanged += ShowOfflineSummary; UpdateSaveStatus();
            Refresh(); selection.text = "THE EMBER OUTPOST\nTap a building to inspect • Drag to explore"; ShowOfflineSummary();
        }
        private void OnDestroy()
        {
            if (gameManager != null && gameManager.IsInitialized)
            { gameManager.Resources.Changed -= Refresh; gameManager.HeroArmies.Changed -= Refresh; gameManager.Fragments.Changed -= Refresh; gameManager.Buildings.Changed -= Refresh; }
            if (gameManager != null) { gameManager.SaveStatusChanged -= UpdateSaveStatus; gameManager.OfflineProgressChanged -= ShowOfflineSummary; }
            if (controls != null) controls.BuildingSelected -= Inspect;
            if(controls!=null){controls.WorldMapRequested-=ShowMapFromZoom;controls.MoveStarted-=BeginBuildingMove;controls.MoveRequested-=CommitBuildingMove;controls.MoveCancelled-=CancelBuildingMove;}
            if (presentation != null) Destroy(presentation);
            developmentArt?.Dispose();
            art?.Dispose();
            if (art != null) { RenderSettings.ambientMode=oldAmbientMode; RenderSettings.ambientLight = oldAmbient; QualitySettings.shadowDistance = oldShadowDistance; }
        }
        private void Update()
        {
            if(resetDeadline>0 && Time.unscaledTime>resetDeadline) { CancelReset(); toast.text="Reset cancelled."; }
        }
        private void ShowOfflineSummary() { if(gameManager.OfflineSummary.Length==0)return; if(toast!=null)toast.text=gameManager.OfflineSummary; if(scavengingMap!=null)scavengingMap.ShowOfflineSummary(gameManager.OfflineSummary); }
        private void UpdateSaveStatus() { if(saveStatus!=null)saveStatus.text=gameManager.SaveStatus; }
        private void CancelReset() { resetDeadline=0; if(resetButton!=null)resetButton.text="RESET"; }
        private void SaveProgress()
        {
            CancelReset(); gameManager.SaveProgress(); toast.text=gameManager.SaveStatus;
        }
        private void LoadProgress()
        {
            CancelReset();
            if(gameManager.TryLoadProgress()) { results.Clear(); Refresh(); }
            toast.text=gameManager.SaveStatus;
        }
        private void RequestReset()
        {
            if(resetDeadline<=Time.unscaledTime)
            {
                resetDeadline=Time.unscaledTime+8; resetButton.text="CONFIRM";
                toast.text="Erase all saved progress? Click CONFIRM again within 8 seconds."; return;
            }
            CancelReset();
            if(gameManager.ResetProgress()) { results.Clear(); Refresh(); }
            toast.text=gameManager.SaveStatus;
        }

        private void BuildWorld()
        {
            var camera = new GameObject("Isometric Settlement Camera",typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(presentation.transform,false); camera.tag = "MainCamera";
            IsometricView.Configure(camera);
            camera.cullingMask = ~(1<<PortraitLayer); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(69,76,73,255);
            controls = camera.gameObject.AddComponent<SettlementCamera>(); controls.BuildingSelected += Inspect;
            controls.WorldMapRequested+=ShowMapFromZoom;controls.MoveStarted+=BeginBuildingMove;controls.MoveRequested+=CommitBuildingMove;controls.MoveCancelled+=CancelBuildingMove;
            controls.InputAllowed=SettlementInputAllowed;
            controls.PlacementAllowed=(building,point)=>{var p=world.InverseTransformPoint(point);return gameManager.Buildings.CanMoveBuilding(building.PlacementId,p.x,p.z)==PlacementResult.Success;};
            var sun = new GameObject("Late Afternoon Sun",typeof(Light)).GetComponent<Light>(); sun.transform.SetParent(presentation.transform,false);
            sun.cullingMask=~((1<<PortraitLayer)|(1<<VisualBattlePlayback.Layer));
            sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(48,-38,0); sun.intensity = 1.08f;
            sun.color = new Color(1,.91f,.79f); sun.shadows = LightShadows.Soft; sun.shadowStrength=.82f; sun.shadowBias=.025f; sun.shadowNormalBias=.25f;
            art.Box(world,"Ashland terrain",new Vector3(0,-.25f,0),new Vector3(120,.45f,120),SettlementArt.Sand);
            art.Box(world,"Settlement slab",new Vector3(0,0,0),new Vector3(SettlementLayout.HalfWidth*2,.12f,SettlementLayout.HalfDepth*2),new Color32(144,130,101,255));
            art.Box(world,"North south road",new Vector3(0,.08f,0),new Vector3(1.8f,.03f,SettlementLayout.HalfDepth*2-1),new Color32(96,100,93,255));
            art.Box(world,"Crossing road",new Vector3(0,.08f,-1),new Vector3(SettlementLayout.HalfWidth*2-1,.03f,1.5f),new Color32(96,100,93,255));
            for (int i = 0; i < 12; i++) art.Box(world,"Road marking",new Vector3(0,.11f,-7+i*1.2f),new Vector3(.09f,.012f,.45f),SettlementArt.Amber);
            Building("HEADQUARTERS","Your command post. Open HEROES to manage survivors and their armies.",new Vector3(-3,0,3),new Vector3(3.4f,2.3f,2.8f),"Headquarters",BuildingKind.Headquarters);
            Building("WORKSHOP","A salvaged warehouse of tools, spare parts, and forgotten machines.",new Vector3(3,0,2.5f),new Vector3(3,2.3f,2.8f),"Workshop");
            Building("BARRACKS","Prepare each hero’s independent army here. Select a hero, choose fighter types and use the slider or FILL.",new Vector3(-3.3f,0,-4),new Vector3(3,1.8f,2.6f),"Barracks");
            Building("WATER STATION","Protected tanks hold the outpost's water. Expeditions can bring back more.",new Vector3(3.4f,0,-4),new Vector3(3,2.6f,2.6f),"WaterStation",BuildingKind.WaterStation);
            Building("FOOD GARDEN","Raised beds and reclaimed soil: a promise of life returning to the Ashlands.",new Vector3(3.5f,0,6),new Vector3(3,.9f,2.5f),"FoodGarden",BuildingKind.FoodGarden);
            var clinic=Marker("HOSPITAL","Heal wounded fighters; upgrade beds and recovery speed.",new Vector3(-3,0,6.5f),new Vector3(3,2.5f,2.5f));
            clinic.GetComponent<SettlementBuilding>().Configure("HOSPITAL","All wounded are recoverable. Heroes cannot be wounded.",hospital:true,placementId:"core-hospital");
            RealisticAssets.Building(clinic,"Barracks",new Vector2(3,2.5f)).transform.localScale=Vector3.one*.8f;
            art.Box(clinic,"Clinic roof sign",new Vector3(0,1.92f,0),new Vector3(1,.04f,1),new Color32(40,103,86,255),false);
            art.Box(clinic,"Roof plus upright",new Vector3(0,1.95f,0),new Vector3(.16f,.03f,.7f),Color.white,false);
            art.Box(clinic,"Roof plus crossbar",new Vector3(0,1.96f,0),new Vector3(.7f,.03f,.16f),Color.white,false);
            art.Box(clinic,"Clinic sign",new Vector3(0,1.3f,-1.35f),new Vector3(.9f,.8f,.05f),new Color32(40,103,86,255),false);
            art.Box(clinic,"Medical plus upright",new Vector3(0,1.3f,-1.39f),new Vector3(.16f,.55f,.04f),Color.white,false);
            art.Box(clinic,"Medical plus crossbar",new Vector3(0,1.3f,-1.40f),new Vector3(.55f,.16f,.04f),Color.white,false);
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*SettlementLayout.HalfWidth;
                for(float z=-SettlementLayout.HalfDepth;z<=SettlementLayout.HalfDepth;z+=1.4f)
                {
                    art.Box(world,"Fence post",new Vector3(x,.65f,z),new Vector3(.16f,1.3f,.16f),SettlementArt.Timber);
                    if(z+1.4f<=SettlementLayout.HalfDepth)for(int rail=0;rail<2;rail++)art.Box(world,"Fence rail",new Vector3(x,.4f+rail*.5f,z+.7f),new Vector3(.1f,.1f,1.4f),SettlementArt.Metal);
                }
                Tower(new Vector3(x,0,SettlementLayout.HalfDepth-1));
            }
            for(float x=-SettlementLayout.HalfWidth;x<=SettlementLayout.HalfWidth;x+=1.4f)
            {
                art.Box(world,"Rear barrier",new Vector3(x,.55f,SettlementLayout.HalfDepth),new Vector3(1.4f,1.1f,.2f),SettlementArt.Rust);
                if(Mathf.Abs(x)>2.4f)art.Box(world,"Gate wall",new Vector3(x,.55f,-SettlementLayout.HalfDepth),new Vector3(1.4f,1.1f,.25f),SettlementArt.Concrete);
            }
            art.Box(world,"Gate sign",new Vector3(0,2.4f,-SettlementLayout.HalfDepth),new Vector3(3.5f,.55f,.18f),SettlementArt.Metal);
            for(int i=0;i<2;i++)art.Box(world,"Gate pillars",new Vector3(-1.8f+i*3.6f,1.25f,-SettlementLayout.HalfDepth),new Vector3(.25f,2.5f,.3f),SettlementArt.Rust);
            for (int i = 0; i < 10; i++)
            {
                float angle = i*2.39996f; float radius = 48+i%3*3;
                var point = new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                art.Rock(world,point,new Vector3(1.2f+i%3,.8f+i%2,1.1f+i%2));
                art.Box(world,"Dead tree",point+new Vector3(1.5f,1,0),new Vector3(.15f,2,.15f),SettlementArt.Timber).localRotation = Quaternion.Euler(0,0,10);
                art.Box(world,"Bare branch",point+new Vector3(1.8f,1.6f,0),new Vector3(.9f,.1f,.1f),SettlementArt.Timber).localRotation = Quaternion.Euler(0,0,35);
            }
            for (int i = 0; i < 6; i++)
            {
                art.Box(world,"Supply crate",new Vector3(5.3f,.32f,-.5f+i*.65f),new Vector3(.6f,.6f,.55f),SettlementArt.Timber);
                art.Box(world,"Crate strap",new Vector3(5.3f,.64f,-.5f+i*.65f),new Vector3(.12f,.035f,.57f),SettlementArt.Metal);
            }
            // A salvaged truck and its tires, all original geometry.
            art.Box(world,"Truck chassis",new Vector3(-5,.55f,-.9f),new Vector3(1.4f,.45f,2.5f),SettlementArt.Metal);
            art.Box(world,"Truck cab",new Vector3(-5,1,-1.65f),new Vector3(1.3f,.75f,1),SettlementArt.Rust);
            art.Box(world,"Truck windshield",new Vector3(-5,1.15f,-2.17f),new Vector3(1.05f,.35f,.03f),SettlementArt.Glass);
            for (int wheel = 0; wheel < 4; wheel++) art.Box(world,"Truck tire",new Vector3(-5+(wheel%2-.5f)*1.4f,.35f,-1.6f+wheel/2*1.5f),new Vector3(.5f,.18f,.5f),new Color32(43,45,43,255),true,PrimitiveType.Cylinder).localRotation = Quaternion.Euler(0,0,90);
            for(int x=-8;x<=8;x+=16)art.Box(world,"District road",new Vector3(x,.08f,0),new Vector3(1.2f,.03f,SettlementLayout.HalfDepth*2-2),new Color32(96,100,93,255));
            foreach(int z in new[]{-10,10})art.Box(world,"District crossroad",new Vector3(0,.08f,z),new Vector3(SettlementLayout.HalfWidth*2-2,.03f,1.2f),new Color32(96,100,93,255));
            art.Combine(world);
            RefreshPlots();
            SyncPlacement();
            RealisticAssets.Character(world,"Engineer","Workshop engineer",new Vector3(3,0,-.65f),new[] { new Vector3(3,0,-.65f),new Vector3(1.4f,0,-.65f) },false,.10f);
            RealisticAssets.Character(world,"Sentry","Gate sentry",new Vector3(-1.2f,0,-SettlementLayout.HalfDepth+2));
        }
        private void RefreshPlots()
        {
            if(world==null)return;var saved=gameManager.Buildings.CaptureDevelopment();var signature=new StringBuilder();
            foreach(var p in saved.Plots)signature.Append(p.Id).Append(':').Append((int)p.Kind).Append(':').Append(p.Level).Append(';');
            if(plotVisualSignature==signature.ToString())return;plotVisualSignature=signature.ToString();
            if(developmentWorld!=null){developmentWorld.SetActive(false);Destroy(developmentWorld);}developmentArt?.Dispose();developmentArt=new SettlementArt();
            developmentWorld=new GameObject("Settlement building plots");developmentWorld.transform.SetParent(world,false);
            foreach(var definition in SettlementLayout.Plots)
            {
                var p=gameManager.Buildings.Plot(definition.Id);
                var pad=new GameObject("Fixed building spot "+definition.Id).transform;pad.SetParent(developmentWorld.transform,false);pad.localPosition=new Vector3(definition.X,0,definition.Z);
                pad.gameObject.AddComponent<SettlementBuilding>().Configure("Building spot","",plotId:definition.Id,movable:false,spot:true);
                var padCollider=pad.gameObject.AddComponent<BoxCollider>();padCollider.center=new Vector3(0,.10f,0);padCollider.size=new Vector3(6,.10f,6);
                developmentArt.Box(pad,"Plot foundation",new Vector3(0,.06f,0),new Vector3(6,.08f,6),new Color32(125,115,91,255));
                for(int side=0;side<2;side++)
                {
                    developmentArt.Box(pad,"Plot side marker",new Vector3(side==0?-2.9f:2.9f,.13f,0),new Vector3(.08f,.08f,5.8f),SettlementArt.Timber);
                    developmentArt.Box(pad,"Plot end marker",new Vector3(0,.13f,side==0?-2.9f:2.9f),new Vector3(5.8f,.08f,.08f),SettlementArt.Timber);
                }
                developmentArt.Combine(pad);
                if(p.Kind==PlotKind.Empty)continue;
                var position=gameManager.Buildings.Position(p.Id);
                var root=new GameObject(p.Id+" "+BuildingService.PlotName(p.Kind)).transform;root.SetParent(developmentWorld.transform,false);root.localPosition=new Vector3(position.X,0,position.Z);
                root.gameObject.AddComponent<SettlementBuilding>().Configure(BuildingService.PlotName(p.Kind),"",plotId:p.Id);
                if(p.Kind!=PlotKind.Empty)
                {
                    string asset=p.Kind==PlotKind.BikerCenter?"Workshop":p.Kind==PlotKind.InfantryCenter || p.Kind==PlotKind.ArmyCenter?"Barracks":p.Kind==PlotKind.Farm?"FoodGarden":p.Kind==PlotKind.WaterWell?"WaterStation":p.Kind==PlotKind.Scrapyard?"Workshop":"Barracks";
                    var model=RealisticAssets.Building(root,asset,new Vector2(4,4));model.transform.localScale=Vector3.one*(.9f+.02f*p.Level);
                    if(BuildingService.IsTraining(p.Kind))BuildTrainingProps(root,p.Kind);
                    if(p.Kind==PlotKind.WaterWell)model.transform.localPosition=new Vector3(.55f,0,.55f);
                    if(p.Kind==PlotKind.Scrapyard)for(int n=0;n<5;n++)
                    {
                        var scrap=developmentArt.Box(root,"Salvage pile",new Vector3(-2+n*.9f,.35f,-1.8f),new Vector3(.65f,.55f,.65f),n%2==0?SettlementArt.Rust:SettlementArt.Metal);scrap.localRotation=Quaternion.Euler(12,n*27,14);
                    }
                    if(p.Kind==PlotKind.WaterWell)
                    {
                        developmentArt.Box(root,"Well stone ring",new Vector3(-1.8f,.45f,-1.7f),new Vector3(.9f,.4f,.9f),SettlementArt.Concrete,true,PrimitiveType.Cylinder);
                        developmentArt.Box(root,"Well opening",new Vector3(-1.8f,.86f,-1.7f),new Vector3(.65f,.015f,.65f),new Color32(25,44,49,255),true,PrimitiveType.Cylinder);
                        developmentArt.Box(root,"Well handle post",new Vector3(-2.2f,1.05f,-1.7f),new Vector3(.1f,.6f,.1f),SettlementArt.Timber);
                        developmentArt.Box(root,"Well hand pump",new Vector3(-1.9f,1.36f,-1.7f),new Vector3(.65f,.08f,.08f),SettlementArt.Metal);
                    }
                }
                developmentArt.Combine(root);
                FitBuildingCollider(root);
            }
        }
        private static void FitBuildingCollider(Transform root)
        {
            var bounds=new Bounds(Vector3.up,Vector3.zero);bool first=true;
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.name=="Soft contact grounding")continue;
                var b=renderer.bounds;
                for(int corner=0;corner<8;corner++)
                {
                    var point=root.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                    if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                }
            }
            var collider=root.gameObject.AddComponent<BoxCollider>();collider.center=bounds.center;
            collider.size=new Vector3(Mathf.Max(1,bounds.size.x),Mathf.Max(.5f,bounds.size.y),Mathf.Max(1,bounds.size.z));
        }
        private Transform Marker(string title,string description,Vector3 position,Vector3 size,BuildingKind? kind=null)
        {
            var root = new GameObject(title).transform; root.SetParent(world,false); root.localPosition = position;
            root.gameObject.AddComponent<SettlementBuilding>().Configure(title,description,kind);
            var collider = root.gameObject.AddComponent<BoxCollider>(); collider.center = new Vector3(0,size.y/2,0); collider.size = size;
            return root;
        }
        private void Building(string title,string description,Vector3 position,Vector3 size,string assetName,BuildingKind? kind=null)
        {
            var root = Marker(title,description,position,size+new Vector3(0,.8f,0),kind);
            root.GetComponent<SettlementBuilding>().Configure(title,description,kind,placementId:"core-"+assetName.ToLowerInvariant());
            if(kind==BuildingKind.Headquarters)RealisticAssets.Headquarters(root);
            else RealisticAssets.Building(root,assetName,new Vector2(size.x,size.z));
        }
        private void Tower(Vector3 position)
        {
            for (int i = 0; i < 4; i++) art.Box(world,"Tower strut",position+new Vector3((i%2-.5f)*1.2f,1.6f,(i/2-.5f)*1.2f),new Vector3(.16f,3.2f,.16f),SettlementArt.Timber);
            art.Box(world,"Lookout deck",position+new Vector3(0,3.1f,0),new Vector3(1.8f,.18f,1.8f),SettlementArt.Timber);
            art.Box(world,"Lookout shelter",position+new Vector3(0,3.55f,0),new Vector3(1.3f,.75f,1.3f),SettlementArt.Metal);
            art.Roof(world,position+new Vector3(0,4,0),new Vector3(1.8f,.4f,1.8f));
        }
        private void Inspect(SettlementBuilding building)
        {
            selection.text = building.DisplayName+"\n"+building.Description;
            if(building.IsSpot)
            {
                var occupant=gameManager.Buildings.BuildingAt(building.transform.localPosition.x,building.transform.localPosition.z);
                if(occupant!=null)
                {
                    foreach(var item in world.GetComponentsInChildren<SettlementBuilding>())if(item.PlacementId==occupant){Inspect(item);return;}
                }
                ShowDevelopment();developmentUI.ShowSpot(building.PlotId);
                controls.FocusOn(building.transform.position,8);
            }
            else if(building.PlotId!=null)ShowDevelopment(building.PlotId);
            else if(building.PlacementId=="core-barracks")ShowBarracks();
            else if(building.IsHospital)ShowHospital();
            else if(building.Kind.HasValue)ShowBuilding(building.Kind.Value);
        }
        private bool SettlementInputAllowed()=>Time.unscaledTime>=navigationReadyAt && (barracksPanel==null || !barracksPanel.activeSelf)
            && (heroPanel==null || !heroPanel.activeSelf) && (raidPanel==null || !raidPanel.activeSelf)
            && (buildingPanel==null || !buildingPanel.activeSelf) && (scavengingMap==null || !scavengingMap.gameObject.activeSelf)
            && (hospitalUI==null || !hospitalUI.gameObject.activeSelf) && (developmentUI==null || !developmentUI.gameObject.activeSelf);
        private void BeginBuildingMove(SettlementBuilding building)
        {
            CancelReset();if(toast!=null)toast.text="MOVE "+building.DisplayName+" • Drag, then release • Green = valid • Red = blocked";
        }
        private void CancelBuildingMove(){if(toast!=null)toast.text="Move cancelled. Original position kept.";}
        private void CommitBuildingMove(SettlementBuilding building,Vector3 point)
        {
            var p=world.InverseTransformPoint(point);var result=gameManager.Buildings.MoveBuilding(building.PlacementId,p.x,p.z);
            var position=gameManager.Buildings.Position(building.PlacementId);building.transform.localPosition=new Vector3(position.X,0,position.Z);
            if(result==PlacementResult.Success)toast.text=gameManager.SaveProgress()?"Building moved • Saved locally":gameManager.SaveStatus;
            else toast.text="Cannot place here: "+result+". Original position kept.";
        }
        private void SyncPlacement()
        {
            if(world==null)return;
            foreach(var building in world.GetComponentsInChildren<SettlementBuilding>())
            {
                if(building.PlacementId==null || (controls!=null && controls.IsMoving(building)))continue;
                var position=gameManager.Buildings.Position(building.PlacementId);building.transform.localPosition=new Vector3(position.X,0,position.Z);
            }
        }
        private void ShowMapFromZoom()
        {
            ShowMap();scavengingMap.EnterFromSettlement();FadeNavigation();
        }
        private void ReturnFromWorldMap()
        {
            scavengingMap.Hide();controls.ResetView();controls.SuspendInput(.4f);FadeNavigation();
            toast.text="Settlement • Hold a building for 1.5 seconds to move it.";
        }
        private void FadeNavigation()
        {
            if(!Application.isPlaying)return;navigationReadyAt=Time.unscaledTime+.3f;
            if(navigationCover==null)
            {
                var cover=Panel(ui,"Map navigation fade",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,Color.black);
                navigationCover=cover.gameObject.AddComponent<CanvasGroup>();
            }
            navigationCover.gameObject.SetActive(true);navigationCover.transform.SetAsLastSibling();navigationCover.alpha=1;navigationCover.blocksRaycasts=true;
            StartCoroutine(FinishNavigationFade());
        }
        private System.Collections.IEnumerator FinishNavigationFade()
        {
            while(Time.unscaledTime<navigationReadyAt){navigationCover.alpha=Mathf.Clamp01((navigationReadyAt-Time.unscaledTime)/.3f);yield return null;}
            navigationCover.alpha=0;navigationCover.blocksRaycasts=false;navigationCover.gameObject.SetActive(false);
        }
        private void ShowBuilding(BuildingKind kind)
        {
            if(barracksPanel!=null)barracksPanel.SetActive(false);
            developmentUI.Hide();scavengingMap.Hide();hospitalUI.Hide();
            CancelReset(); selectedBuilding=kind; heroPanel.SetActive(false); raidPanel.SetActive(false);
            buildingPanel.SetActive(true); RefreshBuilding();
        }
        private void RefreshBuilding()
        {
            if(buildingStats==null)return;
            var buildings=gameManager.Buildings; var kind=selectedBuilding;
            var text=new StringBuilder(BuildingService.Name(kind).ToUpperInvariant());
            text.Append("  LV ").Append(buildings.Level(kind)).Append('/').Append(BuildingService.MaximumLevel(kind));
            if(kind==BuildingKind.Headquarters)text.Append("\n\nGoverns the maximum level of the Garden and Water Station.");
            else {
                text.Append("\n\nProduction: ").Append(buildings.Rate(kind)).Append(' ').Append(BuildingService.Product(kind)).Append(" / minute")
                    .Append("\nStored: ").Append(buildings.Stored(kind)).Append('/').Append(buildings.Capacity(kind))
                    .Append("\n").Append(buildings.Stored(kind)==buildings.Capacity(kind) ? "Storage full — collect to resume." : "Next batch in "+(60-buildings.Seconds(kind))+"s")
                    .Append("\nProduces while the game is open.");
            }
            var status=buildings.CanUpgrade(kind);
            if(status==BuildingCommandResult.MaximumLevel)text.Append("\n\nMaximum level reached.");
            else {
                text.Append("\n\nNext level cost:");
                foreach(var cost in buildings.UpgradeCost(kind))text.Append("\n").Append(cost.Type).Append(' ').Append(cost.Amount).Append(" / have ").Append(gameManager.Resources.Get(cost.Type));
                text.Append("\n").Append(status==BuildingCommandResult.HeadquartersRequired ? "Upgrade Headquarters first." : status==BuildingCommandResult.InsufficientResources ? "Gather more resources to upgrade." : "Ready — upgrade completes instantly.");
            }
            buildingStats.text=text.ToString();
            // The panel starts inactive. Read the known button parent directly: ancestor searches skip inactive objects.
            upgradeBuilding.text=status==BuildingCommandResult.MaximumLevel ? "MAX LEVEL" : "UPGRADE";
            upgradeBuilding.transform.parent.GetComponent<UnityEngine.UI.Button>().interactable=status==BuildingCommandResult.Success;
            collectBuilding.text=kind==BuildingKind.Headquarters ? "VIEW HQ" : "COLLECT "+buildings.Stored(kind);
            collectBuilding.transform.parent.GetComponent<UnityEngine.UI.Button>().interactable=kind==BuildingKind.Headquarters || buildings.Stored(kind)>0;
        }
        private void UpgradeBuilding()
        {
            CancelReset(); var result=gameManager.Buildings.Upgrade(selectedBuilding);
            toast.text=result==BuildingCommandResult.Success ? BuildingService.Name(selectedBuilding)+" upgraded to level "+gameManager.Buildings.Level(selectedBuilding)+"." : result.ToString();
            RefreshBuilding();
        }
        private void CollectBuilding()
        {
            CancelReset();
            if(selectedBuilding==BuildingKind.Headquarters) {
                buildingPanel.SetActive(false);var hq=gameManager.Buildings.Position("core-headquarters");controls.FocusOn(new Vector3(hq.X,1.5f,hq.Z),6.5f);
                toast.text="Headquarters — drag or zoom to explore. HOME returns to the overview."; return;
            }
            int amount=gameManager.Buildings.Stored(selectedBuilding);
            var result=gameManager.Buildings.Collect(selectedBuilding);
            toast.text=result==BuildingCommandResult.Success ? "Collected "+amount+" "+BuildingService.Product(selectedBuilding)+"." : result==BuildingCommandResult.ResourceLimit ? "Resource balance is full. Spend resources first." : "Nothing ready to collect.";
            RefreshBuilding();
        }
        private void BuildPortrait()
        {
            HeroPortraitCatalog.Apply(heroIllustration,gameManager.Heroes[selectedHero].Definition.Id);
        }
        private void UpdateLeader()
        {
            if (selectedHero==renderedHero) return; renderedHero=selectedHero;
            if (leader != null) { leader.gameObject.SetActive(false); Destroy(leader.gameObject); }
            var hero=gameManager.Heroes[selectedHero].Definition;
            leader = RealisticAssets.Hero(world,hero.Id,hero.DisplayName,new Vector3(-1.4f,0,-1)).transform;
        }
        private void Refresh()
        {
            var hero = gameManager.Heroes[selectedHero];
            resources.text = "FOOD  "+gameManager.Resources.Get(ResourceType.Food)+"      SCRAP  "+gameManager.Resources.Get(ResourceType.Scrap)
                +"\nWATER  "+gameManager.Resources.Get(ResourceType.Water)+"      FUEL  "+gameManager.Resources.Get(ResourceType.Fuel);
            resources.text += "\nLUMBER  "+gameManager.Resources.Get(ResourceType.Lumber);
            leaderLabel.text = HeroRarityColors.Text(hero.Definition.DisplayName.ToUpperInvariant(),hero.Definition.Rarity)+"\nLv "+hero.Level+"  •  Army "+hero.Army.TotalUnits+"/"+hero.Army.Capacity;
            var text = new StringBuilder(HeroRarityColors.Text(hero.Definition.DisplayName+"\n"+hero.Definition.Rarity,hero.Definition.Rarity)+" • "+hero.Definition.Specialization+"\n\n");
            text.Append("LEVEL ").Append(hero.Level).Append("    XP ").Append(hero.Xp).Append('/').Append(hero.XpToNextLevel)
                .Append("\nARMY ").Append(hero.Army.TotalUnits).Append('/').Append(hero.Army.Capacity).Append("\nPOWER ").Append(hero.Army.Power.ToString("0.##"))
                .Append("\nCAPACITY +").Append(hero.Definition.CapacityPerLevel).Append(" per level");
            foreach(var unit in gameManager.Units)text.Append("\n").Append(unit.Definition.Type).Append(": ").Append(hero.Army.QuantityOf(unit.Definition.Id,unit.Level));
            text
                .Append("\nBONUS +").Append(hero.Definition.BonusAt(hero.Level)/100m).Append("% matching fighters")
                .Append("\nFRAGMENTS ").Append(gameManager.Fragments.Get(hero.Definition.Id)).Append("\n\n").Append(hero.Definition.Biography);
            heroStats.text = text.ToString();
            heroPortraitFrame.color=HeroRarityColors.For(hero.Definition.Rarity);
            raidHeader.text = "WASTELAND RAID\n"+HeroRarityColors.Text(hero.Definition.DisplayName,hero.Definition.Rarity)+" • "+hero.Army.TotalUnits+" fighters";
            if (camps.Count>0) campButton.text = camps[selectedCamp].Name.ToUpperInvariant();
            if(heroIllustration!=null)BuildPortrait();
            if (world != null) UpdateLeader();
            RefreshBuilding();
            RefreshFighterControls();RefreshBarracksRoster();
            RefreshPlots();
            SyncPlacement();
        }
        private void ShowDevelopment(string id=null)
        {
            if(barracksPanel!=null)barracksPanel.SetActive(false);
            CancelReset();scavengingMap.Hide();hospitalUI.Hide();heroPanel.SetActive(false);raidPanel.SetActive(false);buildingPanel.SetActive(false);if(id==null)developmentUI.ShowConstruction();else developmentUI.Show(id);
        }
        private void ShowHospital()
        {
            if(barracksPanel!=null)barracksPanel.SetActive(false);
            CancelReset();developmentUI.Hide();scavengingMap.Hide();heroPanel.SetActive(false);raidPanel.SetActive(false);buildingPanel.SetActive(false);hospitalUI.Show();
        }
        private void ShowMap()
        {
            if(barracksPanel!=null)barracksPanel.SetActive(false);
            developmentUI.Hide();hospitalUI.Hide();
            CancelReset(); heroPanel.SetActive(false); raidPanel.SetActive(false); buildingPanel.SetActive(false);
            scavengingMap.Show(); scavengingMap.ShowOfflineSummary(gameManager.OfflineSummary); scavengingMap.transform.SetAsLastSibling();
        }
        private void ShowHeroes(bool visible) { preparingFromBarracks=false;if(barracksPanel!=null)barracksPanel.SetActive(false); heroHeading.text="SURVIVOR DOSSIER"; developmentUI.Hide();hospitalUI.Hide();scavengingMap.Hide(); buildingPanel.SetActive(false); heroPanel.SetActive(visible); raidPanel.SetActive(false); Refresh(); }
        private void ShowRaid()=>ShowMap();
        private void ShowWorldRaidResult(CombatResult outcome)
        {
            for(int i=0;i<gameManager.Heroes.Count;i++)if(gameManager.Heroes[i].InstanceId==outcome.HeroInstanceId)selectedHero=i;
            for(int i=0;i<camps.Count;i++)if(camps[i].Id==outcome.EncounterId)selectedCamp=i;
            scavengingMap.Hide();raidPanel.SetActive(true);raidPanel.transform.SetAsLastSibling();results.Show(outcome,true);Refresh();
        }
        private int MaximumReinforcements()
        {
            var hero=gameManager.Heroes[selectedHero];var fighter=gameManager.Units[selectedFighter];
            if(gameManager.WorldMap.IsDeployed(hero.InstanceId))return 0;
            return Math.Min(Math.Max(0,hero.Army.Capacity-hero.Army.TotalUnits),gameManager.HeroArmies.Available(fighter.Definition.Id));
        }
        private void RefreshFighterControls()
        {
            if(fighterQuantity==null)return;
            int maximum=MaximumReinforcements();
            fighterQuantity.minValue=0;fighterQuantity.maxValue=Math.Max(1,maximum);
            fighterQuantity.SetValueWithoutNotify(Mathf.Clamp(fighterQuantity.value,0,maximum));fighterQuantity.interactable=maximum>0;
            for(int i=0;i<fighterTypeLabels.Count;i++)fighterTypeLabels[i].color=i==selectedFighter?SettlementArt.Amber:new Color32(234,227,204,255);
            UpdateFighterQuantity();
        }
        private void UpdateFighterQuantity()
        {
            int quantity=Mathf.RoundToInt(fighterQuantity.value),maximum=MaximumReinforcements();
            var fighter=gameManager.Units[selectedFighter];var hero=gameManager.Heroes[selectedHero];
            fighterQuantityLabel.text=fighter.Definition.Type+" +"+quantity+" / "+maximum+" • Army "+hero.Army.TotalUnits+" → "+(hero.Army.TotalUnits+quantity)+"/"+hero.Army.Capacity;
            addFightersLabel.text="ADD "+quantity;
            addFightersLabel.transform.parent.GetComponent<UnityEngine.UI.Button>().interactable=quantity>0;
            fillFightersLabel.text="FILL +"+maximum;
            fillFightersLabel.transform.parent.GetComponent<UnityEngine.UI.Button>().interactable=maximum>0;
        }
        private void Assign(bool fill)
        {
            CancelReset();var hero=gameManager.Heroes[selectedHero];var fighter=gameManager.Units[selectedFighter];
            int quantity=fill?MaximumReinforcements():Math.Min(Mathf.RoundToInt(fighterQuantity.value),MaximumReinforcements());
            if(quantity==0){toast.text="Army full or no fighters of this type in reserve.";return;}
            var result=gameManager.HeroArmies.AssignFighters(hero.InstanceId,fighter.Definition.Id,quantity);
            toast.text=result==ArmyCommandResult.Success ? quantity+" "+fighter.Definition.Type+" assigned to "+hero.Definition.DisplayName : "Assignment: "+result;
            RefreshFighterControls();
        }
        private void Raid(bool apply)
        {
            if(playback!=null && playback.IsOpen)return;
            CancelReset();
            if (camps.Count==0) { toast.text="No encounters configured."; return; }
            var hero=gameManager.Heroes[selectedHero];
            try
            {
                var outcome=apply ? gameManager.Battles.RunBattle(hero.InstanceId,camps[selectedCamp]) : gameManager.Battles.PreviewBattle(hero.InstanceId,camps[selectedCamp]);
                if(playback==null)playback=gameObject.AddComponent<VisualBattlePlayback>();
                playback.Play(outcome,hero.Definition.Id,hero.Definition.DisplayName,camps[selectedCamp].Name,hero.Definition.Rarity,apply,()=>
                { results.Show(outcome,apply);Refresh();toast.text=apply?"Raid settled. Survivors and rewards updated.":"Preview only — army and resources unchanged."; });
                if (apply) foreach (var error in gameManager.Battles.NotificationErrors) Debug.LogWarning(error,this);
            }
            catch (Exception error) { toast.text=error.Message+" Assign fighters from HEROES first."; }
        }
        private void BuildUI()
        {
            var canvas=new GameObject("Settlement HUD",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); canvas.transform.SetParent(presentation.transform,false);
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(540,960); scaler.matchWidthOrHeight=.5f;
            if (FindFirstObjectByType<EventSystem>()==null) new GameObject("Settlement Input",typeof(EventSystem),typeof(StandaloneInputModule)).transform.SetParent(presentation.transform,false);
            ui=new GameObject("Safe Area",typeof(RectTransform)).GetComponent<RectTransform>(); ui.SetParent(canvas.transform,false);
            var safe=Screen.safeArea; ui.anchorMin=new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height); ui.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height); ui.offsetMin=ui.offsetMax=Vector2.zero;
            resources=TextPanel(ui,0,1,1,1,12,-104,-12,-12,23,"",ink);
            Button(ui,"HOME",.79f,1,1,1,4,-150,-12,-112,()=>controls.ResetView());
            var resourceRect=resources.GetComponent<RectTransform>(); resourceRect.anchorMin=new Vector2(0,1); resourceRect.anchorMax=Vector2.one;
            resourceRect.offsetMin=new Vector2(12,-77); resourceRect.offsetMax=new Vector2(-12,-4);
            resources.fontSize=19;
            var statusObject=new GameObject("Local save status",typeof(RectTransform),typeof(Text)); statusObject.transform.SetParent(resources.transform.parent,false);
            var statusRect=statusObject.GetComponent<RectTransform>(); statusRect.anchorMin=Vector2.zero; statusRect.anchorMax=new Vector2(1,0); statusRect.offsetMin=new Vector2(12,4); statusRect.offsetMax=new Vector2(-12,24);
            saveStatus=statusObject.GetComponent<Text>(); saveStatus.font=font; saveStatus.fontSize=13; saveStatus.color=SettlementArt.Amber; saveStatus.raycastTarget=false; saveStatus.alignment=TextAnchor.MiddleLeft;
            Button(ui,"SAVE",0,1,.26f,1,12,-150,-4,-112,SaveProgress);
            Button(ui,"LOAD",.26f,1,.52f,1,4,-150,-4,-112,LoadProgress);
            resetButton=Button(ui,"RESET",.52f,1,.78f,1,4,-150,-4,-112,RequestReset);
            selection=TextPanel(ui,0,0,.56f,0,12,144,-4,196,15,"",ink);
            Button(ui,"CONSTRUCTION",0,0,.56f,0,12,200,-4,239,14,()=>ShowDevelopment());
            Button(ui,"BUILDINGS",.56f,0,.78f,0,4,193,-4,239,12,()=>ShowBuilding(BuildingKind.Headquarters));
            Button(ui,"HOSPITAL",.56f,0,.78f,0,4,144,-4,189,12,ShowHospital);
            Button(ui,"MAP",.78f,0,1,0,4,144,-12,239,16,ShowMap);
            leaderLabel=TextPanel(ui,0,0,.55f,0,12,88,-4,136,18,"",new Color32(41,57,59,246));
            Button(ui,"VIEW HERO",.55f,0,1,0,4,88,-12,136,()=>ShowHeroes(true));
            Button(ui,"HEROES",0,0,.5f,0,12,14,-4,76,22,()=>ShowHeroes(true));
            Button(ui,"WASTELAND RAID",.5f,0,1,0,4,14,-12,76,20,ShowRaid);
            heroPanel=Panel(ui,"Hero dossier",new Vector2(.04f,.30f),new Vector2(.96f,.88f),Vector2.zero,Vector2.zero,ink).gameObject;
            var heroRoot=heroPanel.GetComponent<RectTransform>();
            heroHeading=TextPanel(heroRoot,0,1,.8f,1,18,-66,-8,-12,25,"SURVIVOR DOSSIER",Color.clear);heroHeading.color=SettlementArt.Amber;
            heroHeading.resizeTextForBestFit=true;heroHeading.resizeTextMinSize=14;heroHeading.resizeTextMaxSize=25;
            Button(heroRoot,"X",.8f,1,1,1,4,-62,-12,-12,CloseHeroPanel);
            var portraitRect=Panel(heroRoot,"Original portrait",new Vector2(0,1),new Vector2(.38f,1),new Vector2(18,-294),new Vector2(-6,-82),new Color32(51,68,73,255));
            heroPortraitFrame=portraitRect.GetComponent<Image>();
            var inner=new GameObject("Portrait margin",typeof(RectTransform)).GetComponent<RectTransform>();inner.SetParent(portraitRect,false);
            inner.anchorMin=Vector2.zero;inner.anchorMax=Vector2.one;inner.offsetMin=new Vector2(5,5);inner.offsetMax=new Vector2(-5,-5);
            var portraitImage=new GameObject("Illustrated hero portrait",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter)); portraitImage.transform.SetParent(inner,false);
            var fit=portraitImage.GetComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=.75f;
            heroIllustration=portraitImage.GetComponent<RawImage>();heroIllustration.raycastTarget=false;
            heroStats=TextPanel(heroRoot,.38f,0,1,1,10,210,-18,-82,18,"",Color.clear);
            heroStats.resizeTextForBestFit=true;heroStats.resizeTextMinSize=12;heroStats.resizeTextMaxSize=17;
            Button(heroRoot,"<",0,1,.19f,1,18,-342,-4,-302,()=> { selectedHero=(selectedHero+7)%8; Refresh(); });
            Button(heroRoot,">",.19f,1,.38f,1,4,-342,-6,-302,()=> { selectedHero=(selectedHero+1)%8; Refresh(); });
            for(int i=0;i<3;i++) { int index=i;fighterTypeLabels.Add(Button(heroRoot,gameManager.Units[i].Definition.Type.ToString(),i/3f,0,(i+1)/3f,0,12,164,-8,202,14,()=> { selectedFighter=index;fighterQuantity.SetValueWithoutNotify(0);RefreshFighterControls(); })); }
            fighterQuantityLabel=TextPanel(heroRoot,0,0,1,0,12,127,-12,163,13,"",Color.clear);
            fighterQuantityLabel.resizeTextForBestFit=true;fighterQuantityLabel.resizeTextMinSize=10;fighterQuantityLabel.resizeTextMaxSize=13;
            var sliderRoot=Panel(heroRoot,"Fighter quantity slider",Vector2.zero,new Vector2(.72f,0),new Vector2(18,84),new Vector2(-8,126),Color.clear);
            sliderRoot.GetComponent<Image>().raycastTarget=true;
            fighterQuantity=sliderRoot.gameObject.AddComponent<Slider>();fighterQuantity.wholeNumbers=true;
            var track=Panel(sliderRoot,"Track",new Vector2(0,.35f),new Vector2(1,.65f),Vector2.zero,Vector2.zero,new Color32(63,75,76,255));
            var fillArea=Panel(track,"Fill area",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,Color.clear);fillArea.GetComponent<Image>().raycastTarget=false;
            var fill=Panel(fillArea,"Fill",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,SettlementArt.Amber);fill.GetComponent<Image>().raycastTarget=false;fighterQuantity.fillRect=fill;
            var handleArea=Panel(sliderRoot,"Handle area",Vector2.zero,Vector2.one,new Vector2(12,0),new Vector2(-12,0),Color.clear);handleArea.GetComponent<Image>().raycastTarget=false;
            var handle=Panel(handleArea,"Handle",new Vector2(0,.5f),new Vector2(0,.5f),Vector2.zero,Vector2.zero,new Color32(245,224,170,255));handle.sizeDelta=new Vector2(24,38);
            fighterQuantity.handleRect=handle;fighterQuantity.targetGraphic=handle.GetComponent<Image>();fighterQuantity.direction=Slider.Direction.LeftToRight;
            fighterQuantity.onValueChanged.AddListener(_=>UpdateFighterQuantity());
            fillFightersLabel=Button(heroRoot,"FILL",.72f,0,1,0,4,84,-12,126,15,()=>Assign(true));
            Button(heroRoot,"−1",0,0,.22f,0,12,46,-4,80,16,()=>fighterQuantity.value=Mathf.Max(0,fighterQuantity.value-1));
            addFightersLabel=Button(heroRoot,"ADD",.22f,0,.78f,0,4,46,-4,80,16,()=>Assign(false));
            Button(heroRoot,"+1",.78f,0,1,0,4,46,-12,80,16,()=>fighterQuantity.value=Mathf.Min(MaximumReinforcements(),fighterQuantity.value+1));
            Button(heroRoot,"TAKE THIS HERO ON A RAID",0,0,1,0,18,4,-18,42,17,ShowRaid);
            heroPanel.SetActive(false);
            raidPanel=Panel(ui,"Wasteland raid",new Vector2(.04f,.30f),new Vector2(.96f,.82f),Vector2.zero,Vector2.zero,ink).gameObject;
            var raidRoot=raidPanel.GetComponent<RectTransform>();
            raidHeader=TextPanel(raidRoot,0,1,.84f,1,18,-80,-4,-12,21,"",Color.clear);
            Button(raidRoot,"X",.84f,1,1,1,4,-62,-12,-12,()=>raidPanel.SetActive(false));
            campButton=Button(raidRoot,"CAMP",0,1,1,1,18,-135,-18,-88,19,()=> { if(camps.Count==0)return; selectedCamp=(selectedCamp+1)%camps.Count; results.Clear(); Refresh(); });
            var report=TextPanel(raidRoot,0,0,1,1,18,80,-18,-144,17,"",new Color32(32,42,43,255));
            var viewport=report.transform.parent.GetComponent<RectTransform>(); viewport.gameObject.AddComponent<RectMask2D>();
            var content=report.GetComponent<RectTransform>(); content.anchorMin=new Vector2(0,1); content.anchorMax=Vector2.one; content.pivot=new Vector2(.5f,1); content.offsetMin=new Vector2(12,0); content.offsetMax=new Vector2(-12,-8);
            report.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.content=content; scroll.horizontal=false; scroll.movementType=ScrollRect.MovementType.Clamped;
            results=viewport.gameObject.AddComponent<CombatResultsUI>(); results.Bind(report);
            Button(raidRoot,"PREVIEW",0,0,.5f,0,18,14,-4,65,19,()=>Raid(false));
            Button(raidRoot,"WORLD MAP",.5f,0,1,0,4,14,-18,65,19,ShowRaid);
            raidPanel.SetActive(false);
            buildingPanel=Panel(ui,"Building management",new Vector2(.04f,.30f),new Vector2(.96f,.82f),Vector2.zero,Vector2.zero,ink).gameObject;
            var buildingRoot=buildingPanel.GetComponent<RectTransform>();
            TextPanel(buildingRoot,0,1,.8f,1,12,-62,-4,-8,23,"OUTPOST BUILDINGS",Color.clear).color=SettlementArt.Amber;
            Button(buildingRoot,"X",.8f,1,1,1,4,-62,-12,-8,()=>buildingPanel.SetActive(false));
            Button(buildingRoot,"HQ",0,1,.33f,1,12,-111,-4,-68,()=>ShowBuilding(BuildingKind.Headquarters));
            Button(buildingRoot,"GARDEN",.33f,1,.66f,1,4,-111,-4,-68,()=>ShowBuilding(BuildingKind.FoodGarden));
            Button(buildingRoot,"WATER",.66f,1,1,1,4,-111,-12,-68,()=>ShowBuilding(BuildingKind.WaterStation));
            buildingStats=TextPanel(buildingRoot,0,0,1,1,12,80,-12,-117,18,"",Color.clear);
            buildingStats.resizeTextForBestFit=true; buildingStats.resizeTextMinSize=12; buildingStats.resizeTextMaxSize=18;
            upgradeBuilding=Button(buildingRoot,"UPGRADE",0,0,.5f,0,12,14,-4,70,18,UpgradeBuilding);
            collectBuilding=Button(buildingRoot,"COLLECT",.5f,0,1,0,4,14,-12,70,18,CollectBuilding);
            buildingPanel.SetActive(false);
            scavengingMap=ScavengingMapUI.Create(ui,gameManager.Scavenging,font,gameManager.SaveProgress,()=>gameManager.SaveStatus);
            scavengingMap.ConfigureWorld(gameManager.WorldMap,()=>gameManager.Heroes[selectedHero],delta=>{selectedHero=(selectedHero+delta+gameManager.Heroes.Count)%gameManager.Heroes.Count;Refresh();},ShowWorldRaidResult,id=>{for(int i=0;i<gameManager.Heroes.Count;i++)if(gameManager.Heroes[i].InstanceId==id){selectedHero=i;Refresh();break;}},PrepareSelectedHero);
            scavengingMap.SettlementRequested+=ReturnFromWorldMap;
            hospitalUI=HospitalUI.Create(ui,gameManager,font);
            developmentUI=DevelopmentUI.Create(ui,gameManager,font);
            developmentUI.PlotSelected+=id=>{var p=gameManager.Buildings.Position(id);controls.FocusOn(new Vector3(p.X,0,p.Z),8);};
            BuildBarracksRoster();
            toast=TextPanel(ui,0,0,1,0,12,244,-12,282,15,"Welcome to Ember Outpost. Choose a survivor and assemble an army.",new Color32(35,44,44,210));
            toast.resizeTextForBestFit=true; toast.resizeTextMinSize=9; toast.resizeTextMaxSize=15;
        }
        private RectTransform Panel(Transform parent,string name,Vector2 min,Vector2 max,Vector2 lower,Vector2 upper,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image)); obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>(); rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=lower; rect.offsetMax=upper;
            obj.GetComponent<Image>().color=color; return rect;
        }
        private Text TextPanel(Transform parent,float x0,float y0,float x1,float y1,float l,float b,float r,float t,int size,string value,Color background)
        {
            var panel=Panel(parent,"HUD panel",new Vector2(x0,y0),new Vector2(x1,y1),new Vector2(l,b),new Vector2(r,t),background);
            var child=new GameObject("Label",typeof(RectTransform),typeof(Text)); child.transform.SetParent(panel,false);
            var rect=child.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=new Vector2(12,8); rect.offsetMax=new Vector2(-12,-8);
            var text=child.GetComponent<Text>(); text.font=font; text.fontSize=size; text.color=new Color32(234,227,204,255); text.text=value; text.raycastTarget=false; text.alignment=TextAnchor.MiddleLeft;
            if (background.a==0) panel.GetComponent<Image>().raycastTarget=false;
            return text;
        }
        private Text Button(Transform parent,string title,float x0,float y0,float x1,float y1,float l,float b,float r,float t,Action action)
            => Button(parent,title,x0,y0,x1,y1,l,b,r,t,17,action);
        private Text Button(Transform parent,string title,float x0,float y0,float x1,float y1,float l,float b,float r,float t,int size,Action action)
        {
            var text=TextPanel(parent,x0,y0,x1,y1,l,b,r,t,size,title,new Color32(111,78,43,255)); text.alignment=TextAnchor.MiddleCenter;
            var button=text.transform.parent.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic=text.transform.parent.GetComponent<Image>(); button.onClick.AddListener(()=>action()); return text;
        }
    }
}
