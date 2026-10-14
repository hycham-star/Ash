using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ash
{
    [RequireComponent(typeof(GameManager))]
    public sealed class SettlementView : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private EncounterConfig[] encounters = Array.Empty<EncounterConfig>();
        private readonly List<NpcEncounter> camps = new List<NpcEncounter>();
        private SettlementArt art;
        private GameObject presentation, heroPanel, raidPanel, portraitStage;
        private Transform world, leader;
        private RectTransform ui;
        private SettlementCamera controls;
        private Camera portraitCamera;
        private RenderTexture portrait;
        private Font font;
        private Text resources, selection, heroStats, raidHeader, campButton, toast, leaderLabel;
        private CombatResultsUI results;
        private int selectedHero, selectedCamp, renderedHero = -1;
        private Color oldAmbient;
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
            art = new SettlementArt(); font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            presentation = new GameObject("Original Ashlands Settlement");
            world = new GameObject("Settlement World").transform; world.SetParent(presentation.transform,false);
            oldAmbient = RenderSettings.ambientLight; oldShadowDistance = QualitySettings.shadowDistance; QualitySettings.shadowDistance = Mathf.Max(oldShadowDistance,65); RenderSettings.ambientLight = new Color(.58f,.59f,.55f);
            BuildWorld(); BuildUI(); BuildPortrait();
            gameManager.Resources.Changed += Refresh; gameManager.HeroArmies.Changed += Refresh; gameManager.Fragments.Changed += Refresh;
            Refresh(); selection.text = "THE EMBER OUTPOST\nTap a building to inspect • Drag to explore";
        }
        private void OnDestroy()
        {
            if (gameManager != null && gameManager.IsInitialized)
            { gameManager.Resources.Changed -= Refresh; gameManager.HeroArmies.Changed -= Refresh; gameManager.Fragments.Changed -= Refresh; }
            if (controls != null) controls.BuildingSelected -= Inspect;
            if (portraitCamera != null) portraitCamera.targetTexture = null;
            if (portrait != null) { portrait.Release(); Destroy(portrait); }
            if (presentation != null) Destroy(presentation);
            art?.Dispose();
            if (art != null) { RenderSettings.ambientLight = oldAmbient; QualitySettings.shadowDistance = oldShadowDistance; }
        }
        private void BuildWorld()
        {
            var camera = new GameObject("Isometric Settlement Camera",typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(presentation.transform,false); camera.tag = "MainCamera";
            camera.transform.position = new Vector3(24,30,-24); camera.transform.LookAt(new Vector3(0,0,0));
            camera.orthographic = true; camera.orthographicSize = 20; camera.farClipPlane = 150;
            camera.cullingMask = ~(1<<PortraitLayer); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(69,76,73,255);
            controls = camera.gameObject.AddComponent<SettlementCamera>(); controls.BuildingSelected += Inspect;
            var sun = new GameObject("Late Afternoon Sun",typeof(Light)).GetComponent<Light>(); sun.transform.SetParent(presentation.transform,false);
            sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(48,-38,0); sun.intensity = 1.15f;
            sun.color = new Color(1,.9f,.73f); sun.shadows = LightShadows.Soft;
            art.Box(world,"Ashland terrain",new Vector3(0,-.25f,0),new Vector3(55,.45f,55),SettlementArt.Sand);
            art.Box(world,"Settlement slab",new Vector3(0,0,0),new Vector3(14,.12f,17),new Color32(144,130,101,255));
            art.Box(world,"North south road",new Vector3(0,.08f,0),new Vector3(1.8f,.03f,16),new Color32(96,100,93,255));
            art.Box(world,"Crossing road",new Vector3(0,.08f,-1),new Vector3(13,.03f,1.5f),new Color32(96,100,93,255));
            for (int i = 0; i < 12; i++) art.Box(world,"Road marking",new Vector3(0,.11f,-7+i*1.2f),new Vector3(.09f,.012f,.45f),SettlementArt.Amber);
            Building("HEADQUARTERS","Your command post. Open HEROES to manage survivors and their armies.",new Vector3(-3,0,3),new Vector3(3.4f,2.3f,2.8f),SettlementArt.Concrete);
            Building("WORKSHOP","A salvaged warehouse of tools, spare parts, and forgotten machines.",new Vector3(3,0,2.5f),new Vector3(3,1.7f,2.8f),SettlementArt.Metal);
            Building("BARRACKS","Open HEROES to assign reserve fighters to your chosen survivor.",new Vector3(-3.3f,0,-4),new Vector3(3,1.5f,2.6f),new Color32(134,108,79,255));
            var pump = Marker("WATER STATION","Protected tanks hold the outpost's water. Expeditions can bring back more.",new Vector3(3.4f,0,-4),new Vector3(3,2.6f,2.6f));
            for (int i = 0; i < 2; i++)
            {
                art.Box(pump,"Water tank",new Vector3(-.7f+i*1.4f,1.4f,0),new Vector3(1,1.1f,1),new Color32(96,142,148,255),true,PrimitiveType.Cylinder);
                for (int leg = 0; leg < 4; leg++) art.Box(pump,"Tank supports",new Vector3(-.7f+i*1.4f+(leg%2-.5f)*.7f,.5f,(leg/2-.5f)*.7f),new Vector3(.1f,1,.1f),SettlementArt.Metal);
            }
            art.Box(pump,"Pump housing",new Vector3(0,.35f,-1),new Vector3(.6f,.7f,.45f),SettlementArt.Rust);
            var farm = Marker("GARDEN","Raised beds and reclaimed soil: a promise of life returning to the Ashlands.",new Vector3(3.5f,0,6),new Vector3(3,.7f,2.5f));
            for (int row = 0; row < 3; row++)
            {
                art.Box(farm,"Raised soil bed",new Vector3(0,.2f,-.8f+row*.8f),new Vector3(2.8f,.25f,.52f),SettlementArt.Timber);
                for (int leaf = 0; leaf < 6; leaf++) art.Box(farm,"Growing crops",new Vector3(-1.1f+leaf*.43f,.43f,-.8f+row*.8f),new Vector3(.25f,.25f,.3f),new Color32(90,123,62,255));
            }
            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? -7 : 7;
                for (int post = 0; post < 12; post++)
                {
                    float z = -7.6f+post*1.4f;
                    art.Box(world,"Fence post",new Vector3(x,.65f,z),new Vector3(.16f,1.3f,.16f),SettlementArt.Timber);
                    if (post < 11) for (int rail = 0; rail < 2; rail++) art.Box(world,"Fence rail",new Vector3(x,.4f+rail*.5f,z+.7f),new Vector3(.1f,.1f,1.4f),SettlementArt.Metal);
                }
                Tower(new Vector3(x,0,7));
            }
            for (int post = 0; post < 11; post++)
            {
                float x = -7+post*1.4f;
                art.Box(world,"Rear barrier",new Vector3(x,.55f,8),new Vector3(1.25f,1.1f,.2f),SettlementArt.Rust);
                if (Mathf.Abs(x)>1.5f) art.Box(world,"Gate wall",new Vector3(x,.55f,-8),new Vector3(1.25f,1.1f,.25f),SettlementArt.Concrete);
            }
            art.Box(world,"Gate sign",new Vector3(0,2.4f,-8),new Vector3(3.5f,.55f,.18f),SettlementArt.Metal);
            for (int i = 0; i < 2; i++) art.Box(world,"Gate pillars",new Vector3(-1.8f+i*3.6f,1.25f,-8),new Vector3(.25f,2.5f,.3f),SettlementArt.Rust);
            for (int i = 0; i < 10; i++)
            {
                float angle = i*2.39996f; float radius = 12+i%3*2;
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
            art.Combine(world);
            SurvivorView.Create(art,world,"Supply runner",2,new Vector3(0,0,-5),new[] { new Vector3(0,0,-5),new Vector3(0,0,4),new Vector3(1,0,4),new Vector3(1,0,-5) });
            SurvivorView.Create(art,world,"Workshop engineer",3,new Vector3(3,0,-1),new[] { new Vector3(3,0,-1),new Vector3(-3,0,-1) });
            SurvivorView.Create(art,world,"Gate sentry",1,new Vector3(-1.2f,0,-6));
        }
        private Transform Marker(string title,string description,Vector3 position,Vector3 size)
        {
            var root = new GameObject(title).transform; root.SetParent(world,false); root.localPosition = position;
            root.gameObject.AddComponent<SettlementBuilding>().Configure(title,description);
            var collider = root.gameObject.AddComponent<BoxCollider>(); collider.center = new Vector3(0,size.y/2,0); collider.size = size;
            return root;
        }
        private void Building(string title,string description,Vector3 position,Vector3 size,Color color)
        {
            var root = Marker(title,description,position,size+new Vector3(0,.8f,0));
            art.Box(root,"Foundation",new Vector3(0,.16f,0),new Vector3(size.x+.3f,.3f,size.z+.3f),SettlementArt.Concrete);
            art.Box(root,"Walls",new Vector3(0,size.y/2+.2f,0),size,color);
            art.Roof(root,new Vector3(0,size.y+.2f,0),new Vector3(size.x+.4f,.75f,size.z+.4f));
            art.Box(root,"Front door",new Vector3(0,.8f,-size.z/2-.03f),new Vector3(.62f,1.25f,.07f),SettlementArt.Timber);
            for (int i = 0; i < 2; i++)
            {
                float x = (i==0 ? -1 : 1)*size.x*.3f;
                art.Box(root,"Window frame",new Vector3(x,1.25f,-size.z/2-.04f),new Vector3(.7f,.6f,.09f),SettlementArt.Metal);
                art.Box(root,"Window glass",new Vector3(x,1.25f,-size.z/2-.1f),new Vector3(.55f,.42f,.03f),SettlementArt.Glass);
            }
            art.Box(root,"Chimney",new Vector3(size.x*.3f,size.y+.85f,.55f),new Vector3(.28f,1,.3f),SettlementArt.Metal);
            if (title=="HEADQUARTERS")
            {
                art.Box(root,"Radio mast",new Vector3(-.8f,size.y+1.5f,.5f),new Vector3(.09f,2.5f,.09f),SettlementArt.Metal);
                art.Box(root,"Radio crossbar",new Vector3(-.8f,size.y+2,.5f),new Vector3(1,.06f,.06f),SettlementArt.Metal);
                art.Box(root,"Faction flag",new Vector3(-.35f,size.y+1.5f,.5f),new Vector3(.85f,.55f,.03f),SettlementArt.Amber);
            }
        }
        private void Tower(Vector3 position)
        {
            for (int i = 0; i < 4; i++) art.Box(world,"Tower strut",position+new Vector3((i%2-.5f)*1.2f,1.6f,(i/2-.5f)*1.2f),new Vector3(.16f,3.2f,.16f),SettlementArt.Timber);
            art.Box(world,"Lookout deck",position+new Vector3(0,3.1f,0),new Vector3(1.8f,.18f,1.8f),SettlementArt.Timber);
            art.Box(world,"Lookout shelter",position+new Vector3(0,3.55f,0),new Vector3(1.3f,.75f,1.3f),SettlementArt.Metal);
            art.Roof(world,position+new Vector3(0,4,0),new Vector3(1.8f,.4f,1.8f));
        }
        private void Inspect(SettlementBuilding building) { selection.text = building.DisplayName+"\n"+building.Description; }
        private void BuildPortrait()
        {
            portrait = new RenderTexture(256,384,16) { name = "Original hero portrait", antiAliasing = 2 }; portrait.Create();
            portraitStage = new GameObject("Portrait Stage"); portraitStage.transform.SetParent(presentation.transform,false); portraitStage.transform.position = new Vector3(1000,0,0);
            portraitCamera = new GameObject("Hero Portrait Camera",typeof(Camera)).GetComponent<Camera>(); portraitCamera.transform.SetParent(portraitStage.transform,false);
            portraitCamera.transform.localPosition = new Vector3(1.5f,1.5f,-4); portraitCamera.transform.LookAt(portraitStage.transform.position+new Vector3(0,1.1f,0));
            portraitCamera.orthographic = true; portraitCamera.orthographicSize = .85f; portraitCamera.nearClipPlane = .1f; portraitCamera.farClipPlane = 10;
            portraitCamera.cullingMask = 1<<PortraitLayer; portraitCamera.targetTexture = portrait; portraitCamera.depth = -1;
            portraitCamera.clearFlags = CameraClearFlags.SolidColor; portraitCamera.backgroundColor = new Color32(52,69,73,255);
            var image = heroPanel.GetComponentInChildren<RawImage>(true); image.texture = portrait;
            portraitCamera.enabled = false;
        }
        private void UpdateLeader()
        {
            if (selectedHero==renderedHero) return; renderedHero=selectedHero;
            if (leader != null) { leader.gameObject.SetActive(false); Destroy(leader.gameObject); }
            leader = SurvivorView.Create(art,world,gameManager.Heroes[selectedHero].Definition.DisplayName,selectedHero,new Vector3(-1.4f,0,1.2f)).transform;
            leader.localScale = Vector3.one*1.15f;
            foreach (Transform child in portraitStage.transform) if (child.gameObject!=portraitCamera.gameObject) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            var model = SurvivorView.Create(art,portraitStage.transform,"Hero portrait model",selectedHero,Vector3.zero);
            foreach (var child in model.GetComponentsInChildren<Transform>()) child.gameObject.layer=PortraitLayer;
        }
        private void Refresh()
        {
            var hero = gameManager.Heroes[selectedHero];
            resources.text = "FOOD  "+gameManager.Resources.Get(ResourceType.Food)+"      SCRAP  "+gameManager.Resources.Get(ResourceType.Scrap)
                +"\nWATER  "+gameManager.Resources.Get(ResourceType.Water)+"      FUEL  "+gameManager.Resources.Get(ResourceType.Fuel);
            leaderLabel.text = hero.Definition.DisplayName.ToUpperInvariant()+"\nLv "+hero.Level+"  •  Army "+hero.Army.TotalUnits+"/"+hero.Army.Capacity;
            var text = new StringBuilder(hero.Definition.DisplayName+"\n"+hero.Definition.Rarity+" • "+hero.Definition.Specialization+"\n\n");
            text.Append("LEVEL ").Append(hero.Level).Append("    XP ").Append(hero.Xp).Append('/').Append(hero.XpToNextLevel)
                .Append("\nARMY ").Append(hero.Army.TotalUnits).Append('/').Append(hero.Army.Capacity).Append("\nPOWER ").Append(hero.Army.Power.ToString("0.##"))
                .Append("\nBONUS +").Append(hero.Definition.BonusAt(hero.Level)/100m).Append("% matching fighters")
                .Append("\nFRAGMENTS ").Append(gameManager.Fragments.Get(hero.Definition.Id)).Append("\n\n").Append(hero.Definition.Biography);
            heroStats.text = text.ToString();
            raidHeader.text = "WASTELAND RAID\n"+hero.Definition.DisplayName+" • "+hero.Army.TotalUnits+" fighters";
            if (camps.Count>0) campButton.text = camps[selectedCamp].Name.ToUpperInvariant();
            if (portraitStage != null) UpdateLeader();
        }
        private void ShowHeroes(bool visible) { heroPanel.SetActive(visible); raidPanel.SetActive(false); portraitCamera.enabled=visible; Refresh(); }
        private void ShowRaid() { heroPanel.SetActive(false); portraitCamera.enabled=false; raidPanel.SetActive(true); results.Clear(); Refresh(); }
        private void Assign(int type)
        {
            var hero=gameManager.Heroes[selectedHero]; var fighter=gameManager.Units[type];
            var result=gameManager.HeroArmies.AssignFighters(hero.InstanceId,fighter.Definition.Id,5);
            toast.text=result==ArmyCommandResult.Success ? "5 "+fighter.Definition.Type+" assigned to "+hero.Definition.DisplayName : "Assignment: "+result;
        }
        private void Raid(bool apply)
        {
            if (camps.Count==0) { toast.text="No encounters configured."; return; }
            var hero=gameManager.Heroes[selectedHero];
            try
            {
                var outcome=apply ? gameManager.Battles.RunBattle(hero.InstanceId,camps[selectedCamp]) : gameManager.Battles.PreviewBattle(hero.InstanceId,camps[selectedCamp]);
                results.Show(outcome,apply); Refresh(); toast.text=apply ? "Raid settled. Survivors and rewards updated." : "Preview only — army and resources unchanged.";
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
            var identity=TextPanel(ui,0,1,.77f,1,12,-150,-4,-112,16,"EMBER OUTPOST  /  ASHLANDS",new Color32(41,57,59,230)); identity.color=SettlementArt.Amber;
            selection=TextPanel(ui,0,0,1,0,12,144,-12,239,18,"",ink);
            leaderLabel=TextPanel(ui,0,0,.55f,0,12,88,-4,136,18,"",new Color32(41,57,59,246));
            Button(ui,"VIEW HERO",.55f,0,1,0,4,88,-12,136,()=>ShowHeroes(true));
            Button(ui,"HEROES",0,0,.5f,0,12,14,-4,76,22,()=>ShowHeroes(true));
            Button(ui,"WASTELAND RAID",.5f,0,1,0,4,14,-12,76,20,ShowRaid);
            heroPanel=Panel(ui,"Hero dossier",new Vector2(.04f,.30f),new Vector2(.96f,.86f),Vector2.zero,Vector2.zero,ink).gameObject;
            var heroRoot=heroPanel.GetComponent<RectTransform>();
            TextPanel(heroRoot,0,1,.8f,1,18,-66,-8,-12,25,"SURVIVOR DOSSIER",Color.clear).color=SettlementArt.Amber;
            Button(heroRoot,"X",.8f,1,1,1,4,-62,-12,-12,()=>ShowHeroes(false));
            var portraitRect=Panel(heroRoot,"Original portrait",new Vector2(0,1),new Vector2(.38f,1),new Vector2(18,-330),new Vector2(-6,-82),new Color32(51,68,73,255));
            var portraitImage=new GameObject("Rendered survivor portrait",typeof(RectTransform),typeof(RawImage)); portraitImage.transform.SetParent(portraitRect,false);
            var imageRect=portraitImage.GetComponent<RectTransform>(); imageRect.anchorMin=Vector2.zero; imageRect.anchorMax=Vector2.one; imageRect.offsetMin=imageRect.offsetMax=Vector2.zero;
            portraitImage.GetComponent<RawImage>().raycastTarget=false;
            heroStats=TextPanel(heroRoot,.38f,0,1,1,10,142,-18,-82,18,"",Color.clear);
            Button(heroRoot,"<",0,1,.19f,1,18,-386,-4,-342,()=> { selectedHero=(selectedHero+7)%8; Refresh(); });
            Button(heroRoot,">",.19f,1,.38f,1,4,-386,-6,-342,()=> { selectedHero=(selectedHero+1)%8; Refresh(); });
            for (int i=0;i<3;i++) { int index=i; Button(heroRoot,"+5 "+gameManager.Units[i].Definition.Type, i/3f,0,(i+1)/3f,0,12,76,-8,126,15,()=>Assign(index)); }
            Button(heroRoot,"TAKE THIS HERO ON A RAID",0,0,1,0,18,14,-18,62,19,ShowRaid);
            heroPanel.SetActive(false);
            raidPanel=Panel(ui,"Wasteland raid",new Vector2(.04f,.30f),new Vector2(.96f,.86f),Vector2.zero,Vector2.zero,ink).gameObject;
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
            Button(raidRoot,"RAID",.5f,0,1,0,4,14,-18,65,19,()=>Raid(true));
            raidPanel.SetActive(false);
            toast=TextPanel(ui,0,0,1,0,12,244,-12,282,15,"Welcome to Ember Outpost. Choose a survivor and assemble an army.",new Color32(35,44,44,210));
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
