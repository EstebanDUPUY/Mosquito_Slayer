using UnityEngine;

public class TargetZone : MonoBehaviour
{
    [Header("Réglages de score")]
    public int maxScore = 5;           // Score maximum au centre
    public float radius = 0.5f;        // Rayon de précision
    public string zoneName = "Zone";

    [Header("Apparence")]
    public Color hitColor = Color.red; // Couleur quand piquée
    public float visibleDuration = 0.25f; // Temps d'affichage après la piqûre

    private SpriteRenderer sr;
    private Color baseColor;
    private bool isVisible = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            baseColor = sr.color;
            // On rend la zone invisible au départ
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
        }
    }

    /// <summary>
    /// Retourne le score selon la distance au centre,
    /// et rend la zone visible brièvement.
    /// </summary>
    public int GetScore(Vector2 hitPosition)
    {
        float dist = Vector2.Distance(hitPosition, transform.position);

        if (dist > radius)
            return 0;

        float t = Mathf.Clamp01(1f - (dist / radius));
        int score = Mathf.CeilToInt(t * maxScore);

        // Feedback visuel
        if (!isVisible && sr != null)
            StartCoroutine(FlashColor(hitColor));

        return score;
    }

    private System.Collections.IEnumerator FlashColor(Color c)
    {
        isVisible = true;

        // Passe la zone visible et rouge
        Color visible = c;
        visible.a = 1f;
        sr.color = visible;

        yield return new WaitForSeconds(visibleDuration);

        // Redevient invisible
        Color invisible = baseColor;
        invisible.a = 0f;
        sr.color = invisible;

        isVisible = false;
    }
}
