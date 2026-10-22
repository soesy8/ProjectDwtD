using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ProjectWait.M.Elevator
{
    public enum ElevatorClue { Friend, Door, Display, Intercom, Notice, Identity }

    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class ElevatorObservation : MonoBehaviour
    {
        public ElevatorLesson lesson;
        public ElevatorClue clue;
        public LineRenderer outline;
        bool hovered;
        int runVersion;
        XRSimpleInteractable interaction;
        void Awake()
        {
            interaction = GetComponent<XRSimpleInteractable>();
            interaction.hoverEntered.AddListener(_ => Hover(true));
            interaction.hoverExited.AddListener(_ => Hover(false));
            interaction.selectEntered.AddListener(_ => Activate());
        }
        public void Hover(bool value)
        {
            if (value && (!hovered || runVersion != lesson.RunVersion)) { lesson.Inspect(clue); runVersion = lesson.RunVersion; }
            hovered = value;
            if(!value)lesson.ClearThought(clue);
        }
        public void Activate() { lesson.Activate(clue); }
        public void ResetHover() { hovered = false; if (outline) outline.enabled = false; }
        void LateUpdate()
        {
            if (outline == null || lesson.Flow == null) return;
            outline.enabled = lesson.Flow.CanObserve;
            outline.widthMultiplier = hovered ? .012f : .006f;
            // Same teal affordance on all six targets; no correct-answer color coding.
        }
        void OnDisable() { ResetHover(); }
    }
}
