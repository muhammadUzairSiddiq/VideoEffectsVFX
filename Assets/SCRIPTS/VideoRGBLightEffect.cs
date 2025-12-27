using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VideoRGBLightEffect : MonoBehaviour
{
    public Volume globalVolume;

    ColorAdjustments colorAdj;
    public bool rgbEnabled = false;

    void Start()
    {
        if (!globalVolume.profile.TryGet(out colorAdj))
        {
            Debug.LogError("Color Adjustments not found");
            return;
        }

        colorAdj.hueShift.value = 0;
        colorAdj.saturation.value = 0;
    }

    private float cachedTime = 0f;
    private float updateInterval = 0.033f; // Will be set by PerformanceMonitor
    private float lastUpdateTime = 0f;

    void Update()
    {
        if (!rgbEnabled) return;

        // Update interval based on performance
        if (PerformanceMonitor.Instance != null)
        {
            updateInterval = PerformanceMonitor.Instance.GetRecommendedUpdateInterval();
        }
        
        // Throttle updates to reduce CPU usage
        float currentTime = Time.time;
        if (currentTime - lastUpdateTime < updateInterval)
            return;
        
        lastUpdateTime = currentTime;
        cachedTime = currentTime;

        // Optimized RGB light flicker (cached time)
        colorAdj.hueShift.value = Mathf.Sin(cachedTime * 5f) * 180f;
        colorAdj.saturation.value = Mathf.PingPong(cachedTime * 100f, 60f);
    }

    // 🔥 UI Button
    public void ToggleRGB()
    {
        rgbEnabled = !rgbEnabled;

        if (!rgbEnabled)
        {
            colorAdj.hueShift.value = 0;
            colorAdj.saturation.value = 0;
        }
    }
}
