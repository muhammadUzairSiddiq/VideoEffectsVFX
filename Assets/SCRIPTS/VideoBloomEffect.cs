using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VideoBloomEffect : MonoBehaviour
{
    public Volume globalVolume;


   public float Intensityl;
   public float Ihreshold; 
   public float Scatter;

    Bloom bloom;

    // Store original values
    float originalIntensity;
    float originalThreshold;
    float originalScatter;

    public bool bloomEnabled = false;

    void Start()
    {
        if (globalVolume.profile.TryGet(out bloom))
        {
            // Cache original values
            originalIntensity = bloom.intensity.value;
            originalThreshold = bloom.threshold.value;
            originalScatter = bloom.scatter.value;

            // Ensure bloom starts OFF
            bloom.intensity.value = originalIntensity;
        }
        else
        {
            Debug.LogError("Bloom not found in Volume Profile");
        }
    }

    // 🔥 UI BUTTON CALL
    public void ToggleBloom()
    {
        bloomEnabled = !bloomEnabled;

        if (bloomEnabled)
        {
            ApplyStrongBloom();
        }
        else
        {
            RestoreBloom();
        }
    }

    void ApplyStrongBloom()
    {
        bloom.intensity.value = Intensityl;     // HIGH intensity (viral)
        bloom.threshold.value = Ihreshold;   // Lower threshold = more glow
        bloom.scatter.value = Scatter;    // Softer bloom spread
    }


    void RestoreBloom()
    {
        bloom.intensity.value = originalIntensity;
        bloom.threshold.value = originalThreshold;
        bloom.scatter.value = originalScatter;
    }
}
