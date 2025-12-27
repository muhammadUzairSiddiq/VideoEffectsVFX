using UnityEngine;

/// <summary>
/// Lightweight camera rotation effect - rotates camera slightly on beat
/// </summary>
public class CameraRotateEffect : MonoBehaviour
{
    public Transform cameraTransform;
    
    [Header("Rotation Settings")]
    public float rotationIntensity = 2f; // Degrees
    public float rotationSpeed = 8f;
    
    [Header("Video Check")]
    [Tooltip("Only run effect when video is playing")]
    public UnityEngine.Video.VideoPlayer videoPlayer;
    
    private Quaternion originalRotation;
    public bool rotateEnabled = false;
    
    private float cachedTime = 0f;
    private float lastUpdateTime = 0f;
    private float updateInterval = 0.033f; // Will be set by PerformanceMonitor
    
    void Start()
    {
        // Auto-find VideoPlayer if not assigned
        if (videoPlayer == null)
        {
            videoPlayer = FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();
        }
        
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
        }
        
        if (cameraTransform != null)
        {
            originalRotation = cameraTransform.localRotation;
        }
        
        // Get update interval from PerformanceMonitor if available
        if (PerformanceMonitor.Instance != null)
        {
            updateInterval = PerformanceMonitor.Instance.GetRecommendedUpdateInterval();
        }
    }
    
    void Update()
    {
        if (cameraTransform == null) return;
        
        // CRITICAL: Only run effect when video is playing
        if (videoPlayer == null)
        {
            videoPlayer = FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();
        }
        
        // If video is not playing, disable effect and reset
        if (videoPlayer == null || !videoPlayer.isPlaying || string.IsNullOrEmpty(videoPlayer.url))
        {
            if (rotateEnabled)
            {
                rotateEnabled = false;
                cameraTransform.localRotation = originalRotation;
            }
            return;
        }
        
        // Update interval based on performance
        if (PerformanceMonitor.Instance != null)
        {
            updateInterval = PerformanceMonitor.Instance.GetRecommendedUpdateInterval();
        }
        
        if (!rotateEnabled)
        {
            // Reset rotation when disabled
            if (cameraTransform.localRotation != originalRotation)
            {
                cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, originalRotation, Time.deltaTime * 5f);
            }
            return;
        }
        
        // Throttle updates
        float currentTime = Time.time;
        if (currentTime - lastUpdateTime < updateInterval)
            return;
        
        lastUpdateTime = currentTime;
        cachedTime = currentTime;
        
        // Lightweight rotation (sine wave)
        float rotationZ = Mathf.Sin(cachedTime * rotationSpeed) * rotationIntensity;
        cameraTransform.localRotation = originalRotation * Quaternion.Euler(0, 0, rotationZ);
    }
    
    public void TriggerRotate(float duration = 0.1f)
    {
        if (cameraTransform == null) return;
        
        rotateEnabled = true;
        StartCoroutine(StopRotateAfter(duration));
    }
    
    System.Collections.IEnumerator StopRotateAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        rotateEnabled = false;
    }
    
    public void ToggleRotate()
    {
        rotateEnabled = !rotateEnabled;
        if (!rotateEnabled && cameraTransform != null)
        {
            cameraTransform.localRotation = originalRotation;
        }
    }
}

