using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace DwtD.TestScene
{
    // Kept on a separate empty object so the desktop harness can be disabled for VR.
    public sealed class DesktopTestControls : MonoBehaviour
    {
        [Header("Test camera")]
        public Camera targetCamera;
        public Transform xrOrigin;
        public LineRenderer centerLaser;
        public Transform laserDot;
        [Header("Mouse look")]
        [Range(.02f,.4f)] public float sensitivity=.10f;
        [Range(10,85)] public float pitchLimit=65;
        public float laserRange=20;

        public bool IsDesktopMode { get; private set; }
        public bool MouseLookActive { get; private set; } = true;
        public string HoverTargetName => hovered == null ? "" : hovered.name;
        public int ClickCount { get; private set; }
        public Vector2 LookAngles => new Vector2(yaw,pitch);

        float yaw,pitch;
        bool initialized;
        bool skipLookDelta;
        bool wasFocused;
        bool selectRequested, escapeRequested;
        InputAction clickAction, escapeAction;
        TrackedPoseDriver poseDriver;
        XRUIInputModule uiModule;
        GameObject hovered;
        PointerEventData pointer;
        Renderer dotRenderer;
        MaterialPropertyBlock dotColor;
        UnityEngine.UI.Button[] buttons;
        readonly Color idleColor=new Color(.91f,.95f,.85f);
        readonly Color targetColor=new Color(.25f,.90f,.72f);

        void Start()
        {
            poseDriver=targetCamera.GetComponent<TrackedPoseDriver>();
            uiModule=FindFirstObjectByType<XRUIInputModule>();
            dotRenderer=laserDot.GetComponent<Renderer>();
            dotColor=new MaterialPropertyBlock();
            buttons=FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            clickAction=new InputAction("DesktopCenterClick",InputActionType.Button,"<Mouse>/leftButton");
            escapeAction=new InputAction("DesktopMouseToggle",InputActionType.Button,"<Keyboard>/escape");
            clickAction.performed+=_=>selectRequested=true;
            escapeAction.performed+=_=>escapeRequested=true;
            clickAction.Enable(); escapeAction.Enable();
            RefreshMode();
            wasFocused=Application.isFocused;
            if(!wasFocused) SetMouseLook(false);
        }

        void RefreshMode()
        {
            bool desktop=!XRSettings.isDeviceActive;
            if(initialized && desktop==IsDesktopMode) return;
            initialized=true; IsDesktopMode=desktop;
            if(poseDriver!=null) poseDriver.enabled=!desktop;
            if(uiModule!=null) uiModule.enableMouseInput=!desktop;
            centerLaser.enabled=desktop; laserDot.gameObject.SetActive(desktop);
            if(desktop)
            {
                targetCamera.transform.position=xrOrigin.position+Vector3.up*1.62f;
                targetCamera.transform.rotation=Quaternion.identity;
                targetCamera.fieldOfView=65;
                yaw=0; pitch=0; SetMouseLook(true);
            }
            else SetMouseLook(false);
        }

        void Update()
        {
            RefreshMode();
            if(!IsDesktopMode) return;
            bool focused=Application.isFocused;
            if(focused!=wasFocused) { wasFocused=focused; SetMouseLook(focused); }
            if(escapeRequested) { escapeRequested=false; SetMouseLook(!MouseLookActive); }
            var mouse=Mouse.current;
            if(mouse==null) return;
            bool click=selectRequested; selectRequested=false;
            if(focused && click && !MouseLookActive) { SetMouseLook(true); click=false; }
            if(MouseLookActive)
            {
                Vector2 delta=skipLookDelta?Vector2.zero:mouse.delta.ReadValue();
                skipLookDelta=false;
                yaw+=delta.x*sensitivity;
                pitch=Mathf.Clamp(pitch-delta.y*sensitivity,-pitchLimit,pitchLimit);
                targetCamera.transform.rotation=Quaternion.Euler(pitch,yaw,0);
            }
            UpdateCenterRay();
            if(MouseLookActive && click) SelectCenterTarget();
        }

        public void SetMouseLook(bool active)
        {
            MouseLookActive=active && IsDesktopMode;
            Cursor.lockState=MouseLookActive?CursorLockMode.Locked:CursorLockMode.None;
            Cursor.visible=!MouseLookActive;
            skipLookDelta=true;
            // Suppress the one-time cursor warp delta on the transition frame.
            if(MouseLookActive && Mouse.current!=null) Mouse.current.WarpCursorPosition(new Vector2(Screen.width*.5f,Screen.height*.5f));
        }

        void UpdateCenterRay()
        {
            Ray ray=new Ray(targetCamera.transform.position,targetCamera.transform.forward);
            float distance=laserRange;
            GameObject target=null;
            if(Physics.Raycast(ray,out RaycastHit physicsHit,laserRange))
            {
                var floorTarget=physicsHit.collider.GetComponentInParent<CorridorActionTarget>();
                if(floorTarget!=null) { target=floorTarget.gameObject; distance=physicsHit.distance; }
            }
            var eventSystem=EventSystem.current;
            if(eventSystem!=null)
            {
                if(pointer==null)
                    pointer=new PointerEventData(eventSystem);
                pointer.Reset(); pointer.pointerId=-99; pointer.button=PointerEventData.InputButton.Left;
                pointer.position=new Vector2(Screen.width*.5f,Screen.height*.5f);
                // This stationary prototype has flat world-space buttons without masks.
                // Intersect their rectangles with the same center ray used by the laser.
                foreach(var button in buttons)
                {
                    if(button==null || !button.IsActive() || !button.IsInteractable()) continue;
                    var rect=(RectTransform)button.transform;
                    var plane=new Plane(rect.forward,rect.position);
                    if(!plane.Raycast(ray,out float hitDistance) || hitDistance>distance) continue;
                    Vector3 worldHit=ray.GetPoint(hitDistance);
                    if(!rect.rect.Contains((Vector2)rect.InverseTransformPoint(worldHit))) continue;
                    target=button.gameObject; distance=hitDistance;
                    pointer.pointerCurrentRaycast=new RaycastResult { gameObject=target, distance=distance, worldPosition=worldHit, worldNormal=-rect.forward };
                }
            }
            if(target!=hovered)
            {
                if(hovered!=null && pointer!=null) ExecuteEvents.Execute(hovered,pointer,ExecuteEvents.pointerExitHandler);
                hovered=target;
                if(hovered!=null && pointer!=null) ExecuteEvents.Execute(hovered,pointer,ExecuteEvents.pointerEnterHandler);
            }
            Vector3 end=ray.GetPoint(Mathf.Clamp(distance,.4f,laserRange));
            centerLaser.SetPosition(0,targetCamera.transform.TransformPoint(new Vector3(.065f,-.065f,.18f)));
            centerLaser.SetPosition(1,end);
            Color color=hovered==null?idleColor:targetColor;
            centerLaser.startColor=new Color(color.r,color.g,color.b,.42f); centerLaser.endColor=color;
            laserDot.position=ray.GetPoint(Mathf.Clamp(distance-.015f,.3f,laserRange));
            laserDot.localScale=Vector3.one*Mathf.Clamp(distance*.005f,.012f,.065f);
            dotColor.SetColor("_BaseColor",color); dotColor.SetColor("_Color",color); dotRenderer.SetPropertyBlock(dotColor);
        }

        public void SelectCenterTarget()
        {
            if(!IsDesktopMode || hovered==null) return;
            ClickCount++;
            var floorTarget=hovered.GetComponent<CorridorActionTarget>();
            if(floorTarget!=null) { floorTarget.Activate(); return; }
            if(pointer==null) return;
            ExecuteEvents.Execute(hovered,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(hovered,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(hovered,pointer,ExecuteEvents.pointerClickHandler);
        }

        void OnDisable()
        {
            clickAction?.Disable(); escapeAction?.Disable();
            if(IsDesktopMode) { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
            if(centerLaser!=null) centerLaser.enabled=false;
            if(laserDot!=null) laserDot.gameObject.SetActive(false);
        }

        void OnEnable() { clickAction?.Enable(); escapeAction?.Enable(); }
        void OnDestroy() { clickAction?.Dispose(); escapeAction?.Dispose(); }
    }
}
