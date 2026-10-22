using DwtD.TestScene;
using UnityEngine;

namespace ProjectWait.M
{
    /// <summary>Adapts the copied corridor's existing movement to the imported generic rig.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class FriendCorridorAnimation : MonoBehaviour
    {
        public WetCorridorPrototype story;
        public Animator actor;
        public Transform visual;
        public float groundHeight = .05f;
        public string CurrentMotion { get; private set; } = "Idle";
        bool fell;

        void LateUpdate()
        {
            if (story==null || actor==null || visual==null) return;
            // The original prototype rolls its parent as a placeholder for slipping.
            // The new clip owns the fall, so keep the visual's upright parent orientation stable.
            visual.rotation=Quaternion.Euler(0,180,0);
            visual.position=new Vector3(story.friend.position.x,groundHeight,story.friend.position.z);
            var phase=story.CurrentPhase;
            if (phase==WetCorridorPrototype.Phase.UnsafeMotion)
            {
                if(Quaternion.Angle(story.friend.rotation,Quaternion.Euler(0,180,0))>.5f) fell=true;
                SetMotion(fell ? "Fall" : "Walk");
            }
            else if (phase==WetCorridorPrototype.Phase.UnsafeExplanation) SetMotion("Fall");
            else if (phase==WetCorridorPrototype.Phase.SafeMotion) { fell=false; SetMotion("Walk"); }
            else { fell=false; SetMotion("Idle"); }
        }

        void SetMotion(string motion)
        {
            if(CurrentMotion==motion) return;
            actor.CrossFadeInFixedTime(motion,.10f,0,0);
            CurrentMotion=motion;
        }
    }
}
