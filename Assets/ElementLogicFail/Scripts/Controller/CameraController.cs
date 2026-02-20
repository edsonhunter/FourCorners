using UnityEngine;

namespace ElementLogicFail.Scripts.Controller
{
    public class CameraController : MonoBehaviour
    {
        private PlayerControl _controls;
        private Vector3 _targetPosition;
        private float _targetZoom;

        [SerializeField]
        private Transform cameraTransform;

        public float moveSpeed = 40f;
        public float moveSmoothing = 10f;

        public float zoomSpeed = 2f;
        public float zoomSmoothing = 5f;
        public Vector2 zoomLimits = new Vector2(10f, 50f);

        public Vector2 mapLimitX = new Vector2(-50f, 50f);
        public Vector2 mapLimitZ = new Vector2(-50f, 50f);

        public void Init(Vector3 min, Vector3 max)
        {
            mapLimitX = new Vector2(min.x, max.x);
            mapLimitZ = new Vector2(min.z, max.z);
        }

        private void Awake()
        {
            _controls = new PlayerControl();
            _targetPosition = transform.position;
            _targetZoom = cameraTransform != null ? cameraTransform.localPosition.z : 0f;
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
            Vector2 moveInput = _controls.Gameplay.Movement.ReadValue<Vector2>();
            float scrollInput = _controls.Gameplay.Zoom.ReadValue<float>();

            // Normalize scroll input
            float zoomInput = Mathf.Clamp(scrollInput, -1f, 1f);

            Vector3 moveDir = (Vector3.forward * moveInput.y) + (Vector3.right * moveInput.x);

            float heightFactor = Mathf.InverseLerp(zoomLimits.x, zoomLimits.y, Mathf.Abs(_targetZoom));
            float currentSpeed = moveSpeed * (0.5f + heightFactor);

            Vector3 targetMove = _targetPosition + (moveDir * currentSpeed * Time.deltaTime);

            targetMove.x = Mathf.Clamp(targetMove.x, mapLimitX.x, mapLimitX.y);
            targetMove.z = Mathf.Clamp(targetMove.z, mapLimitZ.x, mapLimitZ.y);

            _targetPosition.x = targetMove.x;
            _targetPosition.z = targetMove.z;

            // Scroll up (positive) = zoom in (move Z towards 0 / positive)
            float zoomStep = zoomInput * zoomSpeed;
            _targetZoom += zoomStep;
            _targetZoom = Mathf.Clamp(_targetZoom, -zoomLimits.y, -zoomLimits.x);
        }

        private void MoveRig()
        {
            // Smooth horizontal positioning
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * moveSmoothing);

            // Smooth local zoom
            if (cameraTransform != null)
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