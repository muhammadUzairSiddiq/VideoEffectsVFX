using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VideoGlitchEffect : MonoBehaviour
{
    public Volume globalVolume;

    ChromaticAberration chroma;

    float originalIntensity;
    public bool glitchEnabled = false;

    void Start()
    {
        if (globalVolume.profile.TryGet(out chroma))
        {
            originalIntensity = chroma.intensity.value;
            chroma.intensity.value = originalIntensity;
        }
        else
        {
            Debug.LogError("Chromatic Aberration not found");
        }
    }

    // 🔥 Button call
    public void ToggleGlitch()
    {
        glitchEnabled = !glitchEnabled;

        if (glitchEnabled)
            ApplyGlitch();
        else
            Restore();
    }

    void ApplyGlitch()
    {
        chroma.intensity.value = 1f; // STRONG RGB split
    }

    void Restore()
    {
        chroma.intensity.value = originalIntensity;
    }
}
