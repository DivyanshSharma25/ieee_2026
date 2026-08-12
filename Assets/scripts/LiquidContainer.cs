using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class LiquidContainer : MonoBehaviour
{
    [SerializeField]
    protected FluidData fluidData = new FluidData("Water", new Color(0.35f, 0.6f, 1f, 1f), 100f, 100f);

    public FluidData CurrentFluidData => fluidData;

    public IReadOnlyList<FluidCompositionEntry> MixtureContents => fluidData.mixture ?? new List<FluidCompositionEntry>();

    public float GetConcentration(string fluidName)
    {
        if (string.IsNullOrWhiteSpace(fluidName))
            return 0f;

        if (fluidData.mixture == null || fluidData.mixture.Count == 0)
            return fluidData.currentVolume > 0f && string.Equals(fluidData.fluidName, fluidName, StringComparison.OrdinalIgnoreCase) ? 1f : 0f;

        float totalVolume = 0f;
        foreach (var component in fluidData.mixture)
            totalVolume += Mathf.Max(0f, component.volume);

        if (totalVolume <= 0f)
            return 0f;

        foreach (var component in fluidData.mixture)
        {
            if (string.Equals(component.fluidName, fluidName, StringComparison.OrdinalIgnoreCase))
                return component.volume / totalVolume;
        }

        return 0f;
    }

    public virtual FluidData PullFluid(float amount)
    {
        float requested = Mathf.Max(0f, amount);
        float extracted = Mathf.Min(requested, fluidData.currentVolume);

        var sample = new FluidData(fluidData.fluidName, fluidData.color, extracted, extracted);
        sample.mixture = new List<FluidCompositionEntry>();

        if (extracted <= 0f)
        {
            fluidData.currentVolume = 0f;
            ResetComposition();
            return sample;
        }

        float totalVolume = 0f;
        if (fluidData.mixture != null)
        {
            foreach (var component in fluidData.mixture)
                totalVolume += Mathf.Max(0f, component.volume);
        }

        if (totalVolume <= 0f || fluidData.mixture == null || fluidData.mixture.Count == 0)
        {
            fluidData.currentVolume = Mathf.Max(0f, fluidData.currentVolume - extracted);
            sample.fluidName = fluidData.fluidName;
            sample.color = fluidData.color;
            sample.mixture = new List<FluidCompositionEntry>
            {
                new FluidCompositionEntry
                {
                    fluidName = fluidData.fluidName,
                    color = fluidData.color,
                    volume = extracted,
                    concentration = 1f
                }
            };
            RecalculateComposition();
            return sample;
        }

        for (int i = fluidData.mixture.Count - 1; i >= 0; i--)
        {
            var component = fluidData.mixture[i];
            if (component.volume <= 0f)
            {
                fluidData.mixture.RemoveAt(i);
                continue;
            }

            float removedVolume = (component.volume / totalVolume) * extracted;
            if (removedVolume <= 0f)
                continue;

            sample.mixture.Add(new FluidCompositionEntry
            {
                fluidName = component.fluidName,
                color = component.color,
                volume = removedVolume,
                concentration = extracted > 0f ? removedVolume / extracted : 0f
            });

            component.volume -= removedVolume;
            if (component.volume <= 0.0001f)
                fluidData.mixture.RemoveAt(i);
            else
                fluidData.mixture[i] = component;
        }

        fluidData.currentVolume = Mathf.Max(0f, fluidData.currentVolume - extracted);
        RecalculateComposition();

        sample.fluidName = sample.mixture.Count > 1 ? "Mixture" : (sample.mixture.Count == 1 ? sample.mixture[0].fluidName : fluidData.fluidName);
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
                    fluidName = incomingFluid.fluidName,
                    color = incomingFluid.color,
                    volume = incomingVolume,
                    concentration = 1f
                }
            };

        float incomingTotal = 0f;
        foreach (var component in incomingMixture)
            incomingTotal += Mathf.Max(0f, component.volume);

        if (incomingTotal <= 0f)
            incomingTotal = incomingVolume;

        if (fluidData.currentVolume <= 0.0001f)
        {
            fluidData.mixture = new List<FluidCompositionEntry>();
            fluidData.fluidName = incomingFluid.fluidName;
            fluidData.color = incomingFluid.color;
        }

        foreach (var component in incomingMixture)
        {
            float componentVolume = incomingTotal > 0f ? (component.volume / incomingTotal) * accepted : 0f;
            if (componentVolume <= 0f)
                continue;

            if (fluidData.mixture == null)
                fluidData.mixture = new List<FluidCompositionEntry>();

            bool found = false;
            for (int i = 0; i < fluidData.mixture.Count; i++)
            {
                var existing = fluidData.mixture[i];
                if (string.Equals(existing.fluidName, component.fluidName, StringComparison.OrdinalIgnoreCase))
                {
                    existing.volume += componentVolume;
                    existing.color = component.color;
                    fluidData.mixture[i] = existing;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                fluidData.mixture.Add(new FluidCompositionEntry
                {
                    fluidName = component.fluidName,
                    color = component.color,
                    volume = componentVolume,
                    concentration = 0f
                });
            }
        }

        fluidData.currentVolume += accepted;
        RecalculateComposition();
        return accepted;
    }

    private void RecalculateComposition()
    {
        if (fluidData.mixture == null)
            fluidData.mixture = new List<FluidCompositionEntry>();

        float totalVolume = 0f;
        for (int i = fluidData.mixture.Count - 1; i >= 0; i--)
        {
            if (fluidData.mixture[i].volume <= 0.0001f)
            {
                fluidData.mixture.RemoveAt(i);
                continue;
            }

            totalVolume += fluidData.mixture[i].volume;
        }

        if (totalVolume <= 0.0001f)
        {
            fluidData.mixture.Clear();
            fluidData.fluidName = "Empty";
            fluidData.color = Color.clear;
            return;
        }

        for (int i = 0; i < fluidData.mixture.Count; i++)
        {
            var component = fluidData.mixture[i];
            component.concentration = component.volume / totalVolume;
            fluidData.mixture[i] = component;
        }

        fluidData.color = CalculateAverageColor(fluidData.mixture);
        fluidData.fluidName = fluidData.mixture.Count == 1 ? fluidData.mixture[0].fluidName : "Mixture";
    }

    private void ResetComposition()
    {
        fluidData.fluidName = "Empty";
        fluidData.color = Color.clear;
        fluidData.mixture = new List<FluidCompositionEntry>();
    }

    private static Color CalculateAverageColor(List<FluidCompositionEntry> composition)
    {
        if (composition == null || composition.Count == 0)
            return Color.clear;

        Vector4 total = Vector4.zero;
        float totalVolume = 0f;

        foreach (var component in composition)
        {
            if (component.volume <= 0f)
                continue;

            totalVolume += component.volume;
            total += new Vector4(component.color.r, component.color.g, component.color.b, component.color.a) * component.volume;
        }

        if (totalVolume <= 0f)
            return Color.clear;

        return new Color(total.x / totalVolume, total.y / totalVolume, total.z / totalVolume, total.w / totalVolume);
    }
}
