using UnityEngine;

public class burner : MonoBehaviour
{
    [Header("Burner")]
    [SerializeField]
    private ParticleSystem flameParticleSystem;

    [SerializeField]
    private bool isOn;

    private void Awake()
    {
        if (flameParticleSystem == null)
            flameParticleSystem = GetComponentInChildren<ParticleSystem>();

        SetBurnerState(isOn);
    }

    public void ToggleBurner()
    {
        SetBurnerState(!isOn);
    }

    public void SetBurnerState(bool enabled)
    {
        isOn = enabled;

        var particleSystems = GetComponentsInChildren<ParticleSystem>(true);

        foreach (var particleSystem in particleSystems)
        {
            var emission = particleSystem.emission;
            emission.enabled = isOn;

            if (isOn)
                particleSystem.Play();
            else
                particleSystem.Stop();
        }
    }
}

