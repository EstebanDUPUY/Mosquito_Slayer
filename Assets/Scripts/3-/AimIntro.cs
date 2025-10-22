using UnityEngine;
using TMPro;
using System.Collections;

public class AimIntro : MonoBehaviour
{
    [Header("Références")]
    public TMP_Text instructionText;
    public GameObject miniGame; // ton GameLogic

    [Header("Paramètres")]
    public float displayTime = 5f;
    [TextArea] public string introMessage = "Visez les zones non couvertes !";

    void Start()
    {
        if (instructionText) instructionText.text = introMessage;
        if (miniGame) miniGame.SetActive(false);
        StartCoroutine(StartAfterDelay());
    }

    private IEnumerator StartAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);
        if (miniGame) miniGame.SetActive(true);
        gameObject.SetActive(false);
    }
}
