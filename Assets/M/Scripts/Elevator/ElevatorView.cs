using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;
using DwtD.TestScene;

namespace ProjectWait.M.Elevator
{
    public sealed class ElevatorView : MonoBehaviour
    {
        public TMP_FontAsset font;
        public Camera playerCamera;
        public ElevatorLesson lesson;
        public TMP_Text title, subtitle, thought;
        public RectTransform actionPanel, thoughtPanel, developerPanel;
        public RectTransform dialoguePanel, infoPanel;
        public TMP_Text dialogueText, infoText;
        public int InteractionRevision { get; private set; }
        public readonly List<UnityEngine.UI.Button> ChoiceButtons = new List<UnityEngine.UI.Button>();
        readonly List<Action> actions = new List<Action>();
        static readonly Color Ink = new Color(.28f,.19f,.14f), Paper = new Color(1,.965f,.86f), Teal = new Color(.16f,.48f,.44f), Mint = new Color(.79f,.89f,.76f);
        bool developer;
        int historyPage;
        Vector3 layoutOrigin;
        float layoutAspect=-1;
        public void Initialize(ElevatorLesson value) { lesson=value; PrepareCompactPresentation(); Reanchor(playerCamera.transform.position); }

        // Called once in Edit Mode by the scene builder; all scene references are serialized.
        public void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = playerCamera;
            var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>(); scaler.dynamicPixelsPerUnit = 2;
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>(); gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            var root = (RectTransform)transform; root.sizeDelta = new Vector2(1280,720);
            transform.position = new Vector3(0,1.63f,-.60f); transform.localScale = Vector3.one * .00168f;
            var header = Card("Mission",root,new Vector2(750,96),new Vector2(-230,294),Paper);
            title = Label("Title",header,new Vector2(710,38),new Vector2(0,19),27,true);
            subtitle = Label("Subtitle",header,new Vector2(710,30),new Vector2(0,-22),18);
            var instructions = Label("Controls",root,new Vector2(1260,28),new Vector2(0,-343),16);
            instructions.text = "PC: 마우스로 관찰·선택 / 오른쪽 버튼을 누르고 시선 이동 / 숫자 1–4 선택 / R 다시 시작 / F8 개발 도구";
            var dev = MakeButton("DeveloperToggle",root,new Vector2(124,42),new Vector2(550,317),"개발 도구",ToggleDeveloper);
            actionPanel = Card("ActionCard",root,new Vector2(790,325),new Vector2(215,-157),Paper);
            thoughtPanel = Card("ThoughtCard",root,new Vector2(400,190),new Vector2(-420,-182),Paper);
            var badge = Label("ThoughtBadge",thoughtPanel,new Vector2(358,26),new Vector2(0,65),18,true); badge.text = "나의 생각 · 관찰"; badge.color = Teal;
            thought = Label("Thought",thoughtPanel,new Vector2(358,132),new Vector2(0,-14),21);
            developerPanel = Card("DeveloperPanel",root,new Vector2(1110,200),new Vector2(0,148),Mint);
            var devTitle = Label("DevTitle",developerPanel,new Vector2(1060,28),new Vector2(0,70),18,true);
            devTitle.text = "개발용: 단계 이동은 이전 기록·타이머·소리·관찰을 초기화합니다.";
            string[] names = {"처음부터", "제안", "점프", "고장·대처", "통화", "구조 대기", "완료"};
            for (int i=0;i<names.Length;i++) { int n=i; MakeButton("Stage"+i,developerPanel,new Vector2(140,43),new Vector2(-465+i*155,14),names[i],()=>lesson.DeveloperJump(n)); }
            MakeButton("NoResponse",developerPanel,new Vector2(470,43),new Vector2(-265,-49),"비상통화 응답/무응답 전환",()=> { lesson.simulateNoResponse=!lesson.simulateNoResponse; Refresh(); });
            MakeButton("CloseDeveloper",developerPanel,new Vector2(180,43),new Vector2(390,-49),"개발 도구 닫기",ToggleDeveloper);
            developerPanel.gameObject.SetActive(false);
            PrepareCompactPresentation(); Reanchor(playerCamera.transform.position);
        }
        public void PrepareCompactPresentation()
        {
            title.gameObject.SetActive(false);
            var mission=(RectTransform)subtitle.transform.parent;mission.sizeDelta=new Vector2(290,38);
            subtitle.rectTransform.sizeDelta=new Vector2(266,32);subtitle.rectTransform.anchoredPosition=Vector2.zero;subtitle.fontSize=17;
            var controls=transform.Find("Controls").GetComponent<TMP_Text>();controls.fontSize=13;
            controls.text="마우스 시선 · 클릭 선택 · Esc 커서 · R 재시작 · F8 도구";
            var dev=(RectTransform)transform.Find("DeveloperToggle");dev.sizeDelta=new Vector2(80,34);
            var devLabel=dev.GetComponentInChildren<TMP_Text>();devLabel.text="도구";devLabel.fontSize=16;devLabel.rectTransform.sizeDelta=new Vector2(66,30);
            foreach(var text in GetComponentsInChildren<TMP_Text>(true))
            {
                var p=text.rectTransform.localPosition;p.z=-2;text.rectTransform.localPosition=p;
            }
            actionPanel.GetComponent<CorridorBubbleGraphic>().enabled=false;
            var cloud=thoughtPanel.GetComponent<CorridorBubbleGraphic>();cloud.thoughtCloud=true;cloud.borderWidth=3;cloud.extrusion=7;cloud.bevelWidth=5;
            var badge=thoughtPanel.Find("ThoughtBadge").GetComponent<TMP_Text>();badge.text="속마음";badge.fontSize=19;badge.alignment=TextAlignmentOptions.Center;badge.rectTransform.sizeDelta=new Vector2(110,30);
            if(!thoughtPanel.Find("ThoughtTab"))Card("ThoughtTab",thoughtPanel,new Vector2(120,34),Vector2.zero,Paper).SetAsFirstSibling();
            if(!dialoguePanel)
            {
                dialoguePanel=Card("FriendSpeech",transform,new Vector2(500,66),Vector2.zero,Paper);
                dialoguePanel.GetComponent<CorridorBubbleGraphic>().speechTail=true;
                dialogueText=Label("Dialogue",dialoguePanel,new Vector2(460,52),Vector2.zero,23,true);
                dialogueText.alignment=TextAlignmentOptions.Center;
            }
            if(!infoPanel)
            {
                infoPanel=Card("ContextNote",transform,new Vector2(380,110),Vector2.zero,Paper);
                infoText=Label("Explanation",infoPanel,new Vector2(344,84),Vector2.zero,19);
            }
            dialoguePanel.gameObject.SetActive(false);infoPanel.gameObject.SetActive(false);thoughtPanel.gameObject.SetActive(false);
            Bind((UnityEngine.UI.Button)dev.GetComponent<UnityEngine.UI.Button>(),ToggleDeveloper);
            for(int i=0;i<7;i++) {int stage=i;Bind(developerPanel.Find("Stage"+i).GetComponent<UnityEngine.UI.Button>(),()=>lesson.DeveloperJump(stage));}
            Bind(developerPanel.Find("NoResponse").GetComponent<UnityEngine.UI.Button>(),()=>{lesson.simulateNoResponse=!lesson.simulateNoResponse;Refresh();});
            Bind(developerPanel.Find("CloseDeveloper").GetComponent<UnityEngine.UI.Button>(),ToggleDeveloper);
            ApplyCompactLayout();
        }
        static void Bind(UnityEngine.UI.Button button,Action action) {button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>action());}
        public void Reanchor(Vector3 origin) { layoutOrigin=origin; ApplyCompactLayout(); }
        public void ApplyCompactLayout()
        {
            if(!playerCamera)return;
            var root=(RectTransform)transform;root.sizeDelta=new Vector2(720*playerCamera.aspect,720);
            float distance=1.15f,height=2*distance*Mathf.Tan(playerCamera.fieldOfView*.5f*Mathf.Deg2Rad);
            transform.SetPositionAndRotation(layoutOrigin+Vector3.forward*distance,Quaternion.identity);
            transform.localScale=Vector3.one*(height*.94f/720);
            Place((RectTransform)subtitle.transform.parent,new Vector2(.16f,.95f));
            Place((RectTransform)transform.Find("DeveloperToggle"),new Vector2(.94f,.95f));
            var controls=(RectTransform)transform.Find("Controls");controls.sizeDelta=new Vector2(root.rect.width-40,22);Place(controls,new Vector2(.5f,.025f));
            Place(thoughtPanel,lesson&&lesson.Flow!=null&&lesson.Flow.Step==ElevatorStep.Complete?new Vector2(.16f,.53f):new Vector2(.15f,.69f));
            if(dialoguePanel)Place(dialoguePanel,new Vector2(.43f,.80f));
            if(infoPanel)Place(infoPanel,lesson&&lesson.Flow!=null&&lesson.Flow.Step==ElevatorStep.Complete?new Vector2(.62f,.58f):new Vector2(.81f,.77f));
            if(lesson&&lesson.Flow!=null)Place(actionPanel,lesson.Flow.Step==ElevatorStep.Observe?new Vector2(.88f,.12f):new Vector2(.57f,.095f));
            Place(developerPanel,new Vector2(.5f,.65f));
            layoutAspect=playerCamera.aspect;
        }
        void Place(RectTransform rect,Vector2 anchor)
        {
            var root=(RectTransform)transform;rect.anchoredPosition=new Vector2((anchor.x-.5f)*root.rect.width,(anchor.y-.5f)*root.rect.height);
        }
        void OnEnable() { Application.onBeforeRender+=FollowThought; }
        void OnDisable() { Application.onBeforeRender-=FollowThought; }
        void LateUpdate()
        {
            if(playerCamera&&Mathf.Abs(playerCamera.aspect-layoutAspect)>.001f)ApplyCompactLayout();
            FollowThought();
        }
        void FollowThought()
        {
            if(!playerCamera||!thoughtPanel||!thoughtPanel.gameObject.activeInHierarchy||!lesson||lesson.Flow==null||lesson.Flow.Step==ElevatorStep.Complete)return;
            thoughtPanel.SetPositionAndRotation(playerCamera.ViewportToWorldPoint(new Vector3(.17f,.69f,1.15f)),playerCamera.transform.rotation);
        }
        public void ToggleDeveloper() { developer=!developer; developerPanel.gameObject.SetActive(developer);InteractionRevision++; }
        public void ShowThought(string value)
        {
            if(lesson!=null&&lesson.Flow!=null&&lesson.Flow.Step!=ElevatorStep.Complete)
            {
                bool longText=value!=null&&value.Length>90;
                float height=longText?260:136;
                thoughtPanel.sizeDelta=new Vector2(310,height);Place(thoughtPanel,new Vector2(.15f,.69f));
                thought.rectTransform.sizeDelta=new Vector2(266,height-36);thought.rectTransform.anchoredPosition=Vector2.zero;thought.fontSize=longText?18:23;
                var tab=new Vector2(-55,height*.5f+12);
                thoughtPanel.Find("ThoughtBadge").GetComponent<RectTransform>().anchoredPosition=tab;
                ((RectTransform)thoughtPanel.Find("ThoughtTab")).anchoredPosition=tab;
            }
            thought.text=value; thoughtPanel.gameObject.SetActive(!string.IsNullOrEmpty(value));
        }
        public void InvokeChoice(int index)
        {
            if(index>=0 && index<ChoiceButtons.Count && ChoiceButtons[index] && ChoiceButtons[index].isActiveAndEnabled && ChoiceButtons[index].interactable)
                ChoiceButtons[index].onClick.Invoke();
        }
        public void Refresh()
        {
            if(lesson == null || lesson.Flow == null) return;
            // Disable immediately before deferred destruction: repeated input cannot hit an old callback.
            foreach(Transform c in actionPanel) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
            actions.Clear(); ChoiceButtons.Clear(); ShowThought(lesson.Thought);
            var f=lesson.Flow; string heading="", body="", phase="";
            switch(f.Step)
            {
                case ElevatorStep.Observe:
                    phase="01  탑승 · 관찰"; heading="어떤 단서가 보이나요?";
                    body="같은 청록 테두리는 모두 ‘관찰 가능’ 표시예요.\n가리켜 생각을 읽고, 준비되면 친구와 이야기해요.";
                    Add("친구와 이야기하기",lesson.Offer); break;
                case ElevatorStep.Proposal:
                    phase="02  친구의 제안"; heading="친구  “"+lesson.settings.proposal+"”";
                    body="어떻게 이야기할까요? 주변을 더 살펴봐도 괜찮아요.";
                    Add("위험해. 뛰지 말자.",()=>lesson.Choose(0)); Add("조금만 뛰어보자.",()=>lesson.Choose(1)); Add("주변을 더 살펴본다.",()=>lesson.Choose(2)); break;
                case ElevatorStep.Jump:
                    phase="03  친구의 행동"; heading=f.Prevention==PreventionChoice.Warned ? "친구  “괜찮아, 한 번만!”" : "친구  “좋아, 한 번만 뛰어볼게!”";
                    body="친구가 몸을 움직이고 있어요."; break;
                case ElevatorStep.Stopped:
                    phase="03  고장 · 정지"; heading="친구  “"+(f.Prevention==PreventionChoice.Warned?lesson.settings.stoppedAfterWarning:lesson.settings.stoppedAfterAgreement)+"”";
                    body=f.Prevention==PreventionChoice.Warned?lesson.settings.preventionFeedback:lesson.settings.agreementFeedback;
                    if(f.HintLevel==1) body+="\n힌트: 주변의 장치와 안내문을 살펴볼까요?";
                    if(f.HintLevel==2) body+="\n힌트: 벨 표시가 있는 비상통화 장치를 확인해요.";
                    Add("비상통화 장치 누르기",lesson.Intercom); Add("문을 억지로 열어 본다.",lesson.UnsafeDoor); Add("스스로 빠져나가려고 한다.",lesson.UnsafeExit); break;
                case ElevatorStep.Connecting:
                    phase="04  도움 요청"; heading="비상통화 연결 중…"; body="스피커의 응답을 기다리고 있어요."; break;
                case ElevatorStep.Unanswered:
                    phase="04  다른 방법으로 도움 요청"; heading="비상통화에 응답이 없어요.";
                    body="연결이 안 될 때는 휴대폰으로 119에 도움을 요청해요.\n이 체험의 전화는 모두 모의 상황이에요.";
                    if(f.HintLevel>0) body+="\n힌트: 건물명과 승강기 식별 정보를 함께 확인해요.";
                    Add("휴대폰으로 119 모의 신고",lesson.Phone); Add("문을 억지로 열어 본다.",lesson.UnsafeDoor); break;
                case ElevatorStep.Call:
                    phase="04  "+(f.UsedPhone?"119 모의 통화":"관리실 모의 통화");
                    if(!f.LocationReported)
                    {
                        heading="상담원  “위치와 상황을 알려 주세요.”";
                        body="옆의 위치 안내를 확인해요. 현재 층을 추측할 필요는 없어요.";
                        Add(lesson.settings.building+" / "+lesson.settings.liftId+"\n2명이 갇혔어요.",lesson.Report);
                        Add("위치·식별 안내 더 살펴보기",()=>lesson.Inspect(ElevatorClue.Identity));
                    }
                    else
                    {
                        heading="상담원  “확인했어요. 구조를 요청할게요.”";
                        body="문을 억지로 열지 마세요. 문에서 떨어져 자세를 낮추고\n안전손잡이를 잡아 침착하게 기다려 주세요.";
                        Add("안내를 듣고 안전하게 기다리기",lesson.Advice);
                    }
                    break;
                case ElevatorStep.Waiting:
                    phase="05  도움 요청 후 안전 대기"; heading=f.Reassured?"친구  “고마워. 함께 기다리자.”":"친구  “조금 무서워…”";
                    body="도움 요청을 전달했어요. 안내에 따라 기다려요.\n훈련을 위해 대기 시간을 줄였습니다.";
                    if(!f.Reassured) Add("도움을 요청했어. 우리 차분히 기다리자.",lesson.Reassure);
                    Add("기다리다가 스스로 나가려고 한다.",lesson.UnsafeExit); break;
                case ElevatorStep.Retry:
                    phase="잠깐 멈추고 다시 생각해요"; heading="Wait!  잠깐!"; body=lesson.settings.doorWarning;
                    Add("이유를 확인했어요. 다시 선택할게요.",lesson.Retry); break;
                case ElevatorStep.Rescue:
                    phase="06  구조대원 도착"; heading="구조대원  “도착했습니다. 안내에 따라 주세요.”";
                    body="문을 직접 조작하지 않고 구조대원의 안내를 기다려요.\n안전한 곳으로 이동하고 있어요…"; break;
                case ElevatorStep.Complete:
                    phase="06  구조 완료"; heading="함께 안전하게 나왔어요.";
                    body=(f.Prevention==PreventionChoice.Warned?"예방 판단: 친구를 말렸어요. 잘한 행동이에요.":f.Prevention==PreventionChoice.Agreed?"예방 판단: 뛰는 행동에 동조했어요. 다음에는 말려요.":"예방 판단: 개발용 이동으로 생략했어요.")+
                        "\n고장 후 대처: "+(f.UsedPhone?"119 모의 신고":"비상통화")+" → 위치 전달 → 안내에 따라 대기"+
                        "\n뛰거나 문에 기대지 않기 / 억지로 탈출하지 않기"+
                        "\n비상통화가 연결 안 되면 휴대폰으로 119에 도움 요청.";
                    Add("내 선택 기록 보기 · 다시 누르면 다음 기록",ShowHistory); Add("처음부터 다시 시작하기",lesson.Restart); break;
            }
            title.text="Wait!  엘리베이터에서 생긴 일";
            subtitle.text=phase+(lesson.simulateNoResponse?"   · 개발 설정: 비상통화 무응답":"");
            bool observing=f.Step==ElevatorStep.Observe;
            dialoguePanel.gameObject.SetActive(!observing);dialogueText.text=heading;
            bool explanation=!observing&&f.Step!=ElevatorStep.Proposal&&f.Step!=ElevatorStep.Jump;
            infoPanel.gameObject.SetActive(explanation);infoText.text=body;
            bool completed=f.Step==ElevatorStep.Complete;
            float infoWidth=completed?700:344;infoText.fontSize=completed?21:19;
            float infoHeight=Mathf.Clamp(infoText.GetPreferredValues(body,infoWidth,0).y+8,50,220);
            infoPanel.sizeDelta=new Vector2(infoWidth+36,infoHeight+24);infoText.rectTransform.sizeDelta=new Vector2(infoWidth,infoHeight);
            Place(infoPanel,completed?new Vector2(.62f,.58f):new Vector2(.81f,.77f));
            float cardWidth=observing?160:actions.Count==1?460:actions.Count==2?360:250;
            float totalWidth=cardWidth*actions.Count+18*Mathf.Max(0,actions.Count-1);
            actionPanel.sizeDelta=new Vector2(totalWidth,112);
            Place(actionPanel,observing?new Vector2(.88f,.12f):new Vector2(.57f,.095f));
            for(int i=0;i<actions.Count;i++)
            {
                var item=actions[i];
                var button=MakeButton("Choice"+(i+1),actionPanel,new Vector2(cardWidth,112),new Vector2(-totalWidth*.5f+cardWidth*.5f+i*(cardWidth+18),0),(i+1)+"  "+labels[i],()=>item());
                StyleChoice(button,labels[i]);
                ChoiceButtons.Add(button);
            }
            labels.Clear();
            InteractionRevision++;
        }
        readonly List<string> labels=new List<string>();
        void Add(string label,Action callback) { labels.Add(label); actions.Add(callback); }
        void ShowHistory()
        {
            thoughtPanel.GetComponent<CorridorBubbleGraphic>().thoughtCloud=false;
            thoughtPanel.Find("ThoughtBadge").GetComponent<TMP_Text>().text="내 선택";
            thoughtPanel.sizeDelta=new Vector2(370,340); Place(thoughtPanel,new Vector2(.16f,.53f));
            thought.rectTransform.sizeDelta=new Vector2(322,280); thought.rectTransform.anchoredPosition=new Vector2(0,-4);
            thought.fontSize=17;
            thoughtPanel.Find("ThoughtBadge").GetComponent<RectTransform>().anchoredPosition=new Vector2(-55,182);
            ((RectTransform)thoughtPanel.Find("ThoughtTab")).anchoredPosition=new Vector2(-55,182);
            int pages=Mathf.Max(1,Mathf.CeilToInt(lesson.Flow.History.Count/4f));
            var entries=lesson.Flow.History.GetRange(historyPage*4,Mathf.Min(4,lesson.Flow.History.Count-historyPage*4));
            ShowThought("선택 기록 "+(historyPage+1)+" / "+pages+"\n\n"+string.Join("\n\n",entries));
            historyPage=(historyPage+1)%pages;
        }
        public void RestoreThoughtLayout()
        {
            thoughtPanel.GetComponent<CorridorBubbleGraphic>().thoughtCloud=true;
            thoughtPanel.Find("ThoughtBadge").GetComponent<TMP_Text>().text="속마음";
            thoughtPanel.sizeDelta=new Vector2(310,136);Place(thoughtPanel,new Vector2(.15f,.69f));
            thought.rectTransform.sizeDelta=new Vector2(266,100);thought.fontSize=23;
            thoughtPanel.Find("ThoughtBadge").GetComponent<RectTransform>().anchoredPosition=new Vector2(-55,80);historyPage=0;
            if(thoughtPanel.Find("ThoughtTab"))((RectTransform)thoughtPanel.Find("ThoughtTab")).anchoredPosition=new Vector2(-55,80);
        }
        void StyleChoice(UnityEngine.UI.Button button,string label)
        {
            var size=((RectTransform)button.transform).sizeDelta;
            var text=button.GetComponentInChildren<TMP_Text>();text.fontSize=size.x<200?20:21;text.alignment=TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta=new Vector2(size.x-24,60);text.rectTransform.anchoredPosition=new Vector2(0,-23);
            text.enableAutoSizing=true;text.fontSizeMin=17;text.fontSizeMax=size.x<200?20:21;
            var badge=Card("IconMedallion",button.transform,new Vector2(46,46),new Vector2(0,26),Mint);
            var face=badge.GetComponent<CorridorBubbleGraphic>();face.cornerRadius=23;face.borderColor=Teal;face.borderWidth=2;face.extrusion=4;
            var icon=Rect("Symbol",button.transform,new Vector2(29,29),new Vector2(0,26)).gameObject.AddComponent<CorridorSymbolGraphic>();
            icon.color=Teal;icon.raycastTarget=false;icon.symbol=label.Contains("살펴")?CorridorSymbolGraphic.Symbol.Eye:CorridorSymbolGraphic.Symbol.Wave;
            var depth=button.gameObject.AddComponent<CorridorButtonDepth>();depth.Initialize(button.GetComponent<CorridorBubbleGraphic>());
        }
        RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 pos)
        {
            var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform; r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f); r.sizeDelta=size;r.anchoredPosition=pos;return r;
        }
        RectTransform Card(string name,Transform parent,Vector2 size,Vector2 pos,Color color)
        {
            var r=Rect(name,parent,size,pos); var g=r.gameObject.AddComponent<CorridorBubbleGraphic>();
            g.color=color;g.borderColor=Ink;g.cornerRadius=22;g.borderWidth=2;g.extrusion=5;g.bevelWidth=4;g.shadowOffset=new Vector2(3,-8);g.raycastTarget=false;return r;
        }
        TMP_Text Label(string name,Transform parent,Vector2 size,Vector2 pos,float point,bool bold=false)
        {
            var r=Rect(name,parent,size,pos);r.localPosition+=Vector3.back*2;
            var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=point;t.color=Ink;
            t.fontStyle=FontStyles.Bold;t.alignment=TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;
            t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Overflow;return t;
        }
        UnityEngine.UI.Button MakeButton(string name,Transform parent,Vector2 size,Vector2 pos,string text,Action callback)
        {
            var r=Card(name,parent,size,pos,Paper);var graphic=r.GetComponent<CorridorBubbleGraphic>();graphic.raycastTarget=true;
            var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=graphic;
            var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.86f,.95f,.89f);colors.pressedColor=new Color(.75f,.86f,.79f);colors.selectedColor=Color.white;b.colors=colors;
            var t=Label("Label",r,size-new Vector2(24,4),Vector2.zero,19,true);t.text=text;
            b.onClick.AddListener(()=>callback());return b;
        }
    }
}
