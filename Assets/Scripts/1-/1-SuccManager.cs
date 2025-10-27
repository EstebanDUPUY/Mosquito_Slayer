using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; // pour geler l'input du joueur

public class SuccManager : MonoBehaviour
{
    #region VARIABLES

    [Header("Règles")]
    [SerializeField] int targetScore = 20;
    [SerializeField] float tickInterval = 0.25f;

    [Header("Blood Decay")]
    [SerializeField] bool enableBloodDecay = true;
    [SerializeField] float bloodDecayPerSecond = 1.0f; // le sang de la jauge diminue si on ne suce pas

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
    public PlayerSucc[] players;   // les joueurs de CETTE lane (chez toi normalement 1 joueur par lane)
    public SuccHUD hud;            // HUD de CETTE lane
    public HumanAttack human;      // humain qui attaque pour CETTE lane
    public LaserAttack laser;      // laser de CETTE lane (si utilisé)

    // timers
    float elapsed = 0f;
    float attackTimer = 0f;
    bool roundOver = false;                       // la manche est terminée pour CETTE lane ?
    public bool RoundOver => roundOver;           // propriété publique

    private Coroutine tickCo;                     // coroutine du "tick"
    float[] _decayAcc;                            // on efface les accumulateurs de descente pour chaque joueur 

    // --- Planning stochastique des attaques ---
    [SerializeField] float attentionThreshold = 0.15f; // en-dessous, aucune attaque planifiée
    [SerializeField] float minGapAfterAttack = 0.6f;   // gap mini entre deux séquences
    float _nextAttackAt = -1f;                         // horodatage du prochain tirage

    bool Owns(PlayerSucc p)
    {
        if (p == null || players == null) return false;
        for (int i = 0; i < players.Length; i++)
            if (players[i] == p) return true;
        return false;
    }

    #endregion


    #region START/UPDATE

    void Start()
    {
 
    }

    void Update()
    {
        if (roundOver) return;

        elapsed += Time.deltaTime;

        float att = MaxAttention(); // retourne l'attention max parmi les joueurs vivants

        // Tant que l'attention max est sous le seuil : pas d'attaque et PAS de planification en attente
        if (att < attentionThreshold)
        {
            _nextAttackAt = -1f; // on désarme : dès que le seuil sera franchi, on replanifiera proprement
            return;
        }

        // si rien de planifié, on planifie une attaque maintenant 
        if (_nextAttackAt < 0f)
        {
            ScheduleNextAttack(); // on prépare la prochaine attaque
        }
        else
        {
            // si l'attention a fortement augmenté, avance la prochaine attaque pour éviter de longs délais (évite les "999s")
            float suggested = SampleNextAttackDelay(elapsed); // délai “moyen” conseillé 
            float remaining = _nextAttackAt - Time.time;      // Temps restant avant l'attaque planifiée

            // On avance l'attaque si le délai suggéré est beaucoup plus court que le temps restant
            if (remaining > suggested * 2.0f)
            {
                _nextAttackAt = Time.time + suggested;
            }
        }

        // Si on a atteint le moment de l'attaque
        if (Time.time >= _nextAttackAt)
        {
            ResolveAttack();       // on fait l'attaque ici
            ScheduleNextAttack();  // replanifie pour la suite (selon attention/temps)
        }
    }

    void OnDisable()
    {
        if (tickCo != null) StopCoroutine(tickCo); // si on désactive l'objet, on arrête la coroutine de tics
    }

    #endregion


    #region ROUND FLOW

    public void StartRound()
    {
        roundOver = false; // la manche commence 

        elapsed = 0f;
        attackTimer = 0f;
        _nextAttackAt = -1f;

        hud?.ResetAll(); // on reset le HUD

        for (int i = 0; i < players.Length; i++) // on initialise chaque joueur
        {
            var p = players[i];
            if (!p) continue;
            p.Index = i;               // Numéro du joueur
            p.Manager = this;          // on assigne le manager au joueur
            p.ResetState(targetScore); // on remet le joueur à zéro au niveau des points et états
            hud?.SetBlood(i, 0f);      // jauge vide
            hud?.SetAttention(i, 0f);  // attention à 0
        }

        // on efface les accumulateurs de descente pour chaque joueur 
        if (_decayAcc == null || _decayAcc.Length != players.Length)
            _decayAcc = new float[players.Length];
        else
            System.Array.Clear(_decayAcc, 0, _decayAcc.Length);

        if (tickCo != null) StopCoroutine(tickCo); // on relance la coroutine de tics proprement 
        tickCo = StartCoroutine(TickLoop());
    }

    IEnumerator TickLoop()
    {
        while (!roundOver) // tant que la manche n'est pas terminée
        {
            yield return new WaitForSeconds(tickInterval); // On attend le tic
            ProcessTick(); // on fait 1 tic ici
        }
    }

    // >>>>> Cette fonction est appelée par GameRoundController quand cette lane DOIT gagner <<<<<
    public void ForceWin()
    {
        if (roundOver) return;
        roundOver = true;

        // 1. Stoppe l’activité de la lane
        if (tickCo != null)
        {
            StopCoroutine(tickCo);
            tickCo = null;
        }

        // 2. On cherche un joueur vivant local pour afficher sa win
        PlayerSucc localWinner = GetFirstAlivePlayer();

        // 3. HUD : si j’ai un gagnant local et un HUD, j’affiche le panel de victoire
        if (localWinner != null && hud != null)
        {
            hud.ShowWinner(localWinner.Index); // on affiche le panel de victoire pour le gagnant
        }

        // 4. Geler les inputs du/des joueur(s)
        FreezeInputs();
    }

    // >>>>> Cette fonction est appelée par GameRoundController
    //       quand une autre lane a gagné et que celle-ci doit juste s'arrêter, pas gagner <<<<<
    public void ForceStopNoWin()
    {
        if (roundOver) return;
        roundOver = true;

        if (tickCo != null)
        {
            StopCoroutine(tickCo);
            tickCo = null;
        }

        // pas de ShowWinner ici (cette lane n'a pas gagné)

        FreezeInputs();
    }

    // utilisé par GameRoundController pour savoir s'il reste au moins un joueur vivant dans CETTE lane
    public bool HasAlivePlayer()
    {
        return GetFirstAlivePlayer() != null;
    }

    // renvoie le premier joueur encore en vie, ou null
    private PlayerSucc GetFirstAlivePlayer()
    {
        if (players == null) return null;
        foreach (var p in players)
        {
            if (p != null && p.IsAlive) return p;
        }
        return null;
    }

    // coupe l'input du/des joueur(s) de cette lane
    void FreezeInputs()
    {
        // on arrête les tics (déjà fait plus haut normalement)
        if (players == null) return;

        // 4. (optionnel) Geler l’input du/des joueurs locaux
        foreach (var p in players)
        {
            if (p == null) continue;
            var pi = p.GetComponent<PlayerInput>();
            if (pi) pi.enabled = false;
        }
    }

    public void OnPlayerDied(PlayerSucc p)
    {
        if (p == null) return;

        // HUD feedback mort local
        if (hud != null)
            hud.SetDead(p.Index);

        // On dit au LaneManager : "ce joueur est mort, check si on a un gagnant global"
        if (LaneManager.Instance != null)
            LaneManager.Instance.NotifyDeath(p);
    }

    public void EndRoundLocal(PlayerSucc winnerFromLaneManager)
    {
        // si cette lane est déjà finie -> rien à faire
        if (roundOver) return;

        // On marque la lane comme terminée
        roundOver = true;

        // On coupe la coroutine de ticks (plus de gain de sang etc.)
        if (tickCo != null)
        {
            StopCoroutine(tickCo);
            tickCo = null;
        }

        // (Optionnel) arrêter l'humain qui attaque dans CETTE lane,
        // si ton HumanAttack a une méthode pour annuler l'attaque en cours.
        // if (human != null) human.Abort(); // seulement si tu l'as codé

        // On choisit qui afficher comme gagnant sur CETTE lane :
        // priorité 1 : le gagnant donné par le LaneManager
        // priorité 2 : sinon, le dernier joueur encore vivant dans cette lane
        PlayerSucc localWinner = null;

        // priorité 1
        if (winnerFromLaneManager != null && Owns(winnerFromLaneManager))
        {
            localWinner = winnerFromLaneManager;
        }
        else
        {
            // priorité 2
            foreach (var p in players)
            {
                if (p != null && p.IsAlive)
                {
                    localWinner = p;
                    break;
                }
            }
        }

        // HUD victoire locale
        if (localWinner != null && hud != null)
        {
            hud.ShowWinner(localWinner.Index);
        }

        // Optionnel : on coupe l'input des joueurs de CETTE lane
        foreach (var p in players)
        {
            if (p == null) continue;
            var pi = p.GetComponent<PlayerInput>();
            if (pi) pi.enabled = false;
        }
    }

    #endregion


    #region GAMEPLAY TICK

    void ProcessTick()
    {
        for (int i = 0; i < players.Length; i++) // pour chaque joueur
        {
            var p = players[i];
            if (p == null || !p.IsAlive) continue;

            // Si le joueur suce au sol et n'a pas encore atteint le score cible,
            // il gagne des points et de l'attention
            if (p.IsSuccing && p.OnGroundSuck && p.Points < targetScore)
            {
                p.AddPoints(+1);
                p.Attention = Mathf.Clamp01(
                    p.Attention + Random.Range(attentionGainMin, attentionGainMax)
                ); // l'attention monte un peu quand on suce de manière aléatoire

                _decayAcc[i] = 0f; // on efface l'accumulateur de descente de la jauge quand on suce
            }
            else
            {
                // L'attention baisse un peu à chaque tick si on ne suce pas
                p.Attention = Mathf.Clamp01(
                    p.Attention - attentionDecay * tickInterval
                );

                // le sang de la jauge diminue si on ne suce pas
                if (enableBloodDecay && p.Points > 0)
                {
                    _decayAcc[i] += bloodDecayPerSecond * tickInterval; // formule pour la perte des points, ex: 0.25 points par tick si 1pt/sec et tickInterval=0.25s
                    int dec = Mathf.FloorToInt(_decayAcc[i]);           // on enlève des points entiers quand on peut 
                    if (dec > 0)
                    {
                        p.AddPoints(-dec); // on soustrait les points
                        _decayAcc[i] -= dec;  // on garde le reste
                    }
                }
            }

            // ---- HUD ----
            float ratio = Mathf.InverseLerp(0, p.MaxPoints, p.Points); // conversion en ratio 0-1 pour la jauge
            hud?.SetBlood(i, ratio);  // la jauge de sang visuellement 
            hud?.SetAttention(i, p.Attention);

            // Victoire si la jauge est pleine 
            if (p.Points >= p.MaxPoints)
            {
                hud?.SetBlood(i, 1f); // on s'assure d'avoir la jauge pleine visuellement 

                // Ici, on arrête juste CETTE lane et on affiche WIN localement.
                // Le GameRoundController, lui, verra qu'il reste un joueur vivant ici
                // et 0 dans les autres lanes -> donc il déclarera cette lane gagnante globale
                ForceWin();
                return;
            }
        }
    }

    #endregion


    #region ATTAQUES / LASER / MORT

    public void ResolveAttack()
    {
        // On choisit la cible qui a la plus grande attention 
        int target = -1;
        float best = -1f;

        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p == null || !p.IsAlive) continue;
            if (p.Attention > best)
            {
                best = p.Attention;
                target = i;
            }
        }

        if (target < 0) return; // si on n'a pas de cible, on fait rien

        // On déclenche le laser seulement si l'humain est prêt 
        if (human != null && !human.IsBusy && !human.Cooldown)
        {
            human.PlayAttackSequence(target); // l'humain fera ses différents états et attaquera la cible choisie
        }
        else if (human == null)
        {
            // fallback ultra simple si pas d'humain visuel
            players[target].TryKillFromAttack(); // tue la cible si elle succ dans la zone mortelle
            // si ça l'a tué, on met à jour le HUD défaite etc.
            if (!players[target].IsAlive)
            {
                hud?.SetDead(target);
                hud?.ShowDefeat(target);
            }
        }
    }

    public void OnLaserHit(PlayerSucc p)
    {
        if (roundOver || p == null) return; // si la partie est finie ou il n'y a pas de joueur, on ne fait rien
        if (!p.IsAlive) return;             // si le joueur est déjà mort, on ne fait rien

        if (p.IsSuccing) // on punit le joueur seulement s'il est en train de sucer
        {
            p.IsAlive = false;  // il meurt
            p.IsSuccing = false; // il arrête de sucer

            hud?.SetDead(p.Index);     // on montre la croix de mort pour ce joueur
            hud?.ShowDefeat(p.Index);  // on montre le panel de défaite pour ce joueur
        }
        // sinon : il a esquivé (pas de sanction)
    }

    #endregion


    #region ATTENTION / TIMING D'ATTAQUE

    float MaxAttention() // retourne l'attention max parmi les joueurs vivants
    {
        float m = 0f;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] && players[i].IsAlive)
                m = Mathf.Max(m, players[i].Attention);
        }
        return m;
    }

    float SampleNextAttackDelay(float elapsedSec)
    {
        // On calcule le taux d'attaque de l'ennemi en fonction du temps écoulé,
        // plus le temps passe, plus l'ennemi attaquera souvent 
        float t01 = Mathf.Clamp01(elapsedSec / timeToMinAttack);
        float lambda = Mathf.Lerp(1f / baseAttackInterval, 1f / minAttackInterval, t01); // 1/sec

        // On multiplie par un facteur lié à l’attention max:
        // plus elle est haute, plus les attaques sont fréquentes.
        float att = MaxAttention();
        float attFactor = Mathf.Lerp(0.25f, 2.0f, att);
        lambda *= attFactor;

        // On met du hasard pour casser les patterns 
        lambda *= Random.Range(0.8f, 1.25f);

        // Si personne n’attire l’attention, retourne un délai très long pour qu'il n'attaque jamais 
        if (att < attentionThreshold) return 999f;

        // On tire un délai
        float u = Mathf.Clamp01(Random.value);
        float delay = -Mathf.Log(1f - u) / Mathf.Max(0.0001f, lambda);
        return Mathf.Max(minGapAfterAttack, delay);
    }

    public void ScheduleNextAttack()
    {
        _nextAttackAt = Time.time + SampleNextAttackDelay(elapsed); // on prépare la prochaine attaque
    }

    #endregion


    #region SABOTAGE

    public void OnSabotageAsked(int attackerIndex)
    {
        // On récupère le joueur attaquant
        var a = SafePlayer(attackerIndex);
        if (a == null)
        {
            Debug.Log("[SABO] attacker null");
            return;
        }

        if (a.Points < sabotageCost) // si pas assez de points, on annule le sabotage
        {
            Debug.Log("[SABO] not enough points");
            return;
        }

        // On cherche aléatoirement un joueur vivant autre que l'attaquant
        int tIdx = -1;
        {
            System.Collections.Generic.List<int> cand = new System.Collections.Generic.List<int>();
            for (int i = 0; i < players.Length; i++)
            {
                if (i == attackerIndex) continue;
                var p = SafePlayer(i);
                if (p != null && p.IsAlive) cand.Add(i);
            }
            if (cand.Count > 0) tIdx = cand[Random.Range(0, cand.Count)];
        }

        // Si on ne trouve personne, on cible n'importe quel joueur en vie
        if (tIdx < 0)
        {
            for (int i = 0; i < players.Length; i++)
            {
                var p = SafePlayer(i);
                if (p != null && p.IsAlive) { tIdx = i; break; }
            }
        }

        // S'il n'y a personne, on se sabote soi-même pour le feedback visuel
        if (tIdx < 0) tIdx = attackerIndex;

        // On retire les points du joueur et on met à jour sa jauge 
        a.AddPoints(-sabotageCost);
        hud?.SetBlood(attackerIndex, Mathf.InverseLerp(0, a.MaxPoints, a.Points));

        // on affiche le splash sur la cible souhaitée
        if (hud != null && hud.splashMask != null &&
            tIdx >= 0 && tIdx < hud.splashMask.Length && hud.splashMask[tIdx] != null)
        {
            Debug.Log($"[SABO] splash -> P{tIdx} ({splashDuration}s)");
            hud.ShowBloodSplash(tIdx, splashDuration);
        }
        else
        {
            Debug.LogWarning("[SABO] splashMask non assigné / index hors limites");
        }
    }

    PlayerSucc SafePlayer(int i)
        => (i >= 0 && i < players.Length) ? players[i] : null; // on récupère le joueur en sécurité si l'index est bon

    #endregion
}
