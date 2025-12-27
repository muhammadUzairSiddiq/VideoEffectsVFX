using UnityEngine;

public class VideoZoomEffect : MonoBehaviour
{
    public Transform videoQuad;

    [Header("Zoom Settings")]
    public float zoomStrength = 0.08f;
    public float zoomSpeed = 3f;

    Vector3 baseScale;
    public bool isZoomActive = false;

    void Start()
    {
        baseScale = videoQuad.localScale;
        
        // Get update interval from PerformanceMonitor if available
        if (PerformanceMonitor.Instance != null)
        {
            updateInterval = PerformanceMonitor.Instance.GetRecommendedUpdateInterval();
        }
    }

    private float cachedTime = 0f;
    private float updateInterval = 0.033f; // Will be set by PerformanceMonitor
    private float lastUpdateTime = 0f;

    void Update()
    {
        if (!isZoomActive)
        {
            // Reset scale when disabled
            if (videoQuad != null && videoQuad.localScale != baseScale)
            {
                videoQuad.localScale = baseScale;
            }
            return;
        }

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

        // Optimized zoom (cached time)
        float zoom = 1f + Mathf.Sin(cachedTime * zoomSpeed) * zoomStrength;
        videoQuad.localScale = baseScale * zoom;
    }

    // 🔥 CALLED FROM UI BUTTON
    public void ToggleZoom()
    {
        isZoomActive = !isZoomActive;

        if (!isZoomActive)
        {
            videoQuad.localScale = baseScale; // reset
        }
    }
}
