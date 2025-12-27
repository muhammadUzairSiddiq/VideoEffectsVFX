using UnityEngine;
using UnityEngine.UI;

public class EffectPanelController : MonoBehaviour
{
    [Header("Panel References")]
    [Tooltip("The PRESETS panel GameObject (default: closed)")]
    public GameObject presetsPanel;
    
    [Header("Toggle Buttons")]
    [Tooltip("Button to open effects panel")]
    public Button openEffectsButton;
    
    [Tooltip("Button to close effects panel")]
    public Button closeEffectsButton;
    
    [Header("Individual Effect Buttons")]
    public Button zoomEffectButton;
    public Button bloomEffectButton;
    public Button glitchEffectButton;
    public Button vignetteEffectButton;
    public Button shakeEffectButton;
    public Button rgbEffectButton;
    public Button rotateEffectButton;
    public Button lensDistortionEffectButton;
    
    [Header("References")]
    public BeatEffectController beatEffectController;
    public RandomizeButtonController randomizeButtonController;
    
    private string currentSingleEffect = ""; // "Zoom", "Bloom", etc. Empty = all effects enabled
    private EffectTimelineTracker timelineTracker;
    
    void Start()
    {
        // Auto-find panel if not assigned
        if (presetsPanel == null)
        {
            GameObject found = GameObject.Find("PRESETS");
            if (found != null) presetsPanel = found;
        }
        
        // Auto-find buttons
        AutoFindButtons();
        
        // Auto-find controllers
        if (beatEffectController == null)
            beatEffectController = FindFirstObjectByType<BeatEffectController>();
        if (randomizeButtonController == null)
            randomizeButtonController = FindFirstObjectByType<RandomizeButtonController>();
        
        // Auto-find timeline tracker
        if (timelineTracker == null)
            timelineTracker = FindFirstObjectByType<EffectTimelineTracker>();
        
        // Setup initial state: panel closed, open button active, close button deactivated
        if (presetsPanel != null)
            presetsPanel.SetActive(false);
        
        if (openEffectsButton != null)
        {
            openEffectsButton.onClick.AddListener(OnOpenEffectsClicked);
            openEffectsButton.gameObject.SetActive(true); // Active by default
        }
        
        if (closeEffectsButton != null)
        {
            closeEffectsButton.onClick.AddListener(OnCloseEffectsClicked);
            closeEffectsButton.gameObject.SetActive(false); // Deactivated by default (same as panel)
        }
        
        // Setup individual effect buttons
        SetupEffectButtons();
        
        // Connect randomize button to this controller
        if (randomizeButtonController != null)
        {
            randomizeButtonController.effectPanelController = this;
        }
    }
    
    void AutoFindButtons()
    {
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        
        foreach (Button btn in allButtons)
        {
            string name = btn.name.ToLower();
            
            if (name.Contains("open") && name.Contains("effect"))
                openEffectsButton = btn;
            else if (name.Contains("close") && name.Contains("effect"))
                closeEffectsButton = btn;
            else if (name.Contains("zoom"))
                zoomEffectButton = btn;
            else if (name.Contains("bloom"))
                bloomEffectButton = btn;
            else if (name.Contains("glitch"))
                glitchEffectButton = btn;
            else if (name.Contains("vignette"))
                vignetteEffectButton = btn;
            else if (name.Contains("shake") || name.Contains("shaky"))
                shakeEffectButton = btn;
            else if (name.Contains("rgb") || name.Contains("wavy"))
                rgbEffectButton = btn;
            else if (name.Contains("rotate"))
                rotateEffectButton = btn;
            else if (name.Contains("lens") || name.Contains("distort"))
                lensDistortionEffectButton = btn;
        }
    }
    
    void SetupEffectButtons()
    {
        if (zoomEffectButton != null)
            zoomEffectButton.onClick.AddListener(() => OnEffectButtonClicked("Zoom"));
        
        if (bloomEffectButton != null)
            bloomEffectButton.onClick.AddListener(() => OnEffectButtonClicked("Bloom"));
        
        if (glitchEffectButton != null)
            glitchEffectButton.onClick.AddListener(() => OnEffectButtonClicked("Glitch"));
        
        if (vignetteEffectButton != null)
            vignetteEffectButton.onClick.AddListener(() => OnEffectButtonClicked("Vignette"));
        
        if (shakeEffectButton != null)
            shakeEffectButton.onClick.AddListener(() => OnEffectButtonClicked("Shake"));
        
        if (rgbEffectButton != null)
            rgbEffectButton.onClick.AddListener(() => OnEffectButtonClicked("MaterialRGB"));
        
        if (rotateEffectButton != null)
            rotateEffectButton.onClick.AddListener(() => OnEffectButtonClicked("Rotate"));
        
        if (lensDistortionEffectButton != null)
            lensDistortionEffectButton.onClick.AddListener(() => OnEffectButtonClicked("LensDistortion"));
    }
    
    public void OnOpenEffectsClicked()
    {
        if (presetsPanel != null)
            presetsPanel.SetActive(true);
        
        // Deactivate open button, activate close button
        if (openEffectsButton != null)
            openEffectsButton.gameObject.SetActive(false);
        
        if (closeEffectsButton != null)
            closeEffectsButton.gameObject.SetActive(true);
    }
    
    public void OnCloseEffectsClicked()
    {
        ClosePanel();
    }
    
    void OnEffectButtonClicked(string effectName)
    {
        // Record this selection with current video time
        if (timelineTracker != null)
        {
            timelineTracker.RecordEffectSelection(effectName);
        }
        
        // Set single effect mode
        currentSingleEffect = effectName;
        
        // Disable auto mode (beat-synced random effects)
        if (beatEffectController != null)
        {
            beatEffectController.SetSingleEffectMode(effectName);
        }
        
        Debug.Log($"EffectPanelController: Activated single effect mode: {effectName}");
        
        // Close panel and enable open button when effect is clicked
        ClosePanel();
    }
    
    void ClosePanel()
    {
        if (presetsPanel != null)
            presetsPanel.SetActive(false);
        
        // Activate open button, deactivate close button
        if (openEffectsButton != null)
            openEffectsButton.gameObject.SetActive(true);
        
        if (closeEffectsButton != null)
            closeEffectsButton.gameObject.SetActive(false);
    }
    
    public void OnRandomizeButtonClicked()
    {
        // Enable all effects mode (randomize)
        currentSingleEffect = "";
        
        if (beatEffectController != null)
        {
            beatEffectController.SetAllEffectsMode();
        }
        
        Debug.Log("EffectPanelController: Activated all effects mode (randomize)");
    }
    
    public string GetCurrentSingleEffect()
    {
        return currentSingleEffect;
    }
}

