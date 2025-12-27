using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEditor;
#endif

public class VideoUploadController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public AudioSource audioSource; // For beat detection
    
    [Header("UI References")]
    [Tooltip("The upload button to hide/show")]
    public Button uploadButton;
    
    [Tooltip("Button text component (UnityEngine.UI.Text or TMPro.TextMeshProUGUI)")]
    public Component buttonTextComponent;
    
    [Header("Export")]
    [Tooltip("Reference to VideoExporter component")]
    public VideoExporter videoExporter;
    
    [Header("Randomize Button")]
    [Tooltip("Reference to RandomizeButtonController component")]
    public RandomizeButtonController randomizeButtonController;
    
    private string originalButtonText = "UPLOAD";
    private bool isFirstUpload = true;
    private bool videoHasEnded = false; // Track if video actually ended vs just stuck
    private EffectPresetManager presetManager;

    public void UploadVideo()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        PickVideo_PC();
#elif UNITY_ANDROID || UNITY_IOS
        PickVideo_Mobile();
#endif
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    void PickVideo_PC()
    {
        string path = EditorUtility.OpenFilePanel(
            "Select a video",
            "",
            "mp4,mov"
        );

        if (!string.IsNullOrEmpty(path))
        {
            PlayVideo(path);
        }
    }
#endif

#if UNITY_ANDROID || UNITY_IOS
    void PickVideo_Mobile()
    {
        NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
                return;

            PlayVideo(path);
        },
        new string[] { "video/*" });
    }
#endif

    void Start()
    {
        // Subscribe to video events
        if (videoPlayer != null)
        {
            videoPlayer.started += OnVideoStarted;
            videoPlayer.loopPointReached += OnVideoEnded;
        }
        
        // Auto-find button if not assigned
        if (uploadButton == null)
        {
            // Try to find Upload button specifically
            Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (Button btn in buttons)
            {
                if (btn.name.Contains("Upload") || btn.name.Contains("UPLOAD"))
                {
                    uploadButton = btn;
                    break;
                }
            }
        }
        
        // Auto-find button text if not assigned
        if (buttonTextComponent == null && uploadButton != null)
        {
            // Try UnityEngine.UI.Text first
            Text text = uploadButton.GetComponentInChildren<Text>();
            if (text != null)
            {
                buttonTextComponent = text;
                originalButtonText = text.text;
            }
            else
            {
                // Try TMPro
                TMPro.TextMeshProUGUI tmpText = uploadButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmpText != null)
                {
                    buttonTextComponent = tmpText;
                    originalButtonText = tmpText.text;
                }
            }
        }
        
// Auto-find VideoExporter if not assigned
        if (videoExporter == null)
        {
            videoExporter = FindFirstObjectByType<VideoExporter>();
        }
        
        // Auto-find RandomizeButtonController if not assigned
        if (randomizeButtonController == null)
        {
            randomizeButtonController = FindFirstObjectByType<RandomizeButtonController>();
        }
        
        // Get or create preset manager
        if (presetManager == null)
        {
            presetManager = EffectPresetManager.Instance;
            if (presetManager == null)
            {
                GameObject presetObj = new GameObject("EffectPresetManager");
                presetManager = presetObj.AddComponent<EffectPresetManager>();
            }
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (videoPlayer != null)
        {
            videoPlayer.started -= OnVideoStarted;
            videoPlayer.loopPointReached -= OnVideoEnded;
        }
    }
    
    void Update()
    {
        // Only show button if video actually ended, not if it's just stuck/paused
        // Don't check isPlaying here - that causes button to show when video is stuck
    }
    
    void PlayVideo(string path)
    {
        videoPlayer.Stop();
        videoHasEnded = false; // Reset end state when new video loads
        
        // Setup parallel AudioSource for beat detection (even in Direct mode)
        // This AudioSource will be used ONLY for spectrum analysis, not playback
        if (audioSource == null)
        {
            audioSource = videoPlayer.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                // Create AudioSource on VideoPlayer GameObject for beat detection
                audioSource = videoPlayer.gameObject.AddComponent<AudioSource>();
                Debug.Log("VideoUploadController: Created AudioSource for beat detection");
            }
        }
        
        // Configure AudioSource for analysis (hidden, muted for user)
        if (audioSource != null)
        {
            audioSource.enabled = true;
            audioSource.mute = true; // Mute this one - Direct mode handles playback
            audioSource.volume = 1f; // Keep volume at 1 for analysis
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
        
        videoPlayer.url = path;
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnPrepared;
    }

    void OnPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnPrepared;
        
        // CRITICAL: Check if video has audio tracks
        if (vp.audioTrackCount == 0)
        {
            Debug.LogError("VideoUploadController: Video has no audio tracks! Cannot enable beat detection.");
            vp.Play();
            return;
        }
        
        // Use Direct mode for playback to prevent audio buffer conflicts with Unity Recorder
        // Direct mode plays audio directly without using AudioSource, preventing buffer overflow
        vp.audioOutputMode = VideoAudioOutputMode.Direct;
        
        // Ensure AudioSource exists for beat detection
        if (audioSource == null)
        {
            audioSource = vp.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = vp.gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Configure AudioSource ONLY for beat detection (not playback)
        // Direct mode handles playback, AudioSource is only for spectrum analysis
        audioSource.enabled = true;
        audioSource.mute = true; // Muted - Direct mode handles audible playback
        audioSource.volume = 1f; // Full volume for analysis (muted doesn't affect spectrum data)
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        
        // Optimize AudioSource settings to prevent buffer overflow
        audioSource.bypassEffects = true; // Bypass effects to reduce processing
        audioSource.bypassListenerEffects = true; // Bypass listener effects
        audioSource.bypassReverbZones = true; // Bypass reverb zones
        audioSource.priority = 256; // Lowest priority to reduce conflicts
        
        // CRITICAL: Enable audio track
        vp.EnableAudioTrack(0, true);
        
        // Set AudioSource as target for analysis (Direct mode handles playback)
        vp.SetTargetAudioSource(0, audioSource);
        
        // Ensure AudioListener exists
        if (FindFirstObjectByType<AudioListener>() == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.GetComponent<AudioListener>() == null)
            {
                mainCam.gameObject.AddComponent<AudioListener>();
            }
        }
        
        // Create/load preset on first video upload
        if (presetManager != null && isFirstUpload)
        {
            Debug.Log("📹 VideoUploadController: First video upload detected - loading/creating preset...");
            presetManager.LoadOrCreatePreset();
            
            // Apply preset to effects
            BeatEffectController controller = FindFirstObjectByType<BeatEffectController>();
            if (controller != null && randomizeButtonController != null)
            {
                presetManager.ApplyPresetToEffects(controller, randomizeButtonController);
            }
            
            isFirstUpload = false;
            Debug.Log($"📁 Preset file location: {presetManager.GetPresetFilePath()}");
            Debug.Log("💡 TIP: You can edit the JSON file and restart the app to test new values!");
        }
        
        // Play video in AudioSource mode (audio is audible in editor)
        vp.Play();
        
        // Start beat detection using AudioSource
        StartCoroutine(SetupDirectModeBeatDetection(vp));
    }
    
    void OnVideoStarted(VideoPlayer vp)
    {
        // Hide upload button when video starts
        videoHasEnded = false;
        if (uploadButton != null)
        {
            uploadButton.gameObject.SetActive(false);
        }
        
        // Hide export button when video starts
        if (videoExporter != null)
        {
            videoExporter.OnVideoStarted();
        }
        
        // Clear effect timeline for new video
        EffectTimelineTracker tracker = FindFirstObjectByType<EffectTimelineTracker>();
        if (tracker != null)
        {
            tracker.ClearTimeline();
        }
        
        // Notify controllers to reset warmup
        BeatEffectController effectController = FindFirstObjectByType<BeatEffectController>();
        if (effectController != null)
        {
            effectController.OnVideoStarted();
        }
        
        AudioBeatDetector beatDetector = FindFirstObjectByType<AudioBeatDetector>();
        if (beatDetector != null)
        {
            beatDetector.OnVideoStarted();
        }
        
        // Enable randomize button when video starts
        if (randomizeButtonController != null)
        {
            randomizeButtonController.EnableRandomizeButton();
        }
    }
    
    void OnVideoEnded(VideoPlayer vp)
    {
        // Only show button when video ACTUALLY ends (reaches end)
        videoHasEnded = true;
        if (uploadButton != null)
        {
            uploadButton.gameObject.SetActive(true);
            
            // Update button text
            UpdateButtonText();
        }
        
        // Show and enable export button when video ends
        if (videoExporter != null)
        {
            videoExporter.OnVideoEnded();
            // Make sure export button is visible
            if (videoExporter.exportButton != null)
            {
                videoExporter.exportButton.gameObject.SetActive(true);
            }
        }
        
        // Stop all effects when video ends
        BeatEffectController effectController = FindObjectOfType<BeatEffectController>();
        if (effectController != null)
        {
            effectController.OnVideoEnded();
        }
        
        // Disable randomize button when video ends
        if (randomizeButtonController != null)
        {
            randomizeButtonController.DisableRandomizeButton();
        }
    }
    
    void UpdateButtonText()
    {
        if (buttonTextComponent == null)
            return;
        
        string newText = isFirstUpload ? "UPLOAD" : "UPLOAD AGAIN";
        isFirstUpload = false;
        
        // Update text based on component type
        if (buttonTextComponent is Text uiText)
        {
            uiText.text = newText;
        }
        else if (buttonTextComponent is TMPro.TextMeshProUGUI tmpText)
        {
            tmpText.text = newText;
        }
    }
    
    System.Collections.IEnumerator SetupDirectModeBeatDetection(VideoPlayer vp)
    {
        // Wait for video to start
        yield return new WaitForSeconds(0.5f);
        
        // Try to enable AudioSource mode in parallel for beat detection
        // This is a workaround: Direct mode for playback, AudioSource for analysis
        if (audioSource != null && vp.isPlaying)
        {
            // Temporarily switch to AudioSource mode to get spectrum data
            // Then switch back to Direct mode
            VideoAudioOutputMode originalMode = vp.audioOutputMode;
            
            // Try AudioSource mode for beat detection
            vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
            vp.SetTargetAudioSource(0, audioSource);
            vp.EnableAudioTrack(0, true);
            
            yield return new WaitForSeconds(0.2f);
            
            // Test if we can read spectrum data
            bool canRead = TestAudioSpectrum(audioSource, 0, out float energy);
            
            if (canRead && energy > 0.0001f)
            {
                // Success! AudioSource mode works for analysis
                Debug.Log("VideoUploadController: ✅ Direct mode + AudioSource analysis working! Audio plays in Direct, beats detected via AudioSource.");
                
                // Keep AudioSource mode for beat detection
                // Audio will still play (Unity handles both)
                AudioBeatDetector detector = FindFirstObjectByType<AudioBeatDetector>();
                if (detector != null)
                {
                    detector.SetupDirectModeWithAnalysis(vp, audioSource);
                }
            }
            else
            {
                // Fallback: Use Direct mode with time-based beats
                Debug.LogWarning("VideoUploadController: ⚠️ Using Direct mode with time-based beat detection. Effects will sync to video time.");
                vp.audioOutputMode = VideoAudioOutputMode.Direct;
                
                AudioBeatDetector detector = FindFirstObjectByType<AudioBeatDetector>();
                if (detector != null)
                {
                    detector.EnableFallbackMode(vp);
                }
            }
        }
    }
    
    System.Collections.IEnumerator PersistentAudioConnection(VideoPlayer vp)
    {
        // Wait longer initially for audio system to initialize
        yield return new WaitForSeconds(1f);
        
        int maxAttempts = 30; // Try for up to 15 seconds (30 * 0.5s)
        int attempt = 0;
        bool audioWorking = false;
        
        while (attempt < maxAttempts && !audioWorking && vp.isPlaying)
        {
            yield return new WaitForSeconds(0.5f);
            attempt++;
            
            if (vp.audioOutputMode != VideoAudioOutputMode.AudioSource || audioSource == null)
            {
                break;
            }
            
            // Verify connection is actually set
            AudioSource connectedSource = vp.GetTargetAudioSource(0);
            if (connectedSource != audioSource)
            {
                Debug.LogWarning($"VideoUploadController: Connection lost! Reconnecting... (Attempt {attempt})");
                vp.SetTargetAudioSource(0, audioSource);
                vp.EnableAudioTrack(0, true);
                yield return new WaitForSeconds(0.2f);
                continue;
            }
            
            // Test using multiple methods and channels
            bool spectrumWorks = TestAudioSpectrum(audioSource, 0, out float spectrumEnergy0);
            bool spectrumWorks1 = TestAudioSpectrum(audioSource, 1, out float spectrumEnergy1);
            bool outputWorks = TestAudioOutput(audioSource, out float outputEnergy);
            
            // Use the highest energy value
            float maxEnergy = Mathf.Max(spectrumEnergy0, spectrumEnergy1, outputEnergy);
            
            // SUCCESS if: energy > threshold (real audio data detected)
            if ((spectrumWorks || spectrumWorks1 || outputWorks) && maxEnergy > 0.0001f)
            {
                // SUCCESS! Audio is working
                audioWorking = true;
                Debug.Log($"VideoUploadController: ✅ AudioSource is working! Energy: {maxEnergy:F6} (Attempt {attempt})");
                break;
            }
            
            // If we've tried many times and still no audio data, enable fallback mode
            if (attempt >= 15 && maxEnergy < 0.0001f)
            {
                Debug.LogWarning($"VideoUploadController: ⚠️ Spectrum data unavailable (Unity VideoPlayer limitation). " +
                    "Enabling time-based fallback beat detection. Effects will sync to video playback time instead of audio beats.");
                audioWorking = true; // Mark as "working" so we enable fallback mode
                break;
            }
            
            // Try different connection strategies
            if (attempt % 5 == 0 && attempt > 0) // Every 5 attempts (2.5 seconds)
            {
                Debug.LogWarning($"VideoUploadController: AudioSource not receiving audio. Retrying connection... (Attempt {attempt}/{maxAttempts})");
                
                // Strategy 1: Full reconnection cycle with pause/resume
                vp.Pause();
                vp.EnableAudioTrack(0, false);
                vp.SetTargetAudioSource(0, null);
                yield return new WaitForSeconds(0.1f);
                vp.SetTargetAudioSource(0, audioSource);
                vp.EnableAudioTrack(0, true);
                yield return new WaitForSeconds(0.1f);
                vp.Play();
                yield return new WaitForSeconds(0.3f);
            }
            else if (attempt % 5 == 1)
            {
                // Strategy 2: Force AudioSource to be active
                audioSource.enabled = false;
                yield return null;
                audioSource.enabled = true;
                audioSource.mute = false;
                audioSource.volume = 1f;
                // Try to "wake up" the AudioSource
                if (!audioSource.isPlaying)
                {
                    // Force it to think it's playing (hack for VideoPlayer mode)
                    audioSource.Play();
                    yield return new WaitForSeconds(0.05f);
                    audioSource.Stop();
                }
            }
            else if (attempt % 5 == 2)
            {
                // Strategy 3: Re-enable everything
                vp.EnableAudioTrack(0, true);
                vp.SetTargetAudioSource(0, audioSource);
                audioSource.enabled = true;
            }
            else if (attempt % 5 == 3)
            {
                // Strategy 4: Check and fix AudioListener
                AudioListener listener = FindFirstObjectByType<AudioListener>();
                if (listener == null || !listener.enabled)
                {
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        if (listener == null)
                            mainCam.gameObject.AddComponent<AudioListener>();
                        else
                            listener.enabled = true;
                    }
                }
            }
        }
        
        if (!audioWorking && vp.isPlaying)
        {
            // Final diagnostic check
            AudioSource finalCheck = vp.GetTargetAudioSource(0);
            bool finalSpectrum = TestAudioSpectrum(audioSource, 0, out float finalEnergy);
            
            if (finalEnergy < 0.0001f)
            {
                // Enable fallback mode - time-based beats
                Debug.LogWarning($"VideoUploadController: ⚠️ Spectrum data unavailable after {maxAttempts} attempts. " +
                    $"Connected: {(finalCheck == audioSource ? "YES" : "NO")}, Energy: {finalEnergy:F8}. " +
                    "Enabling time-based fallback beat detection. Effects will sync to video playback.");
                
                AudioBeatDetector detector = FindFirstObjectByType<AudioBeatDetector>();
                if (detector != null)
                {
                    detector.EnableFallbackMode(vp);
                }
            }
        }
    }
    
    bool TestAudioSpectrum(AudioSource source, int channel, out float energy)
    {
        energy = 0f;
        float[] testSpectrum = new float[1024]; // Larger array for better detection
        
        try
        {
            source.GetSpectrumData(testSpectrum, channel, FFTWindow.BlackmanHarris);
            // Check bass range (indices 1-20 for low frequencies)
            for (int i = 1; i < 20 && i < testSpectrum.Length; i++)
            {
                energy += testSpectrum[i];
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    bool TestAudioOutput(AudioSource source, out float energy)
    {
        energy = 0f;
        float[] outputData = new float[512];
        
        try
        {
            source.GetOutputData(outputData, 0);
            // Calculate RMS (Root Mean Square) energy
            float sum = 0f;
            for (int i = 0; i < outputData.Length; i++)
            {
                sum += outputData[i] * outputData[i];
            }
            energy = Mathf.Sqrt(sum / outputData.Length);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
