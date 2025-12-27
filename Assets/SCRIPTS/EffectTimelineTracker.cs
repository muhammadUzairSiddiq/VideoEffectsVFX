using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

[System.Serializable]
public class EffectSelection
{
    public float videoTime;  // When in video (seconds)
    public string effectName; // "Zoom", "Rotate", "Shake", etc.
}

public class EffectTimelineTracker : MonoBehaviour
{
    private List<EffectSelection> effectTimeline = new List<EffectSelection>();
    private VideoPlayer videoPlayer;
    
    void Start()
    {
        if (videoPlayer == null)
            videoPlayer = FindFirstObjectByType<VideoPlayer>();
    }
    
    // Called when user clicks effect button
    public void RecordEffectSelection(string effectName)
    {
        if (videoPlayer == null)
            videoPlayer = FindFirstObjectByType<VideoPlayer>();
            
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            float currentVideoTime = (float)videoPlayer.time;
            effectTimeline.Add(new EffectSelection 
            { 
                videoTime = currentVideoTime, 
                effectName = effectName 
            });
            
            Debug.Log($"EffectTimelineTracker: Recorded {effectName} at {currentVideoTime:F2}s");
        }
    }
    
    // Get effect timeline for export
    public List<EffectSelection> GetEffectTimeline()
    {
        return new List<EffectSelection>(effectTimeline);
    }
    
    // Clear timeline (when new video starts)
    public void ClearTimeline()
    {
        effectTimeline.Clear();
        Debug.Log("EffectTimelineTracker: Timeline cleared");
    }
    
    // Check if timeline has any effects
    public bool HasTimeline()
    {
        return effectTimeline.Count > 0;
    }
}

