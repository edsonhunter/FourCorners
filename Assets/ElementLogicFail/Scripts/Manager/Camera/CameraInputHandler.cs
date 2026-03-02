using ElementLogicFail.Scripts.Controller;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ElementLogicFail.Scripts.Manager.Camera
{
    public class CameraInputHandler : ICameraInputHandler
    {
        private PlayerControl _controls;

        private bool _enableEdgePanning = true;
        private float _edgePanBorderThickness = 20f;

        private bool _enableDragPanning = true;
        private float _dragSensitivity = 1f;

        public CameraInputHandler()
        {
            _controls = new PlayerControl();
        }

        public void EnableControls()
        {
            _controls.Enable();
        }

        public void DisableControls()
        {
            _controls.Disable();
        }

        public Vector2 GetMoveInput()
        {
            Vector2 moveInput = _controls.Gameplay.Movement.ReadValue<Vector2>();

#if (!UNITY_ANDROID && !UNITY_IOS)
            moveInput = HandleEdgePanning(moveInput);
#endif

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
            moveInput = HandleDragPanning(moveInput);
#endif

            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }

            return moveInput;
        }

        public float GetZoomInput()
        {
            float scrollInput = _controls.Gameplay.Zoom.ReadValue<float>();
            return Mathf.Clamp(scrollInput, -1f, 1f);
        }

#if (!UNITY_ANDROID && !UNITY_IOS)
        private Vector2 HandleEdgePanning(Vector2 currentInput)
        {
            if (!_enableEdgePanning || Mouse.current == null) return currentInput;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height)
            {
                if (mousePos.y >= Screen.height - _edgePanBorderThickness) currentInput.y += 1f;
                if (mousePos.y <= _edgePanBorderThickness) currentInput.y -= 1f;
                if (mousePos.x >= Screen.width - _edgePanBorderThickness) currentInput.x += 1f;
                if (mousePos.x <= _edgePanBorderThickness) currentInput.x -= 1f;
            }

            return currentInput;
        }
#endif

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
        private Vector2 HandleDragPanning(Vector2 currentInput)
        {
            if (!_enableDragPanning) return currentInput;

            Vector2 dragDelta = Vector2.zero;
            bool isDragging = false;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                dragDelta = Touchscreen.current.primaryTouch.delta.ReadValue();
                isDragging = true;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                dragDelta = Mouse.current.delta.ReadValue();
                isDragging = true;
            }

            if (isDragging)
            {
                currentInput.x = -dragDelta.x * _dragSensitivity * (1f / Screen.width) * 100f;
                currentInput.y = -dragDelta.y * _dragSensitivity * (1f / Screen.height) * 100f;
            }

            return currentInput;
        }
#endif
    }
}
