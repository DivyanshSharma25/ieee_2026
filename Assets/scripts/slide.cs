using System.Collections.Generic;
using UnityEngine;

public class slide : MonoBehaviour, ILiquidReceiver
{
    [Header("Step Sequence")]
    [SerializeField]
    private List<string> requiredLiquids = new List<string>();

    [Header("Models")]
    [SerializeField]
    private List<GameObject> stepModels = new List<GameObject>();

    private int currentStepIndex;

    private void OnEnable()
    {
        UpdateModelState();
    }

    private void Start()
    {
        UpdateModelState();
    }

    public void ReceiveFluid(FluidData fluidData)
    {
        if (fluidData.mixture == null || fluidData.mixture.Count == 0)
        {
            Debug.LogWarning("Slide received empty fluid data.");
            return;
        }

        if (currentStepIndex >= requiredLiquids.Count)
        {
            Debug.Log("Slide sequence already complete.");
            return;
        }

        string expectedLiquid = requiredLiquids[currentStepIndex];
        string receivedLiquid = fluidData.GetDisplayName();

        if (string.Equals(receivedLiquid, expectedLiquid, System.StringComparison.OrdinalIgnoreCase))
        {
            currentStepIndex++;
            UpdateModelState();
            Debug.Log($"Correct liquid received: {receivedLiquid}. Advanced to step {currentStepIndex}.");
            return;
        }

        Debug.Log($"Wrong liquid on slide. Expected '{expectedLiquid}', got '{receivedLiquid}'.");
    }

    private void UpdateModelState()
    {
        if (stepModels == null)
            return;

        for (int i = 0; i < stepModels.Count; i++)
        {
            if (stepModels[i] == null)
                continue;

            bool shouldBeEnabled = i == currentStepIndex && currentStepIndex < stepModels.Count;
            stepModels[i].SetActive(shouldBeEnabled);
        }

        if (currentStepIndex >= stepModels.Count)
        {
            for (int i = 0; i < stepModels.Count; i++)
            {
                if (stepModels[i] != null)
                    stepModels[i].SetActive(false);
            }
        }
    }

    public void ResetSequence()
    {
        currentStepIndex = 0;
        UpdateModelState();
    }

    public int CurrentStepIndex => currentStepIndex;
    public int TotalSteps => requiredLiquids.Count;
    public bool IsCompleted => currentStepIndex >= requiredLiquids.Count;
}
