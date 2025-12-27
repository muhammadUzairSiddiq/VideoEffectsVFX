using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Controller for randomizing effect parameters within their preset ranges
/// Each effect has its own parameter ranges that can be randomized
/// </summary>
public class RandomizeButtonController : MonoBehaviour
{
    [Header("UI References")]
    public Button randomizeButton;
    [Tooltip("Text component on randomize button (optional - for visual feedback)")]
    public Component randomizeButtonText;
    
    [Header("Effect References")]
    public BeatEffectController beatEffectController;
    public VideoZoomEffect zoomEffect;
    public VideoBloomEffect bloomEffect;
    public VideoGlitchEffect glitchEffect;
    public VideoMaterialRGBEffect materialRGBEffect;
    public CameraShakeEffect shakeEffect;
    public VideoVignetteEffect vignetteEffect;
    public VideoLensDistortionEffect lensDistortionEffect;
    public CameraRotateEffect rotateEffect;
    public MaterialColorEffect materialColorEffect;
    
    [Header("Randomization Ranges")]
    [Tooltip("Minimum zoom strength multiplier")]
    [Range(0.5f, 2f)]
    public float minZoomMultiplier = 0.7f;
    [Tooltip("Maximum zoom strength multiplier")]
    [Range(0.5f, 2f)]
    public float maxZoomMultiplier = 1.3f;
    
    [Tooltip("Minimum shake intensity multiplier")]
    [Range(0.5f, 2f)]
    public float minShakeMultiplier = 0.8f;
    [Tooltip("Maximum shake intensity multiplier")]
    [Range(0.5f, 2f)]
    public float maxShakeMultiplier = 1.5f;
    
    [Tooltip("Minimum RGB intensity multiplier")]
    [Range(0.5f, 2f)]
    public float minRGBMultiplier = 0.6f;
    [Tooltip("Maximum RGB intensity multiplier")]
    [Range(0.5f, 2f)]
    public float maxRGBMultiplier = 1.4f;
    
    [Tooltip("Minimum bloom intensity multiplier")]
    [Range(0.5f, 2f)]
    public float minBloomMultiplier = 0.7f;
    [Tooltip("Maximum bloom intensity multiplier")]
    [Range(0.5f, 2f)]
    public float maxBloomMultiplier = 1.5f;
    
    [Tooltip("Minimum vignette intensity multiplier")]
    [Range(0.5f, 2f)]
    public float minVignetteMultiplier = 0.8f;
    [Tooltip("Maximum vignette intensity multiplier")]
    [Range(0.5f, 2f)]
    public float maxVignetteMultiplier = 1.3f;
    
    [Tooltip("Minimum lens distortion intensity multiplier")]
    [Range(0.5f, 2f)]
    public float minLensDistortionMultiplier = 0.7f;
    [Tooltip("Maximum lens distortion intensity multiplier")]
    [Range(0.5f, 2f)]
    public float maxLensDistortionMultiplier = 1.4f;
    
    // Store original values
    private float originalZoomStrength;
    private float originalShakeIntensity;
    private float originalShakeSpeed;
    private float originalRGBIntensity;
    private float originalRGBSpeed;
    private float originalBloomIntensity;
    private float originalVignetteIntensity;
    private float originalLensDistortionIntensity;
    
    void Start()
    {
        // Auto-find components if not assigned
        if (beatEffectController == null)
            beatEffectController = FindFirstObjectByType<BeatEffectController>();
        
        if (zoomEffect == null)
            zoomEffect = FindFirstObjectByType<VideoZoomEffect>();
        if (shakeEffect == null)
            shakeEffect = FindFirstObjectByType<CameraShakeEffect>();
        if (materialRGBEffect == null)
            materialRGBEffect = FindFirstObjectByType<VideoMaterialRGBEffect>();
        if (bloomEffect == null)
            bloomEffect = FindFirstObjectByType<VideoBloomEffect>();
        if (vignetteEffect == null)
            vignetteEffect = FindFirstObjectByType<VideoVignetteEffect>();
        if (lensDistortionEffect == null)
            lensDistortionEffect = FindFirstObjectByType<VideoLensDistortionEffect>();
        if (rotateEffect == null)
            rotateEffect = FindFirstObjectByType<CameraRotateEffect>();
        if (materialColorEffect == null)
            materialColorEffect = FindFirstObjectByType<MaterialColorEffect>();
        
        // Store original values
        StoreOriginalValues();
        
        // Setup button
        if (randomizeButton != null)
        {
            randomizeButton.onClick.AddListener(OnRandomizeButtonClicked);
            // Disable button initially - will be enabled when video starts
            randomizeButton.interactable = false;
        }
        
        // Auto-find button text if not assigned
        if (randomizeButtonText == null && randomizeButton != null)
        {
            Text text = randomizeButton.GetComponentInChildren<Text>();
            if (text != null)
            {
                randomizeButtonText = text;
            }
            else
            {
                TMPro.TextMeshProUGUI tmpText = randomizeButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmpText != null)
                {
                    randomizeButtonText = tmpText;
                }
            }
        }
    }
    
    // Store original effect values (called before applying preset)
    public void StoreOriginalValues()
    {
        if (zoomEffect != null)
            originalZoomStrength = zoomEffect.zoomStrength;
        if (shakeEffect != null)
        {
            originalShakeIntensity = shakeEffect.intensity;
            originalShakeSpeed = shakeEffect.speed;
        }
        if (materialRGBEffect != null)
        {
            originalRGBIntensity = materialRGBEffect.rgbIntensity;
            originalRGBSpeed = materialRGBEffect.rgbSpeed;
        }
        if (bloomEffect != null && bloomEffect.globalVolume != null)
        {
            if (bloomEffect.globalVolume.profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom))
            {
                originalBloomIntensity = bloom.intensity.value;
            }
        }
        if (vignetteEffect != null && vignetteEffect.globalVolume != null)
        {
            if (vignetteEffect.globalVolume.profile.TryGet<UnityEngine.Rendering.Universal.Vignette>(out var vignette))
            {
                originalVignetteIntensity = vignette.intensity.value;
            }
        }
        if (lensDistortionEffect != null && lensDistortionEffect.globalVolume != null)
        {
            if (lensDistortionEffect.globalVolume.profile.TryGet<UnityEngine.Rendering.Universal.LensDistortion>(out var lensDist))
            {
                originalLensDistortionIntensity = lensDist.intensity.value;
            }
        }
    }
    
    // Enable randomize button (called when video starts)
    public void EnableRandomizeButton()
    {
        if (randomizeButton != null)
        {
            randomizeButton.interactable = true;
        }
    }
    
    // Disable randomize button (called when video ends)
    public void DisableRandomizeButton()
    {
        if (randomizeButton != null)
        {
            randomizeButton.interactable = false;
        }
    }
    
    [Header("Panel Controller")]
    [Tooltip("Reference to EffectPanelController (auto-found if not assigned)")]
    public EffectPanelController effectPanelController;
    
    public void OnRandomizeButtonClicked()
    {
        RandomizeAllEffects();
        // Visual feedback
        StartCoroutine(ShowRandomizeFeedback());
        
        // Notify panel controller to enable all effects mode
        if (effectPanelController == null)
        {
            effectPanelController = FindFirstObjectByType<EffectPanelController>();
        }
        
        if (effectPanelController != null)
        {
            effectPanelController.OnRandomizeButtonClicked();
        }
    }
    
    IEnumerator ShowRandomizeFeedback()
    {
        // Flash button color and text
        if (randomizeButton != null)
        {
            ColorBlock colors = randomizeButton.colors;
            Color originalNormalColor = colors.normalColor;
            Color originalTextColor = Color.white;
            
            // Get original text color
            if (randomizeButtonText != null)
            {
                if (randomizeButtonText is Text uiText)
                {
                    originalTextColor = uiText.color;
                }
                else if (randomizeButtonText is TMPro.TextMeshProUGUI tmpText)
                {
                    originalTextColor = tmpText.color;
                }
            }
            
            // Flash green to show it worked
            colors.normalColor = Color.green;
            randomizeButton.colors = colors;
            
            if (randomizeButtonText != null)
            {
                if (randomizeButtonText is Text uiText)
                {
                    uiText.text = "RANDOMIZED!";
                    uiText.color = Color.white;
                }
                else if (randomizeButtonText is TMPro.TextMeshProUGUI tmpText)
                {
                    tmpText.text = "RANDOMIZED!";
                    tmpText.color = Color.white;
                }
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // Restore original
            colors.normalColor = originalNormalColor;
            randomizeButton.colors = colors;
            
            if (randomizeButtonText != null)
            {
                if (randomizeButtonText is Text uiText)
                {
                    uiText.text = "RANDOMIZE";
                    uiText.color = originalTextColor;
                }
                else if (randomizeButtonText is TMPro.TextMeshProUGUI tmpText)
                {
                    tmpText.text = "RANDOMIZE";
                    tmpText.color = originalTextColor;
                }
            }
        }
    }
    
    public void RandomizeAllEffects()
    {
        // Randomize Zoom
        if (zoomEffect != null)
        {
            float multiplier = Random.Range(minZoomMultiplier, maxZoomMultiplier);
            zoomEffect.zoomStrength = originalZoomStrength * multiplier;
            zoomEffect.zoomSpeed = Random.Range(2f, 5f); // Randomize speed too
        }
        
        // Randomize Shake
        if (shakeEffect != null)
        {
            float multiplier = Random.Range(minShakeMultiplier, maxShakeMultiplier);
            shakeEffect.intensity = originalShakeIntensity * multiplier;
            shakeEffect.speed = Random.Range(20f, 40f);
        }
        
        // Randomize Material RGB
        if (materialRGBEffect != null)
        {
            float multiplier = Random.Range(minRGBMultiplier, maxRGBMultiplier);
            materialRGBEffect.rgbIntensity = Mathf.Clamp01(originalRGBIntensity * multiplier);
            materialRGBEffect.rgbSpeed = Random.Range(3f, 8f);
        }
        
        // Randomize Bloom
        if (bloomEffect != null && bloomEffect.globalVolume != null)
        {
            if (bloomEffect.globalVolume.profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom))
            {
                float multiplier = Random.Range(minBloomMultiplier, maxBloomMultiplier);
                bloom.intensity.value = originalBloomIntensity * multiplier;
                bloom.threshold.value = Random.Range(0.8f, 1.2f);
            }
        }
        
        // Randomize Vignette
        if (vignetteEffect != null && vignetteEffect.globalVolume != null)
        {
            if (vignetteEffect.globalVolume.profile.TryGet<UnityEngine.Rendering.Universal.Vignette>(out var vignette))
            {
                float multiplier = Random.Range(minVignetteMultiplier, maxVignetteMultiplier);
                vignette.intensity.value = Mathf.Clamp01(originalVignetteIntensity * multiplier);
                vignette.smoothness.value = Random.Range(0.2f, 0.5f);
            }
        }
        
        // Randomize Lens Distortion
        if (lensDistortionEffect != null && lensDistortionEffect.globalVolume != null)
        {
            if (lensDistortionEffect.globalVolume.profile.TryGet<UnityEngine.Rendering.Universal.LensDistortion>(out var lensDist))
            {
                float multiplier = Random.Range(minLensDistortionMultiplier, maxLensDistortionMultiplier);
                lensDist.intensity.value = originalLensDistortionIntensity * multiplier;
            }
        }
        
        // Randomize Rotate
        if (rotateEffect != null)
        {
            rotateEffect.rotationSpeed = Random.Range(5f, 12f);
            rotateEffect.rotationIntensity = Random.Range(1f, 4f);
        }
        
        // Randomize Material Color
        if (materialColorEffect != null)
        {
            materialColorEffect.colorIntensity = Random.Range(0.1f, 0.25f);
            materialColorEffect.fadeSpeed = Random.Range(3f, 8f);
        }
        
        Debug.Log("RandomizeButtonController: All effect parameters randomized!");
    }
    
    public void ResetToDefaults()
    {
        // Reset all effects to original values
        if (zoomEffect != null)
            zoomEffect.zoomStrength = originalZoomStrength;
        if (shakeEffect != null)
        {
            shakeEffect.intensity = originalShakeIntensity;
            shakeEffect.speed = originalShakeSpeed;
        }
        if (materialRGBEffect != null)
        {
            materialRGBEffect.rgbIntensity = originalRGBIntensity;
            materialRGBEffect.rgbSpeed = originalRGBSpeed;
        }
        if (bloomEffect != null && bloomEffect.globalVolume != null)
        {
            if (bloomEffect.globalVolume.profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom))
            {
                bloom.intensity.value = originalBloomIntensity;
            }
        }
        if (vignetteEffect != null && vignetteEffect.globalVolume != null)
        {
            if (vignetteEffect.globalVolume.profile.TryGet<UnityEngine.Rendering.Universal.Vignette>(out var vignette))
            {
                vignette.intensity.value = originalVignetteIntensity;
            }
        }
        if (lensDistortionEffect != null && lensDistortionEffect.globalVolume != null)
        {
            if (lensDistortionEffect.globalVolume.profile.TryGet<UnityEngine.Rendering.Universal.LensDistortion>(out var lensDist))
            {
                lensDist.intensity.value = originalLensDistortionIntensity;
            }
        }
    }
}

