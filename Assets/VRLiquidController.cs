using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class VR_LiquidLevel : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Renderer liquidRenderer;
    [SerializeField] private LiquidContainer targetContainer;
    [Range(0f, 1f)] public float fillLevel = 0.5f;

    [Header("Debug Bounds Readout")]
    [SerializeField] private float calculatedMinY;
    [SerializeField] private float calculatedMaxY;
    [SerializeField] private float calculatedHeight;

    private MaterialPropertyBlock propBlock;
    private float baseAlpha = 1f;

    // Shader Property IDs
    private static readonly int FillLevelID = Shader.PropertyToID("_FillLevel");
    private static readonly int HeightID = Shader.PropertyToID("_ContainerHeight");
    private static readonly int OffsetID = Shader.PropertyToID("_ContainerOffset");

    private void OnEnable()
    {
        if (targetContainer == null)
            targetContainer = GetComponentInParent<LiquidContainer>();

        if (liquidRenderer == null)
            liquidRenderer = GetComponent<Renderer>();

        if (liquidRenderer != null)
            baseAlpha = liquidRenderer.sharedMaterial != null ? liquidRenderer.sharedMaterial.GetColor("_BaseColor").a : 1f;

        CalculateAndApplyBounds();
    }

    private void OnValidate()
    {
        if (targetContainer == null)
            targetContainer = GetComponentInParent<LiquidContainer>();

        CalculateAndApplyBounds();
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            UpdateFromContainer();
        }
    }

    [ContextMenu("Recalculate Mesh Bounds")]
    public void CalculateAndApplyBounds()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;
        if (liquidRenderer == null) liquidRenderer = GetComponent<Renderer>();

        Bounds localBounds = mf.sharedMesh.bounds;
        calculatedMinY = localBounds.min.y;
        calculatedMaxY = localBounds.max.y;
        calculatedHeight = calculatedMaxY - calculatedMinY;

        if (calculatedHeight <= 0.0001f) calculatedHeight = 1.0f;

        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        liquidRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(OffsetID, calculatedMinY);
        propBlock.SetFloat(HeightID, calculatedHeight);
        propBlock.SetFloat(FillLevelID, fillLevel);
        liquidRenderer.SetPropertyBlock(propBlock);
    }

    private void UpdateFromContainer()
    {
        if (liquidRenderer == null) liquidRenderer = GetComponent<Renderer>();
        if (liquidRenderer == null)
            return;

        if (targetContainer == null)
            targetContainer = GetComponentInParent<LiquidContainer>();

        if (propBlock == null)
            propBlock = new MaterialPropertyBlock();

        var data = targetContainer != null ? targetContainer.CurrentFluidData : default;
        float nextFill = targetContainer != null && data.maxVolume > 0f
            ? Mathf.Clamp01(data.currentVolume / data.maxVolume)
            : fillLevel;

        liquidRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(FillLevelID, nextFill);

        if (targetContainer != null && targetContainer.UpdateMaterialColor)
        {
            Color liquidColor = new Color(
                data.color.r,
                data.color.g,
                data.color.b,
                baseAlpha);

            propBlock.SetColor("_BaseColor", liquidColor);
        }

        liquidRenderer.SetPropertyBlock(propBlock);
    }
}