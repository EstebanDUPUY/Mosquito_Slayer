/*
 * SuccHUD.cs (version simple & lisible)
 * Affichage uniquement : jauge, attention (alpha), mort, warning, splash, vainqueur.
*/
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SuccHUD : MonoBehaviour
{
    //
    #region VARIABLES

    [Header("Barres de sang (0..1)")]
    public Slider[] bloodBars;

    [Header("Icônes d'attention (alpha = niveau)")]
    public Image[] attentionIcons;

    [Header("Morts")]
    public GameObject[] deadCross;

    [Header("Splash de sang")]
    public Image[] splashMask;
    [SerializeField] float splashFadeOut = 0.3f;

    [Header("Avertissement d'attaque")]
    public GameObject attackWarning;
    [SerializeField] float warningDuration = 0.25f;

    [Header("Vainqueur")]
    public Text winnerText;

    #endregion

    //
    #region API

    public void ResetAll()
    {
        if (bloodBars != null) foreach (var s in bloodBars) if (s) { s.minValue = 0f; s.maxValue = 1f; s.value = 0f; }
        if (attentionIcons != null) foreach (var a in attentionIcons) if (a) { var c = a.color; c.a = 0f; a.color = c; }
        if (deadCross != null) foreach (var d in deadCross) if (d) d.SetActive(false);
        if (splashMask != null) foreach (var m in splashMask) if (m) m.gameObject.SetActive(false);
        if (attackWarning) attackWarning.SetActive(false);
        if (winnerText) { winnerText.text = ""; winnerText.gameObject.SetActive(false); }
    }

    public void SetBlood(int playerIndex, float ratio01)
    {
        Debug.Log($"[UI] SetBlood P{playerIndex} -> {ratio01:0.00}");
        if (bloodBars == null || playerIndex < 0 || playerIndex >= bloodBars.Length || bloodBars[playerIndex] == null)
        {
            Debug.LogWarning("[UI] bloodBars non assigné ou index hors limites.");
            return;
        }
        bloodBars[playerIndex].minValue = 0f;
        bloodBars[playerIndex].maxValue = 1f;
        bloodBars[playerIndex].value = Mathf.Clamp01(ratio01);
    }
    public void SetAttention(int i, float a01)
    {
        if (!Ok(attentionIcons, i)) return;
        var img = attentionIcons[i];
        var c = img.color; c.a = Mathf.Clamp01(a01); img.color = c;
    }

    public void SetDead(int i)
    {
        if (!Ok(deadCross, i)) return;
        deadCross[i].SetActive(true);
    }

    public void ShowAttackWarning(int _)
    {
        if (!attackWarning) return;
        attackWarning.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(HideWarning());
    }

    public void ShowBloodSplash(int i, float hold)
    {
        if (!Ok(splashMask, i)) return;
        var img = splashMask[i];
        StopAllCoroutines();
        StartCoroutine(Splash(img, hold));
    }

    public void ShowWinner(int i)
    {
        if (!winnerText) return;
        winnerText.text = $"Joueur {i + 1} gagne !";
        winnerText.gameObject.SetActive(true);
    }

    #endregion

    //
    #region COROUTINES & UTILS

    IEnumerator HideWarning()
    {
        yield return new WaitForSeconds(warningDuration);
        if (attackWarning) attackWarning.SetActive(false);
    }

    IEnumerator Splash(Image img, float hold)
    {
        img.gameObject.SetActive(true);
        var c = img.color; c.a = 1f; img.color = c;
        yield return new WaitForSeconds(hold);

        float t = 0f;
        while (t < splashFadeOut)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / splashFadeOut);
            img.color = c;
            yield return null;
        }
        img.gameObject.SetActive(false);
    }

    bool Ok<T>(T[] arr, int i) => arr != null && i >= 0 && i < arr.Length && arr[i] != null;

    #endregion
}
