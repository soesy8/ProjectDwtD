using System;
using System.Collections.Generic;

namespace ProjectWait.M.Elevator
{
    public enum ElevatorStep { Observe, Proposal, Jump, Stopped, Connecting, Call, Unanswered, Waiting, Retry, Rescue, Complete }
    public enum PreventionChoice { None, Warned, Agreed, Developer }

    // One state owner for mouse, keyboard, XR and the verification harness.
    public sealed class ElevatorFlow
    {
        public ElevatorStep Step { get; private set; }
        public PreventionChoice Prevention { get; private set; }
        public readonly List<string> History = new List<string>();
        public bool NoResponse { get; set; }
        public bool HelpRequested { get; private set; }
        public bool LocationReported { get; private set; }
        public bool Reassured { get; private set; }
        public bool UsedPhone { get; private set; }
        public int HintLevel { get; private set; }
        public int Revision { get; private set; }
        public int JumpCount { get; private set; }
        public float Elapsed { get; private set; }
        public bool IsMoving => Step == ElevatorStep.Observe || Step == ElevatorStep.Proposal || Step == ElevatorStep.Jump;
        public bool CanObserve => Step != ElevatorStep.Jump && Step != ElevatorStep.Retry && Step != ElevatorStep.Rescue && Step != ElevatorStep.Complete;
        readonly ElevatorSettings settings;
        bool moreLooking;
        ElevatorStep retryReturn;

        public ElevatorFlow(ElevatorSettings value) { settings = value; Reset(); }
        void Enter(ElevatorStep step) { Step = step; Elapsed = 0; HintLevel = 0; Revision++; }
        public void Reset()
        {
            Prevention = PreventionChoice.None; History.Clear(); HelpRequested = LocationReported = Reassured = UsedPhone = moreLooking = false;
            JumpCount = 0; retryReturn = ElevatorStep.Stopped; Enter(ElevatorStep.Observe);
        }
        public void Offer() { if (Step == ElevatorStep.Observe) Enter(ElevatorStep.Proposal); }
        public void Choose(int choice)
        {
            if (Step != ElevatorStep.Proposal) return;
            if (choice == 2) { moreLooking = true; History.Add("제안: 주변을 더 살펴봄"); Enter(ElevatorStep.Observe); return; }
            if (choice != 0 && choice != 1) return;
            Prevention = choice == 0 ? PreventionChoice.Warned : PreventionChoice.Agreed;
            History.Add(choice == 0 ? "예방 판단: 위험해. 뛰지 말자. (올바른 판단)" : "예방 판단: 조금만 뛰어보자. (위험 행동에 동조)");
            JumpCount++; Enter(ElevatorStep.Jump);
        }
        public void RequestIntercom()
        {
            if (Step != ElevatorStep.Stopped) return;
            History.Add("대처: 비상통화 장치 작동"); Enter(ElevatorStep.Connecting);
        }
        public void RequestPhone()
        {
            if (Step != ElevatorStep.Unanswered) return;
            UsedPhone = true; History.Add("대처: 휴대폰 119 모의 신고"); Enter(ElevatorStep.Call);
        }
        public void ReportLocation()
        {
            if (Step != ElevatorStep.Call || LocationReported) return;
            LocationReported = true; History.Add("대처: 건물명·승강기 식별 정보·2명 갇힘 전달"); Revision++;
        }
        public void FollowAdvice()
        {
            if (Step != ElevatorStep.Call || !LocationReported) return;
            HelpRequested = true; History.Add("대처: 안내를 듣고 안전하게 대기"); Enter(ElevatorStep.Waiting);
        }
        public void Reassure()
        {
            if (Step != ElevatorStep.Waiting || Reassured) return;
            Reassured = true; History.Add("대처: 친구를 안심시킴"); Revision++;
        }
        public void UnsafeAction(string action)
        {
            if (Step != ElevatorStep.Stopped && Step != ElevatorStep.Unanswered && Step != ElevatorStep.Waiting && Step != ElevatorStep.Call) return;
            retryReturn = Step; History.Add("첫 시도 기록: " + action + " → Wait! 중단 후 재도전"); Enter(ElevatorStep.Retry);
        }
        public void Retry() { if (Step == ElevatorStep.Retry) Enter(retryReturn); }
        public void Tick(float seconds)
        {
            if (seconds < 0) return;
            Elapsed += seconds;
            switch (Step)
            {
                case ElevatorStep.Observe: if (!moreLooking && Elapsed >= settings.proposalDelay) Offer(); break;
                case ElevatorStep.Jump: if (Elapsed >= settings.friendReplySeconds + settings.jumpSeconds) Enter(ElevatorStep.Stopped); break;
                case ElevatorStep.Connecting:
                    if (Elapsed >= settings.connectionSeconds)
                    {
                        History.Add(NoResponse ? "비상통화: 응답 없음" : "비상통화: 관리실 연결");
                        Enter(NoResponse ? ElevatorStep.Unanswered : ElevatorStep.Call);
                    }
                    break;
                case ElevatorStep.Waiting:
                    if (HelpRequested && LocationReported && Elapsed >= settings.rescueWaitSeconds) Enter(ElevatorStep.Rescue);
                    break;
                case ElevatorStep.Rescue: if (Elapsed >= settings.rescueSubtitleSeconds) Enter(ElevatorStep.Complete); break;
            }
            if (Step == ElevatorStep.Stopped || Step == ElevatorStep.Unanswered)
            {
                int hint = Elapsed >= settings.secondHintSeconds ? 2 : Elapsed >= settings.firstHintSeconds ? 1 : 0;
                if (hint != HintLevel) { HintLevel = hint; Revision++; }
            }
        }
        public void DeveloperJump(int stage)
        {
            Reset();
            if (stage <= 0) return;
            Prevention = PreventionChoice.Developer; History.Add("개발용 단계 이동: 앞 단계 생략");
            if (stage == 1) { Enter(ElevatorStep.Proposal); return; }
            if (stage == 2) { JumpCount = 1; Enter(ElevatorStep.Jump); return; }
            if (stage == 3) { Enter(ElevatorStep.Stopped); return; }
            if (stage == 4) { Enter(ElevatorStep.Call); return; }
            LocationReported = HelpRequested = true; Enter(stage == 5 ? ElevatorStep.Waiting : ElevatorStep.Complete);
        }
    }
}
