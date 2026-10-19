using UnityEngine;
using UnityEngine.XR;

namespace DwtD.TestScene
{
    // Desktop composition aid. Tracked XR controllers remain responsible for XR hands.
    public sealed class CorridorFirstPersonHands : MonoBehaviour
    {
        public GameObject visuals;
        public WetCorridorPrototype prototype;
        void OnEnable()=>Refresh();
        void LateUpdate()=>Refresh();
        void OnDisable() { if(visuals!=null) visuals.SetActive(false); }
        public void Refresh()
        {
            bool show=prototype==null || prototype.CurrentPhase==WetCorridorPrototype.Phase.Title || prototype.CurrentPhase==WetCorridorPrototype.Phase.Observing || prototype.CurrentPhase==WetCorridorPrototype.Phase.SafeMotion || prototype.CurrentPhase==WetCorridorPrototype.Phase.UnsafeMotion;
            if(visuals!=null) visuals.SetActive(!XRSettings.isDeviceActive && show);
        }
    }
}
