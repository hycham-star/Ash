using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Ash
{
    /// <summary>Read-only animation of one resolved result. Never simulates or settles gameplay.</summary>
    public sealed class VisualBattlePlayback : MonoBehaviour
    {
        public const int Layer=29;
        private sealed class Group
        {
            public GameObject Actor; public Vector3 Start;
            public FighterType Type; public int Initial; public Text Badge;
        }
        private readonly List<Group> groups=new List<Group>();
        private GameObject overlay,stage;
        private Camera view;
        private RenderTexture texture;
        private SettlementArt art;
        private Text heading,roundText,totals,skipText;
        private RectTransform viewport,safeRoot;
        private readonly List<Transform> dustClouds=new List<Transform>();
        private Transform leader;
        private Material dustMaterial;
        private bool arrived;
        private const float TravelSeconds=4f, DustSeconds=2.5f, TravelDistance=7f;
        private Action finished;
        private CombatResult result;
        private string title;
        private float elapsed;
        public bool IsOpen { get; private set; }

        public void Play(CombatResult snapshot,string heroId,string heroName,string campName,bool applied,Action onFinished)
        {
            if(IsOpen)throw new InvalidOperationException("A battle presentation is already open.");
            result=snapshot??throw new ArgumentNullException(nameof(snapshot));finished=onFinished;
            title=(applied?"PvE BATTLE":"PREVIEW — NO CHANGES")+"\n"+heroName+" vs "+campName;
            elapsed=0;arrived=false;IsOpen=true;
            try { BuildUI();BuildStage(heroId,heroName);totals.text="YOUR ARMY • "+Initial()+" fighters"; }
            catch(Exception error)
            {
                Debug.LogWarning("Battle visuals unavailable; showing the resolved result. "+error.Message,this);
                Complete();
            }
        }
        private void BuildUI()
        {
            overlay=new GameObject("Visual PvE battle",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));overlay.transform.SetParent(transform,false);
            var canvas=overlay.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=200;
            var scaler=overlay.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(540,960);scaler.matchWidthOrHeight=.5f;
            var backdrop=Rect(overlay.transform,"Battle backdrop",Vector2.zero,Vector2.one);backdrop.gameObject.AddComponent<Image>().color=new Color(.065f,.085f,.09f,1);
            safeRoot=Rect(backdrop,"Safe area",Vector2.zero,Vector2.one);SafeArea();
            heading=Label(safeRoot,"Battle title",new Vector2(.03f,.87f),new Vector2(.97f,.99f),21);heading.text=title;
            var frame=Rect(safeRoot,"Arena frame",new Vector2(.02f,.22f),new Vector2(.98f,.87f));
            viewport=Rect(frame,"Arena",Vector2.zero,Vector2.one);
            viewport.gameObject.AddComponent<RawImage>().raycastTarget=false;
            var aspect=viewport.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=.75f;
            roundText=Label(safeRoot,"Round",new Vector2(.03f,.15f),new Vector2(.97f,.22f),18);
            totals=Label(safeRoot,"Survivors",new Vector2(.03f,.10f),new Vector2(.97f,.15f),17);
            var legend=Label(safeRoot,"Type counters",new Vector2(.03f,.055f),new Vector2(.97f,.10f),12);
            legend.text="Your army is heading to the target.\nThe raid is resolved behind the dust cloud.";
            var button=Rect(safeRoot,"Skip animation",new Vector2(.12f,.005f),new Vector2(.88f,.052f));button.gameObject.AddComponent<Image>().color=new Color(.38f,.25f,.12f);
            var click=button.gameObject.AddComponent<Button>();click.targetGraphic=button.GetComponent<Image>();click.onClick.AddListener(Complete);click.navigation=new Navigation{mode=Navigation.Mode.None};
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(click.gameObject);
            skipText=Label(button,"Skip label",Vector2.zero,Vector2.one,18);skipText.text="SKIP TO RESULTS";
        }
        private void SafeArea()
        {
            if(safeRoot==null)return;var safe=Screen.safeArea;
            safeRoot.anchorMin=new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height);safeRoot.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);
        }
        private void BuildStage(string heroId,string heroName)
        {
            art=new SettlementArt();stage=new GameObject("Resolved battle stage");stage.transform.SetParent(transform,false);stage.transform.position=new Vector3(2000,0,0);
            var cameraObject=new GameObject("Arena camera",typeof(Camera));cameraObject.transform.SetParent(stage.transform,false);view=cameraObject.GetComponent<Camera>();
            view.transform.localPosition=new Vector3(0,23,-11);view.transform.LookAt(stage.transform.position);view.orthographic=true;view.orthographicSize=9.2f;view.nearClipPlane=.1f;view.farClipPlane=70;view.cullingMask=1<<Layer;
            view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.15f,.18f,.18f);
            texture=new RenderTexture(768,1024,16){name="PvE arena",antiAliasing=2};texture.Create();if(!texture.IsCreated())throw new InvalidOperationException("Arena render texture unavailable.");
            view.targetTexture=texture;viewport.GetComponent<RawImage>().texture=texture;
            var lamp=new GameObject("Arena light",typeof(Light));lamp.transform.SetParent(stage.transform,false);var light=lamp.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.color=new Color(1,.91f,.78f);light.cullingMask=1<<Layer;light.shadows=LightShadows.Soft;lamp.transform.rotation=Quaternion.Euler(48,-35,0);
            art.Box(stage.transform,"Wasteland arena",new Vector3(0,-.13f,0),new Vector3(22,.25f,26),SettlementArt.Sand);
            for(int i=0;i<9;i++)art.Rock(stage.transform,new Vector3((i%2==0?-1:1)*(4.5f+i%3*.4f),0,-5+i*1.3f),new Vector3(.4f,.25f,.45f));
            art.Combine(stage.transform);
            var target=RealisticAssets.Building(stage.transform,"Workshop",new Vector2(4,3));target.name="Raid target";target.transform.localPosition=new Vector3(0,0,5.2f);
            AddGroups(result.PlayerFighters);
            leader=RealisticAssets.Hero(stage.transform,heroId,heroName,new Vector3(0,0,-6.2f)).transform;leader.localRotation=Quaternion.Euler(0,180,0);
            March(leader.gameObject,HeroVisualCatalog.ModelFor(heroId));
            BuildDust();
            foreach(var child in stage.GetComponentsInChildren<Transform>(true))child.gameObject.layer=Layer;
        }
        private void AddGroups(IEnumerable<CombatFighterResult> fighters)
        {
            for(int type=0;type<3;type++)
            {
                int initial=0;foreach(var stack in fighters)if((int)stack.Fighter.Definition.Type==type){initial+=stack.InitialQuantity;}
                if(initial==0)continue;
                var position=new Vector3((type-1)*2.4f,0,-4.5f);
                var actor=type==2?RealisticAssets.MountedFighter(stage.transform,position):RealisticAssets.Character(stage.transform,type==0?"Sentry":"JonasReed","Your "+(FighterType)type,position);
                actor.transform.localRotation=Quaternion.Euler(0,180,0);
                if(type!=2)March(actor,type==0?"Sentry":"JonasReed");
                var badge=Label(viewport,"Your group",Vector2.zero,Vector2.zero,11);
                badge.rectTransform.sizeDelta=new Vector2(84,38);badge.color=new Color(1,.88f,.5f);
                groups.Add(new Group{Actor=actor,Start=position,Type=(FighterType)type,Initial=initial,Badge=badge});badge.text=(FighterType)type+"\n×"+initial;
            }
        }
        private static void March(GameObject actor,string assetName)
        {
            var animation=actor.GetComponentInChildren<Animation>(true);
            if(animation==null)throw new InvalidOperationException("March animation missing: "+assetName);
            foreach(var clip in Resources.LoadAll<AnimationClip>("Art/"+assetName))
                if(clip.legacy && !clip.name.StartsWith("__preview__",StringComparison.Ordinal) && clip.name.IndexOf("Walk",StringComparison.OrdinalIgnoreCase)>=0)
                { animation.AddClip(clip,"Raid march");animation.wrapMode=WrapMode.Loop;animation.Play("Raid march");return; }
            throw new InvalidOperationException("Walk clip missing: "+assetName);
        }
        private void BuildDust()
        {
            var shader=Resources.Load<Shader>("AshRaidDust");
            if(shader==null || !shader.isSupported)throw new InvalidOperationException("Raid dust shader unavailable.");
            dustMaterial=new Material(shader){color=new Color(.68f,.57f,.39f,.92f)};
            for(int i=0;i<24;i++)
            {
                var cloud=GameObject.CreatePrimitive(PrimitiveType.Quad);cloud.name="Raid dust";cloud.transform.SetParent(stage.transform,false);
                SettlementArt.Release(cloud.GetComponent<Collider>());
                var renderer=cloud.GetComponent<Renderer>();renderer.sharedMaterial=dustMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                cloud.SetActive(false);dustClouds.Add(cloud.transform);
            }
        }
        private void Update()
        {
            if(!IsOpen)return;SafeArea();elapsed+=Time.unscaledDeltaTime;
            if(elapsed>=TravelSeconds+DustSeconds){Complete();return;}
            if(elapsed<TravelSeconds)
            {
                float distance=Mathf.Clamp01(elapsed/TravelSeconds)*TravelDistance;
                foreach(var group in groups)group.Actor.transform.localPosition=group.Start+Vector3.forward*distance;
                leader.localPosition=new Vector3(0,0,-6.2f+distance);
                roundText.text="MARCHING TO TARGET";
                foreach(var group in groups)
                {
                    var point=view.WorldToViewportPoint(group.Actor.transform.position+Vector3.up*(group.Type==FighterType.Cavalry?2.6f:2));
                    group.Badge.rectTransform.anchorMin=group.Badge.rectTransform.anchorMax=new Vector2(point.x,point.y);group.Badge.rectTransform.anchoredPosition=Vector2.zero;
                }
                return;
            }
            if(!arrived)
            {
                arrived=true;
                foreach(var group in groups){group.Actor.SetActive(false);group.Badge.gameObject.SetActive(false);}
                leader.gameObject.SetActive(false);totals.text="";
                foreach(var cloud in dustClouds)cloud.gameObject.SetActive(true);
            }
            roundText.text="RAID IN PROGRESS…";
            float time=elapsed-TravelSeconds;
            for(int i=0;i<dustClouds.Count;i++)
            {
                float angle=i*2.39996f+time*.18f,radius=.4f+(i%5)*.65f;
                var cloud=dustClouds[i];cloud.localPosition=new Vector3(Mathf.Cos(angle)*radius,.8f+(i%4)*.4f+Mathf.Sin(time*2+i)*.18f,3.8f+Mathf.Sin(angle)*radius);
                cloud.rotation=view.transform.rotation;
                float size=3.5f+Mathf.Sin(time*3+i)*.4f;cloud.localScale=new Vector3(size,size,1);
            }
        }
        private int Initial(){int value=0;foreach(var g in groups)value+=g.Initial;return value;}
        private void Complete()
        {
            if(!IsOpen)return;IsOpen=false;var callback=finished;finished=null;Release();callback?.Invoke();
        }
        private void Release()
        {
            if(overlay!=null){
                var events=EventSystem.current;
                if(events!=null && events.currentSelectedGameObject!=null && events.currentSelectedGameObject.transform.IsChildOf(overlay.transform))events.SetSelectedGameObject(null);
                overlay.SetActive(false);Destroy(overlay);}if(view!=null)view.targetTexture=null;
            if(texture!=null){texture.Release();Destroy(texture);}if(stage!=null){stage.SetActive(false);Destroy(stage);}if(dustMaterial!=null)Destroy(dustMaterial);dustMaterial=null;dustClouds.Clear();leader=null;art?.Dispose();art=null;overlay=null;stage=null;view=null;texture=null;groups.Clear();
        }
        private void OnDestroy(){IsOpen=false;finished=null;Release();}
        private static RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        private static Text Label(Transform parent,string name,Vector2 min,Vector2 max,int size)
        {
            var rect=Rect(parent,name,min,max);var text=rect.gameObject.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=size;text.color=new Color(.96f,.91f,.78f);text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return text;
        }
    }
}
