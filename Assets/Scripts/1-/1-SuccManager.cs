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

        // 1ère planification si besoin
        if (_nextAttackAt < 0f)
            ScheduleNextAttack();

        // lance une attaque quand on atteint l’échéance, puis replanifie
        if (Time.time >= _nextAttackAt)
        {
            ResolveAttack();
            ScheduleNextAttack();
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

    float SampleNextAttackDelay(float elapsedSec)
    {
        // intensité monte avec le temps (comme avant)
        float intensity01 = Mathf.Clamp01(elapsedSec / timeToMinAttack);
        // taux (lambda) en 1/sec : interpole entre base et min (mais on reste en taux, donc 1/intervalle)
        float lambda = Mathf.Lerp(1f / baseAttackInterval, 1f / minAttackInterval, intensity01);

        // bruit multiplicatif (jitter) pour casser les patterns
        float jitter = Random.Range(0.7f, 1.3f);
        lambda *= jitter;

        // échantillon expo: délai = -ln(U)/lambda
        float u = Mathf.Clamp01(Random.value);
        float delay = -Mathf.Log(1f - u) / Mathf.Max(0.0001f, lambda);

        // imposer un gap minimal
        return Mathf.Max(minGapAfterAttack, delay);
    }

    public void ScheduleNextAttack()
    {
        _nextAttackAt = Time.time + SampleNextAttackDelay(elapsed);
    }

    #endregion
}
