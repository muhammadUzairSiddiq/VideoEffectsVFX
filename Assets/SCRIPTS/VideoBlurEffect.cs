using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VideoMotionBlurEffect : MonoBehaviour
{
    public Volume globalVolume;

    MotionBlur motionBlur;

    float originalIntensity;
    bool blurEnabled = false;

    void Start()
    {
        if (!globalVolume.profile.TryGet(out motionBlur))
        {
            Debug.LogError("Motion Blur not found in Volume");
            return;
        }

        originalIntensity = motionBlur.intensity.value;
        motionBlur.intensity.value = originalIntensity;
    }

    // 🔥 UI BUTTON
    public void ToggleBlur()
    {
        blurEnabled = !blurEnabled;

        if (blurEnabled)
            ApplyBlur();
        else
            Restore();
    }

    void ApplyBlur()
    {
        motionBlur.intensity.value = 2f; // STRONG blur
        motionBlur.clamp.value = 2f;
    }

    void Restore()
    {
        motionBlur.intensity.value = originalIntensity;
    }
}
