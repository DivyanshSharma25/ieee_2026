using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class ExperimentStep
{
    [TextArea(3, 6)]
    public string instructionText;

    public string stepTitle;

    // Fires when this step becomes active — wire equipment glow here
    public UnityEvent onStepStart;

    // Fires when the player clicks "Next" and leaves this step
    public UnityEvent onStepComplete;
}