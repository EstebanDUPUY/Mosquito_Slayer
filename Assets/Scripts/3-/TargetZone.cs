using UnityEngine;

public class TargetZone : MonoBehaviour
{
    [Header("Score attribué si touché")]
    public int scoreValue = 3;

    [Header("Nom de la zone")]
    public string zoneName = "Zone";

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public int GetScore()
    {
        if (sr) StartCoroutine(FlashColor());
        return scoreValue;
    }

    private System.Collections.IEnumerator FlashColor()
    {
        Color baseColor = sr.color;
        sr.color = Color.yellow;
        yield return new WaitForSeconds(0.15f);
        sr.color = baseColor;
    }
}
