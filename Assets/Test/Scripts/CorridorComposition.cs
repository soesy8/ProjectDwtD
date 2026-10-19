using UnityEngine;

namespace DwtD.TestScene
{
    // Shared by the authored corridor, observation colliders and both walking branches.
    public static class CorridorComposition
    {
        public const float InitialPitch = 12f;
        public const float FieldOfView = 58f;
        public static readonly Vector3 FriendStart = new Vector3(-.15f,0,1.4f);
        public static readonly Vector3 WaterCenter = new Vector3(-.95f,.045f,-1.5f);
        public static readonly Vector2 WaterRadius = new Vector2(1.30f,1.15f);
        public static readonly Vector3 UnsafeContact = new Vector3(-.35f,0,-1.25f);
        public static readonly Vector3 UnsafeEnd = new Vector3(-.65f,.08f,-2f);
        public static readonly Vector3 SafeTurn = new Vector3(1.1f,0,.6f);
        public static readonly Vector3 SafePass = new Vector3(1.1f,0,-1.0f);
        public static readonly Vector3 SafeEnd = new Vector3(.8f,0,-1.0f);

        // Interactive panels stay in the world so a player can aim at them.
        // Reflow relative to the initial stance, independent of the current look direction.
        public static void PlacePanel(RectTransform panel, Camera camera, Vector2 anchor, float heightInView, float distance=2.35f)
        {
            var layout=panel.GetComponent<CorridorWorldPanel>();if(layout==null) layout=panel.gameObject.AddComponent<CorridorWorldPanel>();
            layout.view=camera;layout.origin=camera.transform.position;layout.anchor=anchor;layout.heightInView=heightInView;layout.distance=distance;layout.RefreshLayout();
        }
    }
}
