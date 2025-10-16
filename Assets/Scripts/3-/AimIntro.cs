using UnityEngine;
using TMPro;
using System.Collections;

public class AimIntro : MonoBehaviour
{
    [Header("Références")]
    public TMP_Text instructionText;
    public GameObject[] miniGames; // les 4 GameLogic à activer après l’intro

    [Header("Paramètres")]
    public float displayTime = 5f;
    [TextArea] public string introMessage = "Visez les zones non couvertes !";

    void Start()
    {
        if (instructionText)
            instructionText.text = introMessage;

        // Désactive les mini-jeux pendant l’intro
        foreach (var mg in miniGames)
            if (mg) mg.SetActive(false);

        StartCoroutine(LaunchMiniGames());
    }

    private IEnumerator LaunchMiniGames()
    {
        yield return new WaitForSeconds(displayTime);

        // Cache l’écran d’intro
        gameObject.SetActive(false);

        // Active les mini-jeux
        foreach (var mg in miniGames)
            if (mg) mg.SetActive(true);
    }
}
