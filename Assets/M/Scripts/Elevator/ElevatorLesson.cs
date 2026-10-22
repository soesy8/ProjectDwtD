using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace ProjectWait.M.Elevator
{
    public sealed class ElevatorLesson : MonoBehaviour
    {
        public ElevatorSettings settings;
        public ElevatorView view;
        public Transform friend;
        public Camera playerCamera;
        public TMP_Text floorLabel;
        public Light cabinLight;
        public AudioSource motor, signal;
        public AudioClip motorClip, stopClip;
        public bool simulateNoResponse;
        public ElevatorFlow Flow { get; private set; }
        public string Thought { get; private set; } = "";
        public int Floor { get; private set; } = 2;
        public int RunVersion { get; private set; }
        Vector3 friendHome;
        float floorClock;
        int renderedRevision = -1;
        bool previousMoving;
        GameObject cabin;
        ElevatorClue? activeClue;

        void Awake()
        {
            friendHome = friend.localPosition;
            cabin = transform.Find("Cabin").gameObject;
            Flow = new ElevatorFlow(settings) { NoResponse = simulateNoResponse };
            motor.clip = motorClip; motor.loop = true; motor.volume = .08f;
            signal.clip = stopClip; signal.volume = .18f;
            view.Initialize(this); ResetPresentation();
        }
        void Update() { Advance(Time.unscaledDeltaTime); }
        public void Advance(float delta)
        {
            Flow.NoResponse = simulateNoResponse;
            Flow.Tick(delta);
            bool completed = Flow.Step == ElevatorStep.Complete;
            cabin.SetActive(!completed); friend.gameObject.SetActive(!completed);
            if (Flow.IsMoving)
            {
                floorClock += delta;
                if (floorClock >= 4) { floorClock %= 4; Floor = Floor >= 9 ? 2 : Floor + 1; }
            }
            float height = 0;
            if (Flow.Step == ElevatorStep.Jump)
            {
                float t = (Flow.Elapsed - settings.friendReplySeconds) / settings.jumpSeconds;
                if (t > 0 && t < 1) height = Mathf.Sin(t * Mathf.PI) * .24f;
            }
            friend.localPosition = friendHome + Vector3.up * height;
            if (previousMoving && !Flow.IsMoving && Flow.Step == ElevatorStep.Stopped) signal.Play();
            previousMoving = Flow.IsMoving;
            if (Flow.IsMoving && !motor.isPlaying) motor.Play();
            if (!Flow.IsMoving && motor.isPlaying) motor.Stop();
            cabinLight.intensity = Flow.Step == ElevatorStep.Stopped && Flow.Elapsed < .5f ? 1.15f : 1.6f;
            floorLabel.text = Flow.IsMoving ? "↑  " + Floor.ToString("00") : "—  " + Floor.ToString("00") + "  정지";
            if (renderedRevision != Flow.Revision)
            {
                renderedRevision = Flow.Revision; Thought = "";activeClue=null; ClearHover(); view.Refresh();
            }
        }
        public void Refresh() { view.Refresh(); }
        public void Choose(int index) { Flow.Choose(index); Sync(); }
        public void Offer() { Flow.Offer(); Sync(); }
        public void Intercom() { Flow.RequestIntercom(); Sync(); }
        public void Phone() { Flow.RequestPhone(); Sync(); }
        public void Report() { Flow.ReportLocation(); Sync(); }
        public void Advice() { Flow.FollowAdvice(); Sync(); }
        public void Reassure() { Flow.Reassure(); Sync(); }
        public void UnsafeDoor() { Flow.UnsafeAction("문을 억지로 열려고 함"); Sync(); }
        public void UnsafeExit() { Flow.UnsafeAction("스스로 빠져나가려고 함"); Sync(); }
        public void Retry() { Flow.Retry(); Sync(); }
        void Sync() { Advance(0); }
        public void Restart() { Flow.Reset(); ResetPresentation(); }
        public void DeveloperJump(int stage) { Flow.DeveloperJump(stage); ResetPresentation(); }
        void ResetPresentation()
        {
            view.RestoreThoughtLayout();
            RunVersion++; floorClock = 0; Floor = 2; renderedRevision = -1;
            Thought = ""; signal.Stop(); motor.Stop(); previousMoving = Flow.IsMoving;
            friend.localPosition = friendHome; ClearHover(); Advance(0);
        }
        void ClearHover()
        {
            foreach (var target in GetComponentsInChildren<ElevatorObservation>(true)) target.ResetHover();
        }
        public void Inspect(ElevatorClue clue)
        {
            if (!Flow.CanObserve) return;
            activeClue=clue;
            bool moving = Flow.IsMoving;
            switch (clue)
            {
                case ElevatorClue.Friend: Thought = moving ? "친구가 몸을 들썩이고 있어. 뭘 하려는 걸까?" : Flow.HelpRequested ? "친구가 걱정하고 있어. 함께 차분히 기다려야겠어." : "친구가 놀랐어. 나도 숨을 고르고 주변을 보자."; break;
                case ElevatorClue.Door: Thought = moving ? "지금은 문이 닫혀 있어." : "층 사이에 멈춘 걸까? 문은 닫혀 있어."; break;
                case ElevatorClue.Display: Thought = moving ? "층 숫자가 바뀌고 있어." : "숫자가 멈췄어. 지금 층은 정확히 알 수 없겠어."; break;
                case ElevatorClue.Intercom: Thought = moving ? "벨 표시와 스피커가 있네." : Flow.HelpRequested ? "도움을 요청했어. 안내에 따라 기다리자." : "도움을 요청할 수 있는 비상통화 장치야."; break;
                case ElevatorClue.Notice: Thought = settings.safetyNotice; break;
                case ElevatorClue.Identity: Thought = settings.building + " / " + settings.liftId + "\n위치와 승강기 정보를 알려줄 수 있겠어. 현재 층은 몰라도 돼."; break;
            }
            view.ShowThought(Thought);
        }
        public void ClearThought(ElevatorClue clue)
        {
            if(activeClue!=clue)return;activeClue=null;Thought="";view.ShowThought("");
        }
        public void Activate(ElevatorClue clue)
        {
            Inspect(clue);
            if (clue == ElevatorClue.Intercom && Flow.Step == ElevatorStep.Stopped) Intercom();
            else if (clue == ElevatorClue.Friend && Flow.Step == ElevatorStep.Observe) Offer();
            else if (clue == ElevatorClue.Friend && Flow.Step == ElevatorStep.Waiting) Reassure();
        }
        void OnDisable()
        {
            if (motor) motor.Stop(); if (signal) signal.Stop();
            if (friend) friend.localPosition = friendHome;
            ClearHover();
        }
        public object Snapshot() => new { step = Flow.Step.ToString(), prevention = Flow.Prevention.ToString(), Flow.HelpRequested, Flow.LocationReported, Flow.UsedPhone, Flow.Reassured, Flow.HintLevel, Flow.JumpCount, Floor, motorPlaying = motor.isPlaying, friendHeight = friend.localPosition.y - friendHome.y, history = Flow.History.ToArray() };
    }
}
