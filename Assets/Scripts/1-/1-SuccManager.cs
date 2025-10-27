using UnityEngine;
using System.Collections;

public class SuccManager : MonoBehaviour
{
    
    #region VARIABLES

    [Header("Règles")]
    [SerializeField] int targetScore = 20;
    [SerializeField] float tickInterval = 0.25f;

    [Header("Blood Decay")]
    [SerializeField] bool enableBloodDecay = true;
    [SerializeField] float bloodDecayPerSecond = 1.0f;

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
        StartRound(); //on démarre une nouvelle manche au lancement
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

        // si rien de planifié, on planifie une attaque maintenant 
        if (_nextAttackAt < 0f)
        {
            ScheduleNextAttack();
        }
        else
        {
            // si l'attention a fortement augmenté, avance la prochaine attaque pour éviter de longs délais  (évite les "999s")
            float suggested = SampleNextAttackDelay(elapsed);         // délai “moyen” conseillé 
            float remaining = _nextAttackAt - Time.time; //Temps restant avant l'attaque planifiée

            // On avance l'attaque si le délai suggéré est beaucoup plus court que le temps restant
            if (remaining > suggested * 2.0f)
            {
                _nextAttackAt = Time.time + suggested;
            }
        }

        // Si on a atteint le moment de l'attaque
        if (Time.time >= _nextAttackAt)
        {
            ResolveAttack(); // on fait l'attaque ici
            ScheduleNextAttack(); // replanifie pour la suite (selon attention/temps)
        }
    }


    #endregion

    void OnDisable()
    {
        if (tickCo != null) StopCoroutine(tickCo); //si on désactive l'objet, on arrête la coroutine de tics
    }

    IEnumerator TickLoop()
    {
        while (!roundOver) //tant que la manche n'est pas terminée
        {
            yield return new WaitForSeconds(tickInterval); // On attend le tic
            ProcessTick(); // on fait 1 tic ici
        }
    }

    void ProcessTick()
    {
        for (int i = 0; i < players.Length; i++) //pour chaque joueur
        {
            var p = players[i]; 
            if (p == null || !p.IsAlive) continue;

            // Si le joueur suce au sol et n'a pas encore atteint le score cible, il gagne des points et de l'attention
            if (p.IsSuccing && p.OnGroundSuck && p.Points < targetScore)
            {
                p.AddPoints(+1);
                p.Attention = Mathf.Clamp01(p.Attention + Random.Range(attentionGainMin, attentionGainMax)); //l'attention monte un peu quand on suce de manière aléatoire

                if (_decayAcc != null) _decayAcc[i] = 0f; //on efface l'accumulateur de descente de la jauge quand on suce
            }
            else
            {
                // L'attention baisse un peu à chaque tick si on ne suce pas
                p.Attention = Mathf.Clamp01(p.Attention - attentionDecay * tickInterval);

                // Le sang de la jauge diminue si on ne suce pas
                if (enableBloodDecay && p.Points > 0)
                {
                    _decayAcc[i] += bloodDecayPerSecond * tickInterval;     // formule pour la perte des points, ici c'est 0.25 points par tick si c'est 1pt/sec et tickInterval=0.25s
                    int dec = Mathf.FloorToInt(_decayAcc[i]); //on enlève des points entiers quand on peut 
                    if (dec > 0)
                    {
                        p.AddPoints(-dec); // on soustrait les points
                        _decayAcc[i] -= dec;  // on garde le reste
                    }
                }
            }

            // ---- HUD ----
            float ratio = Mathf.InverseLerp(0, p.MaxPoints, p.Points); //conversion en ratio 0-1 pour la jauge
            hud?.SetBlood(i, ratio); //la jauge de sang visuellement 
            hud?.SetAttention(i, p.Attention);

            // Victoire si la jauge est pleine 
            if (p.Points >= p.MaxPoints)
            {
                hud?.SetBlood(i, 1f); //on s'assure d'avoir la jauge pleine visuellement 
                EndRound(p); //fin de manche avec ce gagnant 
                return;
            }
        }
    }

    #region LOGIQUE

    public void StartRound()
    {
        roundOver = false; //la manche commence 
        elapsed = attackTimer = 0f; //on remet les timers à zéro
        hud?.ResetAll(); //on reset le HUD

        for (int i = 0; i < players.Length; i++) //on initialise chaque joueur
        {
            var p = players[i]; if (!p) continue;
            p.Index = i; //Numéro du joueur
            p.Manager = this; //on assigne le manager au joueur
            p.ResetState(targetScore); //on remet le joueur à zéro au niveau des points et états
            hud?.SetBlood(i, 0f); //jauge vide
            hud?.SetAttention(i, 0f); //attention à 0
        }
        if (tickCo != null) StopCoroutine(tickCo); //on relance la coroutine de tics proprement 
        tickCo = StartCoroutine(TickLoop());
        // on efface les accumulateurs de descente pour chaque joueur 
        if (_decayAcc == null || _decayAcc.Length != players.Length)
            _decayAcc = new float[players.Length];
        else
            System.Array.Clear(_decayAcc, 0, _decayAcc.Length);
    }

    void EndRound(PlayerSucc winner)
    {
        roundOver = true; //la manche est terminée
        if (tickCo != null) { StopCoroutine(tickCo); tickCo = null; } //on arrête les tics
        hud?.ShowWinner(winner.Index); //on affiche le panel de victoire pour le gagnant
    }


    public void ResolveAttack()
    {
        // On choisit la cible qui a la plus grande attention 
        int target = -1; float best = -1f;
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i]; if (p == null || !p.IsAlive) continue;
            if (p.Attention > best) { best = p.Attention; target = i; }
        }
        if (target < 0) return; //si on a pas de cible, on fait rien

        // On déclenche le laser seulement si l'humain est prêt 
        if (human != null && !human.IsBusy && !human.Cooldown)
            human.PlayAttackSequence(target); //l'humain fera ses différents états et attaquera la cible choisie
        else if (human == null)
        {
            players[target].TryKillFromAttack(); // tue la cible si elle succ en deathzone 
            CheckLastAlive(); //on vérifie s'il reste des joueurs en vie
        }
    }


    public void OnSabotageAsked(int attackerIndex)
    {
        var a = SafePlayer(attackerIndex); //on récupère le joueur attaquant
        if (a == null) { Debug.Log("[SABO] attacker null"); return; }

        if (a.Points < sabotageCost) //si pas assez de points, on annule le sabotage
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

        // Si on ne trouve personne, on cible n'importe quel joueur en vie autre que l'attaquant
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

        if (hud != null && hud.splashMask != null && //on affiche le splash sur la cible souhaitée
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

    void CheckLastAlive()
    {
        PlayerSucc last = null; int alive = 0; //Compte les vivants et garde le dernier trouvé
        foreach (var p in players) if (p && p.IsAlive) { alive++; last = p; }
        if (alive == 1 && last != null) EndRound(last); //s'il n'en reste qu'un, il gagne

        for (int i = 0; i < players.Length; i++)
            if (players[i] && !players[i].IsAlive) hud?.SetDead(i); //on affiche une croix sur les morts
    }

    PlayerSucc SafePlayer(int i) => (i >= 0 && i < players.Length) ? players[i] : null; //on récupère le joueur en sécurité si l'index est bon

    float MaxAttention() // retourne l'attention max parmi les joueurs vivants
    {
        float m = 0f;
        for (int i = 0; i < players.Length; i++)
            if (players[i] && players[i].IsAlive) m = Mathf.Max(m, players[i].Attention);
        return m;
    }


    float SampleNextAttackDelay(float elapsedSec)
    {
        // On calcule le taux d'attaque de l'ennemi en fonction du temps écoulé, plus le temps passe, plus l'ennemi attaquera souvent 
        float t01 = Mathf.Clamp01(elapsedSec / timeToMinAttack);
        float lambda = Mathf.Lerp(1f / baseAttackInterval, 1f / minAttackInterval, t01); // 1/sec

        // On multiplie par un facteur lié à l’attention max: plus elle est haute, plus les attaques sont fréquentes.
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
        _nextAttackAt = Time.time + SampleNextAttackDelay(elapsed); //on prépare la prochaine attaque
    }

    public void OnLaserHit(PlayerSucc p)
    {
        if (roundOver || p == null) return; //si la partie est finie ou il n'y a pas de joueur, on ne fait rien

        if (!p.IsAlive) return; //si le joueur est déjà mort, on ne fait rien

        if (p.IsSuccing) //on punit le joueur seulement s'il est en train de sucer
        {
            p.IsAlive = false; //il meurt
            p.IsSuccing = false; //il arrête de succ

            hud?.SetDead(p.Index); //on montre la croix de mort pour ce joueur
            hud?.ShowDefeat(p.Index); //on montre le panel de défaite pour ce joueur

            CheckLastAlive(); //on vérifie s'il reste des joueurs en vie
        }
        // sinon : il a esquivé (pas de sanction)
    }


    #endregion
}
