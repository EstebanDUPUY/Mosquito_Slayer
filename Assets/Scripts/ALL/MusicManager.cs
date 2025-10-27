using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    //
    #region SINGLETON

    public static MusicManager Instance;

    private void Awake()
    {
        // Singleton classique
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // y en a déjà un -> on détruit le nouveau
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // reste entre les scènes
    }

    #endregion

    //
    #region VARIABLES

    [Header("Audio Source")]
    [SerializeField] private AudioSource musicSource;
    // -> mets un AudioSource sur le même GameObject que MusicManager
    //    désactive "Play On Awake"

    [Header("Transition")]
    [SerializeField] private float fadeOutTime = 0.75f;
    [SerializeField] private float fadeInTime = 0.75f;
    [SerializeField] private float targetVolume = 0.8f;

    [System.Serializable]
    public class SceneMusicPair
    {
        public string sceneName;     // EXACTEMENT le nom de la scène
        public AudioClip musicClip;  // la musique à jouer pour cette scène
    }

    [Header("Musique par scène")]
    [SerializeField] private SceneMusicPair[] sceneMusics;

    // état interne
    private string _currentSceneName = "";
    private AudioClip _currentClip = null;
    private Coroutine _fadeRoutine = null;

    #endregion

    //
    #region START / SCENE LISTENER

    private void Start()
    {
        // On écoute les changements de scène
        SceneManager.sceneLoaded += OnSceneLoaded;

        // On force la musique de la scène de départ
        string startScene = SceneManager.GetActiveScene().name;
        PlayMusicForScene(startScene);
    }

    private void OnDestroy()
    {
        // safety: enlever le listener si ce singleton est détruit (genre à l'arrêt du jeu)
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    #endregion

    //
    #region LOGIQUE PRINCIPALE

    private void PlayMusicForScene(string sceneName)
    {
        // Si on est déjà sur cette scène et on joue déjà la bonne musique -> ne rien faire
        if (_currentSceneName == sceneName)
            return;

        // Trouver le clip à jouer pour cette scène
        AudioClip nextClip = GetClipForScene(sceneName);

        // Mettre à jour la scène actuelle
        _currentSceneName = sceneName;

        // Si pas de clip trouvé -> fade out et stop
        if (nextClip == null)
        {
            _currentClip = null;
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeOutThenStop());
            return;
        }

        // Si c'est le même clip déjà en train de jouer -> ne rien faire
        if (_currentClip == nextClip && musicSource.isPlaying)
            return;

        // Là, on veut changer de musique
        _currentClip = nextClip;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(SwapTrackWithFade(nextClip));
    }

    private AudioClip GetClipForScene(string sceneName)
    {
        // Cherche dans le tableau la scène correspondante
        for (int i = 0; i < sceneMusics.Length; i++)
        {
            if (sceneMusics[i] != null && sceneMusics[i].sceneName == sceneName)
            {
                return sceneMusics[i].musicClip;
            }
        }
        return null; // pas de track assignée pour cette scène
    }

    #endregion

    //
    #region FADE COROUTINES

    private IEnumerator SwapTrackWithFade(AudioClip newClip)
    {
        // 1) Fade out l'ancienne musique si y'en a une
        if (musicSource.isPlaying && musicSource.clip != null)
        {
            yield return StartCoroutine(FadeVolume(musicSource, musicSource.volume, 0f, fadeOutTime));
        }

        // 2) Changer de clip
        musicSource.clip = newClip;
        if (newClip != null)
        {
            musicSource.Play();
        }

        // 3) Fade in vers targetVolume
        yield return StartCoroutine(FadeVolume(musicSource, 0f, targetVolume, fadeInTime));

        _fadeRoutine = null;
    }

    private IEnumerator FadeOutThenStop()
    {
        // Si rien ne joue, rien à faire
        if (!musicSource.isPlaying || musicSource.clip == null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            _fadeRoutine = null;
            yield break;
        }

        // fade-out
        yield return StartCoroutine(FadeVolume(musicSource, musicSource.volume, 0f, fadeOutTime));

        musicSource.Stop();
        musicSource.clip = null;
        _fadeRoutine = null;
    }

    private IEnumerator FadeVolume(AudioSource src, float from, float to, float duration)
    {
        if (src == null)
            yield break;

        if (duration <= 0f)
        {
            src.volume = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // unscaled pour ignorer le Time.timeScale
            float p = t / duration;
            src.volume = Mathf.Lerp(from, to, p);
            yield return null;
        }

        src.volume = to;
    }

    #endregion
}
