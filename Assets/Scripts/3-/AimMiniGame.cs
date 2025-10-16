using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class AimMiniGame : MonoBehaviour
{
    [Header("Références")]
    public Transform human;
    public TMP_Text infoText;
    public TMP_Text timerText;
    public TMP_Text scoreText;

    [Header("Paramètres de jeu")]
    public float moveSpeed = 3f;
    public float roundDuration = 5f;
    public int totalRounds = 3;
    public int maxShotsPerRound = 3;

    [Header("Sabotage")]
    public float sabotageDuration = 1f;     // durée du tremblement
    public float sabotageIntensity = 0.5f;  // amplitude du tremblement
    public float sabotageCooldown = 5f;     // délai avant qu'un joueur puisse resaboter

    [Header("Zones de jeu")]
    public Transform zoneTopLeft;
    public Transform zoneBottomRight;

    [System.Serializable]
    public class PlayerData
    {
        public int playerID = 1;
        public Transform crosshair;
        public KeyCode shootKey = KeyCode.E;
        public KeyCode sabotageKey = KeyCode.A;
        public Color color = Color.white;

        [HideInInspector] public int score;
        [HideInInspector] public int shotsUsed;
        [HideInInspector] public bool isSabotaged;
        [HideInInspector] public float sabotageTimer; // cooldown individuel
    }

    [Header("Joueurs")]
    public PlayerData[] players; // 4 joueurs

    private float timer;
    private int currentRound = 1;
    private bool isRunning = false;

    // ----------------------------------------------------------
    void Start()
    {
        ResetAllPlayers();
        StartCoroutine(StartMiniGame());
    }

    private IEnumerator StartMiniGame()
    {
        yield return new WaitForSeconds(0.5f);
        StartRound();
    }

    private void StartRound()
    {
        isRunning = true;
        timer = roundDuration;
        foreach (var p in players)
        {
            p.shotsUsed = 0;
            p.sabotageTimer = 0;
        }
        infoText.text = $"Manche {currentRound}/{totalRounds}";
    }

    private void EndRound()
    {
        isRunning = false;
        currentRound++;

        if (currentRound > totalRounds)
        {
            EndGame();
        }
        else
        {
            StartCoroutine(NextRoundDelay());
        }
    }

    private IEnumerator NextRoundDelay()
    {
        infoText.text = "Préparez-vous pour la prochaine manche...";
        yield return new WaitForSeconds(2f);
        StartRound();
    }

    private void EndGame()
    {
        isRunning = false;
        infoText.text = "Fin du mini-jeu !";

        string results = "";
        foreach (var p in players)
            results += $"J{p.playerID}: {p.score}  ";
        scoreText.text = results;
    }

    // ----------------------------------------------------------
    void Update()
    {
        if (!isRunning) return;

        timer -= Time.deltaTime;
        timerText.text = timer.ToString("F1") + "s";

        if (timer <= 0)
        {
            EndRound();
            return;
        }

        foreach (var p in players)
        {
            // Cooldown du sabotage
            if (p.sabotageTimer > 0)
                p.sabotageTimer -= Time.deltaTime;

            MoveCrosshair(p);

            if (Input.GetKeyDown(p.shootKey) && p.shotsUsed < maxShotsPerRound)
                HandleShot(p);

            if (Input.GetKeyDown(p.sabotageKey))
                HandleSabotage(p);
        }
    }

    // ----------------------------------------------------------
    private void MoveCrosshair(PlayerData p)
    {
        if (p.isSabotaged)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-sabotageIntensity, sabotageIntensity),
                Random.Range(-sabotageIntensity, sabotageIntensity),
                0
            );
            p.crosshair.position += randomOffset * Time.deltaTime * 20f;
            return;
        }

        Vector2 min = zoneTopLeft.position;
        Vector2 max = zoneBottomRight.position;
        Vector3 target = new Vector3(
            Mathf.Lerp(min.x, max.x, Mathf.PerlinNoise(Time.time * 0.5f + p.playerID, 0)),
            Mathf.Lerp(min.y, max.y, Mathf.PerlinNoise(0, Time.time * 0.5f + p.playerID)),
            0
        );

        p.crosshair.position = Vector3.Lerp(p.crosshair.position, target, Time.deltaTime * moveSpeed);
    }

    private void HandleShot(PlayerData p)
    {
        p.shotsUsed++;

        float dist = Vector2.Distance(p.crosshair.position, human.position);
        int points = CalculateScore(dist);
        p.score += points;

        infoText.text = $"J{p.playerID} tire ! +{points}";
        UpdateScoreDisplay();
    }

    private int CalculateScore(float distance)
    {
        if (distance < 0.2f) return 5;
        if (distance < 0.4f) return 4;
        if (distance < 0.6f) return 3;
        if (distance < 0.8f) return 2;
        if (distance < 1.0f) return 1;
        return 0;
    }

    private void UpdateScoreDisplay()
    {
        string result = "";
        foreach (var p in players)
            result += $"J{p.playerID}:{p.score}  ";
        scoreText.text = result;
    }

    private void ResetAllPlayers()
    {
        foreach (var p in players)
        {
            p.score = 0;
            p.shotsUsed = 0;
            p.isSabotaged = false;
            p.sabotageTimer = 0;
        }
        UpdateScoreDisplay();
    }

    // ----------------------------------------------------------
    private void HandleSabotage(PlayerData attacker)
    {
        if (attacker.sabotageTimer > 0)
        {
            infoText.text = $"J{attacker.playerID} doit attendre ({attacker.sabotageTimer:F1}s)";
            return;
        }

        // Liste des cibles possibles (tous sauf soi-même)
        List<PlayerData> potentialTargets = new List<PlayerData>();
        foreach (var p in players)
            if (p != attacker)
                potentialTargets.Add(p);

        if (potentialTargets.Count == 0) return;

        // Choisit un joueur au hasard
        PlayerData target = potentialTargets[Random.Range(0, potentialTargets.Count)];
        StartCoroutine(SabotageRoutine(target));

        infoText.text = $"J{attacker.playerID} perturbe J{target.playerID} !";

        attacker.sabotageTimer = sabotageCooldown; // reset cooldown
    }

    private IEnumerator SabotageRoutine(PlayerData target)
    {
        target.isSabotaged = true;
        yield return new WaitForSeconds(sabotageDuration);
        target.isSabotaged = false;
    }
}
