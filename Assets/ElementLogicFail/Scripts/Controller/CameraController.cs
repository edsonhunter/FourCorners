using UnityEngine;
using UnityEngine.InputSystem;

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
    
    public float zoomSpeed = 5f;
    public float zoomSmoothing = 5f;
    public Vector2 zoomLimits = new Vector2(10f, 50f);

    public Vector2 mapLimitX = new Vector2(-50f, 50f);
    public Vector2 mapLimitZ = new Vector2(-50f, 50f);

    private void Awake()
    {
        _controls = new PlayerControl();
        _targetPosition = transform.position;
        _targetZoom = transform.position.y;
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
        float zoomInput = _controls.Gameplay.Zoom.ReadValue<float>();

        if (zoomInput > 0) zoomInput = 1;
        else if (zoomInput < 0) zoomInput = -1;

        Vector3 moveDir = (Vector3.forward * moveInput.y) + (Vector3.right * moveInput.x);
        
        float heightFactor = transform.position.y / zoomLimits.y; 
        float currentSpeed = moveSpeed * (0.5f + heightFactor); 

        Vector3 targetMove = _targetPosition + (moveDir * currentSpeed * Time.deltaTime);

        targetMove.x = Mathf.Clamp(targetMove.x, mapLimitX.x, mapLimitX.y);
        targetMove.z = Mathf.Clamp(targetMove.z, mapLimitZ.x, mapLimitZ.y);
        
        _targetPosition = new Vector3(targetMove.x, _targetPosition.y, targetMove.z);

        float zoomStep = -zoomInput * zoomSpeed;
        _targetZoom += zoomStep;
        _targetZoom = Mathf.Clamp(_targetZoom, zoomLimits.x, zoomLimits.y);
        _targetPosition.y = _targetZoom;
    }

    private void MoveRig()
    {
        transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * moveSmoothing);
    }
}
}