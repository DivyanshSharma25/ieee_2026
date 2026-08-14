using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewExperimentData", menuName = "Lab Experiments/Experiment Data", order = 0)]
public class ExperimentData : ScriptableObject
{
    [Header("Experiment Identity")]
    public string experimentName;

    [TextArea(2, 4)]
    public string experimentDescription;

    [Header("Experiment Steps")]
    public List<ExperimentStep> steps = new List<ExperimentStep>();

    [Header("Experiment Chain")]
    [Tooltip("Drag the next experiment's asset here. Leave empty if this is the last one.")]
    public ExperimentData nextExperiment;

    public int StepCount => steps.Count;

    public bool IsValidStepIndex(int index) => index >= 0 && index < steps.Count;

    public ExperimentStep GetStep(int index)
    {
        if (!IsValidStepIndex(index))
        {
            Debug.LogWarning($"[ExperimentData] Step index {index} out of range for '{experimentName}'.");
            return null;
        }
        return steps[index];
    }

    public bool HasNextExperiment() => nextExperiment != null;
}