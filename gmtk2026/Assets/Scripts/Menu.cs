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
}