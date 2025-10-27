using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.SceneManagement;

public class AimMiniGameManager : MonoBehaviour
{
    [Header("Références de zone")]
    public Transform zoneTopLeft;
    public Transform zoneBottomRight;

    [Header("Cible / Zones scorables")]
    public HumanTarget human;
    public List<TargetZone> targetZones = new List<TargetZone>();
    [Range(0f, 1f)] public float chanceToGoNearTarget = 0.75f;
    public float offsetAroundTarget = 0.5f;

    [Header("Viseur / Prefabs")]
    public Transform crosshairPrefab;
    public Color[] playerColors =
    {
        Color.black, Color.white, Color.yellow, Color.green
    };

    [Header("Paramètres de manche")]
    public float moveSpeed = 3f;
    public float moveInterval = 1.1f;
    public float roundDuration = 5f;
    public int totalRounds = 3;
    public int maxShotsPerRound = 3;
    public float shootCooldown = 0.5f;
    public float sabotageDuration = 1.2f;
    public float sabotageCooldown = 4f;

    [Header("UI Générale")]
    public TMP_Text infoText;
    public TMP_Text timerText;

    [Header("UI par joueur (index du PlayerInput)")]
    public TMP_Text[] scoreTexts = new TMP_Text[4];
    public TMP_Text[] shotsTexts = new TMP_Text[4];
    public TMP_Text[] nameTexts = new TMP_Text[4];

    [Header("UI Résultat")]
    public TMP_Text winnerText;

    private class AimPlayerRuntime
    {
        public PlayerData data;
        public CrosshairController crosshair;
        public int score;
        public int shotsRemaining;
        public bool canShoot;
        public float lastShotTime;
        public float lastSabotageTime;
        public int index;
    }

    private readonly List<AimPlayerRuntime> players = new List<AimPlayerRuntime>();
    private bool roundRunning;

    private void Start()
    {
        KeepOnlyMainCamera();

        var found = FindObjectsOfType<PlayerData>(true);
        if (found.Length < 2)
        {
            if (infoText) infoText.text = "Besoin d'au moins 2 joueurs.";
            return;
        }

        foreach (var pd in found)
        {
            var pi = pd.GetComponent<PlayerInput>();
            if (pi == null) continue;

            var p = new AimPlayerRuntime
            {
                data = pd,
                score = 0,
                shotsRemaining = 0,
                canShoot = false,
                lastShotTime = -999f,
                lastSabotageTime = -999f,
                index = pi.playerIndex
            };

            Transform c = Instantiate(crosshairPrefab, GetRandomPointInZone(), Quaternion.identity);
            var rend = c.GetComponent<SpriteRenderer>();
            if (rend)
            {
                var col = playerColors[Mathf.Clamp(p.index, 0, playerColors.Length - 1)];
                rend.color = col;
            }

            var crosshairCtrl = pd.gameObject.GetComponent<CrosshairController>();
            if (crosshairCtrl == null) crosshairCtrl = pd.gameObject.AddComponent<CrosshairController>();

            crosshairCtrl.Init(
                pIndex: p.index,
                crosshairTransform: c,
                zoneTL: zoneTopLeft,
                zoneBR: zoneBottomRight,
                targetZonesRef: targetZones,
                chanceNearTarget: chanceToGoNearTarget,
                targetOffset: offsetAroundTarget,
                moveSpd: moveSpeed,
                moveInt: moveInterval
            );

            crosshairCtrl.OnShoot += () => HandleShoot(p);
            crosshairCtrl.OnSabotage += () => HandleSabotage(p);

            p.crosshair = crosshairCtrl;
            players.Add(p);

            if (p.index >= 0 && p.index < nameTexts.Length && nameTexts[p.index] != null)
            {
                nameTexts[p.index].text = $"J{p.index + 1}";
            }
        }

        StartCoroutine(GameLoop());
    }

    private void KeepOnlyMainCamera()
    {
        Camera mainCam = Camera.main;
        Camera[] allCams = FindObjectsOfType<Camera>(true);

        foreach (var cam in allCams)
        {
            if (cam != mainCam)
            {
                Destroy(cam.gameObject);
            }
        }

        if (mainCam != null && !mainCam.gameObject.activeInHierarchy)
            mainCam.gameObject.SetActive(true);
    }

    private IEnumerator GameLoop()
    {
        if (infoText) infoText.text = "Préparez-vous...";
        yield return new WaitForSeconds(1.0f);

        for (int r = 1; r <= totalRounds; r++)
        {
            yield return StartCoroutine(PlayRound(r));
            yield return new WaitForSeconds(0.8f);
        }

        roundRunning = false;
        if (infoText) infoText.text = "Fin du mini-jeu !";
        if (timerText) timerText.text = "";

        UpdateAllScoreUI();
        AnnounceWinner();
        HideShotsTexts();
    }

    private IEnumerator PlayRound(int roundNumber)
    {
        foreach (var p in players)
        {
            p.shotsRemaining = maxShotsPerRound;
            p.canShoot = true;
            p.lastShotTime = -999f;
            p.crosshair.BeginRound();
            UpdateShotsUI(p);
        }

        if (human != null) human.StartJumpsForRound(roundDuration);

        roundRunning = true;

        if (infoText) infoText.text = $"Manche {roundNumber}";
        float t = roundDuration;

        while (t > 0f)
        {
            t -= Time.deltaTime;
            if (timerText) timerText.text = Mathf.Ceil(t).ToString();
            yield return null;
        }

        roundRunning = false;

        foreach (var p in players)
        {
            p.canShoot = false;
            p.crosshair.EndRound();
        }

        if (infoText) infoText.text = $"Manche {roundNumber} terminée";
        if (timerText) timerText.text = "";
    }

    private void HandleShoot(AimPlayerRuntime p)
    {
        if (!roundRunning) return;
        if (!p.canShoot) return;
        if (p.shotsRemaining <= 0) return;
        if (Time.time - p.lastShotTime < shootCooldown) return;
        p.lastShotTime = Time.time;

        p.shotsRemaining--;
        UpdateShotsUI(p);

        Vector2 origin = p.crosshair.CurrentPosition;
        Collider2D hit = Physics2D.OverlapPoint(origin);

        int pts = 0;
        if (hit != null)
        {
            var zone = hit.GetComponent<TargetZone>();
            if (zone != null)
            {
                pts = zone.GetScore(origin);
                if (human != null) human.OnBitten();
                StartCoroutine(p.crosshair.Flash(Color.red, 0.15f));
                if (infoText) infoText.text = $"J{p.index + 1} touché +{pts}";
            }
            else StartCoroutine(p.crosshair.Flash(Color.gray, 0.12f));
        }
        else StartCoroutine(p.crosshair.Flash(Color.gray, 0.12f));

        p.score += pts;
        UpdateScoreUI(p);
        if (p.shotsRemaining <= 0) p.canShoot = false;
    }

    private void HandleSabotage(AimPlayerRuntime source)
    {
        if (!roundRunning) return;
        if (Time.time - source.lastSabotageTime < sabotageCooldown) return;
        source.lastSabotageTime = Time.time;

        var candidates = new List<AimPlayerRuntime>();
        foreach (var p in players) if (p != source) candidates.Add(p);
        if (candidates.Count == 0) return;

        var target = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        target.crosshair.ApplyPerturbation(sabotageDuration);
        if (infoText) infoText.text = $"J{source.index + 1} sabote J{target.index + 1}";
    }

    private void UpdateScoreUI(AimPlayerRuntime p)
    {
        if (p.index >= 0 && p.index < scoreTexts.Length && scoreTexts[p.index] != null)
            scoreTexts[p.index].text = $"Score: {p.score}";
    }

    private void UpdateShotsUI(AimPlayerRuntime p)
    {
        if (p.index >= 0 && p.index < shotsTexts.Length && shotsTexts[p.index] != null)
            shotsTexts[p.index].text = $"J{p.index + 1}: {p.shotsRemaining} piqûres";
    }

    private void UpdateAllScoreUI()
    {
        foreach (var p in players) UpdateScoreUI(p);
    }

    private void AnnounceWinner()
    {
        if (players.Count == 0)
        {
            if (winnerText) winnerText.text = "Aucun joueur détecté.";
            return;
        }

        int bestScore = int.MinValue;
        List<AimPlayerRuntime> winners = new List<AimPlayerRuntime>();

        foreach (var p in players)
        {
            if (p.score > bestScore)
            {
                bestScore = p.score;
                winners.Clear();
                winners.Add(p);
            }
            else if (p.score == bestScore)
            {
                winners.Add(p);
            }
        }

        if (winners.Count == 1)
        {
            int id = winners[0].index + 1;
            if (winnerText) winnerText.text = $"Joueur {id} remporte la manche avec {bestScore} points !";
            if (infoText) infoText.text = $"Victoire du Joueur {id} !";

            // Appel automatique de la fin de partie
            StartCoroutine(DelayedEnd(winners[0]));
        }
        else
        {
            string msg = "Égalité entre ";
            for (int i = 0; i < winners.Count; i++)
            {
                msg += $"J{winners[i].index + 1}";
                if (i < winners.Count - 1) msg += ", ";
            }
            msg += $" avec {bestScore} points !";
            if (winnerText) winnerText.text = msg;
            if (infoText) infoText.text = "Égalité !";

            // On ne sauvegarde rien en cas d'égalité (à toi de décider si tu veux changer ça)
            StartCoroutine(DelayedEnd(null));
        }
    }

    private IEnumerator DelayedEnd(AimPlayerRuntime winner)
    {
        yield return new WaitForSeconds(2.5f);
        EndMiniGame(winner);
    }

    private void EndMiniGame(AimPlayerRuntime winner)
    {
        if (GameManager.instance != null && winner != null)
        {
            GameManager.instance.AddScoreToPlayer(winner.data);
            Debug.Log($"Victoire enregistrée pour {winner.data.name}");
        }

        // Pour test : on charge directement la scène Score
        SceneManager.LoadScene("Score");
        // Et pour la version finale du Party Game :
        // GameManager.instance.NextMiniGame();
    }

    private void HideShotsTexts()
    {
        foreach (var txt in shotsTexts)
        {
            if (txt != null)
                txt.gameObject.SetActive(false);
        }
    }

    private Vector3 GetRandomPointInZone()
    {
        float minX = zoneTopLeft.position.x;
        float maxX = zoneBottomRight.position.x;
        float maxY = zoneTopLeft.position.y;
        float minY = zoneBottomRight.position.y;

        if (targetZones.Count > 0 && UnityEngine.Random.value < chanceToGoNearTarget)
        {
            TargetZone chosen = targetZones[UnityEngine.Random.Range(0, targetZones.Count)];
            var around = chosen.transform.position;

            float offsetX = UnityEngine.Random.Range(-offsetAroundTarget, offsetAroundTarget);
            float offsetY = UnityEngine.Random.Range(-offsetAroundTarget, offsetAroundTarget);

            var nearTarget = around + new Vector3(offsetX, offsetY, 0);
            nearTarget.x = Mathf.Clamp(nearTarget.x, minX, maxX);
            nearTarget.y = Mathf.Clamp(nearTarget.y, minY, maxY);
            return nearTarget;
        }

        float x = UnityEngine.Random.Range(minX, maxX);
        float y = UnityEngine.Random.Range(minY, maxY);
        return new Vector3(x, y, 0);
    }
}
