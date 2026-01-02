using UnityEngine;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    public GameObject gameOverPanel;  // Assign GameOverPanel
    public Text scoreText;        // Assign ScoreText (TextMeshPro)

    /// <summary>
    /// Call this when the player loses.
    /// </summary>
    public void ShowGameOver(int finalScore)
    {
        gameOverPanel.SetActive(true);
        scoreText.text = "Score: " + finalScore;
    }

    /// <summary>
    /// Example retry button method
    /// </summary>
    public void Retry()
    {
        Debug.Log("Retrying!");
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}
