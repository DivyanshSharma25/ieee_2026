using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VRInstructionUI : MonoBehaviour
{
    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI experimentTitleText;
    [SerializeField] private TextMeshProUGUI stepTitleText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI stepCounterText;

    [Header("Button")]
    [SerializeField] private Button nextStepButton;
    [SerializeField] private TextMeshProUGUI nextStepButtonLabel;

    [Header("Completion UI")]
    [SerializeField] private GameObject completionPanel;

    [Header("Animation Settings")]
    [SerializeField] private float textFadeDuration = 0.3f;

    private Coroutine _fadeCoroutine;

    private void Start()
    {
        if (completionPanel != null) completionPanel.SetActive(false);
        SubscribeToEvents();
        SetInstructionText("Loading experiment...");
    }

    private void OnDestroy() => UnsubscribeFromEvents();

    private void SubscribeToEvents()
    {
        if (ExperimentManager.Instance == null)
        {
            Debug.LogError("[VRInstructionUI] ExperimentManager.Instance is null!");
            return;
        }
        ExperimentManager.Instance.OnExperimentStarted += HandleExperimentStarted;
        ExperimentManager.Instance.OnStepChanged += HandleStepChanged;
        ExperimentManager.Instance.OnExperimentCompleted += HandleExperimentCompleted;
        ExperimentManager.Instance.OnAllExperimentsCompleted += HandleAllExperimentsCompleted;
    }

    private void UnsubscribeFromEvents()
    {
        if (ExperimentManager.Instance == null) return;
        ExperimentManager.Instance.OnExperimentStarted -= HandleExperimentStarted;
        ExperimentManager.Instance.OnStepChanged -= HandleStepChanged;
        ExperimentManager.Instance.OnExperimentCompleted -= HandleExperimentCompleted;
        ExperimentManager.Instance.OnAllExperimentsCompleted -= HandleAllExperimentsCompleted;
    }

    private void HandleExperimentStarted(ExperimentData experiment)
    {
        if (experimentTitleText != null) experimentTitleText.text = experiment.experimentName;
        gameObject.SetActive(true);
        if (completionPanel != null) completionPanel.SetActive(false);
        SetNextStepButtonInteractable(true);
    }

    private void HandleStepChanged(ExperimentStep step, int stepNumber, int totalSteps)
    {
        if (stepCounterText != null) stepCounterText.text = $"Step {stepNumber} / {totalSteps}";
        if (stepTitleText != null)
            stepTitleText.text = string.IsNullOrEmpty(step.stepTitle) ? $"Step {stepNumber}" : step.stepTitle;

        SetInstructionTextAnimated(step.instructionText);

        bool isLastStep = stepNumber == totalSteps;
        if (nextStepButtonLabel != null)
            nextStepButtonLabel.text = isLastStep ? "Finish Experiment" : "Next Step \u25B6";
    }

    private void HandleExperimentCompleted(ExperimentData completed) { }

    private void HandleAllExperimentsCompleted()
    {
        SetNextStepButtonInteractable(false);
        if (completionPanel != null) completionPanel.SetActive(true);
        SetInstructionText("All experiments complete! Great work!");
    }

    private void SetInstructionText(string text)
    {
        if (instructionText != null) instructionText.text = text;
    }

    private void SetInstructionTextAnimated(string newText)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeTextCoroutine(newText));
    }

    private IEnumerator FadeTextCoroutine(string newText)
    {
        if (instructionText == null) yield break;

        float half = textFadeDuration / 2f;
        float elapsed = 0f;
        Color start = instructionText.color;
        Color clear = new Color(start.r, start.g, start.b, 0f);

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            instructionText.color = Color.Lerp(start, clear, elapsed / half);
            yield return null;
        }

        instructionText.text = newText;
        instructionText.color = clear;

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            instructionText.color = Color.Lerp(clear, start, elapsed / half);
            yield return null;
        }

        instructionText.color = start;
        _fadeCoroutine = null;
    }

    private void SetNextStepButtonInteractable(bool interactable)
    {
        if (nextStepButton != null) nextStepButton.interactable = interactable;
    }
}