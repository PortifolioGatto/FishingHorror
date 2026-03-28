using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string firstSceneName;

    [Space]

    [SerializeField] private GameObject mainMenuObject;
    [SerializeField] private OptionsController optionsController;

    public void StartNewGame()
    {
        GameManager.Instance.StartGame();

        UnityEngine.SceneManagement.SceneManager.LoadScene(firstSceneName);
    }

    public void OpenOptions()
    {
        mainMenuObject.SetActive(false);
        optionsController.ToggleOptionsMenu();
    }

    public void BackToMainMenu()
    {
        mainMenuObject.SetActive(true);
        optionsController.ToggleOptionsMenu();
    }


    public void QuitGame()
    {
        Application.Quit();
    }
}
