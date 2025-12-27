using UnityEngine;

/// <summary>
/// Lightweight material-based RGB effect - changes video material color tint on beat
/// Much lighter than post-processing RGB effect
/// </summary>
public class VideoMaterialRGBEffect : MonoBehaviour
{
    [Header("Video Material Settings")]
    [Tooltip("The video quad renderer that displays the video")]
    public Renderer videoRenderer;
    
    [Header("RGB Settings")]
    [Tooltip("RGB color shift speed")]
    public float rgbSpeed = 5f;
    
    [Tooltip("RGB color intensity (0-1)")]
    [Range(0f, 1f)]
    public float rgbIntensity = 0.3f;
    
    [Tooltip("Base color tint multiplier")]
    public Color baseTint = Color.white;
    
    [Header("Video Check")]
    [Tooltip("Only run effect when video is playing")]
    public UnityEngine.Video.VideoPlayer videoPlayer;
    
    private Material videoMaterial;
    private Material originalMaterial;
    private Color originalColor;
    public bool rgbEnabled = false;
    
    // Performance optimization
    private float cachedTime = 0f;
#if UNITY_ANDROID || UNITY_IOS
    private const float UPDATE_INTERVAL = 0.1f; // ~10fps on mobile
#else
    private const float UPDATE_INTERVAL = 0.033f; // ~30fps on desktop
#endif
    private float lastUpdateTime = 0f;
    
    void Start()
    {
        // Auto-find VideoPlayer if not assigned
        if (videoPlayer == null)
        {
            videoPlayer = FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();
        }
        
        // Auto-find video renderer if not assigned
        if (videoRenderer == null)
        {
            // Try to find by name
            GameObject videoQuad = GameObject.Find("VideoQuad");
            if (videoQuad != null)
            {
                videoRenderer = videoQuad.GetComponent<Renderer>();
            }
            
            // Fallback: find any renderer with "Video" in name
            if (videoRenderer == null)
            {
                Renderer[] renderers = FindObjectsOfType<Renderer>();
                foreach (Renderer r in renderers)
                {
                    if (r.name.Contains("Video") || r.name.Contains("video"))
                    {
                        videoRenderer = r;
                        break;
                    }
                }
            }
        }
        
        if (videoRenderer != null)
        {
            // Get or create material instance (to avoid modifying shared material)
            videoMaterial = videoRenderer.material;
            originalMaterial = videoRenderer.sharedMaterial;
            
            // Try to get base color from material
            if (videoMaterial.HasProperty("_BaseColor"))
            {
                originalColor = videoMaterial.GetColor("_BaseColor");
            }
            else if (videoMaterial.HasProperty("_Color"))
            {
                originalColor = videoMaterial.GetColor("_Color");
            }
            else if (videoMaterial.HasProperty("_TintColor"))
            {
                originalColor = videoMaterial.GetColor("_TintColor");
            }
            else
            {
                originalColor = Color.white;
            }
            
            baseTint = originalColor;
        }
        else
        {
            Debug.LogWarning("VideoMaterialRGBEffect: No video renderer found! RGB effect will not work.");
        }
    }
    
    void Update()
    {
        // CRITICAL: Only run effect when video is playing
        if (videoPlayer == null)
        {
            videoPlayer = FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();
        }
        
        // If video is not playing, disable effect and reset
        if (videoPlayer == null || !videoPlayer.isPlaying || string.IsNullOrEmpty(videoPlayer.url))
        {
            if (rgbEnabled)
            {
                rgbEnabled = false;
                ResetColor();
            }
            return;
        }
        
        if (!rgbEnabled || videoMaterial == null)
        {
            // Reset color when disabled
            if (videoMaterial != null && videoMaterial.color != originalColor)
            {
                ResetColor();
            }
            return;
        }
        
        // Throttle updates to reduce CPU usage
        float currentTime = Time.time;
        if (currentTime - lastUpdateTime < UPDATE_INTERVAL)
            return;
        
        lastUpdateTime = currentTime;
        cachedTime = currentTime;
        
        // Calculate RGB shift (hue rotation)
        float hueShift = Mathf.Sin(cachedTime * rgbSpeed) * 180f; // -180 to +180 degrees
        float saturation = Mathf.PingPong(cachedTime * rgbSpeed * 2f, 0.5f) + 0.5f; // 0.5 to 1.0
        
        // Convert hue shift to RGB color tint
        Color rgbTint = Color.HSVToRGB(
            (hueShift + 180f) / 360f, // Normalize to 0-1
            saturation * rgbIntensity,
            1f
        );
        
        // Apply tint to material
        Color finalColor = baseTint * rgbTint;
        
        // Set color based on available shader properties
        if (videoMaterial.HasProperty("_BaseColor"))
        {
            videoMaterial.SetColor("_BaseColor", finalColor);
        }
        else if (videoMaterial.HasProperty("_Color"))
        {
            videoMaterial.SetColor("_Color", finalColor);
        }
        else if (videoMaterial.HasProperty("_TintColor"))
        {
            videoMaterial.SetColor("_TintColor", finalColor);
        }
        else
        {
            // Fallback: try to set main texture color
            videoMaterial.color = finalColor;
        }
    }
    
    void ResetColor()
    {
        if (videoMaterial == null) return;
        
        if (videoMaterial.HasProperty("_BaseColor"))
        {
            videoMaterial.SetColor("_BaseColor", originalColor);
        }
        else if (videoMaterial.HasProperty("_Color"))
        {
            videoMaterial.SetColor("_Color", originalColor);
        }
        else if (videoMaterial.HasProperty("_TintColor"))
        {
            videoMaterial.SetColor("_TintColor", originalColor);
        }
        else
        {
            videoMaterial.color = originalColor;
        }
    }
    
    public void ToggleRGB()
    {
        rgbEnabled = !rgbEnabled;
        
        if (!rgbEnabled)
        {
            ResetColor();
        }
    }
    
    public void TriggerRGB(float duration = 0.1f)
    {
        if (!rgbEnabled)
        {
            rgbEnabled = true;
            StartCoroutine(StopRGBAfter(duration));
        }
    }
    
    System.Collections.IEnumerator StopRGBAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        rgbEnabled = false;
        ResetColor();
    }
    
    void OnDestroy()
    {
        // Reset material color when destroyed
        if (videoMaterial != null)
        {
            ResetColor();
        }
    }
}

