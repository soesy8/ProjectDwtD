using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DwtD.TestScene
{
    // Desktop center rays and XR pointers share event-driven depth feedback.
    [DisallowMultipleComponent]
    public sealed class CorridorButtonDepth : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,
        IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler
    {
        CorridorBubbleGraphic surface;
        UnityEngine.UI.Button button;
        readonly List<RectTransform> content=new List<RectTransform>();
        readonly List<Vector2> restingPositions=new List<Vector2>();
        bool hover,pressed,selected,wasInteractable;

        public void Initialize(CorridorBubbleGraphic graphic)
        {
            ResetDepth();surface=graphic;button=GetComponent<UnityEngine.UI.Button>();
            content.Clear();restingPositions.Clear();
            foreach(Transform child in transform)
                if(child is RectTransform rect) { content.Add(rect);restingPositions.Add(rect.anchoredPosition); }
            wasInteractable=button.IsInteractable();Refresh();
        }
        public void OnPointerEnter(PointerEventData e) { hover=true;Refresh(); }
        public void OnPointerExit(PointerEventData e) { hover=false;pressed=false;Refresh(); }
        public void OnPointerDown(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left) { pressed=true;Refresh(); } }
        public void OnPointerUp(PointerEventData e) { pressed=false;Refresh(); }
        public void OnSelect(BaseEventData e) { selected=true;Refresh(); }
        public void OnDeselect(BaseEventData e) { selected=false;pressed=false;Refresh(); }
        void Update()
        {
            if(button!=null && wasInteractable!=button.IsInteractable()) { wasInteractable=button.IsInteractable();Refresh(); }
        }
        void Refresh()
        {
            if(surface==null || button==null) return;
            bool active=button.IsInteractable(),down=active && pressed,focus=active && (hover || selected);
            float shift=down?-5:focus?2:0;
            for(int i=0;i<content.Count;i++) if(content[i]!=null) content[i].anchoredPosition=restingPositions[i]+Vector2.up*shift;
            surface.SetInteraction(focus,down);
        }
        void ResetDepth()
        {
            for(int i=0;i<content.Count;i++) if(content[i]!=null) content[i].anchoredPosition=restingPositions[i];
            if(surface!=null) surface.SetInteraction(false,false);
            hover=pressed=selected=false;
        }
        void OnDisable()=>ResetDepth();
    }
}
