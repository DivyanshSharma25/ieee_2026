using UnityEngine;

public class PipetteTip : MonoBehaviour
{
    [Header("Nozzle")]
    [SerializeField]
    [Tooltip("Assign the small trigger collider at the bottom of the tip.")]
    Collider nozzleTrigger;

    [Header("Visual")]
    [SerializeField]
    Renderer fluidRenderer;

    [SerializeField]
    [Tooltip("Color property used by your tip material/shader.")]
    string colorProperty = "_BaseColor";

    [SerializeField]
    Color emptyColor = new Color(0f, 0f, 0f, 0f);

    MaterialPropertyBlock m_PropertyBlock;
    PipetteController m_Controller;

    public LiquidContainer CurrentSubmergedContainer { get; private set; }

    void Awake()
    {
        if (nozzleTrigger == null)
            nozzleTrigger = GetComponent<Collider>();

        if (nozzleTrigger != null && !nozzleTrigger.isTrigger)
        {
            nozzleTrigger.isTrigger = true;
            Debug.LogWarning("Nozzle trigger collider was not set to IsTrigger. It has been forced to true.", this);
        }

        m_PropertyBlock = new MaterialPropertyBlock();
        SetFluidVisual(emptyColor, false);
    }

    public void RegisterController(PipetteController controller)
    {
        m_Controller = controller;
    }

    public void UnregisterController(PipetteController controller)
    {
        if (m_Controller == controller)
            m_Controller = null;
    }

    public void SetFluidVisual(Color fluidColor, bool hasFluid)
    {
        if (fluidRenderer == null)
            return;

        fluidRenderer.GetPropertyBlock(m_PropertyBlock);
        m_PropertyBlock.SetColor(colorProperty, hasFluid ? fluidColor : emptyColor);
        fluidRenderer.SetPropertyBlock(m_PropertyBlock);
    }

    void OnTriggerEnter(Collider other)
    {
        UpdateContainerContact(other);
    }

    void OnTriggerStay(Collider other)
    {
        UpdateContainerContact(other);
    }

    void OnTriggerExit(Collider other)
    {
        if (CurrentSubmergedContainer == null)
            return;

        var exitedContainer = other.GetComponentInParent<LiquidContainer>();
        if (exitedContainer != null && exitedContainer == CurrentSubmergedContainer)
        {
            Debug.Log($"Pipette tip exited container '{exitedContainer.gameObject.name}'", this);
            CurrentSubmergedContainer = null;
            m_Controller?.SetSubmergedContainer(null);
        }
    }

    void UpdateContainerContact(Collider other)
    {
        var container = other.GetComponentInParent<LiquidContainer>();
        if (container == null)
            return;

        if (CurrentSubmergedContainer != container)
        {
            CurrentSubmergedContainer = container;
            m_Controller?.SetSubmergedContainer(container);
            var fd = container.CurrentFluidData;
            Debug.Log($"Pipette tip submerged in '{container.gameObject.name}' (fluid: {fd.fluidName}, volume: {fd.currentVolume} / {fd.maxVolume})", this);
        }
    }
}
