using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Renderer))]
public class SpectroGraphDisplay : MonoBehaviour
{
    [Header("Texture Settings")]
    public int textureWidth = 512;
    public int textureHeight = 512;

    [Header("Graph Range")]
    [Tooltip("Max concentration shown on X axis (concentration is 0-1, so 1 is usually right)")]
    public float maxConcentration = 1f;
    [Tooltip("Starting max for Y axis, auto-expands if data goes higher")]
    public float maxAbsorbance = 2f;

    [Header("Colors")]
    public Color backgroundColor = Color.white;
    public Color axisColor = Color.black;
    public Color calibrationLineColor = Color.blue;
    public Color pointColor = Color.red;

    [Header("Style")]
    public int axisThickness = 2;
    public int pointRadius = 4;
    public int margin = 40;

    [Header("Orientation (flip if graph looks mirrored/sideways on your screen mesh)")]
    public bool flipX = false;
    public bool flipY = false;

    private Texture2D graphTexture;
    private Renderer targetRenderer;

    void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        graphTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        graphTexture.filterMode = FilterMode.Bilinear;

        // Use an instanced material so we don't overwrite your shared "Lit" material asset
        targetRenderer.material.mainTexture = graphTexture;

        ClearGraph();
    }

    public void ClearGraph()
    {
        Color[] fill = new Color[textureWidth * textureHeight];
        for (int i = 0; i < fill.Length; i++) fill[i] = backgroundColor;
        graphTexture.SetPixels(fill);
        graphTexture.Apply();
    }

    /// <summary>
    /// Redraws axes, the calibration line (A = slope*C + intercept), and every measured point.
    /// </summary>
    public void UpdateGraph(List<Vector2> measurements, float slope, float intercept)
    {
        float highestNeeded = maxAbsorbance;
        foreach (var m in measurements)
            if (m.y > highestNeeded) highestNeeded = m.y;

        float lineAtMaxX = slope * maxConcentration + intercept;
        if (lineAtMaxX > highestNeeded) highestNeeded = lineAtMaxX;
        maxAbsorbance = highestNeeded * 1.1f;

        ClearGraph();
        DrawAxes();
        DrawCalibrationLine(slope, intercept);

        foreach (var point in measurements)
            DrawPoint(point.x, point.y);

        graphTexture.Apply();
    }

    Vector2Int GraphToPixel(float concentration, float absorbance)
    {
        float xNorm = Mathf.InverseLerp(0f, maxConcentration, concentration);
        float yNorm = Mathf.InverseLerp(0f, maxAbsorbance, absorbance);

        if (flipX) xNorm = 1f - xNorm;
        if (flipY) yNorm = 1f - yNorm;

        int px = margin + Mathf.RoundToInt(xNorm * (textureWidth - margin * 2));
        int py = margin + Mathf.RoundToInt(yNorm * (textureHeight - margin * 2));
        return new Vector2Int(px, py);
    }

    void DrawAxes()
    {
        for (int x = margin; x < textureWidth - margin; x++)
            for (int t = 0; t < axisThickness; t++)
                SetPixelSafe(x, margin + t, axisColor);

        for (int y = margin; y < textureHeight - margin; y++)
            for (int t = 0; t < axisThickness; t++)
                SetPixelSafe(margin + t, y, axisColor);
    }

    void DrawCalibrationLine(float slope, float intercept)
    {
        Vector2Int prev = GraphToPixel(0f, intercept);
        int steps = 100;
        for (int i = 1; i <= steps; i++)
        {
            float c = (maxConcentration / steps) * i;
            float a = slope * c + intercept;
            Vector2Int curr = GraphToPixel(c, a);
            DrawLineOnTexture(prev, curr, calibrationLineColor);
            prev = curr;
        }
    }

    void DrawPoint(float concentration, float absorbance)
    {
        Vector2Int center = GraphToPixel(concentration, absorbance);
        for (int dx = -pointRadius; dx <= pointRadius; dx++)
            for (int dy = -pointRadius; dy <= pointRadius; dy++)
                if (dx * dx + dy * dy <= pointRadius * pointRadius)
                    SetPixelSafe(center.x + dx, center.y + dy, pointColor);
    }

    void DrawLineOnTexture(Vector2Int a, Vector2Int b, Color color)
    {
        int dx = Mathf.Abs(b.x - a.x), sx = a.x < b.x ? 1 : -1;
        int dy = -Mathf.Abs(b.y - a.y), sy = a.y < b.y ? 1 : -1;
        int err = dx + dy;

        int x = a.x, y = a.y;
        while (true)
        {
            SetPixelSafe(x, y, color);
            if (x == b.x && y == b.y) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x += sx; }
            if (e2 <= dx) { err += dx; y += sy; }
        }
    }

    void SetPixelSafe(int x, int y, Color color)
    {
        if (x < 0 || x >= textureWidth || y < 0 || y >= textureHeight) return;
        graphTexture.SetPixel(x, y, color);
    }
}