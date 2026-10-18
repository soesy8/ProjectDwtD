using TMPro;
using UnityEngine;

namespace DwtD.TestScene
{
    // A separately toggleable view. Story text is supplied by CorridorStoryPresentation.
    [DisallowMultipleComponent]
    public sealed class CorridorThoughtBubble : MonoBehaviour
    {
        [Header("View references")]
        public Camera viewCamera;
        public RectTransform bubbleRoot;
        public TMP_Text thoughtLabel, thought;
        [Header("Position within the player's view")]
        public Vector2 viewportAnchor = new Vector2(.80f,.68f);
        [Min(.3f)] public float viewDistance = 2.2f;
        [Range(.08f,.3f)] public float viewportHeight = .16f;

        bool requestedVisible;
        public string ThoughtText => thought == null ? "" : thought.text;

        public void Show(string label,string message,bool visible)
        {
            requestedVisible=visible;
            if(thoughtLabel!=null) thoughtLabel.text=label;
            if(thought!=null) thought.text=message;
            if(bubbleRoot!=null) bubbleRoot.gameObject.SetActive(visible && isActiveAndEnabled);
            if(isActiveAndEnabled) FollowView();
        }

        void OnEnable()
        {
            Application.onBeforeRender+=FollowView;
            if(bubbleRoot!=null) bubbleRoot.gameObject.SetActive(requestedVisible);
            FollowView();
        }

        void OnDisable()
        {
            Application.onBeforeRender-=FollowView;
            if(bubbleRoot!=null) bubbleRoot.gameObject.SetActive(false);
        }

        void LateUpdate() => FollowView();

        // Run after desktop look and again before XR rendering, so the UI follows head tracking.
        public void FollowView()
        {
            if(viewCamera==null || bubbleRoot==null) return;
            float distance=Mathf.Max(viewDistance,viewCamera.nearClipPlane+.1f);
            var anchor=new Vector3(Mathf.Clamp(viewportAnchor.x,.2f,.8f),Mathf.Clamp(viewportAnchor.y,.2f,.8f),distance);
            transform.SetPositionAndRotation(viewCamera.ViewportToWorldPoint(anchor),viewCamera.transform.rotation);
            float viewHeight=viewCamera.orthographic?viewCamera.orthographicSize*2:2*distance*Mathf.Tan(viewCamera.fieldOfView*.5f*Mathf.Deg2Rad);
            // Preserve the cloud proportions and keep it inside narrow headset/desktop views.
            float heightScale=viewHeight*viewportHeight/Mathf.Max(1,bubbleRoot.rect.height);
            float widthScale=viewHeight*viewCamera.aspect*.34f/Mathf.Max(1,bubbleRoot.rect.width);
            bubbleRoot.localScale=Vector3.one*Mathf.Min(heightScale,widthScale);
        }
    }
}
