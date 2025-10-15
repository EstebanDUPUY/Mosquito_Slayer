using UnityEngine;

public class TargetZone : MonoBehaviour
{
    [Header("Réglages de score")]
    public int maxScore = 5;
    public float radius = 0.5f;
    public string zoneName = "Zone";

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public int GetScore(Vector2 hitPosition)
    {
        float dist = Vector2.Distance(hitPosition, transform.position);

        if (dist > radius)
        {
            Flash(Color.gray);
            return 0;
        }

        float t = Mathf.Clamp01(1f - (dist / radius));
        int score = Mathf.CeilToInt(t * maxScore);

        Flash(Color.yellow);
        return score;
    }

    private void Flash(Color c)
    {
        if (sr == null) return;
        StartCoroutine(FlashColor(c));
    }

    private System.Collections.IEnumerator FlashColor(Color c)
    {
        Color baseColor = sr.color;
        sr.color = c;
        yield return new WaitForSeconds(0.15f);
        sr.color = baseColor;
    }
}
