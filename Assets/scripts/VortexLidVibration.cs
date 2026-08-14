using UnityEngine;

public class VortexLidVibration : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to auto-use this object's starting position")]
    public Transform pivotOverride;

    [Header("Vibration Settings")]
    [Tooltip("Current intensity, 0 = fully off, 1 = max speed set by lever")]
    [Range(0f, 1f)] public float intensity = 0f;

    [Tooltip("Max orbital radius in local units at intensity = 1")]
    public float maxOrbitRadius = 0.08f;

    [Tooltip("Max rotations per second at intensity = 1 (real vortex mixers run ~1500-3000 RPM ≈ 25-50 Hz)")]
    public float maxFrequencyHz = 30f;

    [Tooltip("Slight random jitter added on top of the circular orbit for realism")]
    public float jitterAmount = 0.015f;

    [Header("Vertical Bounce (optional, for extra punch)")]
    [Tooltip("Adds a fast vertical bounce on top of the orbital motion. Set to 0 to disable.")]
    public float verticalBounceAmount = 0.01f;

    [Tooltip("How much faster the vertical bounce cycles relative to the orbit")]
    public float verticalBounceFrequencyMultiplier = 2f;

    [Header("Smoothing")]
    [Tooltip("How quickly intensity ramps up/down when changed (prevents instant snap/jerk)")]
    public float intensitySmoothTime = 0.15f;

    private Vector3 pivotLocalPos;
    private Quaternion pivotLocalRot;
    private float currentIntensity;
    private float intensityVelocity;
    private float orbitAngle;
    private float noiseSeedX, noiseSeedZ;

    void Awake()
    {
        pivotLocalPos = transform.localPosition;
        pivotLocalRot = transform.localRotation;

        noiseSeedX = Random.Range(0f, 1000f);
        noiseSeedZ = Random.Range(0f, 1000f);
    }

    void Update()
    {
        // Smoothly approach target intensity (set via SetIntensity from the slider bridge)
        currentIntensity = Mathf.SmoothDamp(currentIntensity, intensity, ref intensityVelocity, intensitySmoothTime);

        if (currentIntensity < 0.001f)
        {
            transform.localPosition = pivotLocalPos;
            return;
        }

        // Frequency and radius both scale with intensity
        float freq = maxFrequencyHz * currentIntensity;
        float radius = maxOrbitRadius * currentIntensity;

        orbitAngle += freq * 360f * Time.deltaTime;
        if (orbitAngle > 360f) orbitAngle -= 360f;

        float rad = orbitAngle * Mathf.Deg2Rad;
        float x = Mathf.Cos(rad) * radius;
        float z = Mathf.Sin(rad) * radius;

        // Small perlin jitter layered on top, scaled by intensity too
        float jx = (Mathf.PerlinNoise(noiseSeedX, Time.time * freq) - 0.5f) * jitterAmount * currentIntensity;
        float jz = (Mathf.PerlinNoise(noiseSeedZ, Time.time * freq) - 0.5f) * jitterAmount * currentIntensity;

        // Optional fast vertical chatter for extra punch, on top of the orbit
        float bounceY = Mathf.Sin(rad * verticalBounceFrequencyMultiplier) * verticalBounceAmount * currentIntensity;

        transform.localPosition = pivotLocalPos + new Vector3(x + jx, bounceY, z + jz);
    }

    /// <summary>
    /// Call this from the slider bridge. value should be 0-1 (already normalized).
    /// </summary>
    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);
    }
}