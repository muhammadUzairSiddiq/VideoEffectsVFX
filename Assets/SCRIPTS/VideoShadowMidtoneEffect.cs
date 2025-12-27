using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VideoShadowMidtoneEffect : MonoBehaviour
{
    public Volume globalVolume;

    ShadowsMidtonesHighlights shadowsMidtonesHighlights;

    // Store original values
    Vector4 originalShadows;
    Vector4 originalMidtones;
    Vector4 originalHighlights;
    public bool effectEnabled = false;

    [Header("Shadows Color Wheel (Ring Values)")]
    [Range(0f, 2f)]
    [Tooltip("Red channel for shadows")]
    public float shadowsR = 1f;
    [Range(0f, 2f)]
    [Tooltip("Green channel for shadows")]
    public float shadowsG = 1f;
    [Range(0f, 2f)]
    [Tooltip("Blue channel for shadows")]
    public float shadowsB = 1f;
    [Range(-1f, 1f)]
    [Tooltip("Intensity/offset for shadows")]
    public float shadowsIntensity = 0f;

    [Header("Midtones Color Wheel (Ring Values)")]
    [Range(0f, 2f)]
    [Tooltip("Red channel for midtones")]
    public float midtonesR = 1f;
    [Range(0f, 2f)]
    [Tooltip("Green channel for midtones")]
    public float midtonesG = 1f;
    [Range(0f, 2f)]
    [Tooltip("Blue channel for midtones")]
    public float midtonesB = 1f;
    [Range(-1f, 1f)]
    [Tooltip("Intensity/offset for midtones")]
    public float midtonesIntensity = 0f;

    [Header("Highlights Color Wheel (Ring Values)")]
    [Range(0f, 2f)]
    [Tooltip("Red channel for highlights")]
    public float highlightsR = 1f;
    [Range(0f, 2f)]
    [Tooltip("Green channel for highlights")]
    public float highlightsG = 1f;
    [Range(0f, 2f)]
    [Tooltip("Blue channel for highlights")]
    public float highlightsB = 1f;
    [Range(-1f, 1f)]
    [Tooltip("Intensity/offset for highlights")]
    public float highlightsIntensity = 0f;

    void Start()
    {
        if (globalVolume.profile.TryGet(out shadowsMidtonesHighlights))
        {
            // Cache original values
            originalShadows = shadowsMidtonesHighlights.shadows.value;
            originalMidtones = shadowsMidtonesHighlights.midtones.value;
            originalHighlights = shadowsMidtonesHighlights.highlights.value;

            // Ensure effect starts OFF
            shadowsMidtonesHighlights.shadows.value = originalShadows;
        }
        else
        {
            Debug.LogWarning("VideoShadowMidtoneEffect: Shadows/Midtones/Highlights not found in Volume Profile. Effect disabled.");
            enabled = false; // Disable script if effect not available
        }
    }

    // 🔥 UI BUTTON CALL
    public void ToggleShadowMidtone()
    {
        effectEnabled = !effectEnabled;

        if (effectEnabled)
        {
            ApplyEffect();
        }
        else
        {
            RestoreEffect();
        }
    }

    void ApplyEffect()
    {
        // Apply shadows color wheel values (RGB + intensity)
        shadowsMidtonesHighlights.shadows.value = new Vector4(shadowsR, shadowsG, shadowsB, shadowsIntensity);
        
        // Apply midtones color wheel values (RGB + intensity)
        shadowsMidtonesHighlights.midtones.value = new Vector4(midtonesR, midtonesG, midtonesB, midtonesIntensity);
        
        // Apply highlights color wheel values (RGB + intensity)
        shadowsMidtonesHighlights.highlights.value = new Vector4(highlightsR, highlightsG, highlightsB, highlightsIntensity);
    }

    void RestoreEffect()
    {
        shadowsMidtonesHighlights.shadows.value = originalShadows;
        shadowsMidtonesHighlights.midtones.value = originalMidtones;
        shadowsMidtonesHighlights.highlights.value = originalHighlights;
    }
}

