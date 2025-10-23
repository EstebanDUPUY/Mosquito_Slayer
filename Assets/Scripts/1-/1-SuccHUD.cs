using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SuccHUD : MonoBehaviour
{
    //
    #region VARIABLES

    [Header("Barres de sang (0..1)")]
    public Slider[] bloodBars; //barres de sang par joueur 

    [Header("Icônes d'attention (alpha = niveau)")]
    public Image[] attentionIcons; //les icônes qui changent en fonction de l'attention des joueurs

    [Header("Morts")]
    public GameObject[] deadCross; //croix qui apparait sur un joueur mort 

    [Header("Splash de sang")]
    public Image[] splashMask; //image de splash de sang par joueur
    [SerializeField] float splashFadeOut = 0.3f; //temps de fondu du splash de sang 

    [SerializeField] GameObject winnerPanel; 
    [SerializeField] bool autoWinFromFill = true; // déclenche si une barre atteint 1.0

    public GameObject[] defeatPanels;   // un panel par joueur

    // état interne
    bool victoryShown = false; //on affiche pas deux fois la victoire au cas où le manager tarderait
    #endregion

    //
    #region AUTRES FONCTIONS

    public void ResetAll() //on remet le HUD à zéro
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
        if (defeatPanels != null)
            foreach (var d in defeatPanels) if (d) d.SetActive(false);

    }

    public void SetBlood(int playerIndex, float ratio01) //on met à jour la jauge de sang du joueur 
    {
        // Si la jauge n'est pas assignée ou l'index est hors limites, on avertit et on sort de la fonction
        if (!Ok(bloodBars, playerIndex)) { Debug.LogWarning("[UI] bloodBars non assigné ou index hors limites."); return; }

        Slider s = bloodBars[playerIndex]; 

        s.minValue = 0f;
        s.maxValue = 1f;
        s.value = Mathf.Clamp01(ratio01); //on garantit que la jauge va bien de 0 à 1 
     
        // Si la barre est pleine, afficher la win (utile même si le manager tarde)
        if (autoWinFromFill && !victoryShown && s.value >= 1f - 0.0001f)
        {
            ShowWinner(playerIndex);
        }
    }

    public void SetAttention(int i, float a01) //on met à jour l'icône d'attention
    {
        if (!Ok(attentionIcons, i)) return;
        var img = attentionIcons[i];
        var c = img.color; c.a = Mathf.Clamp01(a01); img.color = c;
    }

    public void SetDead(int i) //on affiche la croix de mort du joueur 
    {
        if (!Ok(deadCross, i)) return;
        deadCross[i].SetActive(true);
    }

    public void ShowBloodSplash(int i, float hold) //on affiche un splash de sang à l'écran
    {
        if (!Ok(splashMask, i)) { Debug.LogWarning($"[UI] splashMask[{i}] manquant"); return; }
        Debug.Log($"[UI] ShowBloodSplash P{i} ({hold}s)");
        var img = splashMask[i];
        StopAllCoroutines();
        StartCoroutine(Splash(img, hold));
    }

    public void ShowWinner(int i)
    {
        if (victoryShown) return; //on montre qu'une fois la victoire
        victoryShown = true;

        if (winnerPanel) winnerPanel.SetActive(true); //on affiche le panel de victoire 
    }

    public void ShowDefeat(int i)
    {
        if (!Ok(defeatPanels, i)) return;
        defeatPanels[i].SetActive(true); //on affiche le panel de défaite
    }

    #endregion

    //
    #region COROUTINES & UTILS

    IEnumerator Splash(Image img, float hold)
    {
        img.gameObject.SetActive(true); //on affiche l'image de splash 
        var c = img.color; c.a = 1f; img.color = c;
        yield return new WaitForSeconds(hold);

        float t = 0f;
        while (t < splashFadeOut) //tant qu'on fait disparaître le splash 
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / splashFadeOut);//on passe l'alpha de 1 à 0
            img.color = c;
            yield return null; //on attend la frame suivante 
        }
        img.gameObject.SetActive(false); //on cache l'image de splash à la fin
    }

    bool Ok<T>(T[] arr, int i) => arr != null && i >= 0 && i < arr.Length && arr[i] != null;

    #endregion
}
