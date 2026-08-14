using System;
using UnityEngine;

public class ExperimentManager : MonoBehaviour
{
    public static ExperimentManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Header("Starting Experiment")]
    public ExperimentData startingExperiment;

    [Header("Debug Settings")]
    public bool verboseLogging = true;

    public ExperimentData CurrentExperiment { get; private set; }
    public int CurrentStepIndex { get; private set; }
    public ExperimentStep CurrentStep => CurrentExperiment?.GetStep(CurrentStepIndex);
    public bool IsRunning { get; private set; }

    public event Action<ExperimentData> OnExperimentStarted;
    public event Action<ExperimentStep, int, int> OnStepChanged;
    public event Action<ExperimentData> OnExperimentCompleted;
    public event Action OnAllExperimentsCompleted;

    private void Start()
    {
        if (startingExperiment != null)
            StartExperiment(startingExperiment);
        else
            Debug.LogError("[ExperimentManager] No starting experiment assigned!");
    }

    public void StartExperiment(ExperimentData experiment)
    {
        if (experiment == null || experiment.StepCount == 0)
        {
            Debug.LogError("[ExperimentManager] Invalid or empty experiment.");
            return;
        }

        CurrentExperiment = experiment;
        CurrentStepIndex = 0;
        IsRunning = true;

        Log($"Starting '{CurrentExperiment.experimentName}' ({CurrentExperiment.StepCount} steps)");
        OnExperimentStarted?.Invoke(CurrentExperiment);
        ActivateCurrentStep();
    }

    public void NextStep()
    {
        if (!IsRunning || CurrentExperiment == null)
        {
            Debug.LogWarning("[ExperimentManager] NextStep() called but nothing is running.");
            return;
        }

        CurrentStep?.onStepComplete?.Invoke();

        bool isLastStep = CurrentStepIndex >= CurrentExperiment.StepCount - 1;
        if (isLastStep)
            CompleteExperiment();
        else
        {
            CurrentStepIndex++;
            ActivateCurrentStep();
        }
    }

    public void CompleteExperiment()
    {
        if (CurrentExperiment == null) return;

        ExperimentData justCompleted = CurrentExperiment;
        OnExperimentCompleted?.Invoke(justCompleted);

        if (justCompleted.HasNextExperiment())
            StartExperiment(justCompleted.nextExperiment);
        else
        {
            IsRunning = false;
            OnAllExperimentsCompleted?.Invoke();
        }
    }

    private void ActivateCurrentStep()
    {
        ExperimentStep step = CurrentStep;
        if (step == null) return;

        step.onStepStart?.Invoke();
        OnStepChanged?.Invoke(step, CurrentStepIndex + 1, CurrentExperiment.StepCount);
    }

    private void Log(string message)
    {
        if (verboseLogging) Debug.Log($"[ExperimentManager] {message}");
    }
}