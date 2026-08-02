using UnityEngine;
using UnityEngine.SceneManagement; 
public class Menu : MonoBehaviour
{
    public AudioClip menuMusic;

    void Start()
    {
        if (AudioManager.Instance != null && menuMusic != null)
        {
            AudioManager.Instance.PlayMusic(menuMusic);
        }
        else
        {
            Debug.LogWarning("There is no AudioManager");
        }
    }

    public void StartGame()
    {
        Debug.Log("tekst");
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