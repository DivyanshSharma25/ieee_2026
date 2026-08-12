using UnityEngine;

// This ensures the script won't crash if you forget to add a MeshFilter or Renderer
[RequireComponent(typeof(Renderer), typeof(MeshFilter))]
public class LiquidDisplayer : MonoBehaviour
{
    [Header("Settings")]
    public Color liquidColor = Color.blue;

    private Renderer meshRenderer;
    private Material liquidMaterial;
    private LiquidContainer container;

    void Start()
    {
        // 1. Get our components
        meshRenderer = GetComponent<Renderer>();
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        // 2. Create an instance of the material so we don't modify the project asset
        liquidMaterial = meshRenderer.material;

        // 3. Automatically calculate the exact local Y bounds of the mesh
        float minY = meshFilter.mesh.bounds.min.y;
        float maxY = meshFilter.mesh.bounds.max.y;

        // 4. Send those bounds to the shader automatically
        liquidMaterial.SetFloat("_MinY", minY);
        liquidMaterial.SetFloat("_MaxY", maxY);

        container = GetComponentInParent<LiquidContainer>();
    }

    void Update()
    {
        if (container == null)
            container = GetComponentInParent<LiquidContainer>();

        if (container == null)
            return;

        FluidData data = container.CurrentFluidData;
        float fillRatio = data.maxVolume > 0f ? Mathf.Clamp01(data.currentVolume / data.maxVolume) : 0f;

        // Update the dynamic properties every frame based on the real fluid level
        liquidMaterial.SetFloat("_FillAmount", fillRatio);
        liquidMaterial.SetColor("_LiquidColor", data.color.a > 0.01f ? data.color : liquidColor);
    }
}