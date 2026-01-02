using UnityEngine;
using UnityEngine.SceneManagement; // Needed to load scenes

public class GameStart : MonoBehaviour
{
    // Call this when the Start button is pressed
    public void StartGame()
    {
        // Replace "GameScene" with your actual game scene name
        SceneManager.LoadScene("GameScene");
    }

    // Call this when the Exit button is pressed
    public void ExitGame()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();

        // In editor, Application.Quit() does nothing, so this helps for testing
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
