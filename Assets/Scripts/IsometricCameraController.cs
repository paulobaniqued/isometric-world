using UnityEngine;
using UnityEngine.InputSystem;

public class IsometricCameraController : MonoBehaviour
{
    [Header("Target & Positioning")]
    public Transform target;
    public Vector3 targetOffset = Vector3.zero;
    public float defaultDistance = 30.0f;
    
    [Header("Speeds")]
    public float panSpeed = 15.0f;
    public float rotateSpeed = 0.2f;
    public float zoomSpeed = 2.0f;

    [Header("Zoom Limits")]
    public float minOrthographicSize = 2.0f;
    public float maxOrthographicSize = 20.0f;

    [Header("Pitch Limits")]
    public float minPitch = 15.0f;
    public float maxPitch = 60.0f;

    private Vector3 currentTargetPosition;
    private float currentYaw = 45.0f;
    private float currentPitch = 35.264f; // Perfect isometric angle
    private float currentZoom = 8.0f;

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = currentZoom;
        }

        // Initialize target position
        if (target != null)
        {
            currentTargetPosition = target.position + targetOffset;
        }
        else
        {
            currentTargetPosition = Vector3.zero;
        }

        UpdateCameraPosition();
    }

    private void Update()
    {
        HandleKeyboardPan();
        HandleMouseInput();
        UpdateCameraPosition();
    }

    private void HandleKeyboardPan()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        Vector3 moveDir = Vector3.zero;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            moveDir += cam.transform.forward;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            moveDir -= cam.transform.forward;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            moveDir -= cam.transform.right;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            moveDir += cam.transform.right;

        // Flatten movement to XZ plane
        moveDir.y = 0;
        moveDir = moveDir.normalized;

        currentTargetPosition += moveDir * panSpeed * Time.deltaTime;
    }

    private void HandleMouseInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Mouse Pan (Middle Mouse Drag or Right Click drag)
        if (mouse.rightButton.isPressed || mouse.middleButton.isPressed)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            currentYaw += mouseDelta.x * rotateSpeed;
            currentPitch = Mathf.Clamp(currentPitch - mouseDelta.y * rotateSpeed, minPitch, maxPitch);
        }

        // Zoom (Scroll Wheel)
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentZoom = Mathf.Clamp(currentZoom - (scroll * 0.001f * zoomSpeed), minOrthographicSize, maxOrthographicSize);
            if (cam != null)
            {
                cam.orthographicSize = currentZoom;
            }
        }
    }

    private void UpdateCameraPosition()
    {
        // Follow target if defined
        if (target != null)
        {
            currentTargetPosition = Vector3.Lerp(currentTargetPosition, target.position + targetOffset, Time.deltaTime * 5.0f);
        }

        // Calculate rotation based on Yaw and Pitch
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        // Position camera back along the rotation direction
        Vector3 position = currentTargetPosition - (rotation * Vector3.forward * defaultDistance);

        transform.position = position;
        transform.rotation = rotation;
    }

    public void ResetToIsometric()
    {
        currentPitch = 35.264f;
        currentYaw = 45.0f;
        UpdateCameraPosition();
    }
}