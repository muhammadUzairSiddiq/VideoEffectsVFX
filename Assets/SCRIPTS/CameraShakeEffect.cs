using UnityEngine;

public class CameraShakeEffect : MonoBehaviour
{
    public Transform cameraTransform;

    public float intensity = 0.12f;
    public float speed = 30f;

    Vector3 originalPos;
    public bool shakeEnabled = false;

    void Start()
    {
        originalPos = cameraTransform.localPosition;
        
        // Get update interval from PerformanceMonitor if available
        if (PerformanceMonitor.Instance != null)
        {
            updateInterval = PerformanceMonitor.Instance.GetRecommendedUpdateInterval();
        }
    }

    private float cachedTime = 0f;
    private float updateInterval = 0.033f; // Will be set by PerformanceMonitor
    private float lastUpdateTime = 0f;
    private Vector3 cachedOffset = Vector3.zero;

    void Update()
    {
        // Update interval based on performance
        if (PerformanceMonitor.Instance != null)
        {
            updateInterval = PerformanceMonitor.Instance.GetRecommendedUpdateInterval();
        }
        
        if (!shakeEnabled)
        {
            // Reset position when disabled
            if (cameraTransform.localPosition != originalPos)
            {
                cameraTransform.localPosition = originalPos;
            }
            return;
        }

        // Throttle updates to reduce CPU usage
        float currentTime = Time.time;
        if (currentTime - lastUpdateTime < updateInterval)
        {
            // Use cached offset if update skipped
            cameraTransform.localPosition = originalPos + cachedOffset;
            return;
        }
        
        lastUpdateTime = currentTime;
        cachedTime = currentTime;

        // Optimized shake (cache PerlinNoise calculations)
        float x = Mathf.PerlinNoise(cachedTime * speed, 0) - 0.5f;
        float y = Mathf.PerlinNoise(0, cachedTime * speed) - 0.5f;
        
        cachedOffset = new Vector3(x, y, 0) * intensity;
        cameraTransform.localPosition = originalPos + cachedOffset;
    }

    // 🔥 UI Button
    public void ToggleShake()
    {
        shakeEnabled = !shakeEnabled;

        if (!shakeEnabled)
            cameraTransform.localPosition = originalPos;
    }
}
