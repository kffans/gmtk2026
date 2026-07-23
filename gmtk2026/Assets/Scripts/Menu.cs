using UnityEngine;
using UnityEngine.SceneManagement; 
public class Menu : MonoBehaviour
{
    public void StartGame()
    {
        Debug.Log("tekst");
        SceneManager.LoadScene("SampleScene");
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