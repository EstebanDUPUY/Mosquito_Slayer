/*
 * PlayerSucc.cs — HOLD only + DeathZone dodge (propre)
 * +1 toutes les 0.25s si maintien E/A et zone GroundSuck.
 * Attention ↑ pendant succion, ↓ sinon. Sortir de DeathZone = dodge (i-frames).
*/
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSucc : MonoBehaviour
{
    //
    #region VARIABLES


    [SerializeField] private int maxPoints = 20;     // 20 = plein
    [SerializeField] private float tickInterval = 0.25f;  // +1 / 0.25s si maintien
    [SerializeField] private float attentionGainMin = 0.05f;  // gain aléatoire/tick si on suce
    [SerializeField] private float attentionGainMax = 0.12f;
    [SerializeField] private float attentionDecay = 0.15f;  // perte/s si on ne suce pas
    [SerializeField] private float iFrameDuration = 0.25f;  // invulnérable après sortie DeathZone
    [SerializeField] private bool showDodgeFlash = true;   // feedback visuel simple
    [SerializeField] private bool isSuccing = false; // E/A maintenue ?
    [SerializeField] private bool IsAlive = true;
    [SerializeField] private bool onGroundSuck = true; // mets à true si pas de zone
    [SerializeField] private float attention = 0f;    // 0..1
    [SerializeField] private int index = 0;
    [SerializeField] private int currentPoints = 0;
    [SerializeField] private bool inDeathZone = true;  // true si à l'intérieur de la DeathZone
    [SerializeField] private bool invincible = false; // i-frames actives ?
    [SerializeField] private SuccGameManager manager;  // peut rester null si standalone


    // Timers internes
    private float tickTimer = 0f;
    private float lastFrameTime = 0f;
    private float iFrameTimer = 0f;

    #endregion

    //
    #region START, UPDATE, ETC . . .

    private void Awake()
    {
        lastFrameTime = Time.time;
    }

    private void Start()
    {
        NotifyScoreChanged();
        NotifyAttentionChanged();
    }

    private void Update()
    {
        // i-frames countdown
        if (invincible)
        {
            iFrameTimer -= Time.deltaTime;
            if (iFrameTimer <= 0f) invincible = false;
        }

        Succing(); // logique principale
    }

    #endregion

    // 
    #region INPUT

    public void SuccInput(InputAction.CallbackContext ctx)
    {
        if (!IsAlive) return;

        if (ctx.started) isSuccing = true;
        if (ctx.canceled) isSuccing = false;
    }

    public void SabotageInput(InputAction.CallbackContext ctx)
    {
        if (!IsAlive) return;
        if (ctx.started)
        {
            // Sabotage = lâcher + perdre 3 points
            isSuccing = false;
            AddPoints(-3);
        }
    }

    #endregion

    //
    #region OTHER FUNCTIONS

    private void Succing()
    {
        // delta temps pour le decay d'attention
        float dt = Time.time - lastFrameTime;
        lastFrameTime = Time.time;

        // +1 toutes les 0.25s si on maintient ET qu'on est sur la zone de succion
        if (IsAlive && onGroundSuck && isSuccing && currentPoints < maxPoints)
        {
            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                tickTimer -= tickInterval;
                AddPoints(1);

                // attention monte aléatoirement quand on suce
                attention = Mathf.Clamp01(attention + Random.Range(attentionGainMin, attentionGainMax));
                NotifyAttentionChanged();
            }
        }
        else
        {
            // reset chrono (évite demi-ticks)
            tickTimer = 0f;

            // attention décroit quand on ne suce pas
            attention = Mathf.Clamp01(attention - attentionDecay * dt);
            NotifyAttentionChanged();
        }
    }

    private void AddPoints(int amount)
    {
        int prev = currentPoints;
        currentPoints = Mathf.Clamp(currentPoints + amount, 0, maxPoints);

        if (currentPoints != prev)
        {
            NotifyScoreChanged();

            if (currentPoints >= maxPoints)
            {
                // Le manager peut finir la manche si voulu
                manager?.SendMessage("OnPlayerReachedMax", this, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    // Appelé par le manager lorsqu'une claque vise ce joueur.
    public void TryKillFromAttack()
    {
        if (!IsAlive) return;

        // Règle : on ne meurt que si DANS la DeathZone, PAS invincible, et en train de sucer
        if (inDeathZone && !invincible && isSuccing)
        {
            Kill();
        }
        // sinon l'attaque passe (dodge/safe).
    }

    private void Kill()
    {
        if (!IsAlive) return;
        IsAlive = false;
        isSuccing = false;
        manager?.SendMessage("OnPlayerKilled", this, SendMessageOptions.DontRequireReceiver);
    }

    private void EnterIFrames()
    {
        invincible = true;
        iFrameTimer = iFrameDuration;

        if (showDodgeFlash)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.6f);
                Invoke(nameof(_ResetFlash), 0.12f);
            }
        }
    }

    private void _ResetFlash()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
    }

    // ————— Notifications manager/HUD —————

    private void NotifyScoreChanged()
    {
        manager?.SendMessage("OnPlayerScoreChanged", this, SendMessageOptions.DontRequireReceiver);
    }

    private void NotifyAttentionChanged()
    {
        manager?.SendMessage("OnPlayerAttentionChanged", new object[] { index, attention }, SendMessageOptions.DontRequireReceiver);
    }

    // ————— Zones (Tags requis) —————
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("GroundSuck")) onGroundSuck = true;
        if (other.CompareTag("DeathZone")) inDeathZone = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("GroundSuck")) onGroundSuck = false;

        // Sortir de la DeathZone = DODGE garanti + i-frames
        if (other.CompareTag("DeathZone"))
        {
            inDeathZone = false;
            isSuccing = false;  // option : forcer un léger lâcher pour signifier l'esquive
            EnterIFrames();
        }
    }

    #endregion
}
