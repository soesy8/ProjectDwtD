using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace DwtD.TestScene
{
    public sealed class WetCorridorPrototype : MonoBehaviour
    {
        public enum Phase { Title, Choosing, WaitingForPath, UnsafeMotion, UnsafeExplanation, SafeMotion, Reward, CauseCheck, TransferCheck, Completed }
        [Header("Scene references")]
        public Transform friend, leftArm, rightArm, leftLeg, rightLeg, friendShadow;
        public GameObject pathHighlight, causeHighlight, smile, surprisedMouth, stampMark, completionSparkles;
        public GameObject titlePanel, choicePanel, pathPanel, retryPanel, rewardPanel, questionPanel, completedPanel;
        public TMP_Text heading, subtitle, bubble, stageCounter, rewardText, questionTitle, questionFeedback, completedText;
        public UnityEngine.UI.Button startButton, hurryButton, stopButton, waveButton, pathButton, retryButton, stampButton;
        public UnityEngine.UI.Button answerA, answerB, questionNextButton, restartButton;
        public TMP_Text answerAText, answerBText;
        public CorridorSymbolGraphic answerAIcon, answerBIcon;
        public AudioSource feedbackAudio;
        public Camera playerCamera;
        public GameObject xrRig;

        public Phase CurrentPhase { get; private set; }
        public bool HasStamp { get; private set; }
        public int StampCount => HasStamp ? 1 : 0;
        public int RetryCount { get; private set; }
        public int UnsafeAttempts { get; private set; }
        public int CauseFirstAnswer { get; private set; } = -1;
        public int TransferFirstAnswer { get; private set; } = -1;
        public bool UsedPathHint { get; private set; }
        public bool WasGuided => RetryCount > 0 || UsedPathHint;

        readonly Vector3 startPosition = new Vector3(-.55f,0,5.7f);
        Quaternion baseRotation = Quaternion.Euler(0,180,0);
        float gait, walkAmount;
        bool answered;
        AudioClip positiveTone, warningTone, stampTone;
        readonly GameObject[] panelCache = new GameObject[8];

        void Awake()
        {
            panelCache[0]=titlePanel; panelCache[1]=choicePanel; panelCache[2]=pathPanel; panelCache[3]=retryPanel;
            panelCache[4]=rewardPanel; panelCache[5]=questionPanel; panelCache[6]=completedPanel;
            startButton.onClick.AddListener(Begin);
            hurryButton.onClick.AddListener(()=>ChooseUnsafe(true));
            stopButton.onClick.AddListener(StopFriend);
            waveButton.onClick.AddListener(()=>ChooseUnsafe(false));
            pathButton.onClick.AddListener(ChooseDryPath);
            retryButton.onClick.AddListener(Retry);
            stampButton.onClick.AddListener(CollectStamp);
            answerA.onClick.AddListener(()=>Answer(0));
            answerB.onClick.AddListener(()=>Answer(1));
            questionNextButton.onClick.AddListener(NextQuestion);
            restartButton.onClick.AddListener(RestartStage);
            positiveTone=MakeTone(610,830,.17f); warningTone=MakeTone(300,220,.22f); stampTone=MakeTone(750,1080,.25f);
            ResetActor();
            HasStamp=false; stampMark.SetActive(false); stageCounter.text="0 / 3";
            CurrentPhase=Phase.Title;
            ShowPanel(titlePanel);
            heading.text="함께 걷는 복도";
            subtitle.text="친구가 다가오고 있어요. 주변을 먼저 살펴볼까요?";
            bubble.text="안녕!";
        }

        void Update()
        {
            if(!XRSettings.isDeviceActive && Keyboard.current != null)
            {
                var k=Keyboard.current;
                if(k.spaceKey.wasPressedThisFrame && CurrentPhase==Phase.Title) Begin();
                if(k.digit1Key.wasPressedThisFrame) { if(CurrentPhase==Phase.Choosing) ChooseUnsafe(true); else if(CurrentPhase==Phase.CauseCheck || CurrentPhase==Phase.TransferCheck) Answer(0); }
                if(k.digit2Key.wasPressedThisFrame) { if(CurrentPhase==Phase.Choosing) StopFriend(); else if(CurrentPhase==Phase.CauseCheck || CurrentPhase==Phase.TransferCheck) Answer(1); }
                if(k.digit3Key.wasPressedThisFrame && CurrentPhase==Phase.Choosing) ChooseUnsafe(false);
                if(k.digit4Key.wasPressedThisFrame) ChooseDryPath();
                if(k.rKey.wasPressedThisFrame) { if(CurrentPhase==Phase.UnsafeExplanation) Retry(); else if(CurrentPhase==Phase.Completed) RestartStage(); }
                if(k.enterKey.wasPressedThisFrame) { if(CurrentPhase==Phase.Reward) CollectStamp(); else if(answered) NextQuestion(); }
            }
            gait+=Time.unscaledDeltaTime*8*walkAmount;
            if(CurrentPhase==Phase.Choosing || CurrentPhase==Phase.Title)
                rightArm.localRotation=Quaternion.Euler(0,0,145+Mathf.Sin(Time.unscaledTime*2.8f)*12);
            else rightArm.localRotation=Quaternion.Euler(Mathf.Sin(gait)*22*walkAmount,0,-8);
            leftArm.localRotation=Quaternion.Euler(-Mathf.Sin(gait)*22*walkAmount,0,8);
            leftLeg.localRotation=Quaternion.Euler(Mathf.Sin(gait)*24*walkAmount,0,0);
            rightLeg.localRotation=Quaternion.Euler(-Mathf.Sin(gait)*24*walkAmount,0,0);
            friendShadow.position=new Vector3(friend.position.x,.025f,friend.position.z);
            bubble.transform.parent.position=friend.position+Vector3.up*2.12f;
        }

        public void Begin()
        {
            if(CurrentPhase!=Phase.Title) return;
            CurrentPhase=Phase.Choosing; ShowPanel(choicePanel);
            heading.text="친구에게 어떻게 신호를 보낼까요?";
            subtitle.text="바닥과 친구가 지나올 길을 살펴보세요.";
            bubble.text="거기로 갈게!";
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
            UsedPathHint=true;
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
            yield return MoveTo(new Vector3(-.55f,0,2.4f),hurry ? 2.6f : 1.8f);
            heading.text="잠깐, 발밑에 물기가 있어요";
            subtitle.text="서두르던 발걸음이 물기에 닿았어요.";
            causeHighlight.SetActive(true); Play(warningTone);
            smile.SetActive(false); surprisedMouth.SetActive(true); bubble.text="앗!";
            float t=0; walkAmount=.18f;
            Vector3 p=friend.position;
            while(t<1)
            {
                t+=Time.unscaledDeltaTime*.5f;
                friend.position=Vector3.Lerp(p,new Vector3(-.8f,.08f,1.8f),Mathf.Clamp01(t));
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
            if(CurrentPhase!=Phase.UnsafeExplanation) return;
            StopAllCoroutines(); RetryCount++; ResetActor();
            CurrentPhase=Phase.Choosing; ShowPanel(choicePanel);
            heading.text="선택하기 전으로 돌아왔어요";
            subtitle.text="도장은 그대로예요. 주변을 살피고 다시 행동해 보세요.";
            bubble.text="어느 길로 갈까?";
        }

        IEnumerator SafeWalk()
        {
            yield return MoveTo(new Vector3(1.65f,0,4.1f),1.45f);
            yield return MoveTo(new Vector3(1.65f,0,.35f),1.45f);
            yield return MoveTo(new Vector3(.65f,0,-.15f),1.35f);
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
            CurrentPhase=Phase.CauseCheck;
            ShowQuestion();
        }

        void ShowQuestion()
        {
            ShowPanel(questionPanel); answered=false; questionFeedback.text="";
            answerA.interactable=true; answerB.interactable=true; questionNextButton.gameObject.SetActive(false);
            if(CurrentPhase==Phase.CauseCheck)
            {
                heading.text="무엇을 보고 판단했나요?";
                subtitle.text="도장과 별개로, 행동의 이유를 떠올려 보세요.";
                questionTitle.text="친구가 지나갈 길에서 위험했던 것은?";
                answerAText.text="바닥의 물기"; answerBText.text="친구의 인사";
                answerAIcon.symbol=CorridorSymbolGraphic.Symbol.Drop; answerBIcon.symbol=CorridorSymbolGraphic.Symbol.Wave;
            }
            else
            {
                heading.text="다른 장소에서도 생각해 볼까요?";
                subtitle.text="교실 앞에 물이 흘렀고, 마른 통로가 보이지 않아요.";
                questionTitle.text="이럴 때는 어떻게 할까요?";
                answerAText.text="그대로 지나가기"; answerBText.text="멈추고 도움 요청";
                answerAIcon.symbol=CorridorSymbolGraphic.Symbol.Hurry; answerBIcon.symbol=CorridorSymbolGraphic.Symbol.Hand;
            }
            answerAIcon.SetAllDirty(); answerBIcon.SetAllDirty();
        }

        public void Answer(int answer)
        {
            if(answered || (CurrentPhase!=Phase.CauseCheck && CurrentPhase!=Phase.TransferCheck)) return;
            answered=true;
            bool correct;
            if(CurrentPhase==Phase.CauseCheck)
            {
                if(CauseFirstAnswer<0) CauseFirstAnswer=answer;
                correct=answer==0;
                questionFeedback.text="젖은 바닥에서는 미끄러질 수 있어요.\n지나갈 길을 먼저 확인해요.";
            }
            else
            {
                if(TransferFirstAnswer<0) TransferFirstAnswer=answer;
                correct=answer==1;
                questionFeedback.text="안전한 길이 보이지 않으면 멈추고 성인에게 알려요.\n도장은 그대로이며, 도움을 받아도 괜찮아요.";
            }
            Play(correct ? positiveTone : warningTone);
            answerA.interactable=false; answerB.interactable=false; questionNextButton.gameObject.SetActive(true);
        }

        public void NextQuestion()
        {
            if(!answered) return;
            if(CurrentPhase==Phase.CauseCheck) { CurrentPhase=Phase.TransferCheck; ShowQuestion(); }
            else if(CurrentPhase==Phase.TransferCheck)
            {
                answered=false; CurrentPhase=Phase.Completed; ShowPanel(completedPanel);
                heading.text="안전하게 함께 도착했어요";
                subtitle.text="다음에도 주변을 살피고, 필요한 순간 멈춰요.";
                bubble.text="함께 가자!";
                completedText.text="주변 살피기 도장  1 / 3\n친구의 안전한 하루를 도왔어요.";
                Play(stampTone);
            }
        }

        public void RestartStage()
        {
            StopAllCoroutines(); ResetActor();
            CurrentPhase=Phase.Choosing; ShowPanel(choicePanel);
            heading.text="다시 주변을 살펴볼까요?";
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
