using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Quick verification script to check if beat detection setup is correct
/// Attach to any GameObject and check Console for status
/// </summary>
public class BeatDetectionVerifier : MonoBehaviour
{
    void Start()
    {
        VerifySetup();
    }

    [ContextMenu("Verify Setup")]
    public void VerifySetup()
    {
        Debug.Log("=== BEAT DETECTION SETUP VERIFICATION ===");
        
        bool allGood = true;

        // Check AudioBeatDetector
        AudioBeatDetector beatDetector = FindObjectOfType<AudioBeatDetector>();
        if (beatDetector == null)
        {
            Debug.LogError("❌ AudioBeatDetector not found in scene!");
            allGood = false;
        }
        else
        {
            Debug.Log("✅ AudioBeatDetector found");
            
            if (beatDetector.audioSource == null)
            {
                Debug.LogError("❌ AudioBeatDetector.audioSource is not assigned!");
                allGood = false;
            }
            else
            {
                Debug.Log($"✅ AudioSource assigned: {beatDetector.audioSource.name}");
            }
        }

        // Check BeatEffectController
        BeatEffectController controller = FindObjectOfType<BeatEffectController>();
        if (controller == null)
        {
            Debug.LogError("❌ BeatEffectController not found in scene!");
            allGood = false;
        }
        else
        {
            Debug.Log("✅ BeatEffectController found");
            
            if (controller.beatDetector == null)
            {
                Debug.LogError("❌ BeatEffectController.beatDetector is not assigned!");
                allGood = false;
            }
            else
            {
                Debug.Log($"✅ BeatDetector reference assigned");
            }

            // Check effect references
            int effectCount = 0;
            if (controller.zoomEffect != null) effectCount++;
            if (controller.bloomEffect != null) effectCount++;
            if (controller.glitchEffect != null) effectCount++;
            if (controller.rgbEffect != null) effectCount++;
            if (controller.shakeEffect != null) effectCount++;
            if (controller.vignetteEffect != null) effectCount++;
            if (controller.shadowMidtoneEffect != null) effectCount++;
            if (controller.lensDistortionEffect != null) effectCount++;

            Debug.Log($"✅ {effectCount}/8 effects assigned");
            if (effectCount < 8)
            {
                Debug.LogWarning($"⚠️ Some effects are not assigned ({effectCount}/8)");
            }
        }

        // Check VideoPlayer
        VideoPlayer videoPlayer = FindObjectOfType<VideoPlayer>();
        if (videoPlayer == null)
        {
            Debug.LogError("❌ VideoPlayer not found!");
            allGood = false;
        }
        else
        {
            Debug.Log("✅ VideoPlayer found");
            
            if (videoPlayer.audioOutputMode != VideoAudioOutputMode.AudioSource)
            {
                Debug.LogError($"❌ VideoPlayer Audio Output Mode is '{videoPlayer.audioOutputMode}' but should be 'AudioSource'!");
                allGood = false;
            }
            else
            {
                Debug.Log("✅ VideoPlayer Audio Output Mode is 'AudioSource'");
            }

            // Check if AudioSource is connected
            AudioSource[] targetSources = new AudioSource[videoPlayer.audioTrackCount];
            for (ushort i = 0; i < videoPlayer.audioTrackCount; i++)
            {
                targetSources[i] = videoPlayer.GetTargetAudioSource(i);
            }

            bool hasConnectedSource = false;
            foreach (var source in targetSources)
            {
                if (source != null)
                {
                    hasConnectedSource = true;
                    Debug.Log($"✅ VideoPlayer connected to AudioSource: {source.name}");
                    break;
                }
            }

            if (!hasConnectedSource && videoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource)
            {
                Debug.LogWarning("⚠️ VideoPlayer Audio Output Mode is 'AudioSource' but no AudioSource is connected!");
                Debug.LogWarning("   This will be set automatically when video is loaded.");
            }
        }

        // Check VideoUploadController
        VideoUploadController uploadController = FindObjectOfType<VideoUploadController>();
        if (uploadController == null)
        {
            Debug.LogWarning("⚠️ VideoUploadController not found (optional)");
        }
        else
        {
            Debug.Log("✅ VideoUploadController found");
            
            if (uploadController.audioSource == null)
            {
                Debug.LogWarning("⚠️ VideoUploadController.audioSource is not assigned");
            }
            else
            {
                Debug.Log($"✅ VideoUploadController.audioSource assigned: {uploadController.audioSource.name}");
            }
        }

        // Final status
        Debug.Log("========================================");
        if (allGood)
        {
            Debug.Log("✅ SETUP LOOKS GOOD! Ready for beat detection.");
        }
        else
        {
            Debug.LogError("❌ SETUP HAS ISSUES! Check errors above.");
        }
        Debug.Log("========================================");
    }
}

