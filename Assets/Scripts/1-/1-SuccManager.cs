/* 
 * SuccManager.cs (version simple & lisible)
 * Règles de jeu + pilotage HUD. Les joueurs ne font que l'input et les zones.
*/
using UnityEngine;
using System.Collections;

public class SuccManager : MonoBehaviour
{
    //
    #region VARIABLES

    [Header("Règles")]
    [SerializeField] int targetScore = 20;
    [SerializeField] float tickInterval = 0.25f;
    // === Blood decay (points that go down when not sucking) ===
    [Header("Blood Decay")]
    [SerializeField] bool enableBloodDecay = true;
    [SerializeField] float bloodDecayPerSecond = 1.0f; // ex: 1 pt/sec when not sucking

    [Header("Attaques humaines")]
    [SerializeField] float baseAttackInterval = 2.0f;
    [SerializeField] float minAttackInterval = 0.7f;
    [SerializeField] float timeToMinAttack = 30f; // plus le temps passe, plus ça va vite

    [Header("Attention")]
    [SerializeField] float attentionGainMin = 0.05f;
    [SerializeField] float attentionGainMax = 0.12f;
    [SerializeField] float attentionDecay = 0.15f; // par seconde quand on ne suce pas

    [Header("Sabotage")]
    [SerializeField] int sabotageCost = 2;
    [SerializeField] float splashDuration = 1f;

    [Header("Références")]
    public PlayerSucc[] players;
    public SuccHUD hud;
    public HumanAttack human; // assigne l'objet qui a le script HumanAttack

    // timers
    float tickTimer, attackTimer, elapsed;
    bool roundOver;
    private Coroutine tickCo;
    // per-player fractional accumulator for decay
    float[] _decayAcc;
    // --- Planning stochastique des attaques ---
    [SerializeField] float attentionThreshold = 0.15f; // en-dessous, aucune attaque planifiée
    [SerializeField] float minGapAfterAttack = 0.6f; // gap mini entre deux séquences
    float _nextAttackAt = -1f;                       // horodatage du prochain tirage


    #endregion

    //
    #region START/UPDATE

    void Start()
    {
        StartRound();
    }

    void Update()
    {
        if (roundOver) return;

        elapsed += Time.deltaTime;

        float att = MaxAttention();

        // Tant que l'attention max est sous le seuil : pas d'attaque et PAS de planification en attente
        if (att < attentionThreshold)
        {
            _nextAttackAt = -1f; // on désarme : dès que le seuil sera franchi, on replanifiera proprement
            return;
        }

        // si rien de planifié → planifie maintenant en tenant compte de l'attention actuelle
        if (_nextAttackAt < 0f)
        {
            ScheduleNextAttack();
            // Debug.Log($"[SCHED] first plan in {(_nextAttackAt - Time.time):0.00}s (maxAtt={att:0.00})");
        }
        else
        {
            // si l'attention a fortement augmenté, avance la prochaine attaque (évite les "999s")
            float suggested = SampleNextAttackDelay(elapsed);         // délai “moyen” actuel
            float remaining = _nextAttackAt - Time.time;

            // hysteresis : si la date planifiée est beaucoup trop loin par rapport au rythme actuel, on réajuste
            if (remaining > suggested * 2.0f)
            {
                _nextAttackAt = Time.time + suggested;
                // Debug.Log($"[SCHED] tighten to {suggested:0.00}s (was {remaining:0.00}s), maxAtt={att:0.00}");
            }
        }

        // déclenche quand on atteint l'échéance
        if (Time.time >= _nextAttackAt)
        {
            ResolveAttack();
            ScheduleNextAttack(); // replanifie pour la suite (selon attention/temps)
                                  // Debug.Log($"[SCHED] next in {(_nextAttackAt - Time.time):0.00}s (maxAtt={att:0.00})");
        }
    }


    #endregion

    void OnDisable()
    {
        if (tickCo != null) StopCoroutine(tickCo);
    }

    IEnumerator TickLoop()
    {
        while (!roundOver)
        {
            yield return new WaitForSeconds(tickInterval); // stable, pas lié aux FPS
            ProcessTick(); // on fait 1 tick ici
        }
    }

    void ProcessTick()
    {
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i]; if (p == null || !p.IsAlive) continue;

            // ---- GAIN when holding ----
            if (p.IsSuccing && p.OnGroundSuck && p.Points < targetScore)
            {
                p.AddPoints(+1);
                p.Attention = Mathf.Clamp01(p.Attention + Random.Range(attentionGainMin, attentionGainMax));

                // while sucking, don't “carry” decay debt
                if (_decayAcc != null) _decayAcc[i] = 0f;
            }
            else
            {
                // ---- ATTENTION natural decay ----
                p.Attention = Mathf.Clamp01(p.Attention - attentionDecay * tickInterval);

                // ---- BLOOD DECAY (points go down) ----
                if (enableBloodDecay && p.Points > 0)
                {
                    // accumulate fractional decay using tickInterval
                    _decayAcc[i] += bloodDecayPerSecond * tickInterval;     // points to remove (fractional)
                    int dec = Mathf.FloorToInt(_decayAcc[i]);               // whole points to remove now
                    if (dec > 0)
                    {
                        p.AddPoints(-dec);
                        _decayAcc[i] -= dec;                                // keep the fractional remainder
                    }
                }
            }

            // ---- HUD ----
            float ratio = Mathf.InverseLerp(0, p.MaxPoints, p.Points);
            hud?.SetBlood(i, ratio);
            hud?.SetAttention(i, p.Attention);

            // ---- Victory check ----
            if (p.Points >= p.MaxPoints)
            {
                hud?.SetBlood(i, 1f);
                EndRound(p);
                return;
            }
        }
    }


    //
    #region LOGIQUE

    public void StartRound()
    {
        roundOver = false;
        elapsed = attackTimer = 0f;
        hud?.ResetAll();
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i]; if (!p) continue;
            p.Index = i; p.Manager = this; p.ResetState(targetScore);
            hud?.SetBlood(i, 0f); hud?.SetAttention(i, 0f);
        }
        if (tickCo != null) StopCoroutine(tickCo);
        tickCo = StartCoroutine(TickLoop());
        // allocate/reset per-player decay accumulators
        if (_decayAcc == null || _decayAcc.Length != players.Length)
            _decayAcc = new float[players.Length];
        else
            System.Array.Clear(_decayAcc, 0, _decayAcc.Length);
    }

    void EndRound(PlayerSucc winner)
    {
        roundOver = true;
        if (tickCo != null) { StopCoroutine(tickCo); tickCo = null; }
        hud?.ShowWinner(winner.Index);
    }


    public void ResolveAttack()
    {
        // cible = joueur vivant avec la + grande attention
        int target = -1; float best = -1f;
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i]; if (p == null || !p.IsAlive) continue;
            if (p.Attention > best) { best = p.Attention; target = i; }
        }
        if (human != null)
        {
            hud?.ShowAttackWarning(target);  // si tu veux garder le petit warning
            human.PlayAttackSequence(target); // <<< lance la séquence Idle->Alert->Attack
        }
        else
        {
            // fallback : ancienne logique directe
            hud?.ShowAttackWarning(target);
            players[target].TryKillFromAttack();
            CheckLastAlive();
        }
    }

    public void OnSabotageAsked(int attackerIndex)
    {
        var a = SafePlayer(attackerIndex);
        if (a == null || a.Points < sabotageCost) return;

        // choisir cible vivante ≠ attaquant
        int tries = 12, tIdx = -1;
        while (tries-- > 0)
        {
            int r = Random.Range(0, players.Length);
            if (r != attackerIndex && SafePlayer(r)?.IsAlive == true) { tIdx = r; break; }
        }
        if (tIdx < 0) return;

        a.AddPoints(-sabotageCost);
        hud?.ShowBloodSplash(tIdx, splashDuration);
        hud?.SetBlood(attackerIndex, a.Points / (float)targetScore);
    }

    void CheckLastAlive()
    {
        PlayerSucc last = null; int alive = 0;
        foreach (var p in players) if (p && p.IsAlive) { alive++; last = p; }
        if (alive == 1 && last != null) EndRound(last);
        // HUD morts
        for (int i = 0; i < players.Length; i++)
            if (players[i] && !players[i].IsAlive) hud?.SetDead(i);
    }



    PlayerSucc SafePlayer(int i) => (i >= 0 && i < players.Length) ? players[i] : null;

    float MaxAttention()
    {
        float m = 0f;
        for (int i = 0; i < players.Length; i++)
            if (players[i] && players[i].IsAlive) m = Mathf.Max(m, players[i].Attention);
        return m;
    }


    float SampleNextAttackDelay(float elapsedSec)
    {
        // Intensité de base (monte avec le temps, comme avant)
        float t01 = Mathf.Clamp01(elapsedSec / timeToMinAttack);
        float lambda = Mathf.Lerp(1f / baseAttackInterval, 1f / minAttackInterval, t01); // 1/sec

        // Modulation par l’attention max (si faible → beaucoup moins d’attaques)
        float att = MaxAttention(); // 0..1
                                    // facteur 0.25x → 2.0x (à régler)
        float attFactor = Mathf.Lerp(0.25f, 2.0f, att);
        lambda *= attFactor;

        // Jitter pour casser les patterns
        lambda *= Random.Range(0.8f, 1.25f);

        // Si personne n’attire l’attention, retourne un délai très long
        if (att < attentionThreshold) return 999f;

        // Délai ~ expo
        float u = Mathf.Clamp01(Random.value);
        float delay = -Mathf.Log(1f - u) / Mathf.Max(0.0001f, lambda);
        return Mathf.Max(minGapAfterAttack, delay);
    }

    public void ScheduleNextAttack()
    {
        _nextAttackAt = Time.time + SampleNextAttackDelay(elapsed);
    }

    #endregion
}
