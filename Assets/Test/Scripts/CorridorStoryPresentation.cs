using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace DwtD.TestScene
{
    // Extends the existing scene in place. All additions are owned by the prototype.
    public sealed class CorridorStoryPresentation : MonoBehaviour
    {
        readonly Color ink=CorridorUITheme.Ink;
        WetCorridorPrototype owner;
        TMP_FontAsset font;
        GameObject exploreRoot, utilityRoot;
        CorridorThoughtBubble thoughts;
        CorridorObservationTarget waterTarget, friendTarget, cleaningTarget, mopTarget;
        Material outlineMaterial, sparkleMaterial;
        LineRenderer[] sparkles;
        WetCorridorPrototype.Phase lastPhase;
        int hintLevel;
        float initializedAt;
        readonly Vector3[] thoughtCorners=new Vector3[4];
        RectTransform situationHeader;

        public bool CanObserve => owner!=null && owner.CanObserve;
        public string ThoughtText => thoughts==null?"":thoughts.ThoughtText;
        public string LastClue { get; private set; }="";

        public void Initialize(WetCorridorPrototype controller)
        {
            owner=controller;
            thoughts=owner.thoughtBubble;
            font=owner.heading.font;
            situationHeader=owner.heading.GetComponentInParent<Canvas>(true).transform as RectTransform;
            initializedAt=Time.unscaledTime;
            StyleExistingUI();
            CreateStoryControls();
            CreateObservationTargets();
            SyncPhase();
        }

        void StyleExistingUI()
        {
            foreach(var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(text.GetComponentInParent<Canvas>(true)!=null)
                {
                    text.color=ink;
                    if(owner.storybookTextMaterial!=null && text.font==font) text.fontSharedMaterial=owner.storybookTextMaterial;
                }
            // Choices share a visual treatment; color does not reveal the safe answer.
            SetLabel(owner.hurryButton,"1   빨리 와!");
            SetLabel(owner.stopButton,"2   잠깐 멈춰!");
            SetLabel(owner.waveButton,"3   이쪽으로 와!");
            SetLabel(owner.startButton,"친구 만나러 가기   ·   Space");
            SetLabel(owner.pathButton,"4   마른 길 가리키기");
            SetLabel(owner.retryButton,"다시 신호 보내기   ·   R");
            CorridorUITheme.Apply(owner);
            var buttons=new[] { owner.startButton,owner.hurryButton,owner.stopButton,owner.waveButton,
                owner.pathButton,owner.retryButton,owner.stampButton };
            foreach(var button in buttons) CorridorUITheme.StyleButton(button);
            if(owner.finalFeedback!=null)
                foreach(var button in new[] { owner.finalFeedback.answerA,owner.finalFeedback.answerB,
                    owner.finalFeedback.questionNextButton,owner.finalFeedback.restartButton }) CorridorUITheme.StyleButton(button);
            foreach(var button in new[]{owner.hurryButton,owner.stopButton,owner.waveButton})
            {
                var label=button.GetComponentInChildren<TMP_Text>(true);
                if(label!=null) { label.fontSize=33; label.fontStyle=FontStyles.Bold; }
            }
            // Keep the existing scene layout and references, and improve contrast.
            owner.heading.fontStyle=FontStyles.Bold;
            owner.subtitle.color=new Color(.35f,.29f,.23f);
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
            if(owner.storybookTextMaterial!=null) text.fontSharedMaterial=owner.storybookTextMaterial;
            text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false;
            text.textWrappingMode=TextWrappingModes.Normal;
            return text;
        }

        UnityEngine.UI.Button Button(string name,Transform parent,string label,Vector2 size,Vector2 position,UnityEngine.Events.UnityAction action)
        {
            var rect=Rect(name,parent,size,position);
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            var text=Text("Label",rect,label,size-new Vector2(32,12),Vector2.zero,32);
            text.fontStyle=FontStyles.Bold;
            CorridorUITheme.StyleButton(button);
            button.onClick.AddListener(action);
            return button;
        }

        void CreateStoryControls()
        {
            var explore=CanvasRoot("Observe_Then_Act",Vector3.zero,new Vector2(250,250));
            exploreRoot=explore.gameObject;
            Button("OpenSignalChoices",explore,"행동 선택",new Vector2(250,250),Vector2.zero,owner.OpenChoices);
            CorridorComposition.PlacePanel(explore,owner.playerCamera,new Vector2(.86f,.16f),.19f);
            var utility=CanvasRoot("Story_Hint",Vector3.zero,new Vector2(240,84));
            utilityRoot=utility.gameObject;
            Button("Hint",utility,"힌트 · H",new Vector2(240,84),Vector2.zero,owner.RequestHint);
            CorridorComposition.PlacePanel(utility,owner.playerCamera,new Vector2(.86f,.315f),.052f);
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
            water.transform.SetParent(transform,false); water.transform.position=CorridorComposition.WaterCenter;
            var waterCollider=water.AddComponent<BoxCollider>(); waterCollider.size=new Vector3(2.65f,.10f,2.3f);
            waterTarget=water.AddComponent<CorridorObservationTarget>();
            waterTarget.clue=CorridorObservationTarget.Clue.Water; waterTarget.story=this;
            waterTarget.outline=Loop("Water_Contour",water.transform,CorridorComposition.WaterRadius,.025f,true);

            var person=new GameObject("Observe_Friend");
            person.transform.SetParent(owner.friend,false);
            var personCollider=person.AddComponent<BoxCollider>(); personCollider.center=new Vector3(0,1.15f,0); personCollider.size=new Vector3(1.35f,2.35f,.95f);
            friendTarget=person.AddComponent<CorridorObservationTarget>();
            friendTarget.clue=CorridorObservationTarget.Clue.Friend; friendTarget.story=this;
            friendTarget.outline=Loop("Friend_Focus",person.transform,new Vector2(.68f,.45f),.026f,false);

            if(owner.cleaningToolsClue!=null)
            {
                var tools=new GameObject("Observe_CleaningTools");tools.transform.SetParent(owner.cleaningToolsClue.Find("Open_Bucket"),false);
                var collider=tools.AddComponent<BoxCollider>();collider.center=new Vector3(0,.42f,0);collider.size=new Vector3(.85f,.88f,.85f);
                cleaningTarget=tools.AddComponent<CorridorObservationTarget>();cleaningTarget.clue=CorridorObservationTarget.Clue.CleaningTools;cleaningTarget.story=this;
                cleaningTarget.outline=Loop("Cleaning_Tools_Focus",tools.transform,new Vector2(.43f,.39f),.025f,false);
                var mop=owner.cleaningToolsClue.Find("String_Mop");
                var mopObservation=new GameObject("Observe_Mop");mopObservation.transform.SetParent(mop,false);
                var mopCollider=mopObservation.AddComponent<BoxCollider>();mopCollider.center=new Vector3(0,.79f,0);mopCollider.size=new Vector3(.58f,1.62f,.60f);
                mopTarget=mopObservation.AddComponent<CorridorObservationTarget>();mopTarget.clue=CorridorObservationTarget.Clue.CleaningTools;mopTarget.story=this;
                mopTarget.outline=Loop("Mop_Focus",owner.cleaningToolsClue,new Vector2(.35f,.36f),.025f,false);
                mopTarget.outline.transform.position=new Vector3(mop.position.x,.045f,mop.position.z);
            }

            var vfx=new GameObject("Water_Sparkles"); vfx.transform.SetParent(transform,false);
            sparkles=new LineRenderer[6];
            for(int i=0;i<sparkles.Length;i++)
            {
                var line=new GameObject("Water_Glint_"+i).AddComponent<LineRenderer>();
                line.transform.SetParent(vfx.transform,false);
                line.transform.position=CorridorComposition.WaterCenter+new Vector3(-.78f+i*.30f,.025f,-.42f+(i%2)*.80f);
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
            if(cleaningTarget!=null) { cleaningTarget.gameObject.SetActive(CanObserve);cleaningTarget.outline.enabled=false; }
            if(mopTarget!=null) { mopTarget.gameObject.SetActive(CanObserve);mopTarget.outline.enabled=false; }
            situationHeader.gameObject.SetActive(false);
            owner.stageCounter.GetComponentInParent<Canvas>(true).gameObject.SetActive(lastPhase==WetCorridorPrototype.Phase.Reward || lastPhase==WetCorridorPrototype.Phase.Completed);
            bool show=lastPhase!=WetCorridorPrototype.Phase.Reward && lastPhase!=WetCorridorPrototype.Phase.CauseCheck && lastPhase!=WetCorridorPrototype.Phase.TransferCheck && lastPhase!=WetCorridorPrototype.Phase.Completed;
            SetLabel(owner.retryButton,lastPhase==WetCorridorPrototype.Phase.Completed?"다시 둘러보기   ·   R":"다시 신호 보내기   ·   R");
            if(!CanObserve && lastPhase!=WetCorridorPrototype.Phase.UnsafeMotion && lastPhase!=WetCorridorPrototype.Phase.UnsafeExplanation)
                owner.causeHighlight.SetActive(false);
            string message="";
            switch(lastPhase)
            {
                case WetCorridorPrototype.Phase.Title:
                case WetCorridorPrototype.Phase.Observing: message="어떻게\n지나갈까?"; break;
                case WetCorridorPrototype.Phase.Choosing: message=owner.ObservedWater?"바닥이 젖었네.\n어떻게 알려주지?":"친구가 이쪽으로\n오려고 하네."; break;
                case WetCorridorPrototype.Phase.WaitingForPath: message="친구가 멈췄어.\n어느 길로 안내할까?"; break;
                case WetCorridorPrototype.Phase.UnsafeMotion: message="내 신호를 보고\n친구가 오고 있어."; break;
                case WetCorridorPrototype.Phase.UnsafeExplanation: message="젖은 바닥을\n못 봤구나."; break;
                case WetCorridorPrototype.Phase.SafeMotion: message="물기가 없는 쪽으로\n잘 오고 있어!"; break;
            }
            if(thoughts!=null) thoughts.Show("속마음",message,show);
            var hands=owner.playerCamera.GetComponentInChildren<CorridorFirstPersonHands>();if(hands!=null) hands.Refresh();
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
            else if(clue==CorridorObservationTarget.Clue.CleaningTools) message="청소 중인가 봐.\n바닥이 젖었네.";
            else message=owner.CurrentPhase==WetCorridorPrototype.Phase.WaitingForPath?"내가 알려줄 길을\n기다리고 있네.":"친구가 나를 보느라\n발밑을 못 보고 있네.";
            if(thoughts!=null) thoughts.Show("속마음",message,true);
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
            if(thoughts!=null) thoughts.Show("속마음 · 힌트",message,true);
        }

        void Update()
        {
            if(owner==null || sparkles==null) return;
            float t=Time.unscaledTime-initializedAt;
            for(int i=0;i<sparkles.Length;i++)
            {
                sparkles[i].enabled=CanObserve;
                float scale=.65f+.25f*Mathf.Sin(t*1.8f+i*1.4f);
                sparkles[i].transform.localScale=Vector3.one*scale;
            }
        }

        void LateUpdate()=>RefreshSpeechLayout();

        public void RefreshSpeechLayout()
        {
            if(owner==null || owner.playerCamera==null) return;
            var camera=owner.playerCamera;
            var speech=owner.bubble.transform.parent as RectTransform;
            var position=owner.friend.position+Vector3.up*owner.friendSpeechHeight;
            var anchor=camera.WorldToViewportPoint(position);
            if(speech==null || anchor.z<=camera.nearClipPlane) return;
            float viewHeight=camera.orthographic?camera.orthographicSize*2:2*anchor.z*Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad);
            const float heightInView=.052f;
            speech.localScale=Vector3.one*(viewHeight*heightInView/Mathf.Max(1,speech.rect.height));
            // Keep the dialogue above its speaker; shift only when another visible UI overlaps.
            if(anchor.x>.08f && anchor.x<.92f && anchor.y>0 && anchor.y<1.1f)
            {
                anchor.y=Mathf.Min(anchor.y,.945f);
                float halfWidth=heightInView*speech.rect.width/Mathf.Max(1,speech.rect.height)/camera.aspect*.5f;
                var speechBounds=new Rect(anchor.x-halfWidth,anchor.y-heightInView*.5f,halfWidth*2,heightInView);
                if(thoughts!=null && thoughts.bubbleRoot.gameObject.activeInHierarchy)
                {
                    thoughts.FollowView();var thoughtBounds=ViewportBounds(thoughts.bubbleRoot,camera);
                    if(speechBounds.Overlaps(thoughtBounds))
                        anchor.x=thoughtBounds.center.x<.5f?Mathf.Clamp(thoughtBounds.xMax+halfWidth+.025f,.42f,.78f):Mathf.Clamp(thoughtBounds.xMin-halfWidth-.025f,.28f,.68f);
                }
                speechBounds.x=anchor.x-halfWidth;
                if(situationHeader!=null && situationHeader.gameObject.activeInHierarchy)
                {
                    var headerBounds=ViewportBounds(situationHeader,camera);
                    if(speechBounds.Overlaps(headerBounds)) anchor.y=headerBounds.yMin-heightInView*.5f-.025f;
                }
                position=camera.ViewportToWorldPoint(anchor);
            }
            speech.SetPositionAndRotation(position,camera.transform.rotation);
        }

        Rect ViewportBounds(RectTransform rect,Camera camera)
        {
            rect.GetWorldCorners(thoughtCorners);
            Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;
            foreach(var corner in thoughtCorners)
            {
                var point=(Vector2)camera.WorldToViewportPoint(corner);min=Vector2.Min(min,point);max=Vector2.Max(max,point);
            }
            return UnityEngine.Rect.MinMaxRect(min.x-.008f,min.y-.008f,max.x+.008f,max.y+.008f);
        }

        void OnDestroy()
        {
            if(outlineMaterial!=null) Destroy(outlineMaterial);
            if(sparkleMaterial!=null) Destroy(sparkleMaterial);
        }
    }
}
