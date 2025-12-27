using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Monitors FPS and provides performance metrics for adaptive effect system
/// </summary>
public class PerformanceMonitor : MonoBehaviour
{
    private static PerformanceMonitor instance;
    public static PerformanceMonitor Instance => instance;
    
    [Header("FPS Monitoring")]
    [Tooltip("How often to update FPS calculation (seconds)")]
    public float updateInterval = 0.5f;
    
    [Tooltip("Minimum FPS to consider stable (below this, disable heavy effects)")]
    public float stableFPSThreshold = 25f;
    
    [Tooltip("Number of samples to average for FPS stability")]
    public int stabilitySampleCount = 10;
    
    // Current FPS
    private float currentFPS = 60f;
    private float averageFPS = 60f;
    private bool isFPSStable = true;
    
    // FPS history for stability calculation
    private Queue<float> fpsHistory = new Queue<float>();
    private float lastUpdateTime = 0f;
    private int frameCount = 0;
    private float timeAccumulator = 0f;
    
    // Performance level (0 = low, 1 = medium, 2 = high)
    public int PerformanceLevel { get; private set; } = 2;
    
    public float CurrentFPS => currentFPS;
    public float AverageFPS => averageFPS;
    public bool IsFPSStable => isFPSStable;
    public bool CanUseHeavyEffects => isFPSStable && averageFPS >= stableFPSThreshold;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Update()
    {
        frameCount++;
        timeAccumulator += Time.unscaledDeltaTime;
        
        // Update FPS every updateInterval seconds
        if (timeAccumulator >= updateInterval)
        {
            currentFPS = frameCount / timeAccumulator;
            
            // Add to history
            fpsHistory.Enqueue(currentFPS);
            if (fpsHistory.Count > stabilitySampleCount)
            {
                fpsHistory.Dequeue();
            }
            
            // Calculate average FPS
            float sum = 0f;
            foreach (float fps in fpsHistory)
            {
                sum += fps;
            }
            averageFPS = sum / fpsHistory.Count;
            
            // Check stability (low variance = stable)
            float variance = 0f;
            foreach (float fps in fpsHistory)
            {
                variance += Mathf.Pow(fps - averageFPS, 2);
            }
            variance /= fpsHistory.Count;
            float stdDev = Mathf.Sqrt(variance);
            
            // FPS is stable if standard deviation is low
            isFPSStable = stdDev < 5f && fpsHistory.Count >= stabilitySampleCount;
            
            // Determine performance level
            if (averageFPS >= 50f)
                PerformanceLevel = 2; // High
            else if (averageFPS >= 30f)
                PerformanceLevel = 1; // Medium
            else
                PerformanceLevel = 0; // Low
            
            // Reset counters
            frameCount = 0;
            timeAccumulator = 0f;
        }
    }
    
    /// <summary>
    /// Get recommended effect type based on current performance
    /// </summary>
    public bool ShouldUseHeavyEffect()
    {
        return CanUseHeavyEffects && PerformanceLevel >= 1;
    }
    
    /// <summary>
    /// Get recommended update interval based on performance
    /// </summary>
    public float GetRecommendedUpdateInterval()
    {
        if (PerformanceLevel == 0)
            return 0.15f; // ~6.7fps on low-end
        else if (PerformanceLevel == 1)
            return 0.1f; // ~10fps on medium
        else
            return 0.033f; // ~30fps on high-end
    }
}

