/* 
 * SuccManager.cs (version simple & lisible)
 * Règles de jeu + pilotage HUD. Les joueurs ne font que l'input et les zones.
*/
using UnityEngine;

public class SuccManager : MonoBehaviour
{
    //
    #region VARIABLES

    [Header("Règles")]
    [SerializeField] int targetScore = 20;
    [SerializeField] float tickInterval = 0.25f;

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

    // timers
    float tickTimer, attackTimer, elapsed;
    bool roundOver;

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

        float dt = Time.deltaTime;
        elapsed += dt;
        tickTimer += dt;
        attackTimer += dt;

        // ---- SCORE + ATTENTION (toutes les 0.25 s) ----
        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;

            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (p == null || !p.IsAlive) continue;

                // +1 si on tient et qu'on est sur la zone
                if (p.IsSuccing && p.OnGroundSuck && p.Points < targetScore)
                {
                    p.AddPoints(+1);
                    float ratio = p.Points / (float)targetScore;
                    hud?.SetBlood(i, ratio);
                    if (p.Points == targetScore) Debug.Log($"FULL {i} -> {ratio}");
                    Debug.Log($"[HUD] SetBlood P{i} -> {ratio:0.00}");

                    p.Attention = Mathf.Clamp01(p.Attention + Random.Range(attentionGainMin, attentionGainMax));
                }
                else
                {
                    // attention décroit
                    p.Attention = Mathf.Clamp01(p.Attention - attentionDecay * tickInterval);
                }

                // HUD
                hud?.SetBlood(i, p.Points / (float)targetScore);
                hud?.SetAttention(i, p.Attention);

                // Victoire par score
                if (p.Points >= targetScore) { EndRound(p); return; }
            }
        }

        // ---- ATTAQUES (cadence qui accélère) ----
        float curInterval = Mathf.Lerp(baseAttackInterval, minAttackInterval, Mathf.Clamp01(elapsed / timeToMinAttack));
        if (attackTimer >= curInterval)
        {
            attackTimer = 0f;
            ResolveAttack();
        }
    }

    #endregion

    //
    #region LOGIQUE

    public void StartRound()
    {
        roundOver = false;
        elapsed = tickTimer = attackTimer = 0f;
        hud?.ResetAll();

        // init joueurs
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (!p) continue;
            p.Index = i;
            p.Manager = this;
            p.ResetState(targetScore);
            hud?.SetBlood(i, 0f);
            hud?.SetAttention(i, 0f);
        }
    }

    void ResolveAttack()
    {
        // cible = joueur vivant avec la + grande attention
        int target = -1; float best = -1f;
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i]; if (p == null || !p.IsAlive) continue;
            if (p.Attention > best) { best = p.Attention; target = i; }
        }
        if (target < 0) return;

        hud?.ShowAttackWarning(target);
        players[target].TryKillFromAttack(); // meurt seulement si dans DeathZone ET en train de sucer

        // Mort → check dernier survivant
        CheckLastAlive();
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

    void EndRound(PlayerSucc winner)
    {
        roundOver = true;
        hud?.ShowWinner(winner.Index);
        // on pourrait locker ici les inputs si besoin
    }

    PlayerSucc SafePlayer(int i) => (i >= 0 && i < players.Length) ? players[i] : null;

    #endregion
}
