using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FluidCompositionEntry
{
    public string fluidName;
    public Color color;
    public float volume;
    public float concentration;
}

[Serializable]
public struct FluidData
{
    public string fluidName;
    public Color color;
    public float currentVolume;
    public float maxVolume;
    public List<FluidCompositionEntry> mixture;

    public FluidData(string fluidName, Color color, float currentVolume, float maxVolume)
    {
        this.fluidName = fluidName;
        this.color = color;
        this.currentVolume = Mathf.Max(0f, currentVolume);
        this.maxVolume = Mathf.Max(0f, maxVolume);
        this.mixture = new List<FluidCompositionEntry>();

        if (this.currentVolume > 0f)
        {
            this.mixture.Add(new FluidCompositionEntry
            {
                fluidName = fluidName,
                color = color,
                volume = this.currentVolume,
                concentration = 1f
            });
        }
    }

    public FluidData WithVolume(float volume)
    {
        var data = new FluidData(fluidName, color, volume, maxVolume);
        if (mixture != null)
        {
            data.mixture = new List<FluidCompositionEntry>(mixture);
            for (int i = 0; i < data.mixture.Count; i++)
            {
                data.mixture[i].volume = volume > 0f && currentVolume > 0f
                    ? (data.mixture[i].volume / currentVolume) * volume
                    : 0f;
                data.mixture[i].concentration = volume > 0f ? data.mixture[i].volume / volume : 0f;
            }
        }
        return data;
    }
}
