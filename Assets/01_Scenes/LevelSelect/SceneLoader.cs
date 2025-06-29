using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadLevelSelect()
    {
        GameManager.Instance.LoadLevelSelect();
    }

    public void LoadMainMenu()
    {
        GameManager.Instance.LoadMainMenu();
    }

    public void LoadLevelOne()
    {
        GameManager.Instance.LoadLevel1();
    }

    public void LoadLevelTwo()
    {
        GameManager.Instance.LoadLevel2();
    }

    public void LoadLevelThree()
    {
        GameManager.Instance.LoadLevel3();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}