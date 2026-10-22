using UnityEngine;

namespace ProjectWait.M.Elevator
{
    [CreateAssetMenu(menuName = "Wait/Elevator lesson settings")]
    public sealed class ElevatorSettings : ScriptableObject
    {
        [Header("Suggested session: 3–5 minutes; no player time limit")]
        [Min(1)] public float proposalDelay = 18;
        [Min(.1f)] public float friendReplySeconds = 2.8f;
        [Min(.1f)] public float jumpSeconds = .85f;
        [Min(.1f)] public float connectionSeconds = 3;
        [Min(1)] public float firstHintSeconds = 20;
        [Min(1)] public float secondHintSeconds = 40;
        [Min(1)] public float rescueWaitSeconds = 18;
        [Min(1)] public float rescueSubtitleSeconds = 5;
        public string building = "햇살배움센터";
        public string liftId = "교육용 M-01";
        [TextArea] public string proposal = "여기서 한 번 뛰어볼까?";
        [TextArea] public string stoppedAfterWarning = "네 말 들을걸… 이제 어떻게 하지?";
        [TextArea] public string stoppedAfterAgreement = "뛰지 말걸… 이제 어떻게 해?";
        [TextArea] public string preventionFeedback = "친구를 말린 것은 잘한 행동이에요. 친구가 말을 듣지 않은 것은 여러분의 실패가 아니에요. 이제 침착하게 도움을 요청해 봅시다.";
        [TextArea] public string agreementFeedback = "엘리베이터 안에서 뛰는 행동은 위험해요. 이제 침착하게 도움을 요청해 봅시다.";
        [TextArea] public string doorWarning = "문을 억지로 열거나 스스로 나가려 하면 추락하거나 끼일 수 있어요. 문에서 떨어져 도움을 요청하고 안내를 기다려요.";
        [TextArea] public string safetyNotice = "안전하게 함께 타요\n\n안에서 뛰지 않아요.\n문에 기대지 않아요.\n\n멈추면 비상통화로 도움 요청.\n연결이 안 되면 휴대폰으로 119.\n억지로 나가지 않고 안내에 따라 대기.";
    }
}
