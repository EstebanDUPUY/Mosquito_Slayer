using UnityEngine;
using TMPro;
using System.Collections;

public class AimIntro : MonoBehaviour
{
    [Header("Références")]
    public TMP_Text instructionText;
    public TMP_Text timerText; // <-- Texte pour afficher le timer
    public GameObject miniGame;

    [Header("Paramètres")]
    public float displayTime = 5f;
    [TextArea] public string introMessage = "Visez les zones non couvertes !";

    private float timer;

    void Start()
    {
        timer = displayTime;

        if (instructionText) instructionText.text = introMessage;
        if (miniGame) miniGame.SetActive(false);

        StartCoroutine(StartAfterDelay());
    }

    private IEnumerator StartAfterDelay()
    {
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            if (timerText) timerText.text = Mathf.Ceil(timer).ToString(); // Affiche les secondes restantes
            yield return null;
        }

        if (miniGame) miniGame.SetActive(true);
        gameObject.SetActive(false);
    }
}
