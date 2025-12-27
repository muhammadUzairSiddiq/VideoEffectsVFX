using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VideoLensDistortionEffect : MonoBehaviour
{
    public Volume globalVolume;

    LensDistortion lensDistortion;

    // Store original values
    float originalIntensity;
    float originalXMultiplier;
    float originalYMultiplier;
    Vector2 originalCenter;
    float originalScale;
    public bool distortionEnabled = false;

    [Header("Lens Distortion Settings (performance-friendly defaults)")]
    [Range(-1f, 1f)]
    [Tooltip("Distortion intensity (negative = barrel, positive = pincushion)")]
    public float intensity = -0.35f; // lighter distortion to reduce cost
    [Range(0f, 1f)]
    [Tooltip("X axis multiplier")]
    public float xMultiplier = 1f;   // keep symmetric for cheaper warp
    [Range(0f, 1f)]
    [Tooltip("Y axis multiplier")]
    public float yMultiplier = 1f;   // keep symmetric for cheaper warp
    [Tooltip("Distortion center point")]
    public Vector2 center = new Vector2(0.5f, 0.5f);
    [Range(0.01f, 5f)]
    [Tooltip("Scale to compensate for distortion")]
    public float scale = 1.05f;      // slight compensation, avoid heavy zoom-out

    void Start()
    {
        if (globalVolume.profile.TryGet(out lensDistortion))
        {
            // Cache original values
            originalIntensity = lensDistortion.intensity.value;
            originalXMultiplier = lensDistortion.xMultiplier.value;
            originalYMultiplier = lensDistortion.yMultiplier.value;
            originalCenter = lensDistortion.center.value;
            originalScale = lensDistortion.scale.value;

            // Ensure distortion starts OFF
            lensDistortion.intensity.value = originalIntensity;
        }
        else
        {
            Debug.LogError("Lens Distortion not found in Volume Profile");
        }
    }

    // 🔥 UI BUTTON CALL
    public void ToggleLensDistortion()
    {
        distortionEnabled = !distortionEnabled;

        if (distortionEnabled)
        {
            ApplyDistortion();
        }
        else
        {
            RestoreDistortion();
        }
    }

    void ApplyDistortion()
    {
        lensDistortion.intensity.value = intensity;
        lensDistortion.xMultiplier.value = xMultiplier;
        lensDistortion.yMultiplier.value = yMultiplier;
        lensDistortion.center.value = center;
        lensDistortion.scale.value = scale;
    }

    void RestoreDistortion()
    {
        lensDistortion.intensity.value = originalIntensity;
        lensDistortion.xMultiplier.value = originalXMultiplier;
        lensDistortion.yMultiplier.value = originalYMultiplier;
        lensDistortion.center.value = originalCenter;
        lensDistortion.scale.value = originalScale;
    }
}

