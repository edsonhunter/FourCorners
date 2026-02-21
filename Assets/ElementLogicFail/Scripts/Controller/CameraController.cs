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

        private float moveSpeed = 40f;
        private float moveSmoothing = 10f;
        private float zoomSpeed = 2f;
        private float zoomSmoothing = 5f;
        private Vector2 zoomLimits = new Vector2(3f, 15f);
        private Vector2 mapLimitX = Vector2.zero;
        private Vector2 mapLimitZ = Vector2.zero;

        public void Init(Vector3 min, Vector3 max)
        {
            mapLimitX = new Vector2(min.x, max.x);
            mapLimitZ = new Vector2(min.z, max.z);
        }

        private void Awake()
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
            // Wait for map bounds to be injected via Init()
            if (mapLimitX == Vector2.zero && mapLimitZ == Vector2.zero)
                return;

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