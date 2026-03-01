using UnityEngine;
using UnityEngine.InputSystem;

namespace ElementLogicFail.Scripts.Controller
{
    public class CameraController : MonoBehaviour
    {
        private PlayerControl _controls;
        private Vector3 _targetPosition;
        private float _targetZoom;
        private Camera _camera;

        [SerializeField]
        private Transform cameraTransform;

        [SerializeField]
        private float mapPadding = 50f;

        private float _moveSpeed = 40f;
        private float _moveSmoothing = 10f;
        private float _zoomSpeed = 2f;
        private float _zoomSmoothing = 5f;
        private Vector2 _zoomLimits = new Vector2(3f, 15f);
        private Vector2 _mapLimitX = Vector2.zero;
        private Vector2 _mapLimitZ = Vector2.zero;
        private Vector3 _baseMin;
        private Vector3 _baseMax;
        private float _groundY;
        private bool _isInitialized;

        // Caching for Optimization
        private float _lastZoomCache = -999f;
        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;
        private float _lastMapPaddingCache = -1f;

        public void Setup()
        {
            _controls = new PlayerControl();
            _targetPosition = transform.position;

            if (cameraTransform != null)
                _camera = cameraTransform.GetComponent<Camera>();

            // Setup initial zoom target based on projection
            if (_camera != null && _camera.orthographic)
            {
                _targetZoom = _camera.orthographicSize;
            }
            else
            {
                _targetZoom = cameraTransform != null ? cameraTransform.localPosition.z : 0f;
            }
        }
        
        public void Init(Vector3 min, Vector3 max)
        {
            _baseMin = min;
            _baseMax = max;
            _groundY = (min.y + max.y) * 0.5f;
            _isInitialized = true;
            
            SetActiveControls(true);
        }

        private void OnEnable()
        {
            SetActiveControls(true);
        }

        private void OnDisable()
        {
            SetActiveControls(false);
        }

        private void Update()
        {
            HandleInput();
            MoveRig();
        }

        [Header("Edge Panning")]
        [SerializeField] private bool enableEdgePanning = true;
        [SerializeField] private float edgePanBorderThickness = 20f;

        [Header("Drag Panning")]
        [SerializeField] private bool enableDragPanning = true;
        [SerializeField] private float dragSensitivity = 1f;

        private void HandleInput()
        {
            if (!_isInitialized)
                return;

            CalculateDynamicBounds();

            Vector2 moveInput = _controls.Gameplay.Movement.ReadValue<Vector2>();
            float scrollInput = _controls.Gameplay.Zoom.ReadValue<float>();

            if (enableEdgePanning && Mouse.current != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                if (mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height)
                {
                    if (mousePos.y >= Screen.height - edgePanBorderThickness) moveInput.y += 1f;
                    if (mousePos.y <= edgePanBorderThickness) moveInput.y -= 1f;
                    if (mousePos.x >= Screen.width - edgePanBorderThickness) moveInput.x += 1f;
                    if (mousePos.x <= edgePanBorderThickness) moveInput.x -= 1f;
                }
            }

            if (enableDragPanning)
            {
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
                    moveInput.x = -dragDelta.x * dragSensitivity * (1f / Screen.width) * 100f;
                    moveInput.y = -dragDelta.y * dragSensitivity * (1f / Screen.height) * 100f;
                }
            }

            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }

            // Normalize
            float zoomInput = Mathf.Clamp(scrollInput, -1f, 1f);

            Vector3 moveDir = (Vector3.forward * moveInput.y) + (Vector3.right * moveInput.x);

            // Determine factor for dynamic panning speed depending on projection type
            float heightFactor;
            if (_camera != null && _camera.orthographic)
            {
                heightFactor = Mathf.InverseLerp(_zoomLimits.x, _zoomLimits.y, _targetZoom);
            }
            else
            {
                heightFactor = Mathf.InverseLerp(_zoomLimits.x, _zoomLimits.y, Mathf.Abs(_targetZoom));
            }

            float currentSpeed = _moveSpeed * (0.5f + heightFactor);

            Vector3 targetMove = _targetPosition + (moveDir * currentSpeed * Time.deltaTime);

            targetMove.x = Mathf.Clamp(targetMove.x, _mapLimitX.x, _mapLimitX.y);
            targetMove.z = Mathf.Clamp(targetMove.z, _mapLimitZ.x, _mapLimitZ.y);

            _targetPosition.x = targetMove.x;
            _targetPosition.z = targetMove.z;

            // Handle zooming
            if (_camera != null && _camera.orthographic)
            {
                // Orthographic size: Smaller = zoomed in. Scroll up (positive) = zoom in (decrease size)
                float zoomStep = -zoomInput * _zoomSpeed;
                _targetZoom += zoomStep;
                _targetZoom = Mathf.Clamp(_targetZoom, _zoomLimits.x, _zoomLimits.y);
            }
            else
            {
                // Perspective local Z: Typically negative. Scroll up (positive) = zoom in (move towards 0)
                float zoomStep = zoomInput * _zoomSpeed;
                _targetZoom += zoomStep;
                _targetZoom = Mathf.Clamp(_targetZoom, -_zoomLimits.y, -_zoomLimits.x);
            }
        }

        private void CalculateDynamicBounds()
        {
            if (_camera == null || cameraTransform == null) return;

            float currentRealZoom = _camera.orthographic ? _camera.orthographicSize : cameraTransform.localPosition.z;

            if (Mathf.Approximately(_lastZoomCache, currentRealZoom) && 
                _lastScreenWidth == Screen.width && 
                _lastScreenHeight == Screen.height && 
                Mathf.Approximately(_lastMapPaddingCache, mapPadding))
            {
                return;
            }

            _lastZoomCache = currentRealZoom;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastMapPaddingCache = mapPadding;

            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, _groundY, 0));

            Ray centerRay = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!groundPlane.Raycast(centerRay, out float dCenter) || dCenter < 0)
            {
                _mapLimitX = new Vector2(_baseMin.x - mapPadding, _baseMax.x + mapPadding);
                _mapLimitZ = new Vector2(_baseMin.z - mapPadding, _baseMax.z + mapPadding);
                return;
            }

            Vector3 centerHit = centerRay.GetPoint(dCenter);
            Vector2 rigToCenter = new Vector2(centerHit.x - transform.position.x, centerHit.z - transform.position.z);

            Vector3[] viewportCorners = {
                new Vector3(0f, 0f, 0f), 
                new Vector3(1f, 0f, 0f), 
                new Vector3(0f, 1f, 0f), 
                new Vector3(1f, 1f, 0f)  
            };

            float minViewX = float.MaxValue, maxViewX = float.MinValue;
            float minViewZ = float.MaxValue, maxViewZ = float.MinValue;

            foreach (var corner in viewportCorners)
            {
                Ray cornerRay = _camera.ViewportPointToRay(corner);
                if (groundPlane.Raycast(cornerRay, out float dCorner) && dCorner > 0)
                {
                    Vector3 hit = cornerRay.GetPoint(dCorner);
                    if (hit.x < minViewX) minViewX = hit.x;
                    if (hit.x > maxViewX) maxViewX = hit.x;
                    if (hit.z < minViewZ) minViewZ = hit.z;
                    if (hit.z > maxViewZ) maxViewZ = hit.z;
                }
            }

            if (minViewX == float.MaxValue)
            {
                _mapLimitX = new Vector2(_baseMin.x - mapPadding, _baseMax.x + mapPadding);
                _mapLimitZ = new Vector2(_baseMin.z - mapPadding, _baseMax.z + mapPadding);
                return;
            }

            float spanLeft = centerHit.x - minViewX;
            float spanRight = maxViewX - centerHit.x;
            float spanDown = centerHit.z - minViewZ;
            float spanUp = maxViewZ - centerHit.z;

            float minCenterX = _baseMin.x - mapPadding + spanLeft;
            float maxCenterX = _baseMax.x + mapPadding - spanRight;
            float minCenterZ = _baseMin.z - mapPadding + spanDown;
            float maxCenterZ = _baseMax.z + mapPadding - spanUp;

            if (minCenterX > maxCenterX) { float mid = (minCenterX + maxCenterX) * 0.5f; minCenterX = mid; maxCenterX = mid; }
            if (minCenterZ > maxCenterZ) { float mid = (minCenterZ + maxCenterZ) * 0.5f; minCenterZ = mid; maxCenterZ = mid; }

            _mapLimitX = new Vector2(minCenterX - rigToCenter.x, maxCenterX - rigToCenter.x);
            _mapLimitZ = new Vector2(minCenterZ - rigToCenter.y, maxCenterZ - rigToCenter.y);
        }

        private void MoveRig()
        {
            // Smooth horizontal positioning
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * _moveSmoothing);

            // Smooth zooming
            if (_camera != null && _camera.orthographic)
            {
                _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _targetZoom, Time.deltaTime * _zoomSmoothing);
            }
            else if (cameraTransform != null)
            {
                Vector3 targetLocalPos = cameraTransform.localPosition;
                targetLocalPos.z = _targetZoom;
                
                cameraTransform.localPosition = Vector3.Lerp(
                    cameraTransform.localPosition, 
                    targetLocalPos, 
                    Time.deltaTime * _zoomSmoothing
                );
            }
        }

        private void SetActiveControls(bool isActive)
        {
            if (!_isInitialized)
            {
                return;
            }
            
            if (isActive)
            {
                _controls.Enable();
            }
            else
            {
                _controls.Disable();
            }
        }
    }
}