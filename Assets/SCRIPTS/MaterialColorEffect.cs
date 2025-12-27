using UnityEngine;

/// <summary>
/// Lightweight material color change effect - changes screen overlay color on beat
/// </summary>
public class MaterialColorEffect : MonoBehaviour
{
    [Header("Color Settings")]
    public Color[] beatColors = new Color[]
    {
        new Color(1f, 0.3f, 0.3f, 0.1f), // Red
        new Color(0.3f, 1f, 0.3f, 0.1f), // Green
        new Color(0.3f, 0.3f, 1f, 0.1f), // Blue
        new Color(1f, 1f, 0.3f, 0.1f),   // Yellow
        new Color(1f, 0.3f, 1f, 0.1f)    // Magenta
    };
    
    public float colorIntensity = 0.15f;
    public float fadeSpeed = 5f;
    
    private Camera mainCamera;
    private Material overlayMaterial;
    private Color currentColor = Color.clear;
    private Color targetColor = Color.clear;
    
    public bool colorEnabled = false;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        // Create a simple overlay material (use Unlit/Color shader as fallback)
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        
        overlayMaterial = new Material(shader);
    }
    
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!colorEnabled || overlayMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }
        
        // Fade towards target color
        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * fadeSpeed);
        
        // Apply color overlay
        overlayMaterial.SetColor("_Color", currentColor);
        Graphics.Blit(source, destination, overlayMaterial);
    }
    
    public void TriggerColorChange(float duration = 0.1f)
    {
        if (beatColors.Length == 0) return;
        
        // Pick random color
        Color randomColor = beatColors[Random.Range(0, beatColors.Length)];
        targetColor = randomColor * colorIntensity;
        colorEnabled = true;
        
        StartCoroutine(StopColorAfter(duration));
    }
    
    System.Collections.IEnumerator StopColorAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        targetColor = Color.clear;
        yield return new WaitForSeconds(0.2f); // Fade out
        colorEnabled = false;
        currentColor = Color.clear;
    }
    
    public void ToggleColor()
    {
        colorEnabled = !colorEnabled;
        if (!colorEnabled)
        {
            targetColor = Color.clear;
            currentColor = Color.clear;
        }
    }
    
    void OnDestroy()
    {
        if (overlayMaterial != null)
        {
            Destroy(overlayMaterial);
        }
    }
}

