using UnityEngine;
using System.IO;
using System;

[System.Serializable]
public class EffectPreset
{
    [Header("Zoom Effect")]
    public float zoomStrength = 1.2f;
    public float zoomSpeed = 3f;
    
    [Header("Shake Effect")]
    public float shakeIntensity = 0.1f;
    public float shakeSpeed = 30f;
    
    [Header("Material RGB Effect")]
    public float rgbIntensity = 0.3f;
    public float rgbSpeed = 5f;
    
    [Header("Bloom Effect")]
    public float bloomIntensity = 1.5f;
    public float bloomScatter = 0.7f;
    
    [Header("Vignette Effect")]
    public float vignetteIntensity = 0.4f;
    public float vignetteSmoothness = 0.5f;
    
    [Header("Lens Distortion Effect")]
    public float lensDistortionIntensity = 0.3f;
    
    [Header("Rotate Effect")]
    public float rotationIntensity = 2f;
    public float rotationSpeed = 8f;
    
    [Header("Material Color Effect")]
    public float colorIntensity = 0.15f;
    public float fadeSpeed = 5f;
    
    [Header("Glitch Effect")]
    public float glitchIntensity = 0.5f;
}

public class EffectPresetManager : MonoBehaviour
{
    private static EffectPresetManager instance;
    public static EffectPresetManager Instance => instance;
    
    private string presetFilePath;
    private EffectPreset currentPreset;
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        presetFilePath = Path.Combine(Application.persistentDataPath, "EffectPreset.json");
        
        // Log the preset file path for easy access
        Debug.Log($"EffectPresetManager: Preset file path = {presetFilePath}");
        Debug.Log($"EffectPresetManager: Persistent data path = {Application.persistentDataPath}");
    }
    
    public EffectPreset GetCurrentPreset()
    {
        if (currentPreset == null)
        {
            LoadOrCreatePreset();
        }
        return currentPreset;
    }
    
    public void LoadOrCreatePreset()
    {
        Debug.Log($"EffectPresetManager: Checking for preset file at: {presetFilePath}");
        
        if (File.Exists(presetFilePath))
        {
            try
            {
                string json = File.ReadAllText(presetFilePath);
                currentPreset = JsonUtility.FromJson<EffectPreset>(json);
                
                Debug.Log($"✅ EffectPresetManager: JSON file EXISTS and loaded successfully!");
                Debug.Log($"📁 File path: {presetFilePath}");
                Debug.Log($"📄 JSON content preview: {json.Substring(0, Mathf.Min(200, json.Length))}...");
                Debug.Log($"🎨 Loaded values - Zoom: {currentPreset.zoomStrength}, Bloom: {currentPreset.bloomIntensity}, RGB: {currentPreset.rgbIntensity}");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ EffectPresetManager: Failed to load preset: {e.Message}");
                Debug.LogError($"📁 File path was: {presetFilePath}");
                CreateDefaultPreset();
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ EffectPresetManager: JSON file NOT FOUND at {presetFilePath}");
            Debug.Log("📝 Creating new default preset file...");
            CreateDefaultPreset();
        }
    }
    
    void CreateDefaultPreset()
    {
        currentPreset = new EffectPreset();
        SavePreset();
        
        Debug.Log($"✅ EffectPresetManager: Created NEW default preset file!");
        Debug.Log($"📁 File saved to: {presetFilePath}");
        Debug.Log($"📄 You can now edit this file to change effect values");
        Debug.Log($"🎨 Default values - Zoom: {currentPreset.zoomStrength}, Bloom: {currentPreset.bloomIntensity}, RGB: {currentPreset.rgbIntensity}");
        
        // Verify file was actually created
        if (File.Exists(presetFilePath))
        {
            Debug.Log($"✅ Verification: File confirmed to exist at {presetFilePath}");
        }
        else
        {
            Debug.LogError($"❌ Verification FAILED: File was NOT created at {presetFilePath}");
        }
    }
    
    public void SavePreset()
    {
        if (currentPreset == null)
        {
            Debug.LogWarning("EffectPresetManager: Cannot save - preset is null!");
            return;
        }
        
        try
        {
            string json = JsonUtility.ToJson(currentPreset, true);
            File.WriteAllText(presetFilePath, json);
            Debug.Log($"💾 EffectPresetManager: Preset saved successfully to {presetFilePath}");
            Debug.Log($"📄 JSON saved (length: {json.Length} characters)");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ EffectPresetManager: Failed to save preset: {e.Message}");
            Debug.LogError($"📁 File path was: {presetFilePath}");
        }
    }
    
    public void ApplyPresetToEffects(BeatEffectController controller, RandomizeButtonController randomizer)
    {
        if (currentPreset == null)
        {
            Debug.Log("EffectPresetManager: Loading preset before applying...");
            LoadOrCreatePreset();
        }
        
        Debug.Log($"🎨 EffectPresetManager: Applying preset values to effects...");
        Debug.Log($"📊 Preset values - Zoom: {currentPreset.zoomStrength}, Bloom: {currentPreset.bloomIntensity}, RGB: {currentPreset.rgbIntensity}");
        
        // Apply preset values to effects via RandomizeButtonController
        if (randomizer != null)
        {
            // Store original values first
            randomizer.StoreOriginalValues();
            
            int effectsApplied = 0;
            
            // Apply preset values
            if (controller.zoomEffect != null)
            {
                controller.zoomEffect.zoomStrength = currentPreset.zoomStrength;
                controller.zoomEffect.zoomSpeed = currentPreset.zoomSpeed;
                effectsApplied++;
                Debug.Log($"  ✅ Applied Zoom: strength={currentPreset.zoomStrength}, speed={currentPreset.zoomSpeed}");
            }
            
            if (controller.shakeEffect != null)
            {
                controller.shakeEffect.intensity = currentPreset.shakeIntensity;
                controller.shakeEffect.speed = currentPreset.shakeSpeed;
                effectsApplied++;
                Debug.Log($"  ✅ Applied Shake: intensity={currentPreset.shakeIntensity}, speed={currentPreset.shakeSpeed}");
            }
            
            if (controller.materialRGBEffect != null)
            {
                controller.materialRGBEffect.rgbIntensity = currentPreset.rgbIntensity;
                controller.materialRGBEffect.rgbSpeed = currentPreset.rgbSpeed;
                effectsApplied++;
                Debug.Log($"  ✅ Applied MaterialRGB: intensity={currentPreset.rgbIntensity}, speed={currentPreset.rgbSpeed}");
            }
            
            if (controller.bloomEffect != null && controller.bloomEffect.globalVolume != null)
            {
                if (controller.bloomEffect.globalVolume.profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom))
                {
                    bloom.intensity.value = currentPreset.bloomIntensity;
                    bloom.scatter.value = currentPreset.bloomScatter;
                    effectsApplied++;
                    Debug.Log($"  ✅ Applied Bloom: intensity={currentPreset.bloomIntensity}, scatter={currentPreset.bloomScatter}");
                }
            }
            
            if (controller.vignetteEffect != null && controller.vignetteEffect.globalVolume != null)
            {
                if (controller.vignetteEffect.globalVolume.profile.TryGet<UnityEngine.Rendering.Universal.Vignette>(out var vignette))
                {
                    vignette.intensity.value = currentPreset.vignetteIntensity;
                    vignette.smoothness.value = currentPreset.vignetteSmoothness;
                    effectsApplied++;
                    Debug.Log($"  ✅ Applied Vignette: intensity={currentPreset.vignetteIntensity}, smoothness={currentPreset.vignetteSmoothness}");
                }
            }
            
            if (controller.lensDistortionEffect != null && controller.lensDistortionEffect.globalVolume != null)
            {
                if (controller.lensDistortionEffect.globalVolume.profile.TryGet<UnityEngine.Rendering.Universal.LensDistortion>(out var lensDist))
                {
                    lensDist.intensity.value = currentPreset.lensDistortionIntensity;
                    effectsApplied++;
                    Debug.Log($"  ✅ Applied LensDistortion: intensity={currentPreset.lensDistortionIntensity}");
                }
            }
            
            if (controller.rotateEffect != null)
            {
                controller.rotateEffect.rotationIntensity = currentPreset.rotationIntensity;
                controller.rotateEffect.rotationSpeed = currentPreset.rotationSpeed;
                effectsApplied++;
                Debug.Log($"  ✅ Applied Rotate: intensity={currentPreset.rotationIntensity}, speed={currentPreset.rotationSpeed}");
            }
            
            if (controller.materialColorEffect != null)
            {
                controller.materialColorEffect.colorIntensity = currentPreset.colorIntensity;
                controller.materialColorEffect.fadeSpeed = currentPreset.fadeSpeed;
                effectsApplied++;
                Debug.Log($"  ✅ Applied MaterialColor: intensity={currentPreset.colorIntensity}, fadeSpeed={currentPreset.fadeSpeed}");
            }
            
            Debug.Log($"✅ EffectPresetManager: Applied preset to {effectsApplied} effects successfully!");
        }
        else
        {
            Debug.LogWarning("⚠️ EffectPresetManager: RandomizeButtonController is null - cannot apply preset!");
        }
    }
    
    // Public method to get file path (for UI display or external access)
    public string GetPresetFilePath()
    {
        return presetFilePath;
    }
    
    // Reload preset from file (useful for testing - edit JSON file, then call this)
    public void ReloadPreset()
    {
        Debug.Log("🔄 EffectPresetManager: Reloading preset from file...");
        currentPreset = null; // Clear current preset
        LoadOrCreatePreset();
        Debug.Log("✅ EffectPresetManager: Preset reloaded! New values will apply on next video upload.");
    }
    
    // Check if preset file exists
    public bool PresetFileExists()
    {
        bool exists = File.Exists(presetFilePath);
        Debug.Log($"EffectPresetManager: Preset file exists: {exists} at {presetFilePath}");
        return exists;
    }
}

