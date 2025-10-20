/*
 * Ne fait que l'input, les zones, et expose son état. Le manager fait tout le reste.
*/
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerSucc : MonoBehaviour
{
    //
    #region VARIABLES

    [Header("État joueur")]
    public bool IsAlive = true;
    public bool IsSuccing = false;  // E/A maintenue ?
    public bool OnGroundSuck = true;   // true si pas de zones
    public bool InDeathZone = false;

    public int Index { get; set; }
    public int Points { get; private set; }
    public int MaxPoints { get; private set; } = 20;
    public float Attention { get; set; } // 0..1

    public SuccManager Manager { get; set; }

    // dodge i-frames
    [SerializeField] float iFrameDuration = 0.25f;
    float iFrameUntil = 0f;
    [SerializeField] float sabotageCooldown = 2f; // secondes
    float sabotageReadyAt = 0f;
    public bool CanSabotage => Time.time >= sabotageReadyAt;

    [Header("Auto-Dodge (à gauche)")]
    [SerializeField] bool autoDodgeOnRelease = true;
    [SerializeField] float dodgeDistance = 1.2f;   // combien vers la gauche
    [SerializeField] float dodgeSpeed = 6f;        // unités / sec
    [SerializeField] float dodgeMaxTime = 0.35f;   // garde-fou


    [SerializeField] bool autoReturnToStart = true;   // ← revient au point de départ
    [SerializeField] float returnSpeed = 8f;
    [SerializeField] float returnMaxTime = 0.6f;
    [SerializeField] float returnDelay = 0.05f;  // petite pause avant le retour

    Coroutine dodgeCo;
    Rigidbody2D _rb;
    Vector3 _dodgeStartPos; // mémorise la position d’origine

    #endregion

    //
    #region START/UPDATE

    private PlayerInput _pi;

    private void Awake()
    {
        _pi = GetComponent<PlayerInput>();
        // IMPORTANT : le nom doit correspondre exactement à l’action
        var suck = _pi.actions["Suck"];
        suck.started += SuccInput;
        suck.canceled += SuccInput;

        var sab = _pi.actions["BlindEnemies"];
        sab.performed += SabotageInput;

        _rb = GetComponent<Rigidbody2D>(); // optionnel
    }

    #endregion

    private void OnDestroy()
    {
        if (_pi == null) return;
        var suck = _pi.actions["Suck"];
        suck.started -= SuccInput;
        suck.canceled -= SuccInput;

        var sab = _pi.actions["BlindEnemies"];
        sab.performed -= SabotageInput;     // ← pense à te désabonner aussi
    }

    //
    #region INPUT

    public void SuccInput(InputAction.CallbackContext ctx) // action "Suck" (E / bouton South)
    {
        if (!IsAlive) return;
        if (ctx.started) { IsSuccing = true; Debug.Log($"[INPUT] P{Index} HOLD START"); }

        if (ctx.canceled)
        {
            IsSuccing = false;
            Debug.Log($"[INPUT] P{Index} HOLD END");

            if (autoDodgeOnRelease)
                StartDodgeLeft();
        }
    }

    public void SabotageInput(InputAction.CallbackContext ctx) // option (Q / bouton West)
    {
        if (!IsAlive || !ctx.performed) return;

        if (!CanSabotage)
        {
            // feedback console (optionnel)
            Debug.Log($"[SABO] P{Index} cooldown {sabotageReadyAt - Time.time:0.00}s");
            return;
        }
        Manager?.OnSabotageAsked(Index); 
        Debug.Log("Yo");

        // démarre le CD uniquement si on a vraiment tenté (évite le spam)
        sabotageReadyAt = Time.time + sabotageCooldown;
    }

    #endregion

    //
    #region LOGIQUE & ZONES

    public void ResetState(int targetScore)
    {
        IsAlive = true; IsSuccing = false; OnGroundSuck = true; InDeathZone = false;
        Points = 0; Attention = 0f; MaxPoints = targetScore; iFrameUntil = 0f;
        sabotageReadyAt = 0f;
    }

    public void AddPoints(int delta)
    {
        Points = Mathf.Clamp(Points + delta, 0, MaxPoints);
    }

    public void TryKillFromAttack()
    {
        if (!IsAlive) return;
        bool invincible = Time.time < iFrameUntil;
        if (InDeathZone && IsSuccing && !invincible) Die();
    }

    void Die()
    {
        IsAlive = false;
        IsSuccing = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("GroundSuck")) OnGroundSuck = true;
        if (other.CompareTag("DeathZone")) InDeathZone = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("GroundSuck")) OnGroundSuck = false;

        if (other.CompareTag("DeathZone"))
        {
            InDeathZone = false;
        }
    }

    void StartDodgeLeft()
    {
        // déjà en dodge ? on redémarre
        if (dodgeCo != null) StopCoroutine(dodgeCo);
        dodgeCo = StartCoroutine(DodgeLeftAndReturnCo());
    }

    IEnumerator DodgeLeftAndReturnCo()
    {
        // --- phase 1 : mémoriser le départ et aller à gauche ---
        _dodgeStartPos = transform.position;
        Vector3 leftTarget = _dodgeStartPos + Vector3.left * dodgeDistance;

        bool hasRB = _rb != null;
        bool useFixed = hasRB && _rb.bodyType == RigidbodyType2D.Dynamic;

        float endTime = Time.time + dodgeMaxTime;
        while (Time.time < endTime && !IsSuccing)   // si tu recommences à sucer, on interrompt l’esquive
        {
            Vector3 cur = transform.position;
            Vector3 next = Vector3.MoveTowards(cur, leftTarget, dodgeSpeed * (useFixed ? Time.fixedDeltaTime : Time.deltaTime));

            if (hasRB)
            {
                if (useFixed) _rb.MovePosition(next);
                else _rb.position = next;         // Kinematic
            }
            else transform.position = next;

            if ((next - leftTarget).sqrMagnitude <= 0.0001f) break;
            if (useFixed) yield return new WaitForFixedUpdate(); else yield return null;
        }

        // Option : petite pause avant de revenir
        if (autoReturnToStart) yield return new WaitForSeconds(returnDelay);

        // --- phase 2 : retour à la position d’origine ---
        if (autoReturnToStart)
        {
            float endRet = Time.time + returnMaxTime;
            while (Time.time < endRet)
            {
                Vector3 cur = transform.position;
                Vector3 next = Vector3.MoveTowards(cur, _dodgeStartPos, returnSpeed * (useFixed ? Time.fixedDeltaTime : Time.deltaTime));

                if (hasRB)
                {
                    if (useFixed) _rb.MovePosition(next);
                    else _rb.position = next;
                }
                else transform.position = next;

                if ((next - _dodgeStartPos).sqrMagnitude <= 0.0001f) break;
                if (useFixed) yield return new WaitForFixedUpdate(); else yield return null;
            }
        }

        dodgeCo = null;
    }

    #endregion
}
