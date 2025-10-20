/*
 * Ne fait que l'input, les zones, et expose son état. Le manager fait tout le reste.
*/
using UnityEngine;
using UnityEngine.InputSystem;

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

        if (ctx.canceled) { IsSuccing = false; Debug.Log($"[INPUT] P{Index} HOLD END"); }
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
            IsSuccing = false;                 // on “lâche” en esquivant (option)
            iFrameUntil = Time.time + iFrameDuration; // courte invincibilité
        }
    }

    #endregion
}
