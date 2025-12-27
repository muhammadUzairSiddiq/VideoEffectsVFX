using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BeatEffectController : MonoBehaviour
{
    [Header("Beat Detection")]
    public AudioBeatDetector beatDetector;
    [Tooltip("VideoPlayer reference - effects only run when video is playing")]
    public UnityEngine.Video.VideoPlayer videoPlayer;

    [Header("Effect Management")]
    [Tooltip("Enable automatic beat-synced effects")]
    public bool autoModeEnabled = true;
    
    public bool AutoModeEnabled => autoModeEnabled;
    
    [Tooltip("Time to keep effect active after beat (seconds)")]
    public float effectDuration = 0.1f; // shorter for lower cost
    
#if UNITY_ANDROID || UNITY_IOS
    private const float MOBILE_EFFECT_DURATION = 0.05f; // Even shorter on mobile
#endif
    
    [Tooltip("Randomly switch effects on each beat")]
    public bool randomizeEffects = true;
    
    [Tooltip("Minimum time before switching to different effect")]
    public float minSwitchInterval = 0.5f;
    
    [Tooltip("Warmup delay before effects start (seconds) - prevents initial lag")]
    public float warmupDelay = 1.5f;
    
    [Tooltip("Probability of using light effects (0-1). Higher = more zoom/shake")]
    [Range(0f, 1f)]
    public float lightEffectProbability = 0.7f; // 70% chance for light effects
    
    [Header("Mobile Optimization")]
    [Tooltip("On mobile, only use light effects (zoom/shake) for better performance")]
    public bool mobileMode = false;

    [Header("Effect References")]
    public VideoZoomEffect zoomEffect;
    public VideoBloomEffect bloomEffect;
    public VideoGlitchEffect glitchEffect;
    public VideoRGBLightEffect rgbEffect; // Post-processing RGB (keep for compatibility)
    public VideoMaterialRGBEffect materialRGBEffect; // Lightweight material-based RGB (preferred)
    public CameraShakeEffect shakeEffect;
    public VideoVignetteEffect vignetteEffect;
    public VideoShadowMidtoneEffect shadowMidtoneEffect;
    public VideoLensDistortionEffect lensDistortionEffect;
    public CameraRotateEffect rotateEffect;
    public MaterialColorEffect materialColorEffect;

    // Effect states
    private List<System.Action> availableEffects;
    private List<System.Action> lightEffects; // Zoom, Shake (lightweight)
    private List<System.Action> mediumEffects; // Vignette (lightweight but can use more)
    private List<System.Action> heavyEffects; // Bloom, RGB, LensDistortion (heavier - use more when FPS is good)
    private System.Action currentActiveEffect;
    private float lastEffectSwitchTime;
    private bool isEffectActive;
    private bool isWarmedUp = false;
    private float videoStartTime = 0f;
    
    // Public method to skip warmup (for export replay)
    public void SkipWarmup()
    {
        isWarmedUp = true;
        videoStartTime = Time.time;
        Debug.Log("BeatEffectController: Warmup skipped for export");
    }
    private PerformanceMonitor performanceMonitor;
    
    // Single effect mode (empty = all effects, "Zoom" = only zoom, etc.)
    private string singleEffectMode = "";
    
    // Coroutine tracking to prevent overlaps
    private Dictionary<string, Coroutine> activeCoroutines = new Dictionary<string, Coroutine>();
    
    // Cached WaitForSeconds to reduce GC allocations
    private WaitForSeconds cachedWaitForDuration;

    void Start()
    {
        // Get or create PerformanceMonitor
        performanceMonitor = PerformanceMonitor.Instance;
        if (performanceMonitor == null)
        {
            GameObject perfObj = new GameObject("PerformanceMonitor");
            performanceMonitor = perfObj.AddComponent<PerformanceMonitor>();
        }
        
        // Auto-find VideoPlayer if not assigned
        if (videoPlayer == null)
        {
            videoPlayer = FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();
            if (videoPlayer != null)
                Debug.Log("BeatEffectController: Auto-found VideoPlayer");
        }
        
        // Auto-find AudioBeatDetector if not assigned
        if (beatDetector == null)
        {
            beatDetector = FindFirstObjectByType<AudioBeatDetector>();
            if (beatDetector != null)
                Debug.Log("BeatEffectController: Auto-found AudioBeatDetector");
        }

        // Auto-find effects if not assigned
        AutoFindEffects();

        // Build list of available effects with priority
        BuildEffectList();
        
        // Use shorter duration on mobile for better performance
#if UNITY_ANDROID || UNITY_IOS
        float duration = MOBILE_EFFECT_DURATION;
#else
        float duration = effectDuration;
#endif
        // Cache WaitForSeconds to reduce GC allocations
        cachedWaitForDuration = new WaitForSeconds(duration);
        
        // Pre-warm all effects to avoid first-time initialization lag
        StartCoroutine(PreWarmEffects());

        // Subscribe to beat events
        if (beatDetector != null)
        {
            beatDetector.OnBeat += OnBeatDetected;
            Debug.Log($"BeatEffectController: Subscribed to beats. {availableEffects.Count} effects available ({lightEffects.Count} light, {heavyEffects.Count} heavy).");
        }
        else
        {
            Debug.LogError("BeatEffectController: No AudioBeatDetector found!");
        }
    }
    
    IEnumerator PreWarmEffects()
    {
        // Pre-initialize all effects to avoid first-time lag
        yield return null; // Wait one frame
        
        // Touch all effects to initialize them
        if (zoomEffect != null && zoomEffect.videoQuad != null) { }
        if (shakeEffect != null && shakeEffect.cameraTransform != null) { }
        if (bloomEffect != null && bloomEffect.globalVolume != null) { }
        if (glitchEffect != null && glitchEffect.globalVolume != null) { }
        if (rgbEffect != null && rgbEffect.globalVolume != null) { }
        if (vignetteEffect != null && vignetteEffect.globalVolume != null) { }
        if (lensDistortionEffect != null && lensDistortionEffect.globalVolume != null) { }
        
        // Force garbage collection to clean up any initialization overhead
        System.GC.Collect();
        
        Debug.Log("BeatEffectController: Effects pre-warmed. Ready for smooth playback.");
    }

    void AutoFindEffects()
    {
        if (zoomEffect == null) zoomEffect = FindFirstObjectByType<VideoZoomEffect>();
        if (bloomEffect == null) bloomEffect = FindFirstObjectByType<VideoBloomEffect>();
        if (glitchEffect == null) glitchEffect = FindFirstObjectByType<VideoGlitchEffect>();
        if (rgbEffect == null) rgbEffect = FindFirstObjectByType<VideoRGBLightEffect>();
        if (materialRGBEffect == null) materialRGBEffect = FindFirstObjectByType<VideoMaterialRGBEffect>();
        if (shakeEffect == null) shakeEffect = FindFirstObjectByType<CameraShakeEffect>();
        if (vignetteEffect == null) vignetteEffect = FindFirstObjectByType<VideoVignetteEffect>();
        if (shadowMidtoneEffect == null) shadowMidtoneEffect = FindFirstObjectByType<VideoShadowMidtoneEffect>();
        if (lensDistortionEffect == null) lensDistortionEffect = FindFirstObjectByType<VideoLensDistortionEffect>();
        if (rotateEffect == null) rotateEffect = FindFirstObjectByType<CameraRotateEffect>();
        if (materialColorEffect == null) materialColorEffect = FindFirstObjectByType<MaterialColorEffect>();
    }

    void BuildEffectList()
    {
        availableEffects = new List<System.Action>();
        lightEffects = new List<System.Action>();
        mediumEffects = new List<System.Action>();
        heavyEffects = new List<System.Action>();
        
        // Detect mobile platform
#if UNITY_ANDROID || UNITY_IOS
        mobileMode = true; // Force mobile mode on Android/iOS
#endif
        
        // Light effects (cheap - use frequently: Zoom, Shake, Rotate, MaterialRGB, MaterialColor)
        if (zoomEffect != null)
        {
            lightEffects.Add(() => TriggerZoom());
            availableEffects.Add(() => TriggerZoom());
        }
        if (shakeEffect != null)
        {
            lightEffects.Add(() => TriggerShake());
            availableEffects.Add(() => TriggerShake());
        }
        if (rotateEffect != null)
        {
            lightEffects.Add(() => TriggerRotate());
            availableEffects.Add(() => TriggerRotate());
        }
        // Material RGB is lightweight - use instead of post-processing RGB
        if (materialRGBEffect != null)
        {
            lightEffects.Add(() => TriggerMaterialRGB());
            availableEffects.Add(() => TriggerMaterialRGB());
        }
        else if (materialColorEffect != null)
        {
            // Fallback to MaterialColor if MaterialRGB not available
            lightEffects.Add(() => TriggerMaterialColor());
            availableEffects.Add(() => TriggerMaterialColor());
        }
        
        // Medium effects (lightweight - use more frequently, especially Vignette)
        if (vignetteEffect != null)
        {
            mediumEffects.Add(() => TriggerVignette());
            availableEffects.Add(() => TriggerVignette());
        }
        
        // Heavy effects (expensive - use more when FPS is stable: Bloom, LensDistortion, Post-processing RGB, Glitch)
        if (bloomEffect != null)
        {
            heavyEffects.Add(() => TriggerBloom());
            availableEffects.Add(() => TriggerBloom());
        }
        if (lensDistortionEffect != null)
        {
            heavyEffects.Add(() => TriggerLensDistortion());
            availableEffects.Add(() => TriggerLensDistortion());
        }
        // Post-processing RGB is heavy - only use if material RGB not available
        if (materialRGBEffect == null && rgbEffect != null)
        {
            heavyEffects.Add(() => TriggerRGB());
            availableEffects.Add(() => TriggerRGB());
        }
        if (glitchEffect != null)
        {
            heavyEffects.Add(() => TriggerGlitch());
            availableEffects.Add(() => TriggerGlitch());
        }
    }

    void OnDestroy()
    {
        if (beatDetector != null)
        {
            beatDetector.OnBeat -= OnBeatDetected;
        }
    }

    void OnBeatDetected()
    {
        if (!autoModeEnabled || availableEffects.Count == 0)
            return;
        
        // CRITICAL: Only run effects when video is actually playing
        if (videoPlayer == null)
        {
            videoPlayer = FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();
        }
        
        if (videoPlayer == null || !videoPlayer.isPlaying || string.IsNullOrEmpty(videoPlayer.url))
        {
            return; // No video playing - don't trigger effects
        }
        
        // Warmup period - skip effects during initial video load to prevent lag
        if (!isWarmedUp)
        {
            if (videoStartTime == 0f)
            {
                videoStartTime = Time.time;
            }
            
            if (Time.time - videoStartTime < warmupDelay)
            {
                return; // Skip effects during warmup
            }
            else
            {
                isWarmedUp = true;
                Debug.Log("BeatEffectController: Warmup complete. Effects now active.");
            }
        }

        // Single effect mode - only trigger the selected effect
        if (!string.IsNullOrEmpty(singleEffectMode))
        {
            switch (singleEffectMode)
            {
                case "Zoom":
                    TriggerZoom();
                    break;
                case "Bloom":
                    TriggerBloom();
                    break;
                case "Glitch":
                    TriggerGlitch();
                    break;
                case "Vignette":
                    TriggerVignette();
                    break;
                case "Shake":
                    TriggerShake();
                    break;
                case "MaterialRGB":
                    TriggerMaterialRGB();
                    break;
                case "Rotate":
                    TriggerRotate();
                    break;
                case "LensDistortion":
                    TriggerLensDistortion();
                    break;
            }
            return; // Exit early - don't run random effect selection
        }

        // Priority-based effect selection: Use light effects more often
        System.Action selectedEffect = null;
        
        if (randomizeEffects && Time.time - lastEffectSwitchTime > minSwitchInterval)
        {
            // Dynamic effect selection based on FPS performance
            bool canUseHeavy = performanceMonitor != null && performanceMonitor.CanUseHeavyEffects;
            
            if (canUseHeavy && (lightEffects.Count > 0 || mediumEffects.Count > 0 || heavyEffects.Count > 0))
            {
                // FPS is stable - INCREASED usage of heavy effects (Bloom, LensDistortion) and vignette
                float rand = Random.value;
                
                if (rand < 0.4f && heavyEffects.Count > 0)
                {
                    // 40% chance for heavy effects (Bloom, LensDistortion) when FPS is good - INCREASED from 35%
                    int randomIndex = Random.Range(0, heavyEffects.Count);
                    selectedEffect = heavyEffects[randomIndex];
                }
                else if (rand < 0.7f && mediumEffects.Count > 0)
                {
                    // 30% chance for medium effects (Vignette - lightweight, use more) - INCREASED from 20%
                    int randomIndex = Random.Range(0, mediumEffects.Count);
                    selectedEffect = mediumEffects[randomIndex];
                }
                else if (lightEffects.Count > 0)
                {
                    // 30% chance for light effects (Zoom, Shake, MaterialRGB) - DECREASED from 45% to balance
                    int randomIndex = Random.Range(0, lightEffects.Count);
                    selectedEffect = lightEffects[randomIndex];
                }
            }
            else if (mediumEffects.Count > 0 || lightEffects.Count > 0)
            {
                // FPS not stable - use medium (Vignette) and light effects (Shake, Zoom, MaterialRGB) only
                float rand = Random.value;
                
                if (rand < 0.5f && mediumEffects.Count > 0)
                {
                    // 50% chance for Vignette (lightweight, use more) - INCREASED from 40%
                    int randomIndex = Random.Range(0, mediumEffects.Count);
                    selectedEffect = mediumEffects[randomIndex];
                }
                else if (lightEffects.Count > 0)
                {
                    // 50% chance for light effects (Shake, Zoom, MaterialRGB) - balanced
                    int randomIndex = Random.Range(0, lightEffects.Count);
                    selectedEffect = lightEffects[randomIndex];
                }
            }
            else
            {
                // Fallback to all effects if categories are empty
                int randomIndex = Random.Range(0, availableEffects.Count);
                selectedEffect = availableEffects[randomIndex];
            }
            
            currentActiveEffect = selectedEffect;
            lastEffectSwitchTime = Time.time;
        }
        else if (currentActiveEffect == null)
        {
            // First beat - prefer medium (Vignette) or light effect (Shake, Zoom, MaterialRGB), or heavy if FPS is good
            bool canUseHeavy = performanceMonitor != null && performanceMonitor.CanUseHeavyEffects;
            float rand = Random.value;
            
            if (canUseHeavy && rand < 0.35f && heavyEffects.Count > 0)
            {
                // 35% chance for heavy effect (Bloom, LensDistortion) on first beat if FPS is good - INCREASED
                int randomIndex = Random.Range(0, heavyEffects.Count);
                currentActiveEffect = heavyEffects[randomIndex];
            }
            else if (rand < 0.6f && mediumEffects.Count > 0)
            {
                // 25% chance for Vignette (lightweight, use more) - INCREASED
                int randomIndex = Random.Range(0, mediumEffects.Count);
                currentActiveEffect = mediumEffects[randomIndex];
            }
            else if (lightEffects.Count > 0)
            {
                // 40% chance for light effects (Shake, Zoom, MaterialRGB) - balanced
                int randomIndex = Random.Range(0, lightEffects.Count);
                currentActiveEffect = lightEffects[randomIndex];
            }
            else if (availableEffects.Count > 0)
            {
                int randomIndex = Random.Range(0, availableEffects.Count);
                currentActiveEffect = availableEffects[randomIndex];
            }
        }

        // Trigger the selected effect ASYNCHRONOUSLY (non-blocking)
        if (currentActiveEffect != null)
        {
            currentActiveEffect.Invoke();
        }
    }

    // Effect trigger methods (optimized - allow effects to run independently)
    void TriggerZoom()
    {
        if (zoomEffect != null && !zoomEffect.isZoomActive)
        {
            // Don't stop existing coroutine - allow it to finish naturally
            // Only start if not already active
            if (!activeCoroutines.ContainsKey("Zoom") || activeCoroutines["Zoom"] == null)
            {
                activeCoroutines["Zoom"] = StartCoroutine(TemporaryZoom());
            }
        }
    }

    void TriggerBloom()
    {
        if (bloomEffect != null && !bloomEffect.bloomEnabled)
        {
            if (!activeCoroutines.ContainsKey("Bloom") || activeCoroutines["Bloom"] == null)
            {
                activeCoroutines["Bloom"] = StartCoroutine(TemporaryBloom());
            }
        }
    }

    void TriggerGlitch()
    {
        if (glitchEffect != null && !glitchEffect.glitchEnabled)
        {
            if (!activeCoroutines.ContainsKey("Glitch") || activeCoroutines["Glitch"] == null)
            {
                activeCoroutines["Glitch"] = StartCoroutine(TemporaryGlitch());
            }
        }
    }

    void TriggerRGB()
    {
        if (rgbEffect != null && !rgbEffect.rgbEnabled)
        {
            if (!activeCoroutines.ContainsKey("RGB") || activeCoroutines["RGB"] == null)
            {
                activeCoroutines["RGB"] = StartCoroutine(TemporaryRGB());
            }
        }
    }

    void TriggerShake()
    {
        if (shakeEffect != null && !shakeEffect.shakeEnabled)
        {
            if (!activeCoroutines.ContainsKey("Shake") || activeCoroutines["Shake"] == null)
            {
                activeCoroutines["Shake"] = StartCoroutine(TemporaryShake());
            }
        }
    }

    void TriggerVignette()
    {
        if (vignetteEffect != null && !vignetteEffect.vignetteEnabled)
        {
            if (!activeCoroutines.ContainsKey("Vignette") || activeCoroutines["Vignette"] == null)
            {
                activeCoroutines["Vignette"] = StartCoroutine(TemporaryVignette());
            }
        }
    }

    void TriggerShadowMidtone()
    {
        if (shadowMidtoneEffect != null && !shadowMidtoneEffect.effectEnabled)
        {
            if (!activeCoroutines.ContainsKey("ShadowMidtone") || activeCoroutines["ShadowMidtone"] == null)
            {
                activeCoroutines["ShadowMidtone"] = StartCoroutine(TemporaryShadowMidtone());
            }
        }
    }

    void TriggerLensDistortion()
    {
        if (lensDistortionEffect != null && !lensDistortionEffect.distortionEnabled)
        {
            if (!activeCoroutines.ContainsKey("LensDistortion") || activeCoroutines["LensDistortion"] == null)
            {
                activeCoroutines["LensDistortion"] = StartCoroutine(TemporaryLensDistortion());
            }
        }
    }
    
    void TriggerRotate()
    {
        if (rotateEffect != null)
        {
            rotateEffect.TriggerRotate(effectDuration);
        }
    }
    
    void TriggerMaterialColor()
    {
        if (materialColorEffect != null)
        {
            materialColorEffect.TriggerColorChange(effectDuration);
        }
    }
    
    void TriggerMaterialRGB()
    {
        if (materialRGBEffect != null && !materialRGBEffect.rgbEnabled)
        {
            if (!activeCoroutines.ContainsKey("MaterialRGB") || activeCoroutines["MaterialRGB"] == null)
            {
                activeCoroutines["MaterialRGB"] = StartCoroutine(TemporaryMaterialRGB());
            }
        }
    }
    
    // Effects now run independently - no blocking or stopping
    // Each effect can run simultaneously without interfering with others

    // Temporary effect coroutines (optimized - non-blocking, allow simultaneous effects)
    IEnumerator TemporaryZoom()
    {
        if (zoomEffect != null && !zoomEffect.isZoomActive)
        {
            zoomEffect.ToggleZoom();
        }
        
        yield return cachedWaitForDuration;
        
        if (zoomEffect != null && zoomEffect.isZoomActive)
        {
            zoomEffect.ToggleZoom();
        }
        
        activeCoroutines["Zoom"] = null;
    }

    IEnumerator TemporaryBloom()
    {
        if (bloomEffect != null && !bloomEffect.bloomEnabled)
        {
            bloomEffect.ToggleBloom();
        }
        
        yield return cachedWaitForDuration;
        
        if (bloomEffect != null && bloomEffect.bloomEnabled)
        {
            bloomEffect.ToggleBloom();
        }
        
        activeCoroutines["Bloom"] = null;
    }

    IEnumerator TemporaryGlitch()
    {
        if (glitchEffect != null && !glitchEffect.glitchEnabled)
        {
            glitchEffect.ToggleGlitch();
        }
        
        yield return cachedWaitForDuration;
        
        if (glitchEffect != null && glitchEffect.glitchEnabled)
        {
            glitchEffect.ToggleGlitch();
        }
        
        activeCoroutines["Glitch"] = null;
    }

    IEnumerator TemporaryRGB()
    {
        if (rgbEffect != null && !rgbEffect.rgbEnabled)
        {
            rgbEffect.ToggleRGB();
        }
        
        yield return cachedWaitForDuration;
        
        if (rgbEffect != null && rgbEffect.rgbEnabled)
        {
            rgbEffect.ToggleRGB();
        }
        
        activeCoroutines["RGB"] = null;
    }

    IEnumerator TemporaryShake()
    {
        if (shakeEffect != null && !shakeEffect.shakeEnabled)
        {
            shakeEffect.ToggleShake();
        }
        
        yield return cachedWaitForDuration;
        
        if (shakeEffect != null && shakeEffect.shakeEnabled)
        {
            shakeEffect.ToggleShake();
        }
        
        activeCoroutines["Shake"] = null;
    }

    IEnumerator TemporaryVignette()
    {
        if (vignetteEffect != null && !vignetteEffect.vignetteEnabled)
        {
            vignetteEffect.ToggleVignette();
        }
        
        yield return cachedWaitForDuration;
        
        if (vignetteEffect != null && vignetteEffect.vignetteEnabled)
        {
            vignetteEffect.ToggleVignette();
        }
        
        activeCoroutines["Vignette"] = null;
    }

    IEnumerator TemporaryShadowMidtone()
    {
        if (shadowMidtoneEffect != null && !shadowMidtoneEffect.effectEnabled)
        {
            shadowMidtoneEffect.ToggleShadowMidtone();
        }
        
        yield return cachedWaitForDuration;
        
        if (shadowMidtoneEffect != null && shadowMidtoneEffect.effectEnabled)
        {
            shadowMidtoneEffect.ToggleShadowMidtone();
        }
        
        activeCoroutines["ShadowMidtone"] = null;
    }

    IEnumerator TemporaryLensDistortion()
    {
        if (lensDistortionEffect != null && !lensDistortionEffect.distortionEnabled)
        {
            lensDistortionEffect.ToggleLensDistortion();
        }
        
        yield return cachedWaitForDuration;
        
        if (lensDistortionEffect != null && lensDistortionEffect.distortionEnabled)
        {
            lensDistortionEffect.ToggleLensDistortion();
        }
        
        activeCoroutines["LensDistortion"] = null;
    }
    
    IEnumerator TemporaryMaterialRGB()
    {
        if (materialRGBEffect != null && !materialRGBEffect.rgbEnabled)
        {
            materialRGBEffect.TriggerRGB(effectDuration);
        }
        
        yield return cachedWaitForDuration;
        
        if (materialRGBEffect != null && materialRGBEffect.rgbEnabled)
        {
            materialRGBEffect.ToggleRGB();
        }
        
        activeCoroutines["MaterialRGB"] = null;
    }

    // Public methods for UI
    public void ToggleAutoMode()
    {
        autoModeEnabled = !autoModeEnabled;
    }

    public void SetAutoMode(bool enabled)
    {
        autoModeEnabled = enabled;
    }
    
    // Called when new video starts - reset warmup
    public void OnVideoStarted()
    {
        isWarmedUp = false;
        videoStartTime = 0f;
        // Stop all active effects when new video starts
        StopAllEffects();
    }
    
    // Called when video ends - stop all effects
    public void OnVideoEnded()
    {
        StopAllEffects();
        isWarmedUp = false;
        videoStartTime = 0f;
    }
    
    // Set single effect mode (only one effect will trigger on beats)
    public void SetSingleEffectMode(string effectName)
    {
        singleEffectMode = effectName;
        randomizeEffects = false; // Disable random switching
        autoModeEnabled = true; // Keep auto mode on for beat detection
        
        // Stop all other effects
        StopAllEffects();
        
        Debug.Log($"BeatEffectController: Single effect mode set to: {effectName}");
    }
    
    // Set all effects mode (randomize - all effects can trigger)
    public void SetAllEffectsMode()
    {
        singleEffectMode = "";
        randomizeEffects = true; // Enable random switching
        autoModeEnabled = true;
        
        Debug.Log("BeatEffectController: All effects mode enabled (randomize)");
    }
    
    // Stop all active effects immediately
    public void StopAllEffects()
    {
        // Stop all active coroutines
        foreach (var coroutine in activeCoroutines.Values)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        activeCoroutines.Clear();
        
        // Disable all effects
        if (zoomEffect != null && zoomEffect.isZoomActive)
            zoomEffect.ToggleZoom();
        if (shakeEffect != null && shakeEffect.shakeEnabled)
            shakeEffect.ToggleShake();
        if (bloomEffect != null && bloomEffect.bloomEnabled)
            bloomEffect.ToggleBloom();
        if (glitchEffect != null && glitchEffect.glitchEnabled)
            glitchEffect.ToggleGlitch();
        if (rgbEffect != null && rgbEffect.rgbEnabled)
            rgbEffect.ToggleRGB();
        if (materialRGBEffect != null && materialRGBEffect.rgbEnabled)
            materialRGBEffect.ToggleRGB();
        if (vignetteEffect != null && vignetteEffect.vignetteEnabled)
            vignetteEffect.ToggleVignette();
        if (shadowMidtoneEffect != null && shadowMidtoneEffect.effectEnabled)
            shadowMidtoneEffect.ToggleShadowMidtone();
        if (lensDistortionEffect != null && lensDistortionEffect.distortionEnabled)
            lensDistortionEffect.ToggleLensDistortion();
        if (rotateEffect != null && rotateEffect.rotateEnabled)
            rotateEffect.ToggleRotate();
        if (materialColorEffect != null && materialColorEffect.colorEnabled)
            materialColorEffect.ToggleColor();
        
        currentActiveEffect = null;
    }
    
    // Replay effect timeline during export
    public void ReplayEffectTimeline(List<EffectSelection> timeline)
    {
        if (timeline == null || timeline.Count == 0)
        {
            Debug.Log("BeatEffectController: No timeline to replay");
            return;
        }
        
        StartCoroutine(ReplayTimelineCoroutine(timeline));
    }
    
    IEnumerator ReplayTimelineCoroutine(List<EffectSelection> timeline)
    {
        int currentIndex = 0;
        string currentEffect = "";
        float lastTriggerTime = 0f;
        float triggerInterval = 0.5f; // Trigger effect every 0.5s during replay (less frequent = less lag)
        
        // Reduce effect intensity during recording for performance
        float originalDuration = effectDuration;
        effectDuration = effectDuration * 0.7f; // Shorter effects = less processing
        
        Debug.Log($"BeatEffectController: Starting timeline replay with {timeline.Count} effect changes (reduced intensity for recording)");
        
        while (videoPlayer != null && videoPlayer.isPlaying)
        {
            float currentTime = (float)videoPlayer.time;
            
            // Check if we need to switch to next effect
            if (currentIndex < timeline.Count && 
                currentTime >= timeline[currentIndex].videoTime)
            {
                // Switch to this effect
                string effectName = timeline[currentIndex].effectName;
                
                // Don't use SetSingleEffectMode (it stops all effects)
                // Just set the current effect and trigger it
                currentEffect = effectName;
                lastTriggerTime = currentTime;
                
                // Immediately trigger the effect
                TriggerEffectByName(effectName);
                
                Debug.Log($"BeatEffectController: Replaying - {effectName} at {currentTime:F2}s");
                currentIndex++;
            }
            
            // Continuously trigger current effect (don't wait for beats)
            if (!string.IsNullOrEmpty(currentEffect) && (currentTime - lastTriggerTime) >= triggerInterval)
            {
                TriggerEffectByName(currentEffect);
                lastTriggerTime = currentTime;
                Debug.Log($"BeatEffectController: Triggering {currentEffect} at {currentTime:F2}s");
            }
            
            yield return null;
        }
        
        // Restore original effect duration
        effectDuration = originalDuration;
        
        Debug.Log("BeatEffectController: Timeline replay complete");
    }
    
    void TriggerEffectByName(string effectName)
    {
        switch (effectName)
        {
            case "Zoom":
                TriggerZoom();
                break;
            case "Bloom":
                TriggerBloom();
                break;
            case "Glitch":
                TriggerGlitch();
                break;
            case "Vignette":
                TriggerVignette();
                break;
            case "Shake":
                TriggerShake();
                break;
            case "MaterialRGB":
                TriggerMaterialRGB();
                break;
            case "Rotate":
                TriggerRotate();
                break;
            case "LensDistortion":
                TriggerLensDistortion();
                break;
        }
    }
}

