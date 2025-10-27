using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InstructionCountdownIntro : MonoBehaviour
{
    [Header("Panels & UI")]
    [SerializeField] GameObject instructionsPanel;
    [SerializeField] CanvasGroup instructionsCg;
    [SerializeField] GameObject countdownPanel;
    [SerializeField] CanvasGroup countdownCg;

    [Header("Countdown label (met l'objet 'Timer')")]
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] TextMeshProUGUI timerTextTMP;

    [Header("Durées (secondes)")]
    [SerializeField] float instructionsHold = 5f;
    [SerializeField] float fadeDuration = 0.4f;
    [SerializeField] float countdownStep = 1f; // 1 seconde par pas: 3,2,1,0

    [Header("Gameplay")]
    [SerializeField] SuccManager succManager;
    PlayerInput[] _allInputs;

    [Header("Message GO !")]                  
    [SerializeField] string goMessage = "C'est parti !"; 
    [SerializeField] float goHold = 0.8f;              

    void Start()
    {
        if (!succManager) succManager = FindFirstObjectByType<SuccManager>();
        
        SetActive(instructionsPanel, true, instructionsCg, 0f); //on active le panel d'instructions
        SetActive(countdownPanel, false, countdownCg, 0f); //on désactive le panel de countdown

        _allInputs = FindObjectsOfType<PlayerInput>();;

   
        SetInputsEnabled(false);

        StartCoroutine(Flow());
    }

    IEnumerator Flow()
    {
        // 1) INSTRUCTIONS : fade-in, hold, fade-out
        yield return Fade(instructionsCg, 0f, 1f, fadeDuration);
        yield return new WaitForSeconds(instructionsHold);
        yield return Fade(instructionsCg, 1f, 0f, fadeDuration);
        SetActive(instructionsPanel, false, instructionsCg, 0f);

        // 2) COUNTDOWN : activer + fade-in
        SetActive(countdownPanel, true, countdownCg, 0f);
        yield return Fade(countdownCg, 0f, 1f, fadeDuration);

        // Compte à rebours de 3 à 0 
        for (int n = 3; n >= 0; n--)
        {
            SetTimerText(n.ToString());
            yield return new WaitForSeconds(countdownStep);
        }

        SetTimerText(goMessage);
        yield return new WaitForSeconds(goHold);

        yield return Fade(countdownCg, 1f, 0f, fadeDuration); //on fait disparaître le countdown petit à petit 
        SetActive(countdownPanel, false, countdownCg, 0f); //on désactive le countdown

        SetInputsEnabled(true);


        // 3) On lance le mini-jeu
        if (succManager != null) succManager.StartRound();
        // on peut activer ici nos contrôles joueurs, etc.
    }

    void SetInputsEnabled(bool enabled)
    {
        if (_allInputs == null) return;
        foreach (var pi in _allInputs)
        {
            if (pi == null) continue;
            pi.enabled = enabled;
        }
    }

    // --- Helpers ---

    void SetActive(GameObject go, bool on, CanvasGroup cg, float alpha)
    {
        if (go) go.SetActive(on);
        if (cg) cg.alpha = alpha;
        if (cg) cg.blocksRaycasts = on;
        if (cg) cg.interactable = on;
    }

    IEnumerator Fade(CanvasGroup cg, float a, float b, float dur)
    {
        if (!cg || dur <= 0f) { if (cg) cg.alpha = b; yield break; }
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(a, b, t / dur);
            yield return null;
        }
        cg.alpha = b;
    }

    void SetTimerText(string s)
    {
        if (timerText) timerText.text = s;
   
        if (timerTextTMP) timerTextTMP.text = s;
    }
}
