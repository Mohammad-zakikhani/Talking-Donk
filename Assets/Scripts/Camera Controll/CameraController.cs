using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target to look at")]
    public Transform target;

    [Header("Rotation Settings")]
    public float rotationSpeed = 0.2f;
    public float maxYaw = 10f;
    public float maxPitch = 5f;

    [Header("Smoothing")]
    public float rotationSmoothTime = 0.2f;

    private Vector2 inputRotation = Vector2.zero;
    private Vector2 currentRotation = Vector2.zero;
    private Vector2 rotationVelocity = Vector2.zero;

    private Vector2 lastInputPos;
    private bool dragging = false;

    void Update()
    {
        if (!dragging && !Input.GetMouseButton(0) && Input.touchCount == 0)
            return;

        HandleInput();
        ApplyRotation();
    }

    void HandleInput()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                dragging = true;
                lastInputPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved && dragging)
            {
                Vector2 delta = touch.deltaPosition;
                inputRotation.x += delta.x * rotationSpeed;
                inputRotation.y -= delta.y * rotationSpeed;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                dragging = false;
            }
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            dragging = true;
            lastInputPos = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0) && dragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastInputPos;
            inputRotation.x += delta.x * rotationSpeed;
            inputRotation.y -= delta.y * rotationSpeed;
            lastInputPos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }
#endif

        // Clamp input
        inputRotation.x = Mathf.Clamp(inputRotation.x, -maxYaw, maxYaw);
        inputRotation.y = Mathf.Clamp(inputRotation.y, -maxPitch, maxPitch);
    }

    void ApplyRotation()
    {
        if (!target) return;

        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion baseRotation = Quaternion.LookRotation(direction);

        // Smooth input
        currentRotation = Vector2.SmoothDamp(currentRotation, inputRotation, ref rotationVelocity, rotationSmoothTime);

        Quaternion offsetRotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0f);
        Quaternion targetRotation = baseRotation * offsetRotation;

        // Smooth camera rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * (1f / rotationSmoothTime));
    }
}
