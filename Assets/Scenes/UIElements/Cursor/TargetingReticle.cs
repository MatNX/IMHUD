using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TargetingReticle : MonoBehaviour
{
    public Camera arCamera; // Assign the ARCore camera in the inspector

    public int prongCount = 4;           // Number of prongs
    public int dotCountPerProng = 20;    // Lines per prong
    public float lineSpacing = 0.005f;   // Spacing between lines
    public float lineLength = 0.005f;    // Line length
    public float lineThickness = 0.005f; // Line thickness
    public float centralOffset = 0.065f; // Offset from center
    public float animationSpeed = 0.0125f; // Time per line (1/4 second total)
    public Sprite arrowSprite;           // The arrow sprite (set in the inspector)
    public float rotationSpeed = 3f;    // Speed of rotation in degrees per second

    private CursorController cursorController; // Reference to CursorController
    private List<List<Transform>> prongs = new List<List<Transform>>();
    private List<Transform> arrows = new List<Transform>(); // New list for arrows
    private List<Vector3> arrowPositions = new List<Vector3>(); // List to store arrow positions in local space
    private List<Vector3> initialArrowPositions = new List<Vector3>(); // Store initial positions
    private List<Quaternion> initialArrowRotations = new List<Quaternion>(); // Store initial rotations

    public Quaternion storedCameraRotation;
    public bool targetingMode = false; // New variable for targeting mode

    public Vector3 storedClosestPoint; // Assign this from the parent or wherever it is exposed

    void Start()
    {
        cursorController = FindFirstObjectByType<CursorController>(); // Get reference to CursorController
        CreateReticle();
        StartCoroutine(SweepAnimation());
        StartCoroutine(RotateArrowsAroundCenter()); // Start rotating arrows around the center
    }

    void Update()
    {

        storedCameraRotation = cursorController.storedCameraRotation;
        targetingMode = cursorController.targetingMode;
        storedClosestPoint = cursorController.storedClosestPoint;
        
    }

            void CreateReticle()
    {
        for (int prong = 0; prong < prongCount; prong++)
        {
            float angle = prong * 90f; // Each prong at 90-degree intervals
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            List<Transform> prongLines = new List<Transform>();

            for (int i = 0; i < dotCountPerProng; i++)
            {
                float distance = centralOffset + (i * lineSpacing);
                Vector3 position = rotation * (Vector3.right * distance);
                Transform line = CreateLine(position, rotation);
                prongLines.Add(line);
            }

            // Add arrow at the inner tip of each prong
            AddArrow(rotation);

            prongs.Add(prongLines);
        }
    }

    Transform CreateLine(Vector3 position, Quaternion rotation)
    {
        GameObject line = new GameObject("ReticleLine");
        line.transform.SetParent(transform);
        line.transform.localPosition = position;
        line.transform.localRotation = rotation;

        // Add a SpriteRenderer with a thin rectangular sprite
        SpriteRenderer sr = line.AddComponent<SpriteRenderer>();
        sr.sprite = GenerateRectangleSprite();
        sr.color = new Color32(111, 148, 157, 179); // Set initial color

        // Scale to match thickness and length
        line.transform.localScale = new Vector3(lineThickness, lineLength, 1f);

        return line.transform;
    }

    void AddArrow(Quaternion rotation)
    {
        GameObject arrowObject = new GameObject("Arrow");
        arrowObject.transform.SetParent(transform);
        arrowObject.transform.localRotation = rotation; // Rotate according to prong's rotation

        // Move the arrow 4 spacings further inward from the previous position
        Vector3 position = rotation * (Vector3.up * (centralOffset - 3 * lineSpacing));
        arrowObject.transform.localPosition = position;

        // Add the arrow sprite
        SpriteRenderer sr = arrowObject.AddComponent<SpriteRenderer>();
        sr.sprite = arrowSprite;
        sr.color = Color.white; // Set arrow color to white

        // Rotate the arrow to point inward
        arrowObject.transform.localRotation = rotation * Quaternion.Euler(0f, 0f, 180f); // Rotate by 180 degrees to point inward
        arrowPositions.Add(arrowObject.transform.localPosition);
        arrows.Add(arrowObject.transform);

        initialArrowPositions.Add(arrowObject.transform.localPosition);
        initialArrowRotations.Add(arrowObject.transform.localRotation);

        // Start the arrow animation
        StartCoroutine(ArrowAnimation(arrowPositions.Count - 1));
    }

    IEnumerator ArrowAnimation(int index)
    {
        float fastDuration = 0.1f; // Fast outward movement (0.1 seconds)
        float slowDuration = 0.4f; // Slow return to the original position (0.4 seconds)

        Vector3 startPosition = arrows[index].localPosition; // Start from the arrow's actual position
        Vector3 endPosition = arrowPositions[index]; // Target position

        // Get the arrow's transformed up direction
        Vector3 worldUpDirection = arrows[index].localRotation * Vector3.up;

        if (worldUpDirection != Vector3.zero)
        {
            worldUpDirection.Normalize();
        }

        Vector3 targetPosition = endPosition + -worldUpDirection * 5 * lineSpacing;

        // Disable pulsing effect during movement
        StopCoroutine(PulseArrow(index));

        // Move arrow outward quickly (fast animation)
        float elapsed = 0f;
        while (elapsed < fastDuration)
        {
            arrows[index].localPosition = Vector3.Lerp(startPosition, targetPosition, elapsed / fastDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        arrows[index].localPosition = targetPosition; // Ensure it reaches the final position

        // Move arrow back slowly to the original position (slow animation)
        elapsed = 0f;
        while (elapsed < slowDuration)
        {
            arrows[index].localPosition = Vector3.Lerp(targetPosition, endPosition, elapsed / slowDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        arrows[index].localPosition = endPosition; // Ensure it resets exactly

        // Restart pulsing effect after the movement animation is complete
        StartCoroutine(PulseArrow(index));

        // Optional delay between animation repeats
        yield return new WaitForSeconds(0.1f);
    }

    // Separate pulse effect coroutine that runs continuously
    IEnumerator PulseArrow(int index)
    {
        // Define separate pulse durations for outwards and inwards
        float outDuration = 0.1f; // Duration for moving outwards (fast)
        float inDuration = 0.4f; // Duration for moving inwards (slow)
        float cycleTime = outDuration + inDuration; // Total cycle time (0.5 seconds)

        while (true)
        {
            // Calculate the pulse offset
            Vector3 pulseOffset = (arrows[index].localRotation * Vector3.up).normalized * -5 * lineSpacing;

            // Get the time within the current cycle
            float timeInCycle = Time.time % cycleTime;
            float t;

            // Determine which phase we are in:
            // If within the outwards phase, interpolate from 0 to 1 quickly.
            if (timeInCycle < outDuration)
            {
                t = timeInCycle / outDuration;
            }
            // Else, within the inwards phase, interpolate from 1 back to 0 slowly.
            else
            {
                t = 1 - ((timeInCycle - outDuration) / inDuration);
            }

            // Interpolate the arrow position accordingly
            arrows[index].localPosition = Vector3.Lerp(arrowPositions[index],
                                                         arrowPositions[index] + pulseOffset, t);

            yield return null;
        }
    }

    IEnumerator RotateArrowsAroundCenter()
    {
        while (true)
        {
            if (targetingMode)
            {
                // Calculate the difference quaternion between storedCameraRotation and current camera rotation
                Quaternion cameraRotationDifference = Quaternion.Inverse(storedCameraRotation) * arCamera.transform.rotation;

                for (int i = 0; i < arrows.Count; i++)
                {
                    // Isolate the component we need to adjust for each arrow
                    float angle = 2 * Mathf.Acos(cameraRotationDifference[i]) * Mathf.Rad2Deg;
                    if (i == 3)
                    {
                        angle += 180;
                    }
                    angle -= 90;
                    // Calculate the new circular position based on the arrow's part of the rotation
                    float angleOffset = (i * (360f / prongCount));
                    float radian = Mathf.Deg2Rad * (angle + angleOffset); // Include offset for each arrow

                    // Update the position in a circular path around the center
                    Vector3 newPosition = new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0f) * (centralOffset - 3 * lineSpacing);
                    arrowPositions[i] = newPosition;

                    // Compute the direction from the arrow to the center
                    Vector3 directionToCenter = -newPosition.normalized;

                    // Rotate arrow so that it points towards the center
                    float zRotation = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg - 90f;

                    // Apply different components of the quaternion based on the arrow index
                    // For this fix, we isolate the part that causes the flip (likely the 'w' or 'z' component)
                    Quaternion arrowRotation = Quaternion.Euler(0, 0, zRotation);

                    // Here we apply the correct rotation, ensuring no unwanted flips
                    arrows[i].transform.localRotation = arrowRotation;
                }
            } else
            {
                for (int i = 0; i < arrows.Count; i++)
                {
                    arrowPositions[i] = initialArrowPositions[i];
                    Vector3 directionToCenter = -arrowPositions[i].normalized;

                    // Rotate arrow so that it points towards the center
                    float zRotation = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg - 90f;

                    // Apply different components of the quaternion based on the arrow index
                    // For this fix, we isolate the part that causes the flip (likely the 'w' or 'z' component)
                    Quaternion arrowRotation = Quaternion.Euler(0, 0, zRotation);

                    // Here we apply the correct rotation, ensuring no unwanted flips
                    arrows[i].transform.localRotation = arrowRotation;
                }
            }
            yield return null; // Continue checking every frame
        }
    }

    IEnumerator SweepAnimation()
    {
        // Start an individual animation cycle for each prong.
        for (int i = 0; i < prongs.Count; i++)
        {
            // Pass both the prong and its index (for direction calculation)
            StartCoroutine(AnimateProngCycle(prongs[i], i));
        }

        yield break;
    }

    IEnumerator AnimateProngCycle(List<Transform> prong, int prongIndex)
    {
        // Determine the prong’s direction based on its index.
        Vector3 prongDirection = GetProngDirection(prongIndex);

        // Loop indefinitely.
        while (true)
        {
            // Iterate over each dot from the outermost (last index) to the innermost (index 0)
            for (int i = dotCountPerProng - 1; i >= 0; i--)
            {
                if (targetingMode)
                {
                    // === Growing Animation ===
                    // Calculate the direction from the AR camera to the stored closest point.
                    Vector3 screenSpacePoint = arCamera.WorldToScreenPoint(storedClosestPoint);

                    // Get the center of the screen (usually the middle of the viewport).
                    Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, screenSpacePoint.z);

                    // Calculate the direction from the center of the screen to the point in screen space.
                    Vector3 toClosestPoint = screenSpacePoint - screenCenter;

                    // Now, project this 2D vector onto the prong's axis (X/Y)
                    float axisComponent = Vector3.Dot(toClosestPoint.normalized, prongDirection);

                    // Adjust speed based on the axis component and targeting mode
                    float adjustedStartSpeed = (axisComponent > 0) ?
                                                (animationSpeed * Mathf.Abs(axisComponent) / 3f) :
                                                0.0125f;

                    // Animate the dot growing
                    yield return AnimateLine(prong[i], 2.0f, adjustedStartSpeed);

                    // Optional: wait briefly before starting the shrink phase.
                    yield return new WaitForSeconds(adjustedStartSpeed);

                    // === Shrinking Animation ===
                    // Recalculate the parameters (in case they change over time)
                    screenSpacePoint = arCamera.WorldToScreenPoint(storedClosestPoint);
                    screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, screenSpacePoint.z);
                    toClosestPoint = screenSpacePoint - screenCenter;
                    axisComponent = Vector3.Dot(toClosestPoint.normalized, prongDirection);
                    float adjustedReturnSpeed = (axisComponent > 0) ?
                                                (animationSpeed * Mathf.Abs(axisComponent) / 3f) :
                                                0.0125f;

                    // Start the shrinking animation.
                    // If you want to wait until the shrink completes, use yield return here.
                    // Otherwise, starting it this way will let it run concurrently.
                    StartCoroutine(AnimateLine(prong[i], 0.5f, adjustedReturnSpeed));
                } 
                else
                {
                    // Adjust speed based on the axis component and targeting mode
                    float adjustedSpeed = 0.0125f;

                    // Animate the dot growing
                    yield return AnimateLine(prong[i], 2.0f, adjustedSpeed);

                    // Optional: wait briefly before starting the shrink phase.
                    yield return new WaitForSeconds(adjustedSpeed);

                    // Start the shrinking animation.
                    // If you want to wait until the shrink completes, use yield return here.
                    // Otherwise, starting it this way will let it run concurrently.
                    StartCoroutine(AnimateLine(prong[i], 0.5f, adjustedSpeed));
                }
            }

            // Optionally, wait a frame before starting the next cycle for this prong.
            yield return null;
        }
    }


    Vector3 GetProngDirection(int prongIndex)
    {
        switch (prongIndex)
        {
            case 0: return Vector3.up;
            case 1: return Vector3.left;
            case 2: return Vector3.down;
            case 3: return Vector3.right;
            default: return Vector3.zero;
        }
    }

    IEnumerator AnimateLine(Transform line, float scaleFactor, float speed)
    {
        Vector3 originalScale = line.localScale;
        Vector3 targetScale = new Vector3(originalScale.x * scaleFactor, originalScale.y * scaleFactor, 1f);

        // Determine the duration of the animation (half cycle)
        float duration = speed / 2f;
        float elapsed = 0f;

        // Define the colors for interpolation
        Color32 normalColor = new Color32(111, 148, 157, 179); // ~70% opacity
        Color32 scaledColor = new Color32(111, 148, 157, 229); // ~90% opacity

        while (elapsed < duration)
        {
            line.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            line.GetComponent<SpriteRenderer>().color = Color32.Lerp(normalColor, scaledColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure the final values are set.
        line.localScale = targetScale;
        line.GetComponent<SpriteRenderer>().color = scaledColor;
    }

    Sprite GenerateRectangleSprite()
    {
        int width = 16, height = 128;
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), height);
    }
}