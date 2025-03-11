using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;  // Import AR Foundation
using UnityEngine.XR.ARSubsystems;
using System.Collections;

public class CursorController : MonoBehaviour
{
    public Camera arCamera; // Assign the ARCore camera in the inspector
    public float depth = 1.0f; // Distance in front of the camera
    public float lerpSpeed = 5.0f; // Speed at which cursor moves
    private InputAction touchPositionAction;
    private InputAction tapAction;

    public float distance = 0.0f;

    private Vector3 lastCursorLocalPosition; // Stores position relative to the camera
    private bool isCursorOnSurface = false; // Track if cursor is on a surface
    private Vector3 targetPosition; // The target position for the cursor
    private Quaternion targetRotation; // The target rotation for the cursor
    private Coroutine returnCoroutine; // Reference to the coroutine for returning cursor

    private ARPointCloudManager pointCloudManager;

    public bool targetingMode = false; // New variable for targeting mode
    private float lastTapTime = 0.0f; // Track last tap time
    private float doubleTapThreshold = 0.3f; // Time threshold for double tap
    private Coroutine tapTimerCoroutine = null;

    // Store closest point and camera rotation
    public Vector3 storedClosestPoint;
    public Quaternion storedCameraRotation;
    private bool tapInProgress = false; // Prevents rapid unintended taps

    void Awake()
    {
        // Initialize input actions
        touchPositionAction = new InputAction("TouchPosition", binding: "<Pointer>/position");
        tapAction = new InputAction("Tap", binding: "<Pointer>/press");

        // Enable actions
        touchPositionAction.Enable();
        tapAction.Enable();

        // Get ARPointCloudManager to access point cloud data
        pointCloudManager = FindFirstObjectByType<ARPointCloudManager>();
    }

    void Start()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main; // Auto-assign if not set
        }

        // Default cursor position at center of camera view
        lastCursorLocalPosition = new Vector3(0, 0, depth);
        targetPosition = arCamera.transform.TransformPoint(lastCursorLocalPosition);
        targetRotation = arCamera.transform.rotation;
    }

    void Update()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        // Get the 3D position of the cursor in world space
        Vector3 cursorWorldPosition = transform.position;

        // Convert the 3D world position to a 2D screen position
        Vector3 cursorScreenPosition = arCamera.WorldToScreenPoint(cursorWorldPosition);
        Vector2 cursor2D = new Vector2(cursorScreenPosition.x, cursorScreenPosition.y);
        Vector3 closestPoint = FindClosestPointInPointCloud(cursor2D);
        distance = Vector3.Distance(arCamera.transform.position, closestPoint);

        if (tapAction.WasPerformedThisFrame())
        {
            HandleTapAction();
        }

        if (!isCursorOnSurface)
        {
            // Make the cursor face away from the camera when not on a surface
            targetPosition = arCamera.transform.TransformPoint(lastCursorLocalPosition);
            targetRotation = Quaternion.LookRotation(transform.position - arCamera.transform.position); // Face away from the camera
        }

        // Lerp cursor position and rotation
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
    }

    void HandleTapAction()
    {
        if (tapInProgress) return; // Ignore repeated quick taps
        tapInProgress = true;
        StartCoroutine(ResetTapCooldown());

        if (Time.time - lastTapTime < doubleTapThreshold)
        {
            // Double tap detected, toggle targeting mode
            targetingMode = !targetingMode;

            if (targetingMode)
            {
                StoreTargetingData();
            }

            // Cancel the pending single-tap action
            if (tapTimerCoroutine != null)
            {
                StopCoroutine(tapTimerCoroutine);
                tapTimerCoroutine = null;
            }
        }
        else
        {
            // Start a timer to delay MoveCursorToTouch
            if (tapTimerCoroutine != null)
            {
                StopCoroutine(tapTimerCoroutine);
            }
            tapTimerCoroutine = StartCoroutine(TapTimeout());
        }

        lastTapTime = Time.time;
    }

    // Debounce mechanism to avoid rapid unintended taps
    private IEnumerator ResetTapCooldown()
    {
        yield return new WaitForSeconds(0.05f); // Adjust debounce delay as needed
        tapInProgress = false;
    }

    // Timeout for handling single taps
    private IEnumerator TapTimeout()
    {
        yield return new WaitForSeconds(doubleTapThreshold);
        MoveCursorToTouch();
        tapTimerCoroutine = null;
    }

    private void StoreTargetingData()
    {
        // Get the world position of the cursor
        Vector3 cursorWorldPosition = transform.position;

        // Convert the 3D world position to a 2D screen position
        Vector3 cursorScreenPosition = arCamera.WorldToScreenPoint(cursorWorldPosition);
        Vector2 cursor2D = new Vector2(cursorScreenPosition.x, cursorScreenPosition.y);

        // Find the closest point from the cursor's screen position
        storedClosestPoint = FindClosestPointInPointCloud(cursor2D);

        // Store the camera rotation needed to have a central line of sight to the stored closest point
        Vector3 directionToClosestPoint = storedClosestPoint - arCamera.transform.position;
        storedCameraRotation = Quaternion.LookRotation(directionToClosestPoint);
    }

    void MoveCursorToTouch()
    {
            Vector2 screenPosition = touchPositionAction.ReadValue<Vector2>();
            Ray ray = arCamera.ScreenPointToRay(screenPosition);

            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine); // Stop previous coroutine if any
            }

            // Ensure the point cloud manager is not null
            if (pointCloudManager != null && pointCloudManager.enabled && pointCloudManager.trackables.count > 0 && !targetingMode)
            {
                // Find the closest point to the screen touch
                Vector3 closestPoint = FindClosestPointInPointCloud(screenPosition);

                if (closestPoint != Vector3.zero)
                {
                    // Update cursor position based on point cloud hit
                    lastCursorLocalPosition = arCamera.transform.InverseTransformPoint(closestPoint);
                    targetPosition = arCamera.transform.TransformPoint(lastCursorLocalPosition);
                    targetRotation = arCamera.transform.rotation;
                    isCursorOnSurface = true;

                    returnCoroutine = StartCoroutine(ReturnCursorToCamera());
                }
                else
                {
                    // No point cloud available, maintain the cursor behavior at a fixed depth
                    Vector3 worldPosition = ray.origin + ray.direction * depth;
                    lastCursorLocalPosition = arCamera.transform.InverseTransformPoint(worldPosition);
                    isCursorOnSurface = false;
                    targetPosition = arCamera.transform.TransformPoint(lastCursorLocalPosition);
                    targetRotation = arCamera.transform.rotation;
                }
            }
            else
            {
                // No point cloud available, maintain the cursor behavior at a fixed depth
                Vector3 worldPosition = ray.origin + ray.direction * depth;
                lastCursorLocalPosition = arCamera.transform.InverseTransformPoint(worldPosition);
                isCursorOnSurface = false;
                targetPosition = arCamera.transform.TransformPoint(lastCursorLocalPosition);
                targetRotation = arCamera.transform.rotation;
            }
    }

    // Finds the closest point in the point cloud to the touch position
    private Vector3 FindClosestPointInPointCloud(Vector2 touchPosition)
    {
        // Convert screen touch position to world coordinates using AR camera
        Vector3 screenPoint = new Vector3(touchPosition.x, touchPosition.y, 0f);
        Ray ray = arCamera.ScreenPointToRay(screenPoint); // Create a ray from the camera

        float closestDistance = float.MaxValue;
        Vector3 closestPoint = Vector3.zero;

        // Ensure that there are trackables and points in the point cloud before accessing
        if (pointCloudManager != null && pointCloudManager.trackables.count > 0)
        {
            // Access the points from the ARPointCloud
            foreach (var pointCloud in pointCloudManager.trackables)
            {
                // Ensure that the point cloud has valid positions
                if (pointCloud.positions != null)
                {
                    foreach (var point in pointCloud.positions)
                    {
                        // Project point onto the ray to get the closest point along the ray
                        Vector3 rayToPoint = point - ray.origin;  // Vector from ray origin to point
                        float projection = Vector3.Dot(rayToPoint, ray.direction); // Project the point onto the ray
                        Vector3 closestPointOnRay = ray.origin + ray.direction * projection;  // Get the point on the ray

                        // Calculate distance from the point in the cloud to the closest point on the ray
                        float distance = Vector3.Distance(point, closestPointOnRay);

                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestPoint = closestPointOnRay; // Update closest point on the ray
                        }
                    }
                }
            }
        }

        return closestPoint;
    }

    private IEnumerator ReturnCursorToCamera()
    {
        yield return new WaitForSeconds(2f); // Wait for 2 seconds before returning to camera

        // Lerp back to the camera's original position after 2 seconds
        lastCursorLocalPosition = new Vector3(0, 0, depth);
        isCursorOnSurface = false;
    }
}
