using UnityEngine;
using UnityEngine.UI;

public static class HitmaskPicker
{
    /// <summary>
    /// Samples a hitmask texture (ID map) at the current mouse position over a UI Image.
    /// Returns the ID in the RED channel (0..255). 0 means "no hit".
    ///
    /// Hitmask import settings:
    /// - Read/Write Enabled
    /// - Compression: None
    /// - Filter Mode: Point
    /// - MipMaps: Off
    /// - sRGB (Color Texture): Off
    /// </summary>
    public static bool TryGetIdUnderMouse_UIImage(
        Image backgroundImage,
        Texture2D hitmask,
        out byte id,
        out Color32 sampledColor,
        bool flipV = false,
        byte edgeTolerance = 1)
    {
        id = 0;
        sampledColor = new Color32(0, 0, 0, 255);

        if (backgroundImage == null || hitmask == null)
            return false;

        var rectTransform = backgroundImage.rectTransform;
        var canvas = backgroundImage.canvas;
        if (canvas == null)
            return false;

        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, Input.mousePosition, uiCam, out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = rectTransform.rect;
        if (!rect.Contains(localPoint))
            return false;

        // Local -> UV
        float u = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float v = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
        if (flipV) v = 1f - v;

        // UV -> Pixel
        int x = Mathf.Clamp(Mathf.FloorToInt(u * hitmask.width), 0, hitmask.width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(v * hitmask.height), 0, hitmask.height - 1);

        sampledColor = hitmask.GetPixel(x, y);
        byte raw = sampledColor.r;

        // Optional: snap raw to nearest "expected" value if you're using multiples of 10,
        // but WITHOUT ever turning non-zero into zero.
        id = SnapToNearestExpected(raw, edgeTolerance);

        return true;
    }

    /// <summary>
    /// If you use IDs in multiples of 10, this snaps raw to the nearest multiple of 10
    /// within a tolerance band. If it doesn't match any expected value, returns raw.
    /// Never converts a non-zero raw to 0.
    /// </summary>
    private static byte SnapToNearestExpected(byte raw, byte tol)
    {
        if (raw == 0) return 0;

        // If you're using steps of 10, try snapping to nearest multiple of 10.
        // Example: 9/10/11 -> 10
        int nearest10 = Mathf.RoundToInt(raw / 10f) * 10;
        nearest10 = Mathf.Clamp(nearest10, 0, 255);

        if (nearest10 == 0) nearest10 = 10; // don't drop to 0 if raw was non-zero

        if (Mathf.Abs(raw - nearest10) <= tol)
            return (byte)nearest10;

        // If it isn't close, just return raw (so you can see/debug unexpected IDs)
        return raw;
    }
}