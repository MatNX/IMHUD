using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;  // Import AR Foundation
using UnityEngine.XR.ARSubsystems;

public class RangeFinder : MonoBehaviour
{
    [Tooltip("Reference to the CursorController script on the parent.")]
    private CursorController cursorController;
    private ARPointCloudManager pointCloudManager;
    public Camera arCamera; // Assign the ARCore camera in the inspector

    private Vector3 initialPosition; // To store the initial position of the object
    private Vector3 targetPosition;  // The target position for moving forward/backward
    private float moveSpeed = 5f;    // Speed for moving the object
    private float rotationSpeed = 360f;  // Speed for rotating the object

    void Awake()
    {
        // Get ARPointCloudManager to access point cloud data
        pointCloudManager = FindFirstObjectByType<ARPointCloudManager>();
    }

    void Start()
    {
        // Get the CursorController component from the parent object
        cursorController = transform.parent.GetComponent<CursorController>();

        // Check if the CursorController was successfully found
        if (cursorController == null)
        {
            Debug.LogError("CursorController not found on parent object.");
        }

        // Store the initial position of the RangeFinder
        initialPosition = transform.localPosition;
        targetPosition = initialPosition;
    }

    void Update()
    {
        if (cursorController != null)
        {
            // Get the distance from the CursorController and use it to rotate the RangeFinder
            float distance = cursorController.distance;
            float furthestDistance = FindFurthestPointInPointCloud();

            // Rotate the RangeFinder based on the distance
            float rotationAngle = Mathf.Lerp(0f, 360f, distance / furthestDistance); // Modify the 360 as needed
            if (rotationAngle != 0f && transform.localRotation != Quaternion.identity)
            {
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(0f, 0f, rotationAngle), rotationSpeed * Time.deltaTime);
            }
            // Animate the forward/backward movement based on distance
            if (furthestDistance == 0f)
            {
                // Move the object forward if max distance is 0
                targetPosition = initialPosition + new Vector3(0, 0, 0.05f); // Move forward slightly
            }
            else
            {
                // Move the object back to the initial position when a max distance is found
                targetPosition = initialPosition;
            }

            // Smoothly animate the position change
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, moveSpeed * Time.deltaTime);
        }
    }

    private float FindFurthestPointInPointCloud()
    {
        // Get the camera's position directly
        Vector3 cameraPosition = arCamera.transform.position;

        float furthestDistance = 0f; // Start with a minimum distance for the furthest point

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
                        // Calculate distance from the point in the cloud to the furthest point on the ray
                        float distance = Vector3.Distance(cameraPosition, point);

                        if (distance > furthestDistance)
                        {
                            furthestDistance = distance;
                        }
                    }
                }
            }
        }

        return furthestDistance;
    }
}
