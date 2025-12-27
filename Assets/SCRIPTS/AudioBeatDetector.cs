using UnityEngine;
using UnityEngine.Video;
using System;

public class AudioBeatDetector : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Beat Detection Settings")]
    [Range(0.5f, 3f)]
    [Tooltip("Higher = fewer beats detected. Lower = more sensitive")]
    public float sensitivity = 1.3f;
    
    [Range(0.1f, 0.5f)]
    [Tooltip("Minimum time between beats (seconds)")]
    public float minBeatInterval = 0.25f;
    
    [Range(1, 50)]
    [Tooltip("Number of frequency bins to analyze (bass range)")]
    public int bassRange = 20;
    
    [Header("Energy Analysis")]
    [Range(0.01f, 0.2f)]
    [Tooltip("How fast the average energy adapts")]
    public float smoothingSpeed = 0.05f;

    // Beat detection data
    float[] spectrum = new float[1024];
    float lastBeatTime;
    float averageEnergy;
    float currentEnergy;
    
    // Beat strength (0-1)
    public float BeatStrength { get; private set; }
    
    // VideoPlayer reference (for checking if video is playing and Direct mode)
    private VideoPlayer videoPlayer;
    private bool useDirectMode = false;
    private bool useFallbackMode = false;
    
    // Fallback mode: Time-based beats when spectrum data unavailable
    private float lastFallbackBeatTime = 0f;
    [Header("Fallback Mode (Time-Based)")]
    [Range(0.3f, 2f)]
    [Tooltip("Beat interval in seconds when using fallback mode")]
    public float fallbackBeatInterval = 0.5f;
    
    // Events
    public event Action OnBeat; // Fired when beat detected
    public event Action<float> OnBeatWithStrength; // Fired with beat strength (0-1)

    void Start()
    {
        // Auto-find AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = FindObjectOfType<AudioSource>();
            if (audioSource != null)
            {
                Debug.Log($"AudioBeatDetector: Auto-found AudioSource: {audioSource.name}");
            }
            else
            {
                Debug.LogWarning("AudioBeatDetector: No AudioSource found! Beat detection won't work.");
            }
        }
        
        // Find VideoPlayer to check if video is actually playing
        videoPlayer = FindObjectOfType<VideoPlayer>();
    }

    public void SwitchToDirectMode(VideoPlayer vp)
    {
        useDirectMode = true;
        videoPlayer = vp;
        Debug.Log("AudioBeatDetector: Switched to Direct mode - audio will play but beat detection disabled (Direct mode limitation)");
    }
    
    // NEW: Setup for Direct mode playback with AudioSource analysis
    public void SetupDirectModeWithAnalysis(VideoPlayer vp, AudioSource analysisSource)
    {
        useDirectMode = false; // We can use AudioSource for analysis
        useFallbackMode = false;
        videoPlayer = vp;
        audioSource = analysisSource;
        
        // Reset detection state
        averageEnergy = 0f;
        lastBeatTime = 0f;
        warmupStartTime = 0f;
        
        Debug.Log("AudioBeatDetector: ✅ Direct mode playback + AudioSource analysis enabled! Real beat detection active.");
    }
    
    public void EnableFallbackMode(VideoPlayer vp)
    {
        useFallbackMode = true;
        videoPlayer = vp;
        lastFallbackBeatTime = 0f;
        warmupStartTime = 0f; // Reset warmup
        Debug.Log($"AudioBeatDetector: Enabled fallback mode - using time-based beats (interval: {fallbackBeatInterval}s). " +
            "Effects will sync to video playback time instead of audio spectrum analysis.");
    }
    
    // Reset warmup when new video starts
    public void OnVideoStarted()
    {
        warmupStartTime = 0f;
        averageEnergy = 0f; // Reset energy baseline
    }

    private float lastUpdateTime = 0f;
#if UNITY_ANDROID || UNITY_IOS
    private const float UPDATE_INTERVAL = 0.3f; // Update every 300ms (~3.3fps) on mobile for better performance
#else
    private const float UPDATE_INTERVAL = 0.2f; // Update every 200ms (5fps) on desktop
#endif
    private float warmupStartTime = 0f;
    private const float WARMUP_DURATION = 3f; // Skip heavy processing for first 3 seconds to let audio buffer stabilize
    private float lastAudioTime = 0f;
    private int stuckAudioFrames = 0;
    
    // Mute detection
    private bool isMuted = false;
    private float lastMuteCheckTime = 0f;
    private const float MUTE_CHECK_INTERVAL = 0.1f; // Check mute state every 100ms
    
    // Recording detection - pause beat detection during video recording to prevent buffer overflow
    public static bool IsRecordingActive { get; set; } = false;

    void Update()
    {
        // Throttle updates to reduce CPU usage and prevent audio stuttering
        float currentTime = Time.time;
        if (currentTime - lastUpdateTime < UPDATE_INTERVAL)
            return;
        
        lastUpdateTime = currentTime;
        
        // Warmup period - skip heavy processing during initial video load
        if (warmupStartTime == 0f && videoPlayer != null && videoPlayer.isPlaying)
        {
            warmupStartTime = currentTime;
        }
        
        bool inWarmup = warmupStartTime > 0f && (currentTime - warmupStartTime) < WARMUP_DURATION;
        
        // Check if VideoPlayer is playing
        if (videoPlayer == null)
            videoPlayer = FindObjectOfType<VideoPlayer>();
            
        if (videoPlayer != null && !videoPlayer.isPlaying)
        {
            return;
        }
        
        // CRITICAL: Skip beat detection during recording to prevent audio buffer overflow
        // Effects are already being recorded, so we don't need to detect beats during recording
        if (IsRecordingActive)
        {
            return; // Skip all processing during recording
        }
        
        // Check mute state periodically
        if (currentTime - lastMuteCheckTime >= MUTE_CHECK_INTERVAL)
        {
            lastMuteCheckTime = currentTime;
            bool wasMuted = isMuted;
            
            // Check if audio is muted (AudioListener volume = 0 or system mute)
            AudioListener listener = FindObjectOfType<AudioListener>();
            if (listener != null)
            {
                isMuted = AudioListener.volume <= 0.01f || !listener.enabled;
            }
            else
            {
                // Fallback: Check AudioSource mute state (for Direct mode, check if analysis source is the only one)
                if (audioSource != null)
                {
                    // If audioSource is muted AND it's the one we're using for analysis,
                    // check if there's another audio source that's not muted
                    // For Direct mode, we check AudioListener volume
                    isMuted = AudioListener.volume <= 0.01f;
                }
            }
            
            // If mute state changed, reset energy baseline
            if (wasMuted != isMuted)
            {
                if (isMuted)
                {
                    Debug.Log("AudioBeatDetector: Audio muted - effects paused");
                    averageEnergy = 0f; // Reset baseline
                }
                else
                {
                    Debug.Log("AudioBeatDetector: Audio unmuted - effects resumed");
                    averageEnergy = 0f; // Reset baseline for fresh start
                }
            }
        }
        
        // If muted, don't process beats
        if (isMuted)
        {
            return;
        }
        
        // Direct mode: Can't read spectrum data, so beat detection won't work
        if (useDirectMode)
        {
            return; // Beat detection not available in Direct mode
        }
        
        // Fallback mode: Use time-based beats when spectrum data unavailable
        if (useFallbackMode)
        {
            // Don't generate beats if muted
            if (isMuted)
            {
                return;
            }
            
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                float videoTime = (float)videoPlayer.time;
                
                // Generate beats at regular intervals based on video time
                if (videoTime - lastFallbackBeatTime >= fallbackBeatInterval)
                {
                    lastFallbackBeatTime = videoTime;
                    lastBeatTime = Time.time;
                    BeatStrength = 0.7f; // Medium strength for fallback beats
                    
                    // Fire events ASYNCHRONOUSLY (non-blocking)
                    OnBeat?.Invoke();
                    OnBeatWithStrength?.Invoke(BeatStrength);
                }
            }
            return;
        }
        
        if (audioSource == null)
        {
            audioSource = FindObjectOfType<AudioSource>();
            if (audioSource == null)
                return;
        }
        
        // Check if AudioSource is actually playing before reading spectrum data
        // This prevents buffer overflow by not reading when audio isn't ready
        if (!audioSource.isPlaying)
        {
            return; // Audio not playing, skip this frame
        }
        
        // Check if audio time is progressing (prevents reading when buffer is stuck)
        float currentAudioTime = audioSource.time;
        if (currentAudioTime <= 0f)
        {
            return; // Audio not started yet
        }
        
        // If audio time hasn't changed, buffer might be stuck - skip reading
        if (Mathf.Approximately(currentAudioTime, lastAudioTime))
        {
            stuckAudioFrames++;
            if (stuckAudioFrames > 3) // If stuck for 3+ frames, skip reading
            {
                return; // Audio buffer stuck, skip to prevent overflow
            }
        }
        else
        {
            stuckAudioFrames = 0; // Reset counter if audio is progressing
            lastAudioTime = currentAudioTime;
        }
        
        // During warmup, use lighter processing
        if (inWarmup)
        {
            // Skip heavy FFT during warmup - just establish baseline
            try
            {
                // Use smaller spectrum array and lighter window during warmup
                float[] smallSpectrum = new float[512]; // Smaller array
                audioSource.GetSpectrumData(smallSpectrum, 0, FFTWindow.Rectangular); // Lighter window
                
                // Copy to main spectrum array
                for (int i = 0; i < Mathf.Min(smallSpectrum.Length, spectrum.Length); i++)
                {
                    spectrum[i] = smallSpectrum[i];
                }
            }
            catch (System.Exception)
            {
                return; // Buffer overflow or other error - skip this frame
            }
            
            // Minimal energy calculation during warmup
            currentEnergy = 0f;
            for (int i = 1; i < Mathf.Min(10, bassRange) && i < spectrum.Length; i++) // Only first 10 bins
            {
                currentEnergy += spectrum[i];
            }
        }
        else
        {
            // Normal processing after warmup - but still use smaller array to prevent overflow
            try
            {
                // Use 512 instead of 1024 to reduce buffer pressure
                float[] smallSpectrum = new float[512];
                audioSource.GetSpectrumData(smallSpectrum, 0, FFTWindow.BlackmanHarris);
                
                // Copy to main spectrum array
                for (int i = 0; i < Mathf.Min(smallSpectrum.Length, spectrum.Length); i++)
                {
                    spectrum[i] = smallSpectrum[i];
                }
            }
            catch (System.Exception)
            {
                return; // Buffer overflow or other error - skip this frame
            }

            // Calculate low-frequency energy (bass range - where beats live)
            currentEnergy = 0f;
            for (int i = 1; i < bassRange && i < spectrum.Length; i++)
            {
                currentEnergy += spectrum[i];
            }
        }

        // CRITICAL: If energy is zero for too long, switch to fallback mode
        if (currentEnergy < 0.0001f)
        {
            // Reset average if we have no audio for too long (prevents stale data)
            if (Time.time - lastBeatTime > 2f)
            {
                averageEnergy = 0f;
                
                // After 3 seconds of no audio data, enable fallback mode
                if (Time.time - lastBeatTime > 3f && videoPlayer != null)
                {
                    EnableFallbackMode(videoPlayer);
                    return;
                }
            }
            return; // No audio data - exit early, NO BEATS
        }

        // Moving average for adaptive threshold
        averageEnergy = Mathf.Lerp(averageEnergy, currentEnergy, smoothingSpeed);

        // CRITICAL: Only process beats if we have established a baseline (averageEnergy > threshold)
        // This prevents false beats at the start or when audio isn't ready
        if (averageEnergy < 0.0001f)
        {
            return; // Still establishing baseline - no beats yet
        }

        // Calculate beat strength (0-1)
        float energyDifference = currentEnergy - averageEnergy;
        if (averageEnergy > 0.0001f)
        {
            BeatStrength = Mathf.Clamp01(energyDifference / (averageEnergy * sensitivity));
        }
        else
        {
            BeatStrength = 0f;
        }
        
        // Beat detection: energy spike above threshold + minimum interval
        // CRITICAL: Ensure we have REAL energy spike, not just noise
        if (currentEnergy > averageEnergy * sensitivity &&
            currentEnergy > 0.0001f && // Must have actual energy
            averageEnergy > 0.0001f && // Must have established baseline
            energyDifference > 0.0001f && // Must be a real spike
            Time.time - lastBeatTime > minBeatInterval)
        {
            lastBeatTime = Time.time;
            
            // Fire events
            OnBeat?.Invoke();
            OnBeatWithStrength?.Invoke(BeatStrength);
        }
    }

    // Manual beat trigger (for testing)
    public void TriggerBeat()
    {
        OnBeat?.Invoke();
        OnBeatWithStrength?.Invoke(1f);
    }

    // Get current audio energy (for visualizers)
    public float GetCurrentEnergy()
    {
        return currentEnergy;
    }

    // Get average energy (for debugging)
    public float GetAverageEnergy()
    {
        return averageEnergy;
    }
}

