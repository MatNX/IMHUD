using UnityEngine;

public class ScaleController : MonoBehaviour
{
    public SpriteRenderer scaleFrameRenderer; // Drag your ScaleFrame's SpriteRenderer here
    public SpriteRenderer scaleFillRenderer; // Drag your ScaleFill's SpriteRenderer here

    void Start()
    {
        // Dynamically generate the ScaleFill texture
        Texture2D scaleFillTexture = GenerateScaleFillTexture();

        // Create a Sprite from the texture and assign it to the ScaleFill's SpriteRenderer
        Sprite scaleFillSprite = Sprite.Create(scaleFillTexture, new Rect(0, 0, scaleFillTexture.width, scaleFillTexture.height), new Vector2(0.5f, 0.5f), 1000f);
        scaleFillRenderer.sprite = scaleFillSprite;

        // Set the z-order (ScaleFill should be behind ScaleFrame)
        scaleFillRenderer.sortingOrder = 0;  // Ensure ScaleFill is rendered behind ScaleFrame
        scaleFrameRenderer.sortingOrder = 1; // Ensure ScaleFrame is in front
    }

    // Example of dynamically generating a texture for ScaleFill
    public Texture2D GenerateScaleFillTexture()
    {
        // Texture size
        int texWidth = 300;
        int texHeight = 300;
        Texture2D texture = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, false);

        // Clear texture (fully transparent)
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32[] fillColors = new Color32[texWidth * texHeight];
        for (int i = 0; i < fillColors.Length; i++)
            fillColors[i] = clear;
        texture.SetPixels32(fillColors);

        // Define arc parameters (matching the Python script's properties)
        Vector2 center = new Vector2(texWidth / 2f, texHeight / 2f);
        float baseRadius = 100f;           // Base radius (as in Python)
        float scaleRadius = baseRadius + 20f; // Offset of 20px from the main circle (i.e. 120px)
        float actualStart = 160f;          // Start angle in degrees
        float actualEnd = 230f;            // End angle in degrees
        float totalAngle = actualEnd - actualStart; // 70°

        int numSegments = 12;
        float gapPixels = 1f;
        // Calculate the gap in degrees at the scale's radius:
        float circumference = 2 * Mathf.PI * scaleRadius;
        float gapDegrees = (gapPixels / circumference) * 360f;

        float totalGap = gapDegrees * (numSegments - 1);
        float effectiveAngle = totalAngle - totalGap;
        float segmentAngle = effectiveAngle / numSegments;

        // Define segment thicknesses:
        int[] segmentWidths = new int[numSegments];
        for (int i = 0; i < numSegments; i++)
            segmentWidths[i] = (i < 4) ? 24 : 18;

        // Fill color (from your Python script: #0a416a at 70% opacity was used earlier,
        // but here the fill color is given as (111, 148, 157, 179) – adjust as needed)
        Color32 fillColor = new Color32(111, 148, 157, 179);

        // Loop over every pixel in the texture
        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                // Compute pixel's polar coordinates relative to the texture center.
                float dx = x - center.x;
                float dy = y - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (angle < 0)
                    angle += 360f;  // Normalize angle to 0–360°

                bool inSegment = false;
                // Check each segment to see if this pixel should be filled.
                for (int i = 0; i < numSegments; i++)
                {
                    // Calculate the start and end angles for this segment.
                    float segStart = actualStart + i * (segmentAngle + gapDegrees);
                    float segEnd = segStart + segmentAngle;

                    // If the pixel's angle lies within this segment's angular span…
                    if (angle >= segStart && angle <= segEnd)
                    {
                        // Determine inner and outer radii for the segment.
                        float innerRadius = scaleRadius;
                        float outerRadius = scaleRadius + segmentWidths[i];

                        // If the pixel's distance is within this ring segment...
                        if (dist >= innerRadius && dist <= outerRadius)
                        {
                            inSegment = true;
                            break;
                        }
                    }
                }

                if (inSegment)
                    texture.SetPixel(x, y, fillColor);
            }
        }

        texture.Apply();
        return texture;
    }

}
