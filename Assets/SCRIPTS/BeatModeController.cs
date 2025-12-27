using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Controller for switching between Manual (button) and Automatic (beat-synced) modes
/// </summary>
public class BeatModeController : MonoBehaviour
{
    [Header("References")]
    public BeatEffectController beatEffectController;
    public AudioBeatDetector beatDetector;

    [Header("UI Elements (Optional)")]
    public Button toggleAutoModeButton;
    public Text modeStatusText;
    public GameObject manualModePanel; // Panel with manual effect buttons
    public GameObject autoModeIndicator; // Visual indicator for auto mode

    [Header("Settings")]
    public bool startInAutoMode = true;

    void Start()
    {
        // Find components if not assigned
        if (beatEffectController == null)
            beatEffectController = FindObjectOfType<BeatEffectController>();
        
        if (beatDetector == null)
            beatDetector = FindObjectOfType<AudioBeatDetector>();

        // Setup initial mode
        if (beatEffectController != null)
        {
            beatEffectController.SetAutoMode(startInAutoMode);
        }

        // Setup UI
        if (toggleAutoModeButton != null)
        {
            toggleAutoModeButton.onClick.AddListener(ToggleMode);
        }

        UpdateUI();
    }

    public void ToggleMode()
    {
        if (beatEffectController != null)
        {
            beatEffectController.ToggleAutoMode();
            UpdateUI();
        }
    }

    public void SetManualMode()
    {
        if (beatEffectController != null)
        {
            beatEffectController.SetAutoMode(false);
            UpdateUI();
        }
    }

    public void SetAutoMode()
    {
        if (beatEffectController != null)
        {
            beatEffectController.SetAutoMode(true);
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        bool isAutoMode = beatEffectController != null && beatEffectController.autoModeEnabled;

        // Update status text
        if (modeStatusText != null)
        {
            modeStatusText.text = isAutoMode ? "AUTO MODE" : "MANUAL MODE";
        }

        // Show/hide manual buttons panel
        if (manualModePanel != null)
        {
            manualModePanel.SetActive(!isAutoMode);
        }

        // Show/hide auto mode indicator
        if (autoModeIndicator != null)
        {
            autoModeIndicator.SetActive(isAutoMode);
        }
    }
}

