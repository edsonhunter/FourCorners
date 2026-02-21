using UnityEngine;

namespace ElementLogicFail.Scripts.Controller
{
    public class CameraController : MonoBehaviour
    {
        private PlayerControl _controls;
        private Vector3 _targetPosition;
        private float _targetZoom;
        private Camera _cam;

        [SerializeField]
        private Transform cameraTransform;

        [SerializeField]
        private float mapPadding = 50f;

        private float moveSpeed = 40f;
        private float moveSmoothing = 10f;
        private float zoomSpeed = 2f;
        private float zoomSmoothing = 5f;
        private Vector2 zoomLimits = new Vector2(3f, 15f);
        private Vector2 mapLimitX = Vector2.zero;
        private Vector2 mapLimitZ = Vector2.zero;
        private Vector3 _baseMin;
        private Vector3 _baseMax;
        private float _groundY;
        private bool _isInitialized = false;

        public void Setup()
        {
            _controls = new PlayerControl();
            _targetPosition = transform.position;

            if (cameraTransform != null)
                _cam = cameraTransform.GetComponent<Camera>();

            // Setup initial zoom target based on projection
            if (_cam != null && _cam.orthographic)
            {
                _targetZoom = _cam.orthographicSize;
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
        }

        private void OnEnable()
        {
            _controls.Enable();
        }

        private void OnDisable()
        {
            _controls.Disable();
        }

        private void Update()
        {
            HandleInput();
            MoveRig();
        }

        private void HandleInput()
        {
            if (!_isInitialized)
                return;

            CalculateDynamicBounds();

            Vector2 moveInput = _controls.Gameplay.Movement.ReadValue<Vector2>();
            float scrollInput = _controls.Gameplay.Zoom.ReadValue<float>();

            // Normalize scroll input
            float zoomInput = Mathf.Clamp(scrollInput, -1f, 1f);

            Vector3 moveDir = (Vector3.forward * moveInput.y) + (Vector3.right * moveInput.x);

            // Determine factor for dynamic panning speed depending on projection type
            float heightFactor;
            if (_cam != null && _cam.orthographic)
            {
                heightFactor = Mathf.InverseLerp(zoomLimits.x, zoomLimits.y, _targetZoom);
            }
            else
            {
                heightFactor = Mathf.InverseLerp(zoomLimits.x, zoomLimits.y, Mathf.Abs(_targetZoom));
            }

            float currentSpeed = moveSpeed * (0.5f + heightFactor);

            Vector3 targetMove = _targetPosition + (moveDir * currentSpeed * Time.deltaTime);

            targetMove.x = Mathf.Clamp(targetMove.x, mapLimitX.x, mapLimitX.y);
            targetMove.z = Mathf.Clamp(targetMove.z, mapLimitZ.x, mapLimitZ.y);

            _targetPosition.x = targetMove.x;
            _targetPosition.z = targetMove.z;

            // Handle zooming
            if (_cam != null && _cam.orthographic)
            {
                // Orthographic size: Smaller = zoomed in. Scroll up (positive) = zoom in (decrease size)
                float zoomStep = -zoomInput * zoomSpeed;
                _targetZoom += zoomStep;
                _targetZoom = Mathf.Clamp(_targetZoom, zoomLimits.x, zoomLimits.y);
            }
            else
            {
                // Perspective local Z: Typically negative. Scroll up (positive) = zoom in (move towards 0)
                float zoomStep = zoomInput * zoomSpeed;
                _targetZoom += zoomStep;
                _targetZoom = Mathf.Clamp(_targetZoom, -zoomLimits.y, -zoomLimits.x);
            }
        }

        private void CalculateDynamicBounds()
        {
            if (_cam == null || cameraTransform == null) return;

            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, _groundY, 0));

            Ray centerRay = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!groundPlane.Raycast(centerRay, out float dCenter) || dCenter < 0)
            {
                mapLimitX = new Vector2(_baseMin.x - mapPadding, _baseMax.x + mapPadding);
                mapLimitZ = new Vector2(_baseMin.z - mapPadding, _baseMax.z + mapPadding);
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
                Ray cornerRay = _cam.ViewportPointToRay(corner);
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
                mapLimitX = new Vector2(_baseMin.x - mapPadding, _baseMax.x + mapPadding);
                mapLimitZ = new Vector2(_baseMin.z - mapPadding, _baseMax.z + mapPadding);
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

            mapLimitX = new Vector2(minCenterX - rigToCenter.x, maxCenterX - rigToCenter.x);
            mapLimitZ = new Vector2(minCenterZ - rigToCenter.y, maxCenterZ - rigToCenter.y);
        }

        private void MoveRig()
        {
            // Smooth horizontal positioning
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * moveSmoothing);

            // Smooth zooming
            if (_cam != null && _cam.orthographic)
            {
                _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, Time.deltaTime * zoomSmoothing);
            }
            else if (cameraTransform != null)
            {
                Vector3 targetLocalPos = cameraTransform.localPosition;
                targetLocalPos.z = _targetZoom;
                
                cameraTransform.localPosition = Vector3.Lerp(
                    cameraTransform.localPosition, 
                    targetLocalPos, 
                    Time.deltaTime * zoomSmoothing
                );
            }
        }
    }
}