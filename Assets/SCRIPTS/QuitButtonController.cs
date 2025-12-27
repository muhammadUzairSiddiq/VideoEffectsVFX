using UnityEngine;

public class QuitButtonController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Delay before quitting (seconds)")]
    public float quitDelay = 0.1f;
    
    /// <summary>
    /// Quits the game/application
    /// </summary>
    public void QuitApp()
    {
        Debug.Log("QuitButtonController: Quit button clicked - exiting application...");
        
        // Add small delay to ensure button click is registered
        if (quitDelay > 0f)
        {
            Invoke(nameof(QuitApplication), quitDelay);
        }
        else
        {
            QuitApplication();
        }
    }
    
    void QuitApplication()
    {
        #if UNITY_EDITOR
        // In editor, stop playing
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.Log("QuitButtonController: Editor play mode stopped");
        #else
        // On device, quit application
        Application.Quit();
        Debug.Log("QuitButtonController: Application quit called");
        #endif
    }
    
    // Alternative method name (in case you want to use it)
    public void QuitGame()
    {
        QuitApp();
    }
}

