using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FluidCompositionEntry
{
    public string fluidName;
    public Color color;
    public float concentration;
}

[Serializable]
public struct FluidData
{
    public Color color;
    public float currentVolume;
    public float maxVolume;
    public List<FluidCompositionEntry> mixture;

    public FluidData(string fluidName, Color color, float currentVolume, float maxVolume)
    {
        this.color = color;
        this.currentVolume = Mathf.Max(0f, currentVolume);
        this.maxVolume = Mathf.Max(0f, maxVolume);
        this.mixture = new List<FluidCompositionEntry>();

        if (this.currentVolume > 0f)
        {
            this.mixture.Add(new FluidCompositionEntry
            {
                fluidName = string.IsNullOrWhiteSpace(fluidName) ? "Liquid" : fluidName,
                color = color,
                concentration = 1f
            });
        }
    }

    public string GetDisplayName()
    {
        if (mixture == null || mixture.Count == 0)
            return "Empty";

        if (mixture.Count == 1)
            return string.IsNullOrWhiteSpace(mixture[0].fluidName) ? "Liquid" : mixture[0].fluidName;

        return "Mixture";
    }

    public FluidData WithVolume(float volume)
    {
        var data = new FluidData(GetDisplayName(), color, volume, maxVolume);
        if (mixture == null || mixture.Count == 0)
            return data;

        data.mixture = new List<FluidCompositionEntry>(mixture);
        float totalConcentration = 0f;
        foreach (var entry in data.mixture)
            totalConcentration += Mathf.Max(0f, entry.concentration);

        if (totalConcentration <= 0f)
            return data;

        for (int i = 0; i < data.mixture.Count; i++)
        {
            var entry = data.mixture[i];
            entry.concentration = entry.concentration / totalConcentration;
            data.mixture[i] = entry;
        }

        return data;
    }
}
