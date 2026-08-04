using UnityEngine;
using UnityEngine.SceneManagement; 
public class Menu : MonoBehaviour
{
    void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic("menu");
        }
        else
        {
            Debug.LogWarning("There is no AudioManager");
        }
    }

    public void StartGame()
    {
        AudioManager.Instance.PlaySFX("buttonClick");
        SceneManager.LoadScene("Gameplay");
    }

    public void Options()
    {
        // make options section visible
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}