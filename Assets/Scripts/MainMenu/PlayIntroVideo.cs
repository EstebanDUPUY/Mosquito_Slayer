using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.InputSystem; // important pour le new input system

public class PlayIntroVideo : MonoBehaviour
{
    [Header("Références")]
    public VideoPlayer introVideo;
    public string nextSceneName = "Scene_Jeu";
    public GameObject uiToHide;

    [Header("Paramètres")]
    public bool allowSkip = true;  // pour activer/désactiver la possibilité de skip
    public InputAction skipAction; // pour définir la touche de skip (dans l’inspecteur)

    private bool hasStarted = false;

    private void OnEnable()
    {
        skipAction.Enable();
    }

    private void OnDisable()
    {
        skipAction.Disable();
    }

    public void OnPlayClicked()
    {
        if (hasStarted) return;
        hasStarted = true;
        StartCoroutine(PlayIntroAndLoad());
    }

    private IEnumerator PlayIntroAndLoad()
    {
        if (uiToHide != null)
            uiToHide.SetActive(false);

        introVideo.gameObject.SetActive(true);
        introVideo.Play();

        while (!introVideo.isPrepared)
            yield return null;

        yield return new WaitUntil(() => introVideo.isPlaying);

        while (introVideo.isPlaying)
        {
            if (allowSkip && skipAction.WasPerformedThisFrame())
            {
                introVideo.Stop();
                break;
            }
            yield return null;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
