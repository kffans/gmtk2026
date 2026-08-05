using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource sfxSource; 
    public AudioSource musicSource;
    
    [Header("Sound Effects")]
    public AudioClip dialogueClick; 
    public AudioClip statChange;    
    public AudioClip moneyChange;
    public AudioClip interlude;
    public AudioClip buttonClick;

    [Header("Music")]
    public AudioClip menu;
    public AudioClip intro;

    private float lastSFXPreviewTime;
    private const float SFX_PREVIEW_COOLDOWN = 0.15f;    

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return; 
        }

        LoadVolumeSettings();
    }

    
    public void PlaySFX(string soundEffectName)
    {
        AudioClip clipToPlay = null;

        switch (soundEffectName)
        {
            case "dialogueClick": clipToPlay = dialogueClick; break;
            case "statChange": clipToPlay = statChange; break;
            case "moneyChange": clipToPlay = moneyChange; break;
            case "interlude": clipToPlay = interlude; break;
            case "buttonClick": clipToPlay = buttonClick; break;
            default:
                Debug.LogWarning("Nie znaleziono efektu dźwiękowego o nazwie: " + soundEffectName);
                return; 
        }

        if (sfxSource != null && clipToPlay != null)
        {
            sfxSource.PlayOneShot(clipToPlay);
        }
    }

    public void PlayMusic(string musicName)
    {
        AudioClip clipToPlay = null;

        switch (musicName)
        {
            case "menu": clipToPlay = menu; break;
            case "intro": clipToPlay = intro; break;
            default:
                Debug.LogWarning("There is no such: " + musicName);
                return; 
        }

        if (musicSource != null && clipToPlay != null)
        {
            if (musicSource.clip == clipToPlay && musicSource.isPlaying) return;

            musicSource.clip = clipToPlay;
            musicSource.loop = true;
            musicSource.Play(); 
        }
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null) musicSource.volume = volume;
        PlayerPrefs.SetFloat("MusicVolume", volume); 
    }

    public void SetSFXVolume(float volume, bool playPreview = false)
    {
        if (sfxSource != null) sfxSource.volume = volume;
        PlayerPrefs.SetFloat("SFXVolume", volume);

        if (playPreview && Time.time - lastSFXPreviewTime >= SFX_PREVIEW_COOLDOWN)
        {
            PlaySFX("buttonClick");
            lastSFXPreviewTime = Time.time;
        }
    }

    private void LoadVolumeSettings()
    {
        if (musicSource != null) musicSource.volume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        if (sfxSource != null) sfxSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }
}