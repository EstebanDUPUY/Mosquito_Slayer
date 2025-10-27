using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerSucc : MonoBehaviour
{
    //
    #region VARIABLES

    [Header("État joueur")]
    public bool IsAlive = true; // pour voir si le joueur est en vie ou non
    public bool IsSuccing = false;  // pour voir si le joueur est en train de sucer ou non
    public bool OnGroundSuck = true;   // pour voir si le joueur est sur une zone de succion au sol
    public bool InDeathZone = false; // pour voir si le joueur est dans la zone de mort possible

    public int Index { get; set; } //on prend le numéro du joueur dans l'index
    public int Points { get; private set; } //ce sont les points de sang
    public int MaxPoints { get; private set; } = 20; //c'est l'objectif de point pour gagner
    public float Attention { get; set; } // c'est pour savoir à quel point l'ennemi est alerté par les actions du joueur 

    public SuccManager Manager { get; set; } //on récupère le chef d'orchestre pour les règles  du jeu

    // quand on esquive, on est invincible un petit temps
    [SerializeField] float iFrameDuration = 0.25f;
    float iFrameUntil = 0f;
    [SerializeField] float sabotageCooldown = 2f; 
    float sabotageReadyAt = 0f;
    public bool CanSabotage => Time.time >= sabotageReadyAt; // après avoir saboté, c'est le cooldown avant de pouvoir le refaire

    [Header("Auto-Dodge (à gauche)")]
    [SerializeField] bool autoDodgeOnRelease = true; //dès qu'on lâche le bouton de succion, on esquive automatiquement
    [SerializeField] float dodgeDistance = 1.2f;   // on esquive de combien X distance vers la gauche ? 
    [SerializeField] float dodgeSpeed = 6f;        // on esquive vite ou lentement ? 
    [SerializeField] float dodgeMaxTime = 0.35f;   // une sécurité pour arrêter l'esquive si elle dure trop longtemps


    [SerializeField] bool autoReturnToStart = true;   // revient au point de départ après avoir esquivé
    [SerializeField] float returnSpeed = 8f; // la vitesse de retour au point de départ
    [SerializeField] float returnMaxTime = 0.6f; // sécurité pour arrêter le retour si ça dure trop longtemps
    [SerializeField] float returnDelay = 0.05f;  // petite pause avant le retour

    Coroutine dodgeCo; //on contrôle la coroutine d'esquive
    Rigidbody2D _rb;
    Vector3 _dodgeStartPos; // mémorise la position d’origine pour savoir d'où on est parti 

    #endregion

    //
    #region START/UPDATE

    private PlayerInput _pi;

    private void Awake()
    {
        _pi = GetComponent<PlayerInput>();
        var suck = _pi.actions["Suck"];
        suck.started += SuccInput; //quand on appuie sur le bouton Suck, on commence à sucer
        suck.canceled += SuccInput; //quand on arrête d'appuyer sur le bouton Suck, on arrête de sucer

        var sab = _pi.actions["BlindEnemies"];
        sab.performed += SabotageInput; //quand on appuie sur le bouton Sabotage, on lance la fonction de sabotage

        _rb = GetComponent<Rigidbody2D>(); // s'il est présent, on l'utilise 
    }

    #endregion

    private void OnDestroy()
    {
        if (_pi == null) return;
        var suck = _pi.actions["Suck"];
        suck.started -= SuccInput;
        suck.canceled -= SuccInput;

        var sab = _pi.actions["BlindEnemies"];
        sab.performed -= SabotageInput;  
    }

    //
    #region INPUT

    public void SuccInput(InputAction.CallbackContext ctx) // action "Suck" (E / bouton South)
    {
        if (!IsAlive) return; //si on est mort, on ne peut pas sucer
        if (ctx.started) { IsSuccing = true; Debug.Log($"[INPUT] P{Index} HOLD START"); } //si on appuie sur le bouton, on commence à sucer

        if (ctx.canceled) 
        {
            IsSuccing = false; //si on lâche le bouton, on arrête de sucer et on passe en mode esquive
            Debug.Log($"[INPUT] P{Index} HOLD END");

            if (autoDodgeOnRelease)
                StartDodgeLeft(); //fonction pour esquiver automatiquement à gauche
        }
    }

    public void SabotageInput(InputAction.CallbackContext ctx) // option (Q / bouton West)
    {
        if (!IsAlive || !ctx.performed) return; //si on est mort, on ne peut pas saboter

        if (!CanSabotage) //si on peut pas saboter, on montre le cooldown dans la console 
        {
            // feedback console
            Debug.Log($"[SABO] P{Index} cooldown {sabotageReadyAt - Time.time:0.00}s");
            return;
        }
        Manager?.OnSabotageAsked(Index); //on demande au manager de saboter un autre joueur
        Debug.Log("Yo");

        sabotageReadyAt = Time.time + sabotageCooldown;   // démarre le CD uniquement si on a vraiment tenté (évite le spam)
    }

    #endregion

    //
    #region LOGIQUE & ZONES

    public void ResetState(int targetScore) //on remet tout à zéro au début d'une nouvelle partie pour chaque joueur
    {
        IsAlive = true; IsSuccing = false; OnGroundSuck = true; InDeathZone = false;
        Points = 0; Attention = 0f; MaxPoints = targetScore; iFrameUntil = 0f;
        sabotageReadyAt = 0f;

        SurvivorRegistry.Register(this); // ADD : je (re)deviens vivant globalement

    }

    public void AddPoints(int delta) //on ajoute des points de sang pour le joueur quand il suce avec une limite max
    {
        Points = Mathf.Clamp(Points + delta, 0, MaxPoints);
    }

    public void TryKillFromAttack() //si on, se fait attaquer, on meurt sauf si on est invincible 
    {
        if (!IsAlive) return;
        bool invincible = Time.time < iFrameUntil;
        if (InDeathZone && IsSuccing && !invincible) Die();
    }

    void Die() //quand le joueur meurt, on change ses états vivants et de succion pour tout stopper 
    {
        if (!IsAlive) return;

        IsAlive = false;
        IsSuccing = false;

        // dire au manager local "je suis mort"
        if (Manager != null)
            Manager.OnPlayerDied(this);

        // pas besoin d'aller plus loin ici, LaneManager.Instance.NotifyDeath() sera appelé depuis le SuccManager
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
            InDeathZone = false; //on sort de la zone mortelle qui a pour tag DeathZone
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
        _dodgeStartPos = transform.position; //on retient la position de départ avant d'esquiver
        Vector3 leftTarget = _dodgeStartPos + Vector3.left * dodgeDistance;

        bool hasRB = _rb != null;
        bool useFixed = hasRB && _rb.bodyType == RigidbodyType2D.Dynamic;

        float endTime = Time.time + dodgeMaxTime;
        while (Time.time < endTime && !IsSuccing)   // si on recommence à sucer, on interrompt l’esquive
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

        // Petite pause avant de revenir
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

        dodgeCo = null; //on arrête l'esquive complètement 
    }

    #endregion
}
