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
            public GameObject Actor; public Vector3 Start; public bool Player;
            public FighterType Type; public int Initial,Survivors; public Text Badge;
        }
        private readonly List<Group> groups=new List<Group>();
        private GameObject overlay,stage;
        private Camera view;
        private RenderTexture texture;
        private SettlementArt art;
        private Text heading,roundText,totals,skipText;
        private RectTransform viewport,safeRoot;
        private LineRenderer playerShot,enemyShot,abilityRing,enemyAbilityRing;
        private Action finished;
        private CombatResult result;
        private string title,ability;
        private float elapsed,roundSeconds;
        public bool IsOpen { get; private set; }

        public void Play(CombatResult snapshot,string heroId,string heroName,string campName,string abilityName,bool applied,Action onFinished)
        {
            if(IsOpen)throw new InvalidOperationException("A battle presentation is already open.");
            result=snapshot??throw new ArgumentNullException(nameof(snapshot));finished=onFinished;
            title=(applied?"PvE BATTLE":"PREVIEW — NO CHANGES")+"\n"+heroName+" vs "+campName;
            ability=abilityName;elapsed=0;roundSeconds=Mathf.Clamp(12f/Mathf.Max(1,result.Rounds.Count),.12f,1.1f);IsOpen=true;
            try { BuildUI();BuildStage(heroId,heroName);SetTotals(Initial(true),Initial(false)); }
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
            legend.text="Infantry > Cavalry > Sharpshooter > Infantry\nFormations represent fighter groups; badges show quantities.";
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
            view.transform.localPosition=new Vector3(8,10,-13);view.transform.LookAt(stage.transform.position+new Vector3(0,.7f,0));view.orthographic=true;view.orthographicSize=6.3f;view.nearClipPlane=.1f;view.farClipPlane=70;view.cullingMask=1<<Layer;
            view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.15f,.18f,.18f);
            texture=new RenderTexture(768,1024,16){name="PvE arena",antiAliasing=2};texture.Create();if(!texture.IsCreated())throw new InvalidOperationException("Arena render texture unavailable.");
            view.targetTexture=texture;viewport.GetComponent<RawImage>().texture=texture;
            var lamp=new GameObject("Arena light",typeof(Light));lamp.transform.SetParent(stage.transform,false);var light=lamp.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.color=new Color(1,.91f,.78f);light.cullingMask=1<<Layer;light.shadows=LightShadows.Soft;lamp.transform.rotation=Quaternion.Euler(48,-35,0);
            art.Box(stage.transform,"Wasteland arena",new Vector3(0,-.13f,0),new Vector3(15,.25f,18),SettlementArt.Sand);
            for(int i=0;i<9;i++)art.Rock(stage.transform,new Vector3((i%2==0?-1:1)*(4.5f+i%3*.4f),0,-5+i*1.3f),new Vector3(.4f,.25f,.45f));
            art.Combine(stage.transform);
            AddGroups(result.PlayerFighters,true);AddGroups(result.EnemyFighters,false);
            var leader=RealisticAssets.Hero(stage.transform,heroId,heroName,new Vector3(-2.7f,0,-4));leader.transform.localRotation=Quaternion.Euler(0,180,0);
            playerShot=Line("Your shots",SettlementArt.Amber,false);enemyShot=Line("Enemy shots",new Color(.7f,.25f,.12f),false);abilityRing=Line("Hero ability",SettlementArt.Amber,true);enemyAbilityRing=Line("Enemy ability",new Color(.7f,.25f,.12f),true);
            foreach(var child in stage.GetComponentsInChildren<Transform>(true))child.gameObject.layer=Layer;
        }
        private void AddGroups(IEnumerable<CombatFighterResult> fighters,bool player)
        {
            for(int type=0;type<3;type++)
            {
                int initial=0,surviving=0;foreach(var stack in fighters)if((int)stack.Fighter.Definition.Type==type){initial+=stack.InitialQuantity;surviving+=stack.SurvivingQuantity;}
                if(initial==0)continue;
                var position=new Vector3((type-1)*2.1f,0,player?-2.1f:2.1f);
                var actor=type==2?RealisticAssets.MountedFighter(stage.transform,position):RealisticAssets.Character(stage.transform,type==0?"Sentry":"JonasReed",(player?"Your ":"Enemy ")+(FighterType)type,position);
                if(player)actor.transform.localRotation=Quaternion.Euler(0,180,0);
                var badge=Label(viewport,(player?"Your ":"Enemy ")+"group",Vector2.zero,Vector2.zero,13);
                badge.rectTransform.sizeDelta=new Vector2(110,38);badge.color=player?new Color(1,.88f,.5f):new Color(1,.65f,.55f);
                groups.Add(new Group{Actor=actor,Start=position,Player=player,Type=(FighterType)type,Initial=initial,Survivors=surviving,Badge=badge});badge.text=(FighterType)type+"\n×"+initial;
            }
        }
        private LineRenderer Line(string name,Color color,bool loop)
        {
            var obj=new GameObject(name,typeof(LineRenderer));obj.transform.SetParent(stage.transform,false);var line=obj.GetComponent<LineRenderer>();line.sharedMaterial=art.Material(color);line.startWidth=line.endWidth=.045f;line.loop=loop;line.generateLightingData=true;line.enabled=false;return line;
        }
        private void Update()
        {
            if(!IsOpen)return;SafeArea();elapsed+=Mathf.Min(Time.unscaledDeltaTime,.1f);
            int count=result.Rounds.Count;float fighting=Mathf.Max(0,elapsed-.65f);float duration=count*roundSeconds;
            bool ended=elapsed>=.65f+duration;
            if(ended)
            {
                heading.text=result.Outcome.ToString().ToUpperInvariant()+"\n"+result.EndReason;
                SetTotals(Survivors(true),Survivors(false));roundText.text="Battle complete";skipText.text="VIEW RESULTS";
                foreach(var group in groups){group.Actor.transform.localPosition=group.Start;group.Actor.SetActive(group.Survivors>0);group.Badge.text=group.Type+"\n×"+group.Survivors;}
                playerShot.enabled=enemyShot.enabled=abilityRing.enabled=enemyAbilityRing.enabled=false;
                if(elapsed>=.65f+duration+1) { Complete();return; }
            }
            else if(elapsed>=.65f && count>0)
            {
                int index=Mathf.Min(count-1,(int)(fighting/roundSeconds));float phase=(fighting-index*roundSeconds)/roundSeconds;var round=result.Rounds[index];
                roundText.text="ROUND "+round.Number+" / "+count+"    Damage "+round.PlayerDamage.ToString("0.#")+" / "+round.EnemyDamage.ToString("0.#")+(round.PlayerAbilityActivated?"\n"+ability:round.EnemyAbilityActivated?"\nEnemy ability":"");
                if(phase>.75f)SetTotals(round.PlayerSurvivors,round.EnemySurvivors);
                foreach(var group in groups)
                {
                    float reach=group.Type==FighterType.Cavalry?1.65f:group.Type==FighterType.Infantry?1.25f:.08f;
                    group.Actor.transform.localPosition=group.Start+new Vector3(0,Mathf.Sin(phase*Mathf.PI)*.045f,(group.Player?1:-1)*Mathf.Sin(phase*Mathf.PI)*reach);
                }
                Shot(playerShot,true,phase);Shot(enemyShot,false,phase);
                Pulse(abilityRing,round.PlayerAbilityActivated,phase,new Vector3(-2.7f,.12f,-4));
                Pulse(enemyAbilityRing,round.EnemyAbilityActivated,phase,new Vector3(0,.12f,3.5f));
            }
            else roundText.text="Armies approaching";
            foreach(var group in groups)
            {
                var point=view.WorldToViewportPoint(group.Actor.transform.position+Vector3.up*(group.Type==FighterType.Cavalry?2.6f:2));
                group.Badge.rectTransform.anchorMin=group.Badge.rectTransform.anchorMax=new Vector2(point.x,point.y);group.Badge.rectTransform.anchoredPosition=Vector2.zero;
            }
        }
        private void Pulse(LineRenderer line,bool active,float phase,Vector3 center)
        {
            line.enabled=active && phase<.7f;if(!line.enabled)return;
            line.positionCount=32;float radius=.5f+phase*3;
            for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;line.SetPosition(i,stage.transform.position+center+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}
        }
        private void Shot(LineRenderer line,bool player,float phase)
        {
            Group source=null,target=null;foreach(var group in groups){if(group.Player==player && group.Type==FighterType.Sharpshooter)source=group;if(group.Player!=player && (target==null || group.Type==FighterType.Infantry))target=group;}
            line.enabled=source!=null && target!=null && phase>.25f && phase<.55f;
            if(!line.enabled)return;line.positionCount=2;line.SetPosition(0,source.Actor.transform.position+Vector3.up*1.2f);line.SetPosition(1,target.Actor.transform.position+Vector3.up*.9f);
        }
        private int Initial(bool player){int value=0;foreach(var g in groups)if(g.Player==player)value+=g.Initial;return value;}
        private int Survivors(bool player){int value=0;foreach(var g in groups)if(g.Player==player)value+=g.Survivors;return value;}
        private void SetTotals(int yours,int enemy){totals.text="Your survivors: "+yours+"     Enemy: "+enemy;}
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
            if(texture!=null){texture.Release();Destroy(texture);}if(stage!=null){stage.SetActive(false);Destroy(stage);}art?.Dispose();art=null;overlay=null;stage=null;view=null;texture=null;groups.Clear();
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
