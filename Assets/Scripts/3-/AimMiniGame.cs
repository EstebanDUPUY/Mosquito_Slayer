using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class AimMiniGame : MonoBehaviour
{
    [Header("Références")]
    public Transform human;
    public Transform crosshair;
    public TMP_Text infoText;
    public TMP_Text timerText;
    public TMP_Text scoreText;

    [Header("Zones piquables")]
    public List<Transform> targetZones = new List<Transform>();

    [Header("Zones limites (fallback)")]
    public Transform zoneTopLeft;
    public Transform zoneBottomRight;

    [Header("Paramètres de jeu")]
    public float moveSpeed = 3f;
    public float roundDuration = 5f;
    public int totalRounds = 3;
    public int maxShotsPerRound = 3;

    [Header("Sabotage")]
    public float sabotageDuration = 1f;
    public float sabotageIntensity = 0.5f;
    public float sabotageCooldown = 5f;

    private float timer;
    private int currentRound = 1;
    private bool isRunning = false;
    private bool isSabotaged = false;
    private bool canSabotage = true;
    private int shotsUsed = 0;
    private int score = 0;

    private int currentTargetIndex = 0;

    // ----------------------------------------------------------
    void OnEnable()
    {
        // Démarre automatiquement dès activation du GameObject
        ResetGame();
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
        shotsUsed = 0;
        infoText.text = $"Manche {currentRound}/{totalRounds}";
    }

    private void EndRound()
    {
        isRunning = false;
        currentRound++;

        if (currentRound > totalRounds)
            EndGame();
        else
            StartCoroutine(NextRoundDelay());
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
        scoreText.text = $"Score total : {score}";
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

        MoveCrosshair();
    }

    // ----------------------------------------------------------
    private void MoveCrosshair()
    {
        if (isSabotaged)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-sabotageIntensity, sabotageIntensity),
                Random.Range(-sabotageIntensity, sabotageIntensity),
                0
            );
            crosshair.position += randomOffset * Time.deltaTime * 20f;
            return;
        }

        if (targetZones != null && targetZones.Count > 0)
        {
            Transform targetZone = targetZones[currentTargetIndex];
            Vector3 wander = targetZone.position + (Vector3)(Random.insideUnitCircle * 0.2f);
            crosshair.position = Vector3.MoveTowards(crosshair.position, wander, moveSpeed * Time.deltaTime);

            if (Vector2.Distance(crosshair.position, targetZone.position) < 0.1f)
            {
                currentTargetIndex = Random.Range(0, targetZones.Count);
            }
        }
        else
        {
            Vector2 min = zoneTopLeft.position;
            Vector2 max = zoneBottomRight.position;
            Vector3 fallbackTarget = new Vector3(
                Mathf.Lerp(min.x, max.x, Mathf.PerlinNoise(Time.time * 0.5f, 0)),
                Mathf.Lerp(min.y, max.y, Mathf.PerlinNoise(0, Time.time * 0.5f)),
                0
            );
            crosshair.position = Vector3.Lerp(crosshair.position, fallbackTarget, Time.deltaTime * moveSpeed);
        }
    }

    // ----------------------------------------------------------
    public void OnShoot(InputAction.CallbackContext context)
    {
        Debug.Log("OnShoot déclenché !");
        if (!context.performed || !isRunning) return;
        if (shotsUsed >= maxShotsPerRound) return;

        shotsUsed++;

        float dist = Vector2.Distance(crosshair.position, human.position);
        int points = CalculateScore(dist);
        score += points;

        infoText.text = $"Touché ! +{points}";
        scoreText.text = $"Score : {score}";
    }

    public void OnSabotage(InputAction.CallbackContext context)
    {
        if (!context.performed || !canSabotage) return;
        StartCoroutine(SabotageRoutine());
    }

    private IEnumerator SabotageRoutine()
    {
        canSabotage = false;
        isSabotaged = true;
        infoText.text = "Sabotage !";

        yield return new WaitForSeconds(sabotageDuration);

        isSabotaged = false;
        infoText.text = "";

        yield return new WaitForSeconds(sabotageCooldown);
        canSabotage = true;
    }

    // ----------------------------------------------------------
    private int CalculateScore(float distance)
    {
        if (distance < 0.2f) return 5;
        if (distance < 0.4f) return 4;
        if (distance < 0.6f) return 3;
        if (distance < 0.8f) return 2;
        if (distance < 1.0f) return 1;
        return 0;
    }

    private void ResetGame()
    {
        currentRound = 1;
        score = 0;
        shotsUsed = 0;
        isSabotaged = false;
        canSabotage = true;
        infoText.text = "";
        scoreText.text = "Score : 0";
    }
}
