using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string firstSceneName;
    public void StartNewGame()
    {
        GameManager.Instance.StartGame();

        UnityEngine.SceneManagement.SceneManager.LoadScene(firstSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
