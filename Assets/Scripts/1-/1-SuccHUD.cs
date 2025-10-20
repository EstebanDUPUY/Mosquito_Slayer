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

    [SerializeField] GameObject winnerPanel;   // <- assigne ton panel ici
    [SerializeField] Text winnerText;          // <- texte dans le panel
    [SerializeField] bool autoWinFromFill = true; // déclenche si une barre atteint 1.0

    public GameObject[] defeatPanels;   // un panel par joueur

    // état interne
    bool victoryShown = false;
    #endregion

    //
    #region API

    public void ResetAll()
    {
        victoryShown = false;

        if (bloodBars != null)
            foreach (var s in bloodBars) if (s) { s.minValue = 0f; s.maxValue = 1f; s.value = 0f; }

        if (attentionIcons != null)
            foreach (var a in attentionIcons) if (a) { var c = a.color; c.a = 0f; a.color = c; }

        if (deadCross != null)
            foreach (var d in deadCross) if (d) d.SetActive(false);

        if (splashMask != null)
            foreach (var m in splashMask) if (m) m.gameObject.SetActive(false);

        if (winnerPanel) winnerPanel.SetActive(false);
        if (winnerText) { winnerText.text = ""; if (winnerText.gameObject != winnerPanel) winnerText.gameObject.SetActive(false); }
        if (defeatPanels != null)
            foreach (var d in defeatPanels) if (d) d.SetActive(false);

    }

    public void SetBlood(int playerIndex, float ratio01)
    {
        // Debug.Log($"[UI] SetBlood P{playerIndex} -> {ratio01:0.00}");
        if (!Ok(bloodBars, playerIndex)) { Debug.LogWarning("[UI] bloodBars non assigné ou index hors limites."); return; }

        Slider s = bloodBars[playerIndex];

        s.minValue = 0f;
        s.maxValue = 1f;
        s.value = Mathf.Clamp01(ratio01);

        // Option : si la barre est pleine, afficher la win (utile même si le Manager tarde)
        if (autoWinFromFill && !victoryShown && s.value >= 1f - 0.0001f)
        {
            ShowWinner(playerIndex);
        }
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

    public void ShowBloodSplash(int i, float hold)
    {
        if (!Ok(splashMask, i)) { Debug.LogWarning($"[UI] splashMask[{i}] manquant"); return; }
        Debug.Log($"[UI] ShowBloodSplash P{i} ({hold}s)");
        var img = splashMask[i];
        StopAllCoroutines();
        StartCoroutine(Splash(img, hold));
    }

    public void ShowWinner(int i)
    {
        if (victoryShown) return;
        victoryShown = true;

        if (winnerText) { winnerText.text = $"Joueur {i + 1} gagne !"; winnerText.gameObject.SetActive(true); }
        if (winnerPanel) winnerPanel.SetActive(true);
    }

    public void ShowDefeat(int i)
    {
        if (!Ok(defeatPanels, i)) return;
        defeatPanels[i].SetActive(true);
    }

    #endregion

    //
    #region COROUTINES & UTILS

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
