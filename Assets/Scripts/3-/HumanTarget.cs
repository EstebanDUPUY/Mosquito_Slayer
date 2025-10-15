using UnityEngine;
using System.Collections;

public class HumanTarget : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite normalSprite;   // Sprite par défaut
    public Sprite hurtSprite;     // Sprite quand piqué
    public Sprite jumpSprite;     // Sprite pendant le saut

    [Header("Réglages de saut")]
    public float jumpForce = 2f;
    public float jumpDuration = 0.5f;
    public int maxJumpsPerRound = 2;
    public float minTimeBetweenJumps = 1.5f;
    public float maxTimeBetweenJumps = 3.5f;

    [Header("Effet Idle (Flip)")]
    public float idleFlipIntervalMin = 1.5f; // temps min entre deux flips
    public float idleFlipIntervalMax = 3f;   // temps max entre deux flips

    private SpriteRenderer sr;
    private Vector3 basePosition;
    private bool isJumping = false;
    private bool isHurt = false;
    private Coroutine jumpRoutine;
    private Coroutine idleRoutine;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        basePosition = transform.position;
    }

    private void OnEnable()
    {
        // Lance l'effet idle
        idleRoutine = StartCoroutine(IdleFlipRoutine());
    }

    private void OnDisable()
    {
        if (idleRoutine != null)
            StopCoroutine(idleRoutine);
    }

    /// <summary>
    /// Réagit à une piqûre de moustique.
    /// </summary>
    public void OnBitten()
    {
        if (sr == null || hurtSprite == null) return;
        StartCoroutine(ChangeSpriteRoutine());
    }

    private IEnumerator ChangeSpriteRoutine()
    {
        isHurt = true;
        sr.sprite = hurtSprite;
        yield return new WaitForSeconds(0.4f);
        isHurt = false;
        if (!isJumping)
            sr.sprite = normalSprite;
    }

    /// <summary>
    /// Lance les sauts aléatoires pendant la manche.
    /// </summary>
    public void StartJumpsForRound(float roundDuration)
    {
        if (jumpRoutine != null)
            StopCoroutine(jumpRoutine);

        jumpRoutine = StartCoroutine(JumpRoutine(roundDuration));
    }

    private IEnumerator JumpRoutine(float roundDuration)
    {
        int jumpsDone = 0;
        float elapsed = 0f;

        while (elapsed < roundDuration && jumpsDone < maxJumpsPerRound)
        {
            float delay = Random.Range(minTimeBetweenJumps, maxTimeBetweenJumps);
            yield return new WaitForSeconds(delay);

            elapsed += delay;
            if (elapsed >= roundDuration) break;

            jumpsDone++;
            yield return StartCoroutine(DoJump());
        }
    }

    private IEnumerator DoJump()
    {
        isJumping = true;

        if (sr != null && jumpSprite != null)
            sr.sprite = jumpSprite;

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
        isJumping = false;

        if (!isHurt && sr != null)
            sr.sprite = normalSprite;
    }

    /// <summary>
    /// Fait un petit flip horizontal de temps en temps (idle).
    /// </summary>
    private IEnumerator IdleFlipRoutine()
    {
        while (true)
        {
            // Attente aléatoire entre deux flips
            float wait = Random.Range(idleFlipIntervalMin, idleFlipIntervalMax);
            yield return new WaitForSeconds(wait);

            // Ne pas faire l'idle si l'humain saute ou se fait piquer
            if (!isJumping && !isHurt && sr != null)
            {
                sr.flipX = !sr.flipX;
            }
        }
    }
}
