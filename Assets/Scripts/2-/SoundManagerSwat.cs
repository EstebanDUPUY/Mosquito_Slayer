using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundManagerSwat : MonoBehaviour
{
    public static SoundManagerSwat Instance;

    private AudioSource source;

    [Header("Clips & Volumes")]
    public AudioClip countdownBeep;
    [Range(0f, 1f)] public float countdownVolume = 0.8f;

    public AudioClip warningSound;
    [Range(0f, 1f)] public float warningVolume = 1.0f;

    public AudioClip impactSound;
    [Range(0f, 1f)] public float impactVolume = 1.0f;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("Un 2e SoundManagerSwat a été trouvé. Destruction.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        source = GetComponent<AudioSource>();
    }

    public void PlayCountdown()
    {
        PlaySound(countdownBeep, countdownVolume); 
    }

    public void PlayWarning()
    {
        PlaySound(warningSound, warningVolume);
    }

    public void PlayImpact()
    {
        PlaySound(impactSound, impactVolume);
    }
   
    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip != null)
        {
            source.PlayOneShot(clip, volume);
        }
        else
        {
            Debug.LogWarning($"SoundManager: Tentative de jouer un son manquant !");
        }
    }
    
}
