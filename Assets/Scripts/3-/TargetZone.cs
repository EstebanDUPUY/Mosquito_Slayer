using UnityEngine;

public class TargetZone : MonoBehaviour
{
    [Header("Réglages de score")]
    public int maxScore = 5;           // Score maximum au centre
    public float radius = 0.5f;        // Rayon de précision
    public string zoneName = "Zone";

    /// <summary>
    /// Retourne le score selon la distance au centre.
    /// </summary>
    public int GetScore(Vector2 hitPosition)
    {
        float dist = Vector2.Distance(hitPosition, transform.position);

        if (dist > radius)
            return 0;

        float t = Mathf.Clamp01(1f - (dist / radius));
        int score = Mathf.CeilToInt(t * maxScore);

        return score;
    }
}
