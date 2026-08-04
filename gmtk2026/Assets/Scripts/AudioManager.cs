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

    public void PlaySFX(string soundEffectName)
    {
        AudioClip clipToPlay = null;
        

        switch (soundEffectName)
        {
            case "dialogueClick":
                clipToPlay = dialogueClick;
                break;
            case "statChange":
                clipToPlay = statChange;
                break;
            case "moneyChange":
                clipToPlay = moneyChange;
                break;
            case "interlude":
                clipToPlay = interlude;
                break;
            case "buttonClick":
                clipToPlay = buttonClick;
                break;
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
            case "menu":
                clipToPlay = menu; 
                break;
            case "intro":
                clipToPlay = intro; 
                break;
            default:
                Debug.LogWarning("There is no such: " + musicName);
                return; 
        }

        if (musicSource != null && clipToPlay != null)
        {
            if (musicSource.clip == clipToPlay && musicSource.isPlaying) 
            {
                return;
            }

            musicSource.clip = clipToPlay;
            musicSource.loop = true;
            musicSource.Play(); 
        }
    }
}