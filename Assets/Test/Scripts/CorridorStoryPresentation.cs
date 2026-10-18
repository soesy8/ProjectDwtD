using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace DwtD.TestScene
{
    // Extends the existing scene in place. All additions are owned by the prototype.
    public sealed class CorridorStoryPresentation : MonoBehaviour
    {
        readonly Color ink=new Color(.10f,.20f,.24f);
        readonly Color paper=new Color(1f,.985f,.94f);
        WetCorridorPrototype owner;
        TMP_FontAsset font;
        GameObject exploreRoot, utilityRoot, desktopGuide;
        CorridorThoughtBubble thoughts;
        CorridorObservationTarget waterTarget, friendTarget;
        Material outlineMaterial, sparkleMaterial;
        LineRenderer[] sparkles;
        WetCorridorPrototype.Phase lastPhase;
        int hintLevel;
        float initializedAt;

        public bool CanObserve => owner!=null && owner.CanObserve;
        public string ThoughtText => thoughts==null?"":thoughts.ThoughtText;
        public string LastClue { get; private set; }="";

        public void Initialize(WetCorridorPrototype controller)
        {
            owner=controller;
            thoughts=owner.thoughtBubble;
            font=owner.heading.font;
            initializedAt=Time.unscaledTime;
            StyleExistingUI();
            CreateStoryControls();
            CreateObservationTargets();
            SyncPhase();
        }

        void StyleExistingUI()
        {
            foreach(var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(text.GetComponentInParent<Canvas>()!=null) text.color=ink;
            var buttons=new[] { owner.startButton,owner.hurryButton,owner.stopButton,owner.waveButton,
                owner.pathButton,owner.retryButton,owner.stampButton };
            foreach(var button in buttons) StyleButton(button,paper);
            if(owner.finalFeedback!=null)
                foreach(var button in new[] { owner.finalFeedback.answerA,owner.finalFeedback.answerB,
                    owner.finalFeedback.questionNextButton,owner.finalFeedback.restartButton }) StyleButton(button,paper);
            // Choices share a visual treatment; color does not reveal the safe answer.
            SetLabel(owner.hurryButton,"1   빨리 와!");
            SetLabel(owner.stopButton,"2   잠깐 멈춰!");
            SetLabel(owner.waveButton,"3   이쪽으로 와!");
            SetLabel(owner.startButton,"친구 만나러 가기   ·   Space");
            SetLabel(owner.pathButton,"4   마른 길 가리키기");
            SetLabel(owner.retryButton,"다시 신호 보내기   ·   R");
            foreach(var button in new[]{owner.hurryButton,owner.stopButton,owner.waveButton})
            {
                var label=button.GetComponentInChildren<TMP_Text>(true);
                if(label!=null) { label.fontSize=33; label.fontStyle=FontStyles.Bold; }
            }
            // Keep the existing scene layout and references, and improve contrast.
            owner.heading.fontStyle=FontStyles.Bold;
            owner.subtitle.color=new Color(.23f,.33f,.36f);
            owner.bubble.color=ink;
            owner.bubble.fontSize=44;
            var speech=owner.bubble.transform.parent.GetComponent<RectTransform>();
            if(speech!=null)
            {
                speech.sizeDelta=new Vector2(530,125);
                owner.bubble.rectTransform.sizeDelta=new Vector2(500,110);
            }
        }

        void StyleButton(UnityEngine.UI.Button button,Color fill)
        {
            var oldImage=button.GetComponent<UnityEngine.UI.Image>();
            if(oldImage!=null) { oldImage.enabled=false; oldImage.raycastTarget=false; }
            var surface=Rect("ComicButtonSurface",button.transform,Vector2.zero,Vector2.zero);
            surface.anchorMin=Vector2.zero; surface.anchorMax=Vector2.one;
            surface.sizeDelta=Vector2.zero; surface.SetAsFirstSibling();
            var shape=surface.gameObject.AddComponent<CorridorBubbleGraphic>();
            shape.color=fill; shape.borderColor=ink; shape.borderWidth=3; shape.cornerRadius=26;
            shape.raycastTarget=true;
            button.targetGraphic=shape;
            var colors=button.colors;
            colors.normalColor=Color.white;
            colors.highlightedColor=new Color(.84f,1f,.96f);
            colors.pressedColor=new Color(.65f,.89f,.82f);
            colors.selectedColor=Color.white;
            colors.disabledColor=new Color(.7f,.74f,.73f,.6f);
            button.colors=colors;
            foreach(var text in button.GetComponentsInChildren<TMP_Text>(true)) text.color=ink;
            foreach(var icon in button.GetComponentsInChildren<CorridorSymbolGraphic>(true)) icon.color=ink;
        }

        static void SetLabel(UnityEngine.UI.Button button,string text)
        {
            var label=button.GetComponentInChildren<TMP_Text>(true);
            if(label!=null) label.text=text;
        }

        RectTransform CanvasRoot(string name,Vector3 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas));
            go.transform.SetParent(transform,false);
            var rect=(RectTransform)go.transform;
            rect.position=position; rect.rotation=Quaternion.identity;
            rect.localScale=Vector3.one*.0028f; rect.sizeDelta=size;
            var canvas=go.GetComponent<Canvas>();
            canvas.renderMode=RenderMode.WorldSpace; canvas.worldCamera=owner.playerCamera;
            go.AddComponent<TrackedDeviceGraphicRaycaster>();
            return rect;
        }

        RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
        {
            var rect=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;
            rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
            rect.pivot=new Vector2(.5f,.5f); rect.sizeDelta=size; rect.anchoredPosition=position;
            return rect;
        }

        TMP_Text Text(string name,Transform parent,string value,Vector2 size,Vector2 position,float fontSize)
        {
            var rect=Rect(name,parent,size,position);
            var text=rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font=font; text.text=value; text.fontSize=fontSize; text.color=ink;
            text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false;
            text.textWrappingMode=TextWrappingModes.Normal;
            return text;
        }

        UnityEngine.UI.Button Button(string name,Transform parent,string label,Vector2 size,Vector2 position,UnityEngine.Events.UnityAction action)
        {
            var rect=Rect(name,parent,size,position);
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            StyleButton(button,paper);
            var text=Text("Label",rect,label,size-new Vector2(32,12),Vector2.zero,32);
            text.fontStyle=FontStyles.Bold;
            button.onClick.AddListener(action);
            return button;
        }

        void CreateStoryControls()
        {
            var explore=CanvasRoot("Observe_Then_Act",new Vector3(0,.37f,-1.15f),new Vector2(1040,195));
            exploreRoot=explore.gameObject;
            Text("ObserveInstruction",explore,"가리켜서 살펴보고, 준비되면 친구에게 신호를 보내요.",new Vector2(1040,56),new Vector2(0,68),28);
            Button("OpenSignalChoices",explore,"친구에게 신호 보내기   ·   Space",new Vector2(840,115),new Vector2(0,-18),owner.OpenChoices);
            var utility=CanvasRoot("Story_Hint",new Vector3(1.72f,.78f,-.8f),new Vector2(440,110));
            utilityRoot=utility.gameObject;
            Button("Hint",utility,"힌트 보기   ·   H",new Vector2(430,100),Vector2.zero,owner.RequestHint);
            var desktop=FindFirstObjectByType<DesktopTestControls>();
            if(desktop!=null)
            {
                var tips=CanvasRoot("Controls_Guide",new Vector3(0,2.31f,-1.10f),new Vector2(1360,45));
                desktopGuide=tips.gameObject;
                Text("Controls",tips,"마우스로 둘러보기  ·  조준 후 클릭  ·  Esc 마우스 해제",new Vector2(1360,45),Vector2.zero,24);
            }
        }

        void CreateObservationTargets()
        {
            var shader=Shader.Find("Universal Render Pipeline/Unlit");
            if(shader==null) shader=Shader.Find("Sprites/Default");
            outlineMaterial=new Material(shader);
            outlineMaterial.SetColor("_BaseColor",new Color(1f,.69f,.19f));
            outlineMaterial.color=new Color(1f,.69f,.19f);
            sparkleMaterial=new Material(shader);
            sparkleMaterial.SetColor("_BaseColor",new Color(.94f,1f,1f));
            sparkleMaterial.color=new Color(.94f,1f,1f);

            var water=new GameObject("Observe_Water");
            water.transform.SetParent(transform,false); water.transform.position=new Vector3(-.45f,.075f,2.1f);
            var waterCollider=water.AddComponent<BoxCollider>(); waterCollider.size=new Vector3(2.8f,.10f,1.48f);
            waterTarget=water.AddComponent<CorridorObservationTarget>();
            waterTarget.clue=CorridorObservationTarget.Clue.Water; waterTarget.story=this;
            waterTarget.outline=Loop("Water_Contour",water.transform,new Vector2(1.38f,.72f),.032f,true);

            var person=new GameObject("Observe_Friend");
            person.transform.SetParent(owner.friend,false);
            var personCollider=person.AddComponent<BoxCollider>(); personCollider.center=new Vector3(0,.9f,0); personCollider.size=new Vector3(1.1f,1.9f,.7f);
            friendTarget=person.AddComponent<CorridorObservationTarget>();
            friendTarget.clue=CorridorObservationTarget.Clue.Friend; friendTarget.story=this;
            friendTarget.outline=Loop("Friend_Focus",person.transform,new Vector2(.68f,.45f),.026f,false);

            var vfx=new GameObject("Water_Sparkles"); vfx.transform.SetParent(transform,false);
            sparkles=new LineRenderer[6];
            for(int i=0;i<sparkles.Length;i++)
            {
                var line=new GameObject("Water_Glint_"+i).AddComponent<LineRenderer>();
                line.transform.SetParent(vfx.transform,false);
                line.transform.position=new Vector3(-1.30f+i*.33f,.10f,1.88f+(i%2)*.43f);
                line.useWorldSpace=false; line.sharedMaterial=sparkleMaterial; line.widthMultiplier=.024f;
                line.positionCount=5;
                line.SetPositions(new[]{new Vector3(-.085f,0,0),new Vector3(.085f,0,0),Vector3.zero,new Vector3(0,0,-.105f),new Vector3(0,0,.105f)});
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                sparkles[i]=line;
            }
        }

        LineRenderer Loop(string name,Transform parent,Vector2 radius,float width,bool irregular)
        {
            var line=new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(parent,false); line.useWorldSpace=false;
            line.sharedMaterial=outlineMaterial; line.widthMultiplier=width; line.loop=true; line.positionCount=80;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            for(int i=0;i<80;i++)
            {
                float a=i*Mathf.PI*2/80;
                float edge=irregular?1+.06f*Mathf.Sin(a*5):1;
                line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius.x*edge,.018f,Mathf.Sin(a)*radius.y*edge));
            }
            line.enabled=false;
            return line;
        }

        public void SyncPhase()
        {
            if(owner==null) return;
            lastPhase=owner.CurrentPhase; hintLevel=0; LastClue="";
            exploreRoot.SetActive(lastPhase==WetCorridorPrototype.Phase.Observing);
            utilityRoot.SetActive(CanObserve);
            waterTarget.gameObject.SetActive(CanObserve);
            friendTarget.gameObject.SetActive(CanObserve);
            waterTarget.outline.enabled=false; friendTarget.outline.enabled=false;
            bool show=lastPhase!=WetCorridorPrototype.Phase.Title && lastPhase!=WetCorridorPrototype.Phase.Reward && lastPhase!=WetCorridorPrototype.Phase.CauseCheck && lastPhase!=WetCorridorPrototype.Phase.TransferCheck && lastPhase!=WetCorridorPrototype.Phase.Completed;
            SetLabel(owner.retryButton,lastPhase==WetCorridorPrototype.Phase.Completed?"다시 둘러보기   ·   R":"다시 신호 보내기   ·   R");
            if(!CanObserve && lastPhase!=WetCorridorPrototype.Phase.UnsafeMotion && lastPhase!=WetCorridorPrototype.Phase.UnsafeExplanation)
                owner.causeHighlight.SetActive(false);
            string message="";
            switch(lastPhase)
            {
                case WetCorridorPrototype.Phase.Observing: message="친구랑 같이\n교실에 가야지."; break;
                case WetCorridorPrototype.Phase.Choosing: message=owner.ObservedWater?"바닥이 젖었네.\n어떻게 알려주지?":"친구가 이쪽으로\n오려고 하네."; break;
                case WetCorridorPrototype.Phase.WaitingForPath: message="친구가 멈췄어.\n어느 길로 안내할까?"; break;
                case WetCorridorPrototype.Phase.UnsafeMotion: message="내 신호를 보고\n친구가 오고 있어."; break;
                case WetCorridorPrototype.Phase.UnsafeExplanation: message="젖은 바닥을\n못 봤구나."; break;
                case WetCorridorPrototype.Phase.SafeMotion: message="물기가 없는 쪽으로\n잘 오고 있어!"; break;
            }
            if(thoughts!=null) thoughts.Show("내 생각",message,show);
        }

        public void Inspect(CorridorObservationTarget.Clue clue)
        {
            if(!CanObserve) return;
            LastClue=clue.ToString();
            string message;
            if(clue==CorridorObservationTarget.Clue.Water)
            {
                owner.ObserveWater();
                message="바닥이 젖었네.\n미끄럽겠는데?";
            }
            else message=owner.CurrentPhase==WetCorridorPrototype.Phase.WaitingForPath?"내가 알려줄 길을\n기다리고 있네.":"친구가 나를 보느라\n발밑을 못 보고 있네.";
            if(thoughts!=null) thoughts.Show("내 생각 · 살펴보기",message,true);
        }

        public void ShowHint()
        {
            if(!CanObserve) return;
            hintLevel=Mathf.Min(2,hintLevel+1);
            string message;
            if(owner.CurrentPhase==WetCorridorPrototype.Phase.WaitingForPath)
            {
                owner.pathHighlight.SetActive(true);
                message="오른쪽 마른 바닥을\n가리켜 알려주자.";
            }
            else
            {
                owner.causeHighlight.SetActive(true);
                message=hintLevel==1?"친구가 지나올\n바닥을 살펴볼까?":"먼저 멈추게 하고\n마른 길을 알려주자.";
            }
            if(thoughts!=null) thoughts.Show("내 생각 · 힌트",message,true);
        }

        void Update()
        {
            if(owner==null || sparkles==null) return;
            float t=Time.unscaledTime-initializedAt;
            float speechDistance=Vector3.Distance(owner.playerCamera.transform.position,owner.bubble.transform.parent.position);
            owner.bubble.transform.parent.localScale=Vector3.one*Mathf.Clamp(speechDistance*.0005f,.0028f,.0056f);
            if(desktopGuide!=null) desktopGuide.SetActive(!UnityEngine.XR.XRSettings.isDeviceActive);
            for(int i=0;i<sparkles.Length;i++)
            {
                sparkles[i].enabled=CanObserve;
                float scale=.65f+.25f*Mathf.Sin(t*1.8f+i*1.4f);
                sparkles[i].transform.localScale=Vector3.one*scale;
            }
        }

        void OnDestroy()
        {
            if(outlineMaterial!=null) Destroy(outlineMaterial);
            if(sparkleMaterial!=null) Destroy(sparkleMaterial);
        }
    }
}
