using UnityEngine;

namespace DwtD.TestScene
{
    // Reflow on aspect changes only; looking toward a button never moves its hit target.
    public sealed class CorridorWorldPanel : MonoBehaviour
    {
        public Camera view;
        public Vector3 origin;
        public Vector2 anchor;
        public float heightInView, distance;
        float previousAspect=-1;
        public void RefreshLayout()
        {
            if(view==null) return;
            float height=2*distance*Mathf.Tan(CorridorComposition.FieldOfView*.5f*Mathf.Deg2Rad);
            var rotation=Quaternion.Euler(CorridorComposition.InitialPitch,0,0);
            var offset=new Vector3((anchor.x-.5f)*height*view.aspect,(anchor.y-.5f)*height,distance);
            transform.SetPositionAndRotation(origin+rotation*offset,rotation);
            transform.localScale=Vector3.one*(height*heightInView/Mathf.Max(1,((RectTransform)transform).rect.height));
            previousAspect=view.aspect;
        }
        void LateUpdate() { if(view!=null && Mathf.Abs(view.aspect-previousAspect)>.001f) RefreshLayout(); }
    }
}
