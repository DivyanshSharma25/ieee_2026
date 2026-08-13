using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class LiquidContainer : MonoBehaviour
{
    [SerializeField]
    protected FluidData fluidData = new FluidData("Water", new Color(0.35f, 0.6f, 1f, 1f), 100f, 100f);

    private void Start()
    {
        if (fluidData.mixture == null || fluidData.mixture.Count == 0)
        {
            if (fluidData.currentVolume > 0.0001f)
            {
                fluidData.mixture = new List<FluidCompositionEntry>
                {
                    new FluidCompositionEntry
                    {
                        fluidName = string.IsNullOrWhiteSpace(fluidData.GetDisplayName()) || string.Equals(fluidData.GetDisplayName(), "Empty", StringComparison.OrdinalIgnoreCase) ? "Liquid" : fluidData.GetDisplayName(),
                        color = fluidData.color,
                        concentration = 1f
                    }
                };
            }
            else
            {
                ResetComposition();
                return;
            }
        }

        fluidData.color = CalculateAverageColor(fluidData.mixture);
    }

    public FluidData CurrentFluidData => fluidData;

    public IReadOnlyList<FluidCompositionEntry> MixtureContents => fluidData.mixture ?? new List<FluidCompositionEntry>();

    public string GetFluidDisplayName()
    {
        return fluidData.GetDisplayName();
    }

    public float GetConcentration(string fluidName)
    {
        if (string.IsNullOrWhiteSpace(fluidName))
            return 0f;

        if (fluidData.mixture == null || fluidData.mixture.Count == 0)
            return fluidData.currentVolume > 0f ? 1f : 0f;

        foreach (var component in fluidData.mixture)
        {
            if (string.Equals(component.fluidName, fluidName, StringComparison.OrdinalIgnoreCase))
                return Mathf.Clamp01(component.concentration);
        }

        return 0f;
    }

    public virtual FluidData PullFluid(float amount)
    {
        float requested = Mathf.Max(0f, amount);
        float extracted = Mathf.Min(requested, fluidData.currentVolume);

        var sample = new FluidData(GetFluidDisplayName(), fluidData.color, extracted, extracted);
        sample.mixture = new List<FluidCompositionEntry>();

        if (extracted <= 0f)
        {
            fluidData.currentVolume = 0f;
            ResetComposition();
            return sample;
        }

        if (fluidData.mixture == null || fluidData.mixture.Count == 0)
        {
            fluidData.currentVolume = Mathf.Max(0f, fluidData.currentVolume - extracted);
            sample.color = fluidData.color;
            sample.mixture = new List<FluidCompositionEntry>
            {
                new FluidCompositionEntry
                {
                    fluidName = GetFluidDisplayName() == "Empty" ? "Liquid" : GetFluidDisplayName(),
                    color = fluidData.color,
                    concentration = 1f
                }
            };
            RecalculateComposition();
            return sample;
        }

        float remainingVolume = Mathf.Max(0f, fluidData.currentVolume - extracted);
        var remainingMixture = new List<FluidCompositionEntry>();
        var sampledMixture = new List<FluidCompositionEntry>();

        foreach (var component in fluidData.mixture)
        {
            if (component.concentration <= 0f)
                continue;

            float componentVolume = fluidData.currentVolume * component.concentration;
            float removedVolume = component.concentration * extracted;
            float remainingComponentVolume = Mathf.Max(0f, componentVolume - removedVolume);

            sampledMixture.Add(new FluidCompositionEntry
            {
                fluidName = component.fluidName,
                color = component.color,
                concentration = extracted > 0f ? removedVolume / extracted : 0f
            });

            if (remainingComponentVolume > 0f)
            {
                remainingMixture.Add(new FluidCompositionEntry
                {
                    fluidName = component.fluidName,
                    color = component.color,
                    concentration = remainingVolume > 0f ? remainingComponentVolume / remainingVolume : 0f
                });
            }
        }

        fluidData.currentVolume = remainingVolume;
        fluidData.mixture = remainingMixture;
        RecalculateComposition();

        sample.mixture = sampledMixture;
        sample.color = CalculateAverageColor(sample.mixture);
        sample.maxVolume = extracted;
        sample.currentVolume = extracted;

        return sample;
    }

    public virtual float PushFluid(FluidData incomingFluid, float amount)
    {
        if (amount <= 0f)
            return 0f;

        float freeCapacity = Mathf.Max(0f, fluidData.maxVolume - fluidData.currentVolume);
        float incomingVolume = Mathf.Max(0f, Mathf.Min(incomingFluid.currentVolume, amount));
        float accepted = Mathf.Min(freeCapacity, incomingVolume);

        if (accepted <= 0f)
            return 0f;

        var incomingMixture = incomingFluid.mixture != null && incomingFluid.mixture.Count > 0
            ? incomingFluid.mixture
            : new List<FluidCompositionEntry>
            {
                new FluidCompositionEntry
                {
                    fluidName = incomingFluid.GetDisplayName(),
                    color = incomingFluid.color,
                    concentration = 1f
                }
            };

        float oldTotal = fluidData.currentVolume;
        float newTotal = oldTotal + accepted;
        var mergedMixture = new List<FluidCompositionEntry>();

        if (fluidData.mixture != null)
        {
            foreach (var existing in fluidData.mixture)
                mergedMixture.Add(new FluidCompositionEntry
                {
                    fluidName = existing.fluidName,
                    color = existing.color,
                    concentration = existing.concentration
                });
        }

        foreach (var component in incomingMixture)
        {
            if (component.concentration <= 0f)
                continue;

            float addedVolume = accepted * component.concentration;
            bool found = false;

            for (int i = 0; i < mergedMixture.Count; i++)
            {
                var existing = mergedMixture[i];
                if (string.Equals(existing.fluidName, component.fluidName, StringComparison.OrdinalIgnoreCase))
                {
                    float existingVolume = oldTotal * existing.concentration;
                    float newVolume = existingVolume + addedVolume;
                    existing.color = component.color;
                    existing.concentration = newTotal > 0f ? newVolume / newTotal : 0f;
                    mergedMixture[i] = existing;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                mergedMixture.Add(new FluidCompositionEntry
                {
                    fluidName = component.fluidName,
                    color = component.color,
                    concentration = newTotal > 0f ? addedVolume / newTotal : 1f
                });
            }
        }

        fluidData.currentVolume = newTotal;
        fluidData.mixture = mergedMixture;
        RecalculateComposition();
        return accepted;
    }

    private void RecalculateComposition()
    {
        if (fluidData.mixture == null)
            fluidData.mixture = new List<FluidCompositionEntry>();

        for (int i = fluidData.mixture.Count - 1; i >= 0; i--)
        {
            if (fluidData.mixture[i].concentration <= 0.0001f)
            {
                fluidData.mixture.RemoveAt(i);
            }
        }

        if (fluidData.currentVolume <= 0.0001f || fluidData.mixture.Count == 0)
        {
            fluidData.mixture.Clear();
            fluidData.color = Color.clear;
            return;
        }

        float totalConcentration = 0f;
        foreach (var component in fluidData.mixture)
            totalConcentration += Mathf.Max(0f, component.concentration);

        if (totalConcentration <= 0.0001f)
        {
            fluidData.mixture.Clear();
            fluidData.color = Color.clear;
            return;
        }

        for (int i = 0; i < fluidData.mixture.Count; i++)
        {
            var component = fluidData.mixture[i];
            component.concentration = component.concentration / totalConcentration;
            fluidData.mixture[i] = component;
        }

        fluidData.color = CalculateAverageColor(fluidData.mixture);
    }

    private void ResetComposition()
    {
        fluidData.color = Color.clear;
        fluidData.mixture = new List<FluidCompositionEntry>();
    }

    private static Color CalculateAverageColor(List<FluidCompositionEntry> composition)
    {
        if (composition == null || composition.Count == 0)
            return Color.clear;

        Vector3 total = Vector3.zero;
        float totalConcentration = 0f;

        foreach (var component in composition)
        {
            if (component.concentration <= 0f)
                continue;

            totalConcentration += component.concentration;
            total += new Vector3(component.color.r, component.color.g, component.color.b) * component.concentration;
        }

        if (totalConcentration <= 0f)
            return Color.clear;

        return new Color(total.x / totalConcentration, total.y / totalConcentration, total.z / totalConcentration, 1f);
    }
}
