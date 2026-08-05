using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject uiPanel;
    public GameObject settingsPanel;

    [Header("Audio Settings")]
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Buttons")]
    public Button startGameButton;
    public Button optionsButton;
    public Button quitGameButton;
    public Button quitOptionsButton;

    void Start()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
            musicVolumeSlider.onValueChanged.AddListener(UpdateMusicVolume);
        }
        else Debug.LogWarning("Brak przypisanego Slidera Muzyki w UIManager!");

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxVolumeSlider.onValueChanged.AddListener(UpdateSFXVolume);
        }
        else Debug.LogWarning("Brak przypisanego Slidera SFX w UIManager!");

        if (startGameButton != null)   startGameButton.onClick.AddListener(StartGame);
        if (optionsButton != null)     optionsButton.onClick.AddListener(OpenOptions);
        if (quitGameButton != null)    quitGameButton.onClick.AddListener(QuitGame);
        if (quitOptionsButton != null) quitOptionsButton.onClick.AddListener(CloseOptions);
    }

    private void UpdateMusicVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
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

    private void UpdateSFXVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value, playPreview: true);
        }
    }

    public void StartGame()
    {
        PlayButtonClickSound();
        SceneManager.LoadScene("Gameplay");
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