using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 

public class UIManagerGameplay : MonoBehaviour
{
    [Header("Panels")]
    public GameObject uiPanel; 
    public GameObject settingsPanel; 

    [Header("Audio Settings")]
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Buttons")]
    public Button optionsButton; 
    public Button quitOptionsButton; 
    public Button returnToMenuButton; 
    public Button quitGameButton; 

    void Start()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
            musicVolumeSlider.onValueChanged.AddListener(UpdateMusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxVolumeSlider.onValueChanged.AddListener(UpdateSFXVolume);
        }

        if (optionsButton != null)       optionsButton.onClick.AddListener(OpenOptions);
        if (quitOptionsButton != null)   quitOptionsButton.onClick.AddListener(CloseOptions);
        if (returnToMenuButton != null)  returnToMenuButton.onClick.AddListener(ReturnToMenu);
        if (quitGameButton != null)      quitGameButton.onClick.AddListener(QuitGame);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    private void ToggleSettings()
    {
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseOptions();
        }
        else
        {
            OpenOptions();
        }
    }

    private void UpdateMusicVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
    }

    private void UpdateSFXVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value, playPreview: true);
        }
    }

    public void OpenOptions()
    {
        PlayButtonClickSound();
        if (uiPanel != null) uiPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        PlayButtonClickSound();
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (uiPanel != null) uiPanel.SetActive(true);
    }

    public void ReturnToMenu()
    {
        PlayButtonClickSound();
        SceneManager.LoadScene("Menu"); 
    }

    public void QuitGame()
    {
        PlayButtonClickSound();
        Application.Quit();
    }

    private void PlayButtonClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("buttonClick");
        }
    }
}