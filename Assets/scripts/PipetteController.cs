using System.Collections; // Required for Coroutines
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public enum PipetteState
{
    Empty,
    Ready,
    Filled
}

public class PipetteController : MonoBehaviour
{
    [Header("XR")]
    [SerializeField]
    XRSocketInteractor tipSocketInteractor;

    [Header("Input")]
    [SerializeField]
    InputActionReference transferAction;

    [SerializeField]
    InputActionReference ejectTipAction;

    [SerializeField]
    InputActionReference volumeControlAction;

    [Header("Fluid")]
    [SerializeField]
    [Min(0.01f)]
    float transferAmount = 10f;

    [Header("Volume UI")]
    [SerializeField]
    TMP_Text volumeText;

    [SerializeField]
    [Min(0.1f)]
    float minTransferAmount = 1f;

    [SerializeField]
    float maxTransferAmount = 200f;

    [SerializeField]
    [Tooltip("How quickly the joystick changes the transfer amount (units per second).")]
    float adjustmentSpeed = 30f;

    [SerializeField]
    [Range(0f, 0.5f)]
    float joystickDeadzone = 0.2f;

    [SerializeField]
    [Tooltip("If true, holding the joystick will accelerate the adjustment over time.")]
    bool enableHoldAcceleration = true;

    [SerializeField]
    [Tooltip("Rate at which the adjustment multiplier grows per second while holding the joystick.")]
    float accelerationRate = 0.5f;

    [SerializeField]
    [Tooltip("Maximum multiplier applied to adjustment speed when holding the joystick.")]
    float maxAdjustmentMultiplier = 4f;

    float m_HoldTime;

    [Header("Ejection")]
    [SerializeField]
    float ejectForce = 1.5f;

    [Tooltip("How long the socket waits before it can grab another tip (prevents instant re-attachment)")]
    [SerializeField]
    float socketCooldown = 0.5f;

    public PipetteState CurrentState { get; private set; } = PipetteState.Empty;
    public FluidData LoadedFluid { get; private set; }

    PipetteTip m_CurrentTip;
    LiquidContainer m_SubmergedContainer;

    void OnEnable()
    {
        if (tipSocketInteractor != null)
        {
            tipSocketInteractor.selectEntered.AddListener(OnTipAttached);
            tipSocketInteractor.selectExited.AddListener(OnTipDetached);
        }

        if (transferAction != null)
        {
            transferAction.action.Enable();
            transferAction.action.performed += OnTransferActionPerformed;
        }

        if (ejectTipAction != null)
        {
            ejectTipAction.action.Enable();
            ejectTipAction.action.performed += OnEjectActionPerformed;
        }

        if (volumeControlAction != null)
            volumeControlAction.action.Enable();
    }

    void OnDisable()
    {
        if (tipSocketInteractor != null)
        {
            tipSocketInteractor.selectEntered.RemoveListener(OnTipAttached);
            tipSocketInteractor.selectExited.RemoveListener(OnTipDetached);
        }

        if (transferAction != null)
        {
            transferAction.action.performed -= OnTransferActionPerformed;
            transferAction.action.Disable();
        }

        if (ejectTipAction != null)
        {
            ejectTipAction.action.performed -= OnEjectActionPerformed;
            ejectTipAction.action.Disable();
        }

        if (volumeControlAction != null)
            volumeControlAction.action.Disable();
    }

    void Update()
    {
        bool holdingPipette = CurrentState != PipetteState.Empty;

        // Show/hide volume UI while holding the pipette
        if (volumeText != null)
            volumeText.gameObject.SetActive(holdingPipette);

        if (!holdingPipette || volumeText == null)
            return;

        // If Ready, allow adjusting the transferAmount with the joystick
        if (CurrentState == PipetteState.Ready && volumeControlAction != null && volumeControlAction.action != null)
        {
            Vector2 stick = volumeControlAction.action.ReadValue<Vector2>();
            float input = Mathf.Abs(stick.y) > joystickDeadzone ? stick.y : 0f;
            if (Mathf.Abs(input) > 0f)
            {
                // Track how long the joystick has been held for acceleration
                m_HoldTime += Time.deltaTime;

                float multiplier = 1f;
                if (enableHoldAcceleration)
                {
                    multiplier = 1f + m_HoldTime * accelerationRate;
                    multiplier = Mathf.Clamp(multiplier, 1f, maxAdjustmentMultiplier);
                }

                transferAmount += input * adjustmentSpeed * multiplier * Time.deltaTime;
                transferAmount = Mathf.Clamp(transferAmount, minTransferAmount, maxTransferAmount);
            }
            else
            {
                m_HoldTime = 0f;
            }

            volumeText.text = $"Set Volume: {transferAmount:0.0} µL";
            return;
        }

        // If Filled, lock volume and show the amount that will be dispensed
        if (CurrentState == PipetteState.Filled)
        {
            float amt = LoadedFluid.currentVolume;
            volumeText.text = $"Dispense: {amt:0.0} µL (locked)";
            return;
        }

        // If Empty (shouldn't reach here because early return), hide text
        volumeText.text = string.Empty;
    }

    public void SetSubmergedContainer(LiquidContainer container)
    {
        m_SubmergedContainer = container;
    }

    public void EjectTip()
    {
        print("Ejecting tip");
        if (tipSocketInteractor == null || !tipSocketInteractor.hasSelection)
        {
            ResetToEmptyState();
            return;
        }

        IXRSelectInteractable selected = tipSocketInteractor.interactablesSelected.Count > 0
            ? tipSocketInteractor.interactablesSelected[0]
            : null;

        if (selected != null && tipSocketInteractor.interactionManager != null)
            tipSocketInteractor.interactionManager.SelectExit(tipSocketInteractor, selected);

        var selectedComponent = selected as Component;
        var tipRigidbody = selectedComponent != null ? selectedComponent.GetComponent<Rigidbody>() : null;

        if (tipRigidbody != null)
        {
            // Optional: Reset velocity before adding force so it shoots out cleanly
            tipRigidbody.linearVelocity = Vector3.zero;
            tipRigidbody.angularVelocity = Vector3.zero;
            tipRigidbody.AddForce(-transform.up * ejectForce, ForceMode.Impulse);
        }

        // Disable socket temporarily so it doesn't instantly grab the tip back
        StartCoroutine(TemporarilyDisableSocket());

        ResetToEmptyState();
    }

    private IEnumerator TemporarilyDisableSocket()
    {
        // socketActive turns off the socket's grabbing logic without disabling the GameObject
        tipSocketInteractor.socketActive = false;
        yield return new WaitForSeconds(socketCooldown);
        tipSocketInteractor.socketActive = true;
    }

    void OnTransferActionPerformed(InputAction.CallbackContext context)
    {
        print("clicked");
        TryTransferFluid();
    }

    void OnEjectActionPerformed(InputAction.CallbackContext context)
    {
        EjectTip();
    }

    void OnTipAttached(SelectEnterEventArgs args)
    {
        var tipComponent = (args.interactableObject as Component)?.GetComponentInChildren<PipetteTip>();
        if (tipComponent == null)
            return;

        m_CurrentTip = tipComponent;
        m_CurrentTip.RegisterController(this);
        m_SubmergedContainer = null;

        LoadedFluid = default;
        m_CurrentTip.SetFluidVisual(Color.clear, false);
        CurrentState = PipetteState.Ready;
    }

    void OnTipDetached(SelectExitEventArgs args)
    {
        var tipComponent = (args.interactableObject as Component)?.GetComponentInChildren<PipetteTip>();
        if (tipComponent != null)
            tipComponent.UnregisterController(this);

        ResetToEmptyState();
    }

    void TryTransferFluid()
    {
        if (CurrentState == PipetteState.Empty || m_CurrentTip == null)
            return;

        if (m_SubmergedContainer == null)
            m_SubmergedContainer = m_CurrentTip.CurrentSubmergedContainer;

        if (m_SubmergedContainer == null)
            return;

        if (CurrentState == PipetteState.Ready)
        {
            var pulled = m_SubmergedContainer.PullFluid(transferAmount);
            if (pulled.currentVolume <= 0f)
                return;

            LoadedFluid = pulled;
            CurrentState = PipetteState.Filled;
            m_CurrentTip.SetFluidVisual(LoadedFluid.color, true);
            Debug.Log($"Pulled {pulled.currentVolume:0.##} µL of '{pulled.fluidName}' from '{m_SubmergedContainer.gameObject.name}'. Remaining in container: {m_SubmergedContainer.CurrentFluidData.currentVolume:0.##} µL", this);
            return;
        }

        if (CurrentState == PipetteState.Filled)
        {
            float pushed = m_SubmergedContainer.PushFluid(LoadedFluid, LoadedFluid.currentVolume);
            if (pushed <= 0f)
                return;

            Debug.Log($"Pushed {pushed:0.##} µL of '{LoadedFluid.fluidName}' into '{m_SubmergedContainer.gameObject.name}'. Container now: {m_SubmergedContainer.CurrentFluidData.currentVolume:0.##} µL", this);

            LoadedFluid = default;
            CurrentState = PipetteState.Ready;
            m_CurrentTip.SetFluidVisual(Color.clear, false);
        }
    }

    void ResetToEmptyState()
    {
        if (m_CurrentTip != null)
        {
            m_CurrentTip.SetFluidVisual(Color.clear, false);
            m_CurrentTip.UnregisterController(this);
        }

        m_CurrentTip = null;
        m_SubmergedContainer = null;
        LoadedFluid = default;
        CurrentState = PipetteState.Empty;
    }
}