using UnityEngine;
using Autohand;

public class VortexSliderIntensityBridge : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the Grab object (the one with Physics Gadget Configurable Limit Reader) here")]
    public PhysicsGadgetConfigurableLimitReader slider;

    [Tooltip("The lid's vibration script")]
    public VortexLidVibration vortexLid;

    [Header("Mapping")]
    [Tooltip("If your slider only moves one direction (0 to 1) for this use case, check this. " +
             "If it's a true -1 to 1 double slider and you want both directions to count as intensity, leave unchecked.")]
    public bool useOnlyPositiveRange = true;

    [Tooltip("Deadzone below which intensity is forced to 0, in addition to the slider's own playRange")]
    [Range(0f, 0.5f)] public float extraDeadzone = 0f;

    void Update()
    {
        if (slider == null || vortexLid == null) return;

        float raw = slider.GetValue(); // -1 to 1

        float intensity;
        if (useOnlyPositiveRange)
        {
            // Map 0->1 to 0->1 intensity, treat negative side as 0 (off)
            intensity = Mathf.Clamp01(raw);
        }
        else
        {
            // Map -1..1 to 0..1 intensity, so either direction ramps up intensity
            intensity = Mathf.Abs(raw);
        }

        if (intensity < extraDeadzone) intensity = 0f;

        vortexLid.SetIntensity(intensity);
    }
}