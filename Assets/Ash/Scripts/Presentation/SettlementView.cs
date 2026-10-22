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
        private Text heroStats, raidHeader, campButton, toast, leaderLabel, saveStatus, resetButton;
        private float resetDeadline;
        private BuildingKind selectedBuilding;
        private Text buildingStats, upgradeBuilding, viewHeadquarters;
        private CombatResultsUI results;
        private VisualBattlePlayback playback;
        private ScavengingMapUI scavengingMap;
        private HospitalUI hospitalUI;
        private DevelopmentUI developmentUI;
        private GameObject developmentWorld;
        private SettlementArt developmentArt;
        private string plotVisualSignature;
        private Text heroHeading;
        private int selectedHero, selectedCamp, renderedHero = -1;
        private Image heroPortraitFrame;
        private bool refreshingFighters;
        private readonly List<Slider> fighterSliders=new List<Slider>();
        private readonly List<Text> fighterCounts=new List<Text>();
        private readonly List<Button> fighterMinus=new List<Button>(),fighterPlus=new List<Button>();

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
            oldAmbientMode=RenderSettings.ambientMode; oldAmbient = RenderSettings.ambientLight; RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat; oldShadowDistance = QualitySettings.shadowDistance; QualitySettings.shadowDistance = Mathf.Max(oldShadowDistance,160); RenderSettings.ambientLight = new Color(.32f,.33f,.32f);
            BuildWorld(); BuildUI(); BuildPortrait();
            controls.FocusOn(new Vector3(0,0,1),18);
            gameManager.Resources.Changed += Refresh; gameManager.HeroArmies.Changed += Refresh; gameManager.Fragments.Changed += Refresh; gameManager.Buildings.Changed += Refresh;
            gameManager.SaveStatusChanged += UpdateSaveStatus; gameManager.OfflineProgressChanged += ShowOfflineSummary; UpdateSaveStatus();
            Refresh(); ShowOfflineSummary();
        }
        private void OnDestroy()
        {
            if (gameManager != null && gameManager.IsInitialized)
            { gameManager.Resources.Changed -= Refresh; gameManager.HeroArmies.Changed -= Refresh; gameManager.Fragments.Changed -= Refresh; gameManager.Buildings.Changed -= Refresh; }
            if (gameManager != null) { gameManager.SaveStatusChanged -= UpdateSaveStatus; gameManager.OfflineProgressChanged -= ShowOfflineSummary; }
            if (controls != null) controls.BuildingSelected -= Inspect;
            if(controls!=null){controls.WorldMapRequested-=ShowMapFromZoom;controls.MoveStarted-=BeginBuildingMove;controls.MoveRequested-=CommitBuildingMove;controls.MoveCancelled-=CancelBuildingMove;}
            if (presentation != null) { presentation.SetActive(false); Destroy(presentation); }
            ReleaseWastelandHUD();
            developmentArt?.Dispose();
            districtArt?.Dispose();
            art?.Dispose();
            if (art != null) { RenderSettings.ambientMode=oldAmbientMode; RenderSettings.ambientLight = oldAmbient; QualitySettings.shadowDistance = oldShadowDistance; }
        }
        private void Update()
        {
            if(resetDeadline>0 && Time.unscaledTime>resetDeadline) { CancelReset(); toast.text="Reset cancelled."; }
        }
        private void ShowOfflineSummary() { if(gameManager.OfflineSummary.Length==0)return; if(toast!=null)toast.text=gameManager.OfflineSummary; if(scavengingMap!=null)scavengingMap.ShowOfflineSummary(gameManager.OfflineSummary); }
        private void UpdateSaveStatus() { if(saveStatus!=null)saveStatus.text=gameManager.SaveStatus; }
        private void CancelReset() { resetDeadline=0; if(resetButton!=null)resetButton.text="RESET"; if(saveStatus!=null)saveStatus.text=gameManager.SaveStatus; }
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
                if(saveStatus!=null)saveStatus.text="Erase all saved progress? Click CONFIRM within 8 seconds.";
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
            camera.gameObject.AddComponent<SettlementAtmosphere>().Configure(sun);
            art.Box(world,"Continuous outpost earth",new Vector3(0,-.2f,0),new Vector3(128,.4f,128),SettlementArt.OutpostSoil);
            Building("HEADQUARTERS","Your command post. Open HEROES to manage survivors and their armies.",new Vector3(-3,0,3),new Vector3(3.4f,2.3f,2.8f),"Headquarters",BuildingKind.Headquarters);
            Building("WORKSHOP","A salvaged warehouse of tools, spare parts, and forgotten machines.",new Vector3(3,0,2.5f),new Vector3(3,2.3f,2.8f),"Workshop");
            RealisticAssets.SalvageTruck(world,new Vector3(6.7f,.06f,.2f),-35);
            Building("BARRACKS","Prepare each hero’s independent army here. Select a hero, choose fighter types and use the slider or FILL.",new Vector3(-3.3f,0,-4),new Vector3(3,1.8f,2.6f),"Barracks");
            Building("WATER STATION","Protected tanks hold the outpost's water. Expeditions can bring back more.",new Vector3(3.4f,0,-4),new Vector3(3,2.6f,2.6f),"WaterStation",BuildingKind.WaterStation);
            Building("FOOD GARDEN","Raised beds and reclaimed soil: a promise of life returning to the Ashlands.",new Vector3(3.5f,0,6),new Vector3(3,.9f,2.5f),"FoodGarden",BuildingKind.FoodGarden);
            var clinic=Marker("HOSPITAL","Heal wounded fighters; upgrade beds and recovery speed.",new Vector3(-3,0,6.5f),new Vector3(3,2.5f,2.5f));
            clinic.GetComponent<SettlementBuilding>().Configure("HOSPITAL","All wounded are recoverable. Heroes cannot be wounded.",hospital:true,placementId:"core-hospital");
            RealisticAssets.Building(clinic,"Hospital",new Vector2(3,2.5f));FitBuildingCollider(clinic);
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*SettlementLayout.HalfWidth;
                for(float z=-SettlementLayout.HalfDepth;z<=SettlementLayout.HalfDepth;z+=1.4f)
                {
                    art.Box(world,"Fence post",new Vector3(x,.85f,z),new Vector3(.14f,1.7f,.14f),SettlementArt.Timber);
                    art.Rock(world,new Vector3(x,1.7f,z),new Vector3(.09f,.30f,.09f));
                    if(z+1.4f<=SettlementLayout.HalfDepth)art.Box(world,"Salvage palisade panel",new Vector3(x,.62f,z+.7f),new Vector3(.07f,1.10f,1.15f),((int)((z+SettlementLayout.HalfDepth)/1.4f))%3==0?SettlementArt.Rust:SettlementArt.Metal);
                    if(z+1.4f<=SettlementLayout.HalfDepth)for(int rib=0;rib<4;rib++)
                        art.Box(world,"Salvage wall corrugation",new Vector3(x-side*.045f,.62f,z+.20f+rib*.28f),new Vector3(.025f,1.10f,.025f),SettlementArt.Metal);
                    if(z+1.4f<=SettlementLayout.HalfDepth)for(int rail=0;rail<2;rail++)art.Box(world,"Fence rail",new Vector3(x,.4f+rail*.5f,z+.7f),new Vector3(.1f,.1f,1.4f),SettlementArt.Metal);
                }
                Tower(new Vector3(x,0,SettlementLayout.HalfDepth-1));
                Tower(new Vector3(x,0,-SettlementLayout.HalfDepth+1));
            }
            for(float x=-SettlementLayout.HalfWidth;x<=SettlementLayout.HalfWidth;x+=1.4f)
            {
                art.Box(world,"Rear barrier",new Vector3(x,.75f,SettlementLayout.HalfDepth),new Vector3(1.35f,1.5f,.15f),SettlementArt.Timber);
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
                if(p.Kind==PlotKind.Empty)continue;
                var position=gameManager.Buildings.Position(p.Id);
                var root=new GameObject(p.Id+" "+BuildingService.PlotName(p.Kind)).transform;root.SetParent(developmentWorld.transform,false);root.localPosition=new Vector3(position.X,0,position.Z);
                root.gameObject.AddComponent<SettlementBuilding>().Configure(BuildingService.PlotName(p.Kind),"",plotId:p.Id);
                if(p.Kind==PlotKind.Housing)ResidentialShelters(root,p.Level);
                else if(p.Kind==PlotKind.Market)BuildMarketLandmark(root);
                else if(p.Kind!=PlotKind.Empty)
                {
                    string asset=SettlementVisualCatalog.ModelFor(p.Kind);
                    var model=RealisticAssets.Building(root,asset,new Vector2(4,4));
                    // Keep the new authored yards inside their original six-unit construction footprints.
                    model.transform.localScale=Vector3.one*(.90f+.005f*Mathf.Min(p.Level,10));
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
            var collider=root.GetComponent<BoxCollider>();if(collider==null)collider=root.gameObject.AddComponent<BoxCollider>();collider.center=bounds.center;
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
            FitBuildingCollider(root);
        }
        private void Tower(Vector3 position)
        {
            RealisticAssets.Scenery(world,"OutpostWatchtower",position,1.0f);
        }
        private void Inspect(SettlementBuilding building)
        {
            if(toast!=null)toast.text=building.DisplayName;
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
            else if(building.PlotId!=null && gameManager.Buildings.Plot(building.PlotId).Kind==PlotKind.Market)ShowMarket();
            else if(building.PlotId!=null)ShowDevelopment(building.PlotId);
            else if(building.PlacementId=="core-barracks")ShowBarracks();
            else if(building.IsHospital)ShowHospital();
            else if(building.Kind.HasValue)ShowBuilding(building.Kind.Value);
        }
        private bool SettlementInputAllowed()=>Time.unscaledTime>=navigationReadyAt && (navigationPanel==null || !navigationPanel.activeSelf) && (utilityPanel==null || !utilityPanel.activeSelf) && (barracksPanel==null || !barracksPanel.activeSelf)
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
            RefreshDistrictScenery();
        }
        private void ShowMapFromZoom()
        {
            ShowMap();scavengingMap.EnterFromSettlement();FadeNavigation();
        }
        private void ReturnFromWorldMap()
        {
            scavengingMap.Hide();controls.ResetView();controls.FocusOn(new Vector3(0,0,1),18);controls.SuspendInput(.4f);FadeNavigation();
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
            CloseNavigationPanel();
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
                text.Append("\n\nProduction: ").Append(buildings.HourlyRate(kind)).Append(' ').Append(BuildingService.Product(kind)).Append(" / hour")
                    .Append("\nStored: ").Append(buildings.Stored(kind)).Append('/').Append(buildings.Capacity(kind))
                    .Append("\n").Append(buildings.Stored(kind)==buildings.Capacity(kind) ? "Resource balance full — spend resources to resume." : "Next batch in "+buildings.NextResourceSeconds(kind)+"s")
                    .Append("\nAutomatically added to resources while the game is open.");
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
            viewHeadquarters.text="VIEW HQ";
            viewHeadquarters.transform.parent.gameObject.SetActive(kind==BuildingKind.Headquarters);
        }
        private void UpgradeBuilding()
        {
            CancelReset(); var result=gameManager.Buildings.Upgrade(selectedBuilding);
            toast.text=result==BuildingCommandResult.Success ? BuildingService.Name(selectedBuilding)+" upgraded to level "+gameManager.Buildings.Level(selectedBuilding)+"." : result.ToString();
            RefreshBuilding();
        }
        private void ViewHeadquarters()
        {
            CancelReset();buildingPanel.SetActive(false);
            var hq=gameManager.Buildings.Position("core-headquarters");controls.FocusOn(new Vector3(hq.X,1.5f,hq.Z),6.5f);
            toast.text="Headquarters — drag or zoom to explore. HOME returns to the overview.";
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
            RefreshWastelandHUD(hero);
            leaderLabel.text = HeroRarityColors.Text(hero.Definition.DisplayName.ToUpperInvariant(),hero.Definition.Rarity)+" • Lv "+hero.Level+" • "+hero.Army.TotalUnits+"/"+hero.Army.Capacity;
            var text = new StringBuilder(HeroRarityColors.Text(hero.Definition.DisplayName+"\n"+hero.Definition.Rarity,hero.Definition.Rarity)+" • "+hero.Definition.Specialization+"\n\n");
            text.Append("LEVEL ").Append(hero.Level).Append("    XP ").Append(hero.Xp).Append('/').Append(hero.XpToNextLevel)
                .Append("\nARMY ").Append(hero.Army.TotalUnits).Append('/').Append(hero.Army.Capacity).Append("\nPOWER ").Append(hero.Army.Power.ToString("0.##"))
                .Append("\nFORMATION ").Append(gameManager.HeroArmies.FormationTotal(hero.InstanceId)).Append(": reserves refill at home")
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
            CloseNavigationPanel();CancelReset();scavengingMap.Hide();hospitalUI.Hide();heroPanel.SetActive(false);raidPanel.SetActive(false);buildingPanel.SetActive(false);if(id==null)developmentUI.ShowConstruction();else developmentUI.Show(id);
        }
        private void ShowHospital()
        {
            if(barracksPanel!=null)barracksPanel.SetActive(false);
            CloseNavigationPanel();CancelReset();developmentUI.Hide();scavengingMap.Hide();heroPanel.SetActive(false);raidPanel.SetActive(false);buildingPanel.SetActive(false);hospitalUI.Show();
        }
        private void ShowMap()
        {
            if(barracksPanel!=null)barracksPanel.SetActive(false);
            CloseNavigationPanel();developmentUI.Hide();hospitalUI.Hide();
            CancelReset(); heroPanel.SetActive(false); raidPanel.SetActive(false); buildingPanel.SetActive(false);
            scavengingMap.Show(); scavengingMap.ShowOfflineSummary(gameManager.OfflineSummary); scavengingMap.transform.SetAsFirstSibling();
        }
        private void ShowHeroes(bool visible) { CloseNavigationPanel();preparingFromBarracks=false;if(barracksPanel!=null)barracksPanel.SetActive(false); heroHeading.text="SURVIVOR DOSSIER"; developmentUI.Hide();hospitalUI.Hide();if(!worldMapMode)scavengingMap.Hide();else scavengingMap.DismissSelection(); buildingPanel.SetActive(false); heroPanel.SetActive(visible); raidPanel.SetActive(false); Refresh(); }
        private void ShowRaid()=>ShowMap();
        private void ShowWorldRaidResult(CombatResult outcome)
        {
            for(int i=0;i<gameManager.Heroes.Count;i++)if(gameManager.Heroes[i].InstanceId==outcome.HeroInstanceId)selectedHero=i;
            for(int i=0;i<camps.Count;i++)if(camps[i].Id==outcome.EncounterId)selectedCamp=i;
            scavengingMap.Hide();raidPanel.SetActive(true);raidPanel.transform.SetAsLastSibling();results.Show(outcome,true);Refresh();
        }
        private void RefreshFighterControls()
        {
            refreshingFighters=true;
            try {
                var hero=gameManager.Heroes[selectedHero];bool deployed=gameManager.WorldMap.IsDeployed(hero.InstanceId);
                for(int i=0;i<fighterSliders.Count;i++) {
                    var unit=gameManager.Units[i];int current=hero.Army.QuantityOf(unit.Definition.Id,unit.Level);
                    int reserve=gameManager.HeroArmies.Available(unit.Definition.Id),target=gameManager.HeroArmies.FormationQuantity(hero.InstanceId,unit.Definition.Id);
                    int maximum=target+Math.Min(Math.Max(0,reserve-(target-current)),Math.Max(0,hero.Army.Capacity-gameManager.HeroArmies.FormationTotal(hero.InstanceId)));
                    var slider=fighterSliders[i];slider.minValue=0;slider.maxValue=Math.Max(1,maximum);
                    slider.SetValueWithoutNotify(target);slider.interactable=!deployed && maximum>0;
                    fighterCounts[i].text=unit.Definition.Type+"  "+current+" / "+target+" planned • Reserve "+reserve;
                    fighterMinus[i].interactable=!deployed && target>0;
                    fighterPlus[i].interactable=!deployed && target<maximum;
                }
            } finally { refreshingFighters=false; }
        }
        private void SetFighters(int index,int quantity)
        {
            if(refreshingFighters)return;
            CancelReset();var hero=gameManager.Heroes[selectedHero];var unit=gameManager.Units[index];
            var result=gameManager.HeroArmies.SetFormationQuantity(hero.InstanceId,unit.Definition.Id,quantity);
            if(result!=ArmyCommandResult.Success)toast.text="Army adjustment: "+result;
            RefreshFighterControls();
        }
        private void BuildFighterControls(RectTransform parent)
        {
            for(int i=0;i<gameManager.Units.Count;i++) {
                int index=i;float bottom=48+(2-i)*62;
                var count=TextPanel(parent,0,0,1,0,18,bottom+36,-18,bottom+60,13,"",Color.clear);
                count.resizeTextForBestFit=true;count.resizeTextMinSize=10;count.resizeTextMaxSize=13;fighterCounts.Add(count);
                var minus=Button(parent,"−1",0,0,.15f,0,12,bottom,-4,bottom+35,16,()=>SetFighters(index,Math.Max(0,Mathf.RoundToInt(fighterSliders[index].value)-1)));
                fighterMinus.Add(minus.transform.parent.GetComponent<Button>());
                var root=Panel(parent,gameManager.Units[i].Definition.Type+" assigned slider",new Vector2(.15f,0),new Vector2(.85f,0),new Vector2(8,bottom),new Vector2(-8,bottom+35),Color.clear);
                var slider=root.gameObject.AddComponent<Slider>();slider.wholeNumbers=true;
                var track=Panel(root,"Track",new Vector2(0,.35f),new Vector2(1,.65f),Vector2.zero,Vector2.zero,new Color32(63,75,76,255));
                var fill=Panel(track,"Fill",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,SettlementArt.Amber);fill.GetComponent<Image>().raycastTarget=false;slider.fillRect=fill;
                var area=Panel(root,"Handle area",Vector2.zero,Vector2.one,new Vector2(12,0),new Vector2(-12,0),Color.clear);area.GetComponent<Image>().raycastTarget=false;
                var handle=Panel(area,"Handle",new Vector2(0,.5f),new Vector2(0,.5f),Vector2.zero,Vector2.zero,new Color32(245,224,170,255));handle.sizeDelta=new Vector2(24,35);
                slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();fighterSliders.Add(slider);
                slider.onValueChanged.AddListener(value=>SetFighters(index,Mathf.RoundToInt(value)));
                var plus=Button(parent,"+1",.85f,0,1,0,4,bottom,-12,bottom+35,16,()=>SetFighters(index,Mathf.RoundToInt(fighterSliders[index].value)+1));
                fighterPlus.Add(plus.transform.parent.GetComponent<Button>());
            }
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
            BuildWastelandHUD();
            heroPanel=Panel(ui,"Hero dossier",new Vector2(.04f,.12f),new Vector2(.96f,.88f),Vector2.zero,Vector2.zero,ink).gameObject;
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
            heroStats=TextPanel(heroRoot,.38f,0,1,1,10,244,-18,-82,18,"",Color.clear);
            heroStats.resizeTextForBestFit=true;heroStats.resizeTextMinSize=12;heroStats.resizeTextMaxSize=17;
            Button(heroRoot,"<",0,1,.19f,1,18,-342,-4,-302,()=> { selectedHero=(selectedHero+7)%8; Refresh(); });
            Button(heroRoot,">",.19f,1,.38f,1,4,-342,-6,-302,()=> { selectedHero=(selectedHero+1)%8; Refresh(); });
            BuildFighterControls(heroRoot);
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
            viewHeadquarters=Button(buildingRoot,"VIEW HQ",.5f,0,1,0,4,14,-12,70,18,ViewHeadquarters);
            buildingPanel.SetActive(false);
            scavengingMap=ScavengingMapUI.Create(ui,gameManager.Scavenging,font,gameManager.SaveProgress,()=>gameManager.SaveStatus);
            scavengingMap.ConfigureWorld(gameManager.WorldMap,()=>gameManager.Heroes[selectedHero],delta=>{selectedHero=(selectedHero+delta+gameManager.Heroes.Count)%gameManager.Heroes.Count;Refresh();},ShowWorldRaidResult,id=>{for(int i=0;i<gameManager.Heroes.Count;i++)if(gameManager.Heroes[i].InstanceId==id){selectedHero=i;Refresh();break;}},PrepareSelectedHero,()=>gameManager.TravelSubsecond);
            scavengingMap.SettlementRequested+=ReturnFromWorldMap;
            hospitalUI=HospitalUI.Create(ui,gameManager,font);
            developmentUI=DevelopmentUI.Create(ui,gameManager,font);
            developmentUI.PlotSelected+=id=>{var p=gameManager.Buildings.Position(id);controls.FocusOn(new Vector3(p.X,0,p.Z),8);};
            scavengingMap.VisibilityChanged+=SetWorldHudMode;scavengingMap.InputAllowed=MapInputAllowed;scavengingMap.transform.SetAsFirstSibling();
            WastelandHudSkin.Frame(hospitalUI.GetComponent<RectTransform>(),10);
            WastelandHudSkin.Frame(developmentUI.GetComponent<RectTransform>(),10);
            BuildBarracksRoster();
            toast=TextPanel(ui,.28f,0,1,0,4,100,-4,140,12,"Welcome to Ember Outpost. Choose a survivor and assemble an army.",new Color32(35,44,44,210));
            toast.resizeTextForBestFit=true; toast.resizeTextMinSize=9; toast.resizeTextMaxSize=15;
        }
        private RectTransform Panel(Transform parent,string name,Vector2 min,Vector2 max,Vector2 lower,Vector2 upper,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image)); obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>(); rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=lower; rect.offsetMax=upper;
            obj.GetComponent<Image>().color=color;
            if(color.a>.5f && (name=="HUD panel" || name=="Hero dossier" || name=="Wasteland raid" || name=="Building management" || name=="Barracks hero roster"))
            {obj.GetComponent<Image>().color=new Color32(24,21,17,250);WastelandHudSkin.Frame(rect,9);}
            return rect;
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
            var button=text.transform.parent.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic=(Graphic)text.transform.parent.GetComponentInChildren<WastelandMetalFrame>()??text.transform.parent.GetComponent<Image>(); button.onClick.AddListener(()=>action()); return text;
        }
    }
}
