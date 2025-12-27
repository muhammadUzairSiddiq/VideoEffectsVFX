using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VideoVignetteEffect : MonoBehaviour
{
    public Volume globalVolume;

    Vignette vignette;

    // Store original values
    float originalIntensity;
    Color originalColor;
    Vector2 originalCenter;
    float originalSmoothness;
    bool originalRounded;
    public bool vignetteEnabled = false;

    [Header("Vignette Settings")]
    [Range(0f, 1f)]
    public float intensity = 0.5f;
    public Color color = Color.black;
    [Range(0f, 1f)]
    public Vector2 center = new Vector2(0.5f, 0.5f);
    [Range(0f, 1f)]
    public float smoothness = 0.2f;
    [Tooltip("Should the vignette be perfectly round?")]
    public bool rounded = false;

    void Start()
    {
        if (globalVolume.profile.TryGet(out vignette))
        {
            // Cache original values
            originalIntensity = vignette.intensity.value;
            originalColor = vignette.color.value;
            originalCenter = vignette.center.value;
            originalSmoothness = vignette.smoothness.value;
            originalRounded = vignette.rounded.value;

            // Ensure vignette starts OFF
            vignette.intensity.value = originalIntensity;
        }
        else
        {
            Debug.LogError("Vignette not found in Volume Profile");
        }
    }

    // 🔥 UI BUTTON CALL
    public void ToggleVignette()
    {
        vignetteEnabled = !vignetteEnabled;

        if (vignetteEnabled)
        {
            ApplyVignette();
        }
        else
        {
            RestoreVignette();
        }
    }

    void ApplyVignette()
    {
        vignette.intensity.value = intensity;
        vignette.color.value = color;
        vignette.center.value = center;
        vignette.smoothness.value = smoothness;
        vignette.rounded.value = rounded;
    }

    void RestoreVignette()
    {
        vignette.intensity.value = originalIntensity;
        vignette.color.value = originalColor;
        vignette.center.value = originalCenter;
        vignette.smoothness.value = originalSmoothness;
        vignette.rounded.value = originalRounded;
    }
}

