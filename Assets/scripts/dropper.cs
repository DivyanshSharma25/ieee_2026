using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public interface ILiquidReceiver
{
    void ReceiveFluid(FluidData fluidData);
}

public class dropper : LiquidContainer
{
    [Header("Trigger Input")]
    [SerializeField]
    private InputActionReference triggerAction;

    [SerializeField]
    private float fillAmount = 10f;

    [SerializeField]
    private bool fillWhileHeld = false;

    [Header("Drop Behavior")]
    [SerializeField]
    private float dropRayDistance = 0.3f;

    [SerializeField]
    private Vector3 bottomOffset = new Vector3(0f, -0.12f, 0f);

    [SerializeField]
    private LayerMask dropLayerMask = ~0;

    [Header("Events")]
    [SerializeField]
    private UnityEvent triggerEvent;

    private LiquidContainer currentSourceContainer;
    private bool triggerHeld;

    private void OnEnable()
    {
        if (triggerAction != null)
        {
            triggerAction.action.Enable();
            triggerAction.action.performed += OnTriggerPerformed;
            triggerAction.action.canceled += OnTriggerCanceled;
        }
    }

    private void OnDisable()
    {
        if (triggerAction != null)
        {
            triggerAction.action.performed -= OnTriggerPerformed;
            triggerAction.action.canceled -= OnTriggerCanceled;
            triggerAction.action.Disable();
        }
    }

    private void Update()
    {
        if (!fillWhileHeld || triggerHeld == false)
            return;

        TryFillFromContainer();
    }

    private void OnTriggerEnter(Collider other)
    {
        var container = other.GetComponentInParent<LiquidContainer>();
        if (container == null || container == this)
            return;

        currentSourceContainer = container;
    }

    private void OnTriggerExit(Collider other)
    {
        if (currentSourceContainer == null)
            return;

        var container = other.GetComponentInParent<LiquidContainer>();
        if (container == null || container == currentSourceContainer)
            currentSourceContainer = null;
    }

    private void OnTriggerPerformed(InputAction.CallbackContext context)
    {
        triggerHeld = true;
        triggerEvent?.Invoke();

        if (currentSourceContainer != null)
        {
            TryFillFromContainer();
            return;
        }

        TryDropLiquid();
    }

    private void OnTriggerCanceled(InputAction.CallbackContext context)
    {
        triggerHeld = false;
    }

    public void TryFillFromContainer()
    {
        if (currentSourceContainer == null)
            return;

        if (fluidData.currentVolume >= fluidData.maxVolume)
            return;

        if (currentSourceContainer.CurrentFluidData.currentVolume <= 0.0001f)
            return;

        float availableSpace = Mathf.Max(0f, fluidData.maxVolume - fluidData.currentVolume);
        float availableLiquid = Mathf.Max(0f, currentSourceContainer.CurrentFluidData.currentVolume);
        float amount = Mathf.Min(fillAmount, Mathf.Min(availableSpace, availableLiquid));

        if (amount <= 0.0001f)
            return;

        var pulled = currentSourceContainer.PullFluid(amount);
        if (pulled.currentVolume <= 0.0001f)
            return;

        float accepted = PushFluid(pulled, pulled.currentVolume);
        if (accepted <= 0.0001f)
        {
            currentSourceContainer.PushFluid(pulled, pulled.currentVolume);
        }
    }

    public void TryDropLiquid()
    {
        if (fluidData.currentVolume <= 0.0001f)
            return;

        Vector3 origin = transform.TransformPoint(bottomOffset);
        Vector3 direction = -transform.up;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, dropRayDistance, dropLayerMask, QueryTriggerInteraction.Ignore))
        {
            var targetReceiver = hit.collider.GetComponentInParent<ILiquidReceiver>();
            if (targetReceiver != null)
            {
                var fluidToSend = new FluidData(
                    this.GetFluidDisplayName(),
                    this.CurrentFluidData.color,
                    Mathf.Min(fluidData.currentVolume, fillAmount),
                    Mathf.Max(fluidData.maxVolume, fluidData.currentVolume));

                fluidToSend.mixture = new List<FluidCompositionEntry>();
                if (this.MixtureContents != null)
                {
                    foreach (var entry in this.MixtureContents)
                    {
                        fluidToSend.mixture.Add(new FluidCompositionEntry
                        {
                            fluidName = entry.fluidName,
                            color = entry.color,
                            concentration = entry.concentration
                        });
                    }
                }

                targetReceiver.ReceiveFluid(fluidToSend);
                float amountToRemove = Mathf.Min(fluidData.currentVolume, fillAmount);
                var removed = PullFluid(amountToRemove);
                if (removed.currentVolume <= 0.0001f)
                    return;

                return;
            }

            var liquidTarget = hit.collider.GetComponentInParent<LiquidContainer>();
            if (liquidTarget != null)
            {
                float amountToMove = Mathf.Min(fluidData.currentVolume, fillAmount);
                var removed = PullFluid(amountToMove);
                if (removed.currentVolume <= 0.0001f)
                    return;

                float accepted = liquidTarget.PushFluid(removed, removed.currentVolume);
                if (accepted <= 0.0001f)
                {
                    PushFluid(removed, removed.currentVolume);
                }

                return;
            }
        }

        // If no receiver is hit, simply release the fluid as a drop by reducing the container volume.
        float dropAmount = Mathf.Min(fluidData.currentVolume, fillAmount);
        PullFluid(dropAmount);
    }
}

