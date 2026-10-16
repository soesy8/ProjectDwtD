using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DwtD.TestScene
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class CorridorActionTarget : MonoBehaviour
    {
        public WetCorridorPrototype controller;
        void Awake() => GetComponent<XRSimpleInteractable>().selectEntered.AddListener(_ => Activate());
        public void Activate()
        {
            if(controller != null) controller.ChooseDryPath();
        }
    }
}
