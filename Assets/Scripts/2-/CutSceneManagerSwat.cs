using System.Collections;
using UnityEngine;

public class CutSceneManagerSwat : MonoBehaviour
{

    [Header("Références")]
    public Camera mainCamera; // Caméra de la scène      
    public GameObject introPlan; // L'objet UI/visuel de l'intro


    [Header("Cibles de Caméra")]
    public Transform introTarget;
    public Transform gameTarget;


    [Header("Zooms (taille Orthographique)")]
    public float introZoom = 5f;
    public float gameZoom = 10f;

    [Header("Durées")]
    public float introWaitTime = 2f;
    public float transitionTime = 1.5f;

    public System.Action OnCutsceneFinished;

   




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(IntroSequenceRoutine());
    }

    IEnumerator IntroSequenceRoutine()
    {
        // --- 1. CONFIGURATION INITIALE ---
        // On "téléporte" la caméra à la position d'intro
        mainCamera.transform.position = introTarget.position;
        mainCamera.transform.rotation = introTarget.rotation;
        mainCamera.orthographicSize = introZoom;

        // On active le visuel d'intro
        if (introPlan != null) introPlan.SetActive(true);

        // --- 2. PAUSE SUR L'INTRO ---
        // On attend que le joueur regarde l'intro
        yield return new WaitForSeconds(introWaitTime);

        // --- 3. Transition En Douceur ---
        float t = 0f;
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float startZoom = mainCamera.orthographicSize;


        while (t < transitionTime)
        {
            // t va de 0 à transitionTime. On le divise pour avoir un pourcentage (0 à 1)
            t += Time.deltaTime;
            float percent = t / transitionTime;

            // On utilise une courbe douce (SmoothStep) pour un effet plus pro
            float smoothPercent = Mathf.SmoothStep(0f, 1f, percent);

            // On déplace la caméra image par image
            mainCamera.transform.position = Vector3.Lerp(startPos, gameTarget.position, smoothPercent);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, gameTarget.rotation, smoothPercent);
            mainCamera.orthographicSize = Mathf.Lerp(startZoom, gameZoom, smoothPercent);

            yield return null; // Attend la prochaine frame
        }

        // --- 4. FIN ---
        // Pour être sûr, on force la position finale (évite les micro-erreurs de Lerp)
        mainCamera.transform.position = gameTarget.position;
        mainCamera.transform.rotation = gameTarget.rotation;
        mainCamera.orthographicSize = gameZoom;

        // On cache le visuel d'intro
        if (introPlan != null) introPlan.SetActive(false);

        // On prévient le GameManager !
        OnCutsceneFinished?.Invoke();
    }

   
}
