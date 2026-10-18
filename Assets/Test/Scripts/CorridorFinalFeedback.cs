using TMPro;
using UnityEngine;

namespace DwtD.TestScene
{
    // Optional post-stamp learning activity. The scene keeps this entire object inactive.
    [DisallowMultipleComponent]
    public sealed class CorridorFinalFeedback : MonoBehaviour
    {
        public WetCorridorPrototype controller;
        public GameObject questionPanel, completedPanel;
        public TMP_Text questionTitle, questionFeedback, completedText, answerAText, answerBText;
        public UnityEngine.UI.Button answerA, answerB, questionNextButton, restartButton;
        public CorridorSymbolGraphic answerAIcon, answerBIcon;

        public int CauseFirstAnswer { get; private set; } = -1;
        public int TransferFirstAnswer { get; private set; } = -1;
        bool initialized, answered;

        public void Initialize(WetCorridorPrototype owner)
        {
            controller=owner;
            if(initialized) return;
            initialized=true;
            answerA.onClick.AddListener(AnswerA);
            answerB.onClick.AddListener(AnswerB);
            questionNextButton.onClick.AddListener(NextQuestion);
            restartButton.onClick.AddListener(controller.RestartStage);
            HidePanels();
        }

        public bool TryBegin()
        {
            if(!isActiveAndEnabled || controller==null) return false;
            ShowQuestion(WetCorridorPrototype.Phase.CauseCheck);
            return true;
        }

        void ShowQuestion(WetCorridorPrototype.Phase phase)
        {
            controller.ShowFeedbackPanel(phase,questionPanel);
            answered=false; questionFeedback.text="";
            answerA.interactable=true; answerB.interactable=true; questionNextButton.gameObject.SetActive(false);
            if(phase==WetCorridorPrototype.Phase.CauseCheck)
            {
                controller.heading.text="무엇을 보고 판단했나요?";
                controller.subtitle.text="도장과 별개로, 행동의 이유를 떠올려 보세요.";
                questionTitle.text="친구가 지나갈 길에서 위험했던 것은?";
                answerAText.text="바닥의 물기"; answerBText.text="친구의 인사";
                answerAIcon.symbol=CorridorSymbolGraphic.Symbol.Drop; answerBIcon.symbol=CorridorSymbolGraphic.Symbol.Wave;
            }
            else
            {
                controller.heading.text="다른 장소에서도 생각해 볼까요?";
                controller.subtitle.text="교실 앞에 물이 흘렀고, 마른 통로가 보이지 않아요.";
                questionTitle.text="이럴 때는 어떻게 할까요?";
                answerAText.text="그대로 지나가기"; answerBText.text="멈추고 도움 요청";
                answerAIcon.symbol=CorridorSymbolGraphic.Symbol.Hurry; answerBIcon.symbol=CorridorSymbolGraphic.Symbol.Hand;
            }
            answerAIcon.SetAllDirty(); answerBIcon.SetAllDirty();
        }

        void AnswerA() => Answer(0);
        void AnswerB() => Answer(1);
        public void Answer(int answer)
        {
            if(!isActiveAndEnabled || controller==null || answered || (answer!=0 && answer!=1)) return;
            var phase=controller.CurrentPhase;
            if(phase!=WetCorridorPrototype.Phase.CauseCheck && phase!=WetCorridorPrototype.Phase.TransferCheck) return;
            answered=true;
            bool correct;
            if(phase==WetCorridorPrototype.Phase.CauseCheck)
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
            controller.PlayAnswerTone(correct);
            answerA.interactable=false; answerB.interactable=false; questionNextButton.gameObject.SetActive(true);
        }

        public void NextQuestion()
        {
            if(!isActiveAndEnabled || controller==null || !answered) return;
            if(controller.CurrentPhase==WetCorridorPrototype.Phase.CauseCheck)
                ShowQuestion(WetCorridorPrototype.Phase.TransferCheck);
            else if(controller.CurrentPhase==WetCorridorPrototype.Phase.TransferCheck)
            {
                answered=false;
                controller.ShowFeedbackPanel(WetCorridorPrototype.Phase.Completed,completedPanel);
                controller.heading.text="안전하게 함께 도착했어요";
                controller.subtitle.text="다음에도 주변을 살피고, 필요한 순간 멈춰요.";
                controller.bubble.text="함께 가자!";
                completedText.text="주변 살피기 도장  1 / 3\n친구의 안전한 하루를 도왔어요.";
                controller.PlayCompletionTone();
            }
        }

        public void HidePanels()
        {
            if(questionPanel!=null) questionPanel.SetActive(false);
            if(completedPanel!=null) completedPanel.SetActive(false);
        }

        void OnDisable()
        {
            HidePanels();
            if(!Application.isPlaying || controller==null) return;
            var phase=controller.CurrentPhase;
            if(phase==WetCorridorPrototype.Phase.CauseCheck || phase==WetCorridorPrototype.Phase.TransferCheck || phase==WetCorridorPrototype.Phase.Completed)
                controller.FinishWithoutFeedback();
        }

        void OnDestroy()
        {
            if(!initialized) return;
            if(answerA!=null) answerA.onClick.RemoveListener(AnswerA);
            if(answerB!=null) answerB.onClick.RemoveListener(AnswerB);
            if(questionNextButton!=null) questionNextButton.onClick.RemoveListener(NextQuestion);
            if(restartButton!=null && controller!=null) restartButton.onClick.RemoveListener(controller.RestartStage);
        }
    }
}
