using UnityEngine;
using TMPro;
using System.Collections;

public class AimIntro : MonoBehaviour
{
    [Header("Références")]
    public TMP_Text instructionText;
    public GameObject miniGame;  // ton objet ou ton script du mini-jeu

    [Header("Paramètres")]
    public float displayTime = 5f;
    public string introMessage = "Visez les zones non couvertes !";

    void Start()
    {
        instructionText.text = introMessage;
        miniGame.SetActive(false); // le mini-jeu ne tourne pas encore
        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);

        // Cache l’écran d’intro
        gameObject.SetActive(false);

        // Lance le mini-jeu
        miniGame.SetActive(true);
        // Ou, si tu veux déclencher une fonction spécifique :
        // miniGame.GetComponent<AimMiniGame>().StartMiniGame();
    }
}
