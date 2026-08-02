using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource sfxSource; 
    public AudioSource bgmSource;
    
    [Header("Sound Effects")]
    public AudioClip dialogueClickSound; 
    public AudioClip statChangeSound;    
    public AudioClip moneyChangeSound;
    public AudioClip breakSound;

    [Header("Background Music")]
    public AudioClip backgroundMusic;  

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Uncomment this line if you want the AudioManager to persist across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // if (bgmSource != null && backgroundMusic != null)
        // {
        //     bgmSource.clip = backgroundMusic;
        //     bgmSource.loop = true;
        //     bgmSource.Play();
        // }
    }

    public void PlayDialogueClick()
    {
        PlaySFX(dialogueClickSound);
    }

    public void PlayStatChange()
    {
        PlaySFX(statChangeSound);
    }

    public void PlayMoneyChange()
    {
        PlaySFX(moneyChangeSound);
    }

    public void PlayBreakSound()
    {
        PlaySFX(breakSound);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (bgmSource != null && clip != null)
        {
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }
}