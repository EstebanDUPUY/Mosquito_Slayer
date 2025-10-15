using UnityEngine;
using System.Collections;

public class HumanTarget : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite hurtSprite;

    [Header("Réglages d'animation")]
    public float jumpForce = 2f;
    public float jumpDuration = 0.5f;

    private SpriteRenderer sr;
    private Vector3 basePosition;
    private bool hasJumpedThisRound = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        basePosition = transform.position;
    }

    // Appelée quand le moustique touche
    public void OnBitten()
    {
        if (sr == null || hurtSprite == null) return;
        StartCoroutine(ChangeSpriteRoutine());
    }

    private IEnumerator ChangeSpriteRoutine()
    {
        sr.sprite = hurtSprite;
        yield return new WaitForSeconds(0.4f);
        sr.sprite = normalSprite;
    }

    // Appelée par le mini-jeu une fois par manche
    public IEnumerator RandomJumpInRound(float roundDuration)
    {
        hasJumpedThisRound = false;

        // Détermine un moment aléatoire dans la manche pour sauter
        float jumpTime = Random.Range(1f, roundDuration - 1f);
        yield return new WaitForSeconds(jumpTime);

        if (!hasJumpedThisRound)
        {
            hasJumpedThisRound = true;
            yield return StartCoroutine(DoJump());
        }
    }

    private IEnumerator DoJump()
    {
        float elapsed = 0f;
        Vector3 start = basePosition;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;
            float yOffset = Mathf.Sin(t * Mathf.PI) * jumpForce;
            transform.position = start + new Vector3(0, yOffset, 0);
            yield return null;
        }

        transform.position = basePosition;
    }
}
