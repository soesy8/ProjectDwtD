using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace DwtD.TestScene
{
    public sealed class WetCorridorPrototype : MonoBehaviour
    {
        public enum Phase { Title, Choosing, WaitingForPath, UnsafeMotion, UnsafeExplanation, SafeMotion, Reward, CauseCheck, TransferCheck, Completed, Observing }
        [Header("Scene references")]
        public Transform friend, leftArm, rightArm, leftLeg, rightLeg, friendShadow;
        public GameObject pathHighlight, causeHighlight, smile, surprisedMouth, stampMark, completionSparkles;
        public GameObject titlePanel, choicePanel, pathPanel, retryPanel, rewardPanel;
        public TMP_Text heading, subtitle, bubble, stageCounter, rewardText;
        public UnityEngine.UI.Button startButton, hurryButton, stopButton, waveButton, pathButton, retryButton, stampButton;
        [Header("Independent scene features")]
        public CorridorFinalFeedback finalFeedback;
        public CorridorThoughtBubble thoughtBubble;
        public AudioSource feedbackAudio;
        public Camera playerCamera;
        public GameObject xrRig;
        [UnityEngine.Serialization.FormerlySerializedAs("umbrellaClue")]
        public Transform cleaningToolsClue;
        [Header("Storybook art")]
        [Min(1f)] public float friendSpeechHeight = 2.65f;
        public Material storybookTextMaterial;

        public Phase CurrentPhase { get; private set; }
        public bool HasStamp { get; private set; }
        public int StampCount => HasStamp ? 1 : 0;
        public int RetryCount { get; private set; }
        public int UnsafeAttempts { get; private set; }
        public int CauseFirstAnswer => finalFeedback==null ? -1 : finalFeedback.CauseFirstAnswer;
        public int TransferFirstAnswer => finalFeedback==null ? -1 : finalFeedback.TransferFirstAnswer;
        public bool UsedPathHint { get; private set; }
        public int HintCount { get; private set; }
        public bool ObservedWater { get; private set; }
        public bool WasGuided => RetryCount > 0 || HintCount > 0;
        public bool CanObserve => CurrentPhase == Phase.Observing || CurrentPhase == Phase.Choosing || CurrentPhase == Phase.WaitingForPath;
        public CorridorStoryPresentation Story { get; private set; }

        readonly Vector3 startPosition = CorridorComposition.FriendStart;
        Quaternion baseRotation = Quaternion.Euler(0,180,0);
        float gait, walkAmount;
        AudioClip positiveTone, warningTone, stampTone;
        readonly GameObject[] panelCache = new GameObject[5];

        void Awake()
        {
            panelCache[0]=titlePanel; panelCache[1]=choicePanel; panelCache[2]=pathPanel; panelCache[3]=retryPanel;
            panelCache[4]=rewardPanel;
            startButton.onClick.AddListener(Begin);
            hurryButton.onClick.AddListener(()=>ChooseUnsafe(true));
            stopButton.onClick.AddListener(StopFriend);
            waveButton.onClick.AddListener(()=>ChooseUnsafe(false));
            pathButton.onClick.AddListener(ChooseDryPath);
            retryButton.onClick.AddListener(Retry);
            stampButton.onClick.AddListener(CollectStamp);
            if(finalFeedback!=null) finalFeedback.Initialize(this);
            positiveTone=MakeTone(610,830,.17f); warningTone=MakeTone(300,220,.22f); stampTone=MakeTone(750,1080,.25f);
            ResetActor();
            HasStamp=false; stampMark.SetActive(false); stageCounter.text="0 / 3";
            CurrentPhase=Phase.Title;
            ShowPanel(titlePanel);
            heading.text="친구와 교실에 가는 길";
            subtitle.text="학교 복도에서 시작하는 짧은 이야기";
            bubble.text="같이 교실 갈래?";
            Story=gameObject.AddComponent<CorridorStoryPresentation>();
            Story.Initialize(this);
        }

        void Update()
        {
            if(!XRSettings.isDeviceActive && Keyboard.current != null)
            {
                var k=Keyboard.current;
                if(k.spaceKey.wasPressedThisFrame) { if(CurrentPhase==Phase.Title) Begin(); else if(CurrentPhase==Phase.Observing) OpenChoices(); }
                if(k.hKey.wasPressedThisFrame) RequestHint();
                if(k.digit1Key.wasPressedThisFrame) { if(CurrentPhase==Phase.Choosing) ChooseUnsafe(true); else if(CurrentPhase==Phase.CauseCheck || CurrentPhase==Phase.TransferCheck) Answer(0); }
                if(k.digit2Key.wasPressedThisFrame) { if(CurrentPhase==Phase.Choosing) StopFriend(); else if(CurrentPhase==Phase.CauseCheck || CurrentPhase==Phase.TransferCheck) Answer(1); }
                if(k.digit3Key.wasPressedThisFrame && CurrentPhase==Phase.Choosing) ChooseUnsafe(false);
                if(k.digit4Key.wasPressedThisFrame) ChooseDryPath();
                if(k.rKey.wasPressedThisFrame) { if(CurrentPhase==Phase.UnsafeExplanation) Retry(); else if(CurrentPhase==Phase.Completed) RestartStage(); }
                if(k.enterKey.wasPressedThisFrame) { if(CurrentPhase==Phase.Reward) CollectStamp(); else NextQuestion(); }
            }
            gait+=Time.unscaledDeltaTime*8*walkAmount;
            rightArm.localRotation=Quaternion.Euler(Mathf.Sin(gait)*14*walkAmount,0,-8);
            leftArm.localRotation=Quaternion.Euler(-Mathf.Sin(gait)*14*walkAmount,0,8);
            leftLeg.localRotation=Quaternion.Euler(Mathf.Sin(gait)*24*walkAmount,0,0);
            rightLeg.localRotation=Quaternion.Euler(-Mathf.Sin(gait)*24*walkAmount,0,0);
            friendShadow.position=new Vector3(friend.position.x,.025f,friend.position.z);
            bubble.transform.parent.position=friend.position+Vector3.up*friendSpeechHeight;
        }

        public void Begin()
        {
            if(CurrentPhase!=Phase.Title) return;
            CurrentPhase=Phase.Observing; ShowPanel(null);
            heading.text="친구가 나를 기다리고 있어";
            subtitle.text="주변의 물건과 친구를 가리켜 살펴볼 수 있어요.";
            bubble.text="그쪽으로 갈까?";
        }

        public void OpenChoices()
        {
            if(CurrentPhase!=Phase.Observing) return;
            CurrentPhase=Phase.Choosing; ShowPanel(choicePanel);
            heading.text="친구에게 어떤 신호를 보낼까?";
            subtitle.text="마음이 정해지면 행동을 선택하세요.";
            bubble.text="어떻게 갈까?";
        }

        public void ObserveWater()
        {
            if(CanObserve) ObservedWater=true;
        }

        public void RequestHint()
        {
            if(!CanObserve) return;
            HintCount++;
            if(CurrentPhase==Phase.WaitingForPath) UsedPathHint=true;
            Story.ShowHint();
        }

        public void StopFriend()
        {
            if(CurrentPhase!=Phase.Choosing) return;
            CurrentPhase=Phase.WaitingForPath; ShowPanel(pathPanel);
            heading.text="친구가 잠깐 멈췄어요";
            subtitle.text="물기를 피해 지나갈 길을 가리켜 주세요.";
            bubble.text="어느 쪽으로 갈까?";
            pathHighlight.SetActive(true); Play(positiveTone);
        }

        public void ChooseDryPath()
        {
            if(CurrentPhase!=Phase.WaitingForPath) return;
            CurrentPhase=Phase.SafeMotion; ShowPanel(null); pathHighlight.SetActive(false);
            heading.text="마른 길로 천천히";
            subtitle.text="내가 알려준 길을 따라 친구가 지나가요.";
            bubble.text="알려줘서 고마워!";
            StartCoroutine(SafeWalk());
        }

        public void ChooseUnsafe(bool hurry)
        {
            if(CurrentPhase!=Phase.Choosing) return;
            UnsafeAttempts++;
            CurrentPhase=Phase.UnsafeMotion; ShowPanel(null);
            heading.text=hurry ? "친구가 서두르기 시작했어요" : "친구가 바로 다가오고 있어요";
            subtitle.text="발밑에서 어떤 일이 생기는지 살펴보세요.";
            bubble.text=hurry ? "금방 갈게!" : "응, 갈게!";
            StartCoroutine(UnsafeWalk(hurry));
        }

        IEnumerator UnsafeWalk(bool hurry)
        {
            yield return MoveTo(CorridorComposition.UnsafeContact,hurry ? 2.6f : 1.8f);
            heading.text="잠깐, 발밑에 물기가 있어요";
            subtitle.text="서두르던 발걸음이 물기에 닿았어요.";
            causeHighlight.SetActive(true); Play(warningTone);
            smile.SetActive(false); surprisedMouth.SetActive(true); bubble.text="앗!";
            float t=0; walkAmount=.18f;
            Vector3 p=friend.position;
            while(t<1)
            {
                t+=Time.unscaledDeltaTime*.5f;
                friend.position=Vector3.Lerp(p,CorridorComposition.UnsafeEnd,Mathf.Clamp01(t));
                friend.rotation=baseRotation*Quaternion.Euler(-12,0,Mathf.Lerp(0,hurry ? 38 : 24,t));
                yield return null;
            }
            walkAmount=0; CurrentPhase=Phase.UnsafeExplanation; ShowPanel(retryPanel);
            heading.text="작은 신호가 위험으로 이어졌어요";
            subtitle.text="젖은 바닥을 먼저 확인하고, 친구에게 멈춤 신호를 보내볼까요?";
            bubble.text="다시 해볼까?";
        }

        public void Retry()
        {
            if(CurrentPhase==Phase.Completed) { RestartStage(); return; }
            if(CurrentPhase!=Phase.UnsafeExplanation) return;
            StopAllCoroutines(); RetryCount++; ResetActor();
            CurrentPhase=Phase.Choosing; ShowPanel(choicePanel);
            heading.text="선택하기 전으로 돌아왔어요";
            subtitle.text="서두르지 않아도 괜찮아요. 다시 신호를 보내 보세요.";
            bubble.text="어느 길로 갈까?";
        }

        IEnumerator SafeWalk()
        {
            yield return MoveTo(CorridorComposition.SafeTurn,1.45f);
            yield return MoveTo(CorridorComposition.SafePass,1.45f);
            yield return MoveTo(CorridorComposition.SafeEnd,1.35f);
            friend.rotation=baseRotation; walkAmount=0; Play(positiveTone);
            CurrentPhase=Phase.Reward; ShowPanel(rewardPanel);
            heading.text="친구가 무사히 도착했어요";
            subtitle.text="바닥을 살피고 길을 알려준 행동을 기억해요.";
            bubble.text="덕분에 안전하게 왔어!";
            rewardText.text=HasStamp ? "주변 살피기\n이미 기록한 행동이에요. 다시 실천했어요!" : "주변 살피기\n안전한 길을 알려줘서 친구가 무사히 지나갔어요.";
        }

        IEnumerator MoveTo(Vector3 target,float speed)
        {
            walkAmount=1;
            while(Vector3.Distance(friend.position,target)>.025f)
            {
                friend.position=Vector3.MoveTowards(friend.position,target,speed*Time.unscaledDeltaTime);
                yield return null;
            }
            friend.position=target; walkAmount=0;
        }

        public void CollectStamp()
        {
            if(CurrentPhase!=Phase.Reward) return;
            if(!HasStamp)
            {
                HasStamp=true; stampMark.SetActive(true); stageCounter.text="1 / 3";
                completionSparkles.SetActive(true); Play(stampTone);
            }
            if(finalFeedback==null || !finalFeedback.TryBegin()) FinishWithoutFeedback();
        }

        // The optional component owns its questions, answers, and final explanation.
        public void Answer(int answer)
        {
            if(finalFeedback!=null) finalFeedback.Answer(answer);
        }

        public void NextQuestion()
        {
            if(finalFeedback!=null) finalFeedback.NextQuestion();
        }

        internal void ShowFeedbackPanel(Phase phase,GameObject panel)
        {
            CurrentPhase=phase; ShowPanel(null);
            panel.SetActive(true);
        }

        internal void FinishWithoutFeedback()
        {
            CurrentPhase=Phase.Completed; ShowPanel(retryPanel);
            heading.text="친구가 무사히 도착했어요";
            subtitle.text="";
            bubble.text="덕분에 안전하게 왔어!";
        }

        internal void PlayAnswerTone(bool correct) => Play(correct ? positiveTone : warningTone);
        internal void PlayCompletionTone() => Play(stampTone);

        public void RestartStage()
        {
            if(CurrentPhase!=Phase.Completed) return;
            StopAllCoroutines(); ResetActor();
            ObservedWater=false;
            CurrentPhase=Phase.Observing; ShowPanel(null);
            heading.text="친구와 교실에 가는 길";
            subtitle.text="이미 받은 도장은 수첩에 남아 있어요.";
            bubble.text="안녕!";
        }

        void ResetActor()
        {
            friend.position=startPosition; friend.rotation=baseRotation; walkAmount=0;
            causeHighlight.SetActive(false); pathHighlight.SetActive(false);
            completionSparkles.SetActive(false); smile.SetActive(true); surprisedMouth.SetActive(false);
        }

        void ShowPanel(GameObject panel)
        {
            foreach(var p in panelCache) if(p!=null) p.SetActive(p==panel);
            if(finalFeedback!=null) finalFeedback.HidePanels();
            if(Story!=null) Story.SyncPhase();
        }

        void Play(AudioClip clip) { if(feedbackAudio != null && clip != null) feedbackAudio.PlayOneShot(clip,.16f); }
        AudioClip MakeTone(float from,float to,float duration)
        {
            const int rate=22050; var data=new float[Mathf.CeilToInt(rate*duration)]; float phase=0;
            for(int i=0;i<data.Length;i++)
            {
                float t=(float)i/data.Length;
                phase+=Mathf.Lerp(from,to,t)*Mathf.PI*2/rate;
                data[i]=Mathf.Sin(phase)*Mathf.Sin(t*Mathf.PI)*.45f;
            }
            var clip=AudioClip.Create("SoftFeedback",data.Length,1,rate,false); clip.SetData(data,0); return clip;
        }

        void OnDestroy()
        {
            if(positiveTone!=null) Destroy(positiveTone);
            if(warningTone!=null) Destroy(warningTone);
            if(stampTone!=null) Destroy(stampTone);
        }
    }
}
