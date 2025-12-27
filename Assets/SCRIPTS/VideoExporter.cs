using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System;
using System.Collections;
using System.IO;
using NatSuite.Recorders;
using NatSuite.Recorders.Clocks;
using NatSuite.Recorders.Inputs;

public class VideoExporter : MonoBehaviour
{
    [Header("References")]
    public VideoPlayer videoPlayer;
    public Camera mainCamera;
    
    [Header("UI References")]
    public Button exportButton;
    public Component exportButtonTextComponent;
    [Tooltip("Loading panel/curtain that shows during export (deactivated by default)")]
    public GameObject loadingPanel;
    [Tooltip("Canvas to hide during recording (auto-found if not assigned)")]
    public Canvas uiCanvas;
    
    [Header("Export Settings")]
    [Tooltip("Output video resolution (lower = less lag)")]
    public int exportWidth = 480; // Very low for smooth recording
    [Tooltip("Output video resolution (lower = less lag)")]
    public int exportHeight = 854; // Very low for smooth recording
    [Tooltip("Frame rate for exported video (lower = less lag)")]
    public int frameRate = 15; // Very low for smooth recording
    
    private MP4Recorder recorder;
    private CameraInput cameraInput;
    private AudioInput audioInput;
    private RealtimeClock clock;
    private bool isExporting = false;
    private string recordedVideoPath = "";
    private VideoAudioOutputMode originalAudioMode; // Store original audio mode
    
    void Start()
    {
        // Auto-find references
        if (videoPlayer == null)
            videoPlayer = FindFirstObjectByType<VideoPlayer>();
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        // Auto-find export button
        if (exportButton == null)
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (Button btn in buttons)
            {
                if (btn.name.Contains("Export") || btn.name.Contains("EXPORT"))
                {
                    exportButton = btn;
                    break;
                }
            }
        }
        
        // Auto-find button text
        if (exportButtonTextComponent == null && exportButton != null)
        {
            Text text = exportButton.GetComponentInChildren<Text>();
            if (text != null)
                exportButtonTextComponent = text;
            else
            {
                TMPro.TextMeshProUGUI tmpText = exportButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmpText != null)
                    exportButtonTextComponent = tmpText;
            }
        }
        
        // Connect export button
        if (exportButton != null)
        {
            exportButton.onClick.RemoveAllListeners();
            exportButton.onClick.AddListener(() => StartCoroutine(ExportVideoCoroutine()));
            // Disable export button by default
            exportButton.interactable = false;
        }
        
        // Auto-find Canvas if not assigned
        if (uiCanvas == null)
        {
            uiCanvas = FindFirstObjectByType<Canvas>();
        }
        
        // Hide loading panel
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }
    
    public void OnVideoStarted()
    {
        // Disable export button when video starts
        if (exportButton != null)
        {
            exportButton.interactable = false;
        }
        
        UpdateExportButtonText("EXPORT");
    }
    
    public void OnVideoEnded()
    {
        // Enable export button when video ends
        if (exportButton != null)
        {
            exportButton.interactable = true;
        }
        
        UpdateExportButtonText("EXPORT");
    }
    
    IEnumerator ExportVideoCoroutine()
    {
        if (isExporting)
        {
            Debug.LogWarning("VideoExporter: Export already in progress!");
            yield break;
        }
        
        if (videoPlayer == null || string.IsNullOrEmpty(videoPlayer.url))
        {
            Debug.LogError("VideoExporter: No video to export!");
            UpdateExportButtonText("NO VIDEO");
            yield break;
        }
        
        isExporting = true;
        bool exportSuccess = false;
        string errorMessage = "";
        
        // Show loading panel (will be hidden before recording)
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
        
        UpdateExportButtonText("PREPARING...");
        if (exportButton != null)
            exportButton.interactable = false;
        
        // Wait a frame to show loading
        yield return new WaitForSeconds(0.5f);
        
        // HIDE UI BEFORE RECORDING
        bool canvasWasEnabled = false;
        if (uiCanvas != null)
        {
            canvasWasEnabled = uiCanvas.enabled;
            uiCanvas.enabled = false;
        }
        
        // Hide loading panel before recording
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        
        // MUTE AUDIO LISTENER (user won't hear, but AudioSource will still record)
        float originalListenerVolume = AudioListener.volume;
        AudioListener.volume = 0f;
        Debug.Log($"VideoExporter: AudioListener muted (volume: {AudioListener.volume}) - user won't hear audio during export");
        
        // Get effect timeline
        EffectTimelineTracker tracker = FindFirstObjectByType<EffectTimelineTracker>();
        var timeline = tracker != null ? tracker.GetEffectTimeline() : null;
        
        // Check if video has audio (check BEFORE stopping video)
        bool hasAudio = videoPlayer.audioTrackCount > 0;
        Debug.Log($"VideoExporter: Video has audio: {hasAudio} (audioTrackCount: {videoPlayer.audioTrackCount})");
        
        // Store original state
        string originalUrl = videoPlayer.url;
        originalAudioMode = videoPlayer.audioOutputMode;
        double originalTime = videoPlayer.time;
        bool wasPlaying = videoPlayer.isPlaying;
        
        // Stop current playback
        videoPlayer.Stop();
        yield return new WaitForSeconds(0.2f); // Delay to prevent freeze
        
        // Setup AudioSource BEFORE preparing video (so VideoPlayer can connect to it)
        AudioSource audioSourceForRecording = null;
        if (hasAudio)
        {
            audioSourceForRecording = videoPlayer.GetComponent<AudioSource>();
            if (audioSourceForRecording == null)
            {
                audioSourceForRecording = videoPlayer.gameObject.AddComponent<AudioSource>();
            }
            
            // Configure AudioSource for recording
            audioSourceForRecording.enabled = true;
            audioSourceForRecording.mute = false; // UNMUTED for recording
            audioSourceForRecording.volume = 1f; // Full volume
            audioSourceForRecording.playOnAwake = false;
            audioSourceForRecording.bypassEffects = true;
            audioSourceForRecording.loop = false;
            
            // Set VideoPlayer to AudioSource mode BEFORE preparing
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, audioSourceForRecording);
            
            Debug.Log("VideoExporter: AudioSource configured BEFORE video prepare");
        }
        
        // Prepare video
        videoPlayer.url = originalUrl;
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
            yield return null;
        
        Debug.Log($"VideoExporter: Video prepared - Audio tracks: {videoPlayer.audioTrackCount}, Has audio: {hasAudio}");
        
        yield return new WaitForSeconds(0.2f); // Delay before recording
        
        // Get output path
        recordedVideoPath = GetExportPath();
        
        // DISABLE BEAT DETECTION DURING RECORDING (reduces load)
        AudioBeatDetector.IsRecordingActive = true;
        Debug.Log("VideoExporter: Beat detection disabled during recording");
        
        // Start recording
        try
        {
            StartRecording();
        }
        catch (Exception e)
        {
            errorMessage = $"Failed to start recording: {e.Message}";
            Debug.LogError($"❌ VideoExporter: {errorMessage}");
        }
        
        if (string.IsNullOrEmpty(errorMessage))
        {
            yield return new WaitForSeconds(0.3f); // Delay after recording starts
            
            // Setup effects (but beat detection is disabled, so effects use timeline only)
            BeatEffectController effectController = FindFirstObjectByType<BeatEffectController>();
            if (effectController != null)
            {
                // Don't disable auto mode completely - just skip warmup
                // Effects will be triggered manually from timeline
                effectController.SkipWarmup();
                
                if (timeline != null && timeline.Count > 0)
                {
                    Debug.Log($"VideoExporter: Starting timeline replay with {timeline.Count} effects");
                    effectController.ReplayEffectTimeline(timeline);
                }
                else
                {
                    Debug.LogWarning("VideoExporter: No timeline found - effects won't replay!");
                }
            }
            else
            {
                Debug.LogError("VideoExporter: BeatEffectController not found!");
            }
            
            yield return new WaitForSeconds(0.5f); // Longer delay before playing
            
            // Play video
            videoPlayer.Play();
            
            // Wait for video to actually start
            yield return new WaitForSeconds(0.3f);
            
            // Verify audio is working
            if (hasAudio)
            {
                AudioSource audioSource = videoPlayer.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    Debug.Log($"VideoExporter: After play - AudioSource playing: {audioSource.isPlaying}, enabled: {audioSource.enabled}, volume: {audioSource.volume}, mute: {audioSource.mute}");
                    Debug.Log($"VideoExporter: VideoPlayer audio mode: {videoPlayer.audioOutputMode}, audio track count: {videoPlayer.audioTrackCount}");
                    
                    // CRITICAL: Verify AudioSource is NOT muted (must be false for recording)
                    if (audioSource.mute)
                    {
                        Debug.LogError("❌ VideoExporter: AudioSource is MUTED! Audio won't record! Fixing...");
                        audioSource.mute = false;
                    }
                    
                    // CRITICAL: Verify AudioSource is enabled
                    if (!audioSource.enabled)
                    {
                        Debug.LogError("❌ VideoExporter: AudioSource is DISABLED! Audio won't record! Fixing...");
                        audioSource.enabled = true;
                    }
                    
                    // CRITICAL: Verify AudioListener is muted (user shouldn't hear)
                    if (AudioListener.volume > 0f)
                    {
                        Debug.LogWarning($"VideoExporter: AudioListener volume is {AudioListener.volume} (should be 0 for silent export)");
                    }
                }
                else
                {
                    Debug.LogError("❌ VideoExporter: AudioSource is NULL! Audio won't record!");
                }
            }
            
            // Verify effects are ready
            if (effectController != null)
            {
                Debug.Log($"VideoExporter: Effect controller ready - AutoMode: {effectController.AutoModeEnabled}, Timeline replay active: {timeline != null && timeline.Count > 0}");
                
                if (timeline == null || timeline.Count == 0)
                {
                    Debug.LogWarning("⚠️ VideoExporter: No effect timeline - effects won't replay during export!");
                }
            }
            else
            {
                Debug.LogError("❌ VideoExporter: BeatEffectController is NULL! Effects won't work!");
            }
            
            // Wait a bit more for everything to stabilize
            yield return new WaitForSeconds(0.2f);
            
            if (!videoPlayer.isPlaying)
            {
                errorMessage = "Video playback failed";
            }
            else
            {
                // Wait for video to finish (with delays to prevent freeze)
                float lastLogTime = 0f;
                while (videoPlayer.isPlaying)
                {
                    // Log progress every 2 seconds (not every frame)
                    if (Time.time - lastLogTime > 2f)
                    {
                        Debug.Log($"VideoExporter: Recording... {videoPlayer.time:F1}s");
                        lastLogTime = Time.time;
                    }
                    
                    // Yield every 2-3 frames to reduce load (skip some frames)
                    yield return null;
                    yield return null;
                    yield return null;
                }
                
                // Stop recording
                yield return StartCoroutine(StopRecordingCoroutine());
                
                // Restore audio mode
                videoPlayer.audioOutputMode = originalAudioMode;
                
                exportSuccess = true;
            }
        }
        
        // RE-ENABLE BEAT DETECTION
        AudioBeatDetector.IsRecordingActive = false;
        Debug.Log("VideoExporter: Beat detection re-enabled");
        
        // RESTORE AUDIO LISTENER
        AudioListener.volume = originalListenerVolume;
        Debug.Log($"VideoExporter: AudioListener restored (volume: {AudioListener.volume})");
        
        if (uiCanvas != null)
            uiCanvas.enabled = canvasWasEnabled;
        
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        
        UpdateExportButtonText(exportSuccess ? "EXPORT" : "ERROR");
        if (exportButton != null)
            exportButton.interactable = true;
        
        isExporting = false;
        
        if (exportSuccess)
            Debug.Log($"✅ Export complete! {recordedVideoPath}");
    }
    
    void StartRecording()
    {
        // Create clock
        clock = new RealtimeClock();
        
        // Check if video has audio
        bool hasAudio = videoPlayer.audioTrackCount > 0;
        int sampleRate = hasAudio ? AudioSettings.outputSampleRate : 0;
        int channelCount = hasAudio ? 1 : 0; // Mono for video audio
        
        Debug.Log($"VideoExporter: Audio check - hasAudio: {hasAudio}, sampleRate: {sampleRate}, channelCount: {channelCount}");
        
        // Create recorder
        recorder = new MP4Recorder(exportWidth, exportHeight, frameRate, sampleRate, channelCount, audioBitRate: 96_000);
        
        // Verify camera exists
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            Debug.LogWarning("VideoExporter: mainCamera was null, using Camera.main");
        }
        
        if (mainCamera == null)
        {
            throw new Exception("No camera found for recording!");
        }
        
        Debug.Log($"VideoExporter: Recording from camera: {mainCamera.name}, Position: {mainCamera.transform.position}, Enabled: {mainCamera.enabled}");
        
        // Create camera input with frame skip for performance
        cameraInput = new CameraInput(recorder, clock, mainCamera);
        cameraInput.frameSkip = 1; // Skip every other frame (record at ~12fps effective)
        Debug.Log("VideoExporter: CameraInput frameSkip set to 1 for performance");
        
        // Setup audio input if video has audio
        if (hasAudio)
        {
            // Get the AudioSource we configured earlier (before video prepare)
            AudioSource audioSource = videoPlayer.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("VideoExporter: AudioSource not found! Audio recording will fail.");
            }
            else
            {
                // Verify AudioSource is still configured correctly
                audioSource.enabled = true;
                audioSource.mute = false; // Must be unmuted for recording
                audioSource.volume = 1f; // Full volume for recording
                
                // Verify VideoPlayer is configured for AudioSource mode
                if (videoPlayer.audioOutputMode != VideoAudioOutputMode.AudioSource)
                {
                    Debug.LogWarning($"VideoExporter: VideoPlayer audio mode is {videoPlayer.audioOutputMode}, should be AudioSource. Fixing...");
                    videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                    videoPlayer.EnableAudioTrack(0, true);
                    videoPlayer.SetTargetAudioSource(0, audioSource);
                }
                
                // Create audio input (records from AudioSource)
                // AudioSource will receive audio when VideoPlayer plays
                audioInput = new AudioInput(recorder, clock, audioSource, true);
                
                Debug.Log($"VideoExporter: ✅ Audio input created - AudioSource enabled: {audioSource.enabled}, volume: {audioSource.volume}, mute: {audioSource.mute}, VideoPlayer mode: {videoPlayer.audioOutputMode}");
            }
        }
        else
        {
            Debug.LogWarning("VideoExporter: Video has no audio tracks - recording without audio");
        }
        
        Debug.Log($"🎬 VideoExporter: Recording started - {exportWidth}x{exportHeight} @ {frameRate}fps, Audio: {hasAudio}");
    }
    
    IEnumerator StopRecordingCoroutine()
    {
        // Dispose inputs
        audioInput?.Dispose();
        cameraInput?.Dispose();
        
        // Finish recording (async operation)
        var finishTask = recorder.FinishWriting();
        recorder = null;
        
        // Wait for task to complete
        while (!finishTask.IsCompleted)
        {
            yield return null;
        }
        
        // Get result
        if (finishTask.IsFaulted)
        {
            Debug.LogError($"VideoExporter: Recording failed: {finishTask.Exception}");
            recordedVideoPath = "";
        }
        else
        {
            recordedVideoPath = finishTask.Result;
            Debug.Log($"✅ VideoExporter: Recording stopped. File: {recordedVideoPath}");
        }
        
        // Save to gallery (Android/iOS)
        #if UNITY_ANDROID || UNITY_IOS
        if (File.Exists(recordedVideoPath))
        {
            // Use NativeGallery or similar to save to gallery
            // For now, just log the path
            Debug.Log($"📱 Video saved. Use NativeGallery to save to gallery: {recordedVideoPath}");
        }
        #endif
    }
    
    string GetExportPath()
    {
        string fileName = $"VideoFX_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
        string directory = Path.Combine(Application.persistentDataPath, "Exports");
        
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        
        return Path.Combine(directory, fileName);
    }
    
    void UpdateExportButtonText(string text)
    {
        if (exportButtonTextComponent != null)
        {
            if (exportButtonTextComponent is Text uiText)
                uiText.text = text;
            else if (exportButtonTextComponent is TMPro.TextMeshProUGUI tmpText)
                tmpText.text = text;
        }
    }
}
