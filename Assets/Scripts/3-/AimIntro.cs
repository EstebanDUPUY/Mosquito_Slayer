using UnityEngine;
using TMPro;
using System.Collections;

public class AimIntro : MonoBehaviour
{
    public TMP_Text instructionText;
    public float displayTime = 5f;
    public string introMessage = "Visez les zones non couvertes !";
    public GameObject[] miniGames; // 4 mini-jeux à activer

    void Start()
    {
        instructionText.text = introMessage;

        // Tout désactiver avant de commencer
        foreach (var mg in miniGames)
            mg.SetActive(false);

        StartCoroutine(LaunchMiniGames());
    }

    private IEnumerator LaunchMiniGames()
    {
        yield return new WaitForSeconds(displayTime);
        gameObject.SetActive(false);

        // Active tous les mini-jeux
        foreach (var mg in miniGames)
            mg.SetActive(true);
    }
}
