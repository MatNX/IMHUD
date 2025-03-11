using UnityEngine;
using UnityEngine.InputSystem;

public class ArcScale : MonoBehaviour
{
    public GameObject Thumb; // The thumb GameObject
    public Camera arCamera; // The AR camera (assign this in the inspector)
    public float radius = 100f; // The radius of the arc
    public Vector2 scaleCenter = new Vector2(0, 0); // The center of the scale (origin of the arc)
    public float startAngle = 130f; // Starting angle of the scale
    public float endAngle = 200f; // Ending angle of the scale
    public float arcWidth = 18f; // Width of the arc segments (for the math)

    private InputAction tapAction; // Input Action for tap
    private InputAction touchPositionAction; // Input Action for touch position

    private void OnEnable()
    {
        // Initialize Input Actions for touch position and tap action
        touchPositionAction = new InputAction("TouchPosition", binding: "<Pointer>/position");
        tapAction = new InputAction("Tap", binding: "<Pointer>/press");

        // Enable actions
        tapAction.Enable();
        touchPositionAction.Enable();
    }

    private void OnDisable()
    {
        // Disable actions when the script is disabled
        tapAction.Disable();
        touchPositionAction.Disable();
    }

    void Update()
    {
        if (tapAction.WasPerformedThisFrame())
        {
            OnTouchPosition();
        }
    }

    // Handle touch position and move the thumb accordingly
    private void OnTouchPosition()
    {
        // Get the touch position
        Vector2 touchPosition = touchPositionAction.ReadValue<Vector2>();

        // Convert the touch position from screen space to world space using the AR camera
        Vector3 worldPosition = arCamera.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, arCamera.nearClipPlane));

        // Adjust scale center to the world position of the parent
        Vector3 parentPosition = transform.parent != null ? transform.parent.position : Vector3.zero;
        Vector2 adjustedScaleCenter = new Vector2(scaleCenter.x + parentPosition.x, scaleCenter.y + parentPosition.y);

        // Check if the touch is within the arc region (within radius bounds)
        float distance = Vector2.Distance(worldPosition, adjustedScaleCenter);

        Debug.Log(distance);

        if (distance >= radius - arcWidth / 2 && distance <= radius + arcWidth / 2)
        {
            // Calculate angle of the touch relative to the center of the scale
            float angle = Mathf.Atan2(worldPosition.y - adjustedScaleCenter.y, worldPosition.x - adjustedScaleCenter.x) * Mathf.Rad2Deg;

            // Ensure the angle is within the scale's arc range
            if (angle < startAngle) angle += 360;
            if (angle > endAngle) angle -= 360;

            // Map the angle to the arc (clamp to the start and end angle)
            angle = Mathf.Clamp(angle, startAngle, endAngle);

            // Move the thumb to the calculated position along the arc
            Vector3 thumbPosition = GetPositionOnArc(angle);
            Thumb.transform.position = thumbPosition;
        }
    }

    // Convert angle to a point on the arc's circumference
    Vector3 GetPositionOnArc(float angle)
    {
        // Convert the angle to radians and then to world position
        float radians = (angle - 90f) * Mathf.Deg2Rad; // Subtract 90 to align with Unity's coordinate system
        float x = scaleCenter.x + radius * Mathf.Cos(radians);
        float y = scaleCenter.y + radius * Mathf.Sin(radians);
        return new Vector3(x, y, 0f); // z = 0 to ensure it stays on the 2D plane
    }
}
