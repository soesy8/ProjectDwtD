using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DwtD.TestScene
{
    // Observation has no action side effects: desktop and XR use the same callbacks.
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class CorridorObservationTarget : MonoBehaviour
    {
        public enum Clue { Water, Friend, CleaningTools }
        public Clue clue;
        public CorridorStoryPresentation story;
        public LineRenderer outline;
        bool desktopHover;
        int xrHoverCount;
        XRSimpleInteractable interactable;

        void Awake()
        {
            interactable=GetComponent<XRSimpleInteractable>();
            interactable.hoverEntered.AddListener(_ => { xrHoverCount++; Refresh(); });
            interactable.hoverExited.AddListener(_ => { xrHoverCount=Mathf.Max(0,xrHoverCount-1); Refresh(); });
            interactable.selectEntered.AddListener(_ => Inspect());
        }

        public void SetDesktopHover(bool value) { desktopHover=value; Refresh(); }
        void Refresh()
        {
            bool active=(desktopHover || xrHoverCount>0) && story!=null && story.CanObserve;
            if(outline!=null) outline.enabled=active;
            if(active) Inspect();
        }
        public void Inspect() { if(story!=null && story.CanObserve) story.Inspect(clue); }
        void Update() { if(story!=null && !story.CanObserve && outline!=null) outline.enabled=false; }
        void OnDisable() { desktopHover=false; xrHoverCount=0; if(outline!=null) outline.enabled=false; }
    }
}
