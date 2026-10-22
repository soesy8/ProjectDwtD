using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectWait.M
{
    /// <summary>Small preview controller for the M-only character demonstration scene.</summary>
    public sealed class FriendAnimationPreview : MonoBehaviour
    {
        public Animator actor;
        public Camera previewCamera;
        public TMP_Text status;
        public bool automatic = true;
        public string CurrentMotion { get; private set; } = "Idle";
        float elapsed;

        void Start() { Walk(); }
        void Update()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) { automatic=false; Idle(); }
                if (Keyboard.current.digit2Key.wasPressedThisFrame) { automatic=false; Walk(); }
                if (Keyboard.current.digit3Key.wasPressedThisFrame) { automatic=false; Fall(); }
                if (Keyboard.current.spaceKey.wasPressedThisFrame) ToggleAutomatic();
            }
            if (!automatic) return;
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < 3.0f) SetMotion("Walk");
            else if (elapsed < 5.8f) SetMotion("Fall");
            else if (elapsed < 7.0f) SetMotion("Idle");
            else elapsed=0;
        }
        public void Idle() { automatic=false; SetMotion("Idle"); }
        public void Walk() { SetMotion("Walk"); }
        public void Fall() { SetMotion("Fall",true); }
        public void ManualWalk() { automatic=false; Walk(); }
        public void ManualFall() { automatic=false; Fall(); }
        public void ToggleAutomatic() { automatic=!automatic; elapsed=0; SetMotion(automatic?"Walk":"Idle"); }
        public void SetMotion(string motion, bool restart=false)
        {
            if (actor==null || (!restart && CurrentMotion==motion)) return;
            actor.CrossFadeInFixedTime(motion,.12f,0,0);
            CurrentMotion=motion;
            if(status!=null) status.text=motion=="Walk" ? "걷기" : motion=="Fall" ? "넘어지기" : "기본 자세";
        }
    }
}
