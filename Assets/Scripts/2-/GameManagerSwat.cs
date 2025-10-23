using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GameManagerSwat : MonoBehaviour
{
    public static GameManagerSwat Instance;

    [Header("Références")]
    public CutSceneManagerSwat cutSceneManagerSwat;
    public TextMeshProUGUI countdownText;
    public SwatController swatControllerer;
    public SoundManagerSwat soundManagerSwat;

    [Header("Joueurs")]
    public List<PlayerControllerSwat> allPlayers;

    [Header("Décompte")]
    public float countdownTime = 3f;

    private bool gameStarted = false;
    private List<PlayerControllerSwat> alivePlayers = new();

    private Dictionary<PlayerControllerSwat, int> playerPoints = new();

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("Il y a plus d'un GameManagerSwat ! Destruction du nouveau.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Liste des joueurs en vie
        alivePlayers.AddRange(allPlayers);
        
        foreach (var p in allPlayers)
        {
            // On dit à notre manager : "Quand ce joueur crie OnPlayerDied, exécute ma méthode HandlePlayerDeath"
            p.OnPlayerDied += HandlePlayerDeath;

            // On initialise les points de tout le monde à 0
            playerPoints.Add(p, 0);
        }

        cutSceneManagerSwat.OnCutsceneFinished += StartCountdown;
    }

    void StartCountdown()
    {
        StartCoroutine(CountdownRoutine());
    }
   
    IEnumerator CountdownRoutine()
    {
        countdownText.gameObject.SetActive(true);

        int countdown = Mathf.CeilToInt(countdownTime);

        for (int i = (int)countdownTime; i > 0; i--)
        {
            countdownText.text = i.ToString();
            soundManagerSwat.PlayCountdown();
            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);
        countdownText.gameObject.SetActive(false);

        BeginGameplay();
    }

    void BeginGameplay()
    {
        gameStarted = true;
        foreach (var p in allPlayers)
        {
            p.OnEnableMovement(true);
        }

        swatControllerer.StartAttacking();
    }

    public void OnPlayerDied(PlayerControllerSwat player)
    {
        alivePlayers.Remove(player);

        if (alivePlayers.Count <= 1)
        {
            EndMiniGame(alivePlayers.Count == 1 ? alivePlayers[0] : null);
        }
    }

    public void AwardPushKill(PlayerControllerSwat killer)
    {
        if (killer == null || !playerPoints.ContainsKey(killer)) return;

        playerPoints[killer]++;
        Debug.Log($"{killer.name} a marqué un point ! Total : {playerPoints[killer]}");
    }

    private void HandlePlayerDeath(PlayerControllerSwat playerWhoDied)
    {
        // Si la partie est déjà finie (ex: 2 morts en même temps), on ne fait rien
        if (!gameStarted) return;

        if (alivePlayers.Contains(playerWhoDied))
        {
            alivePlayers.Remove(playerWhoDied);
        }

        // On ne vérifie PAS la victoire tout de suite. On lance une coroutine qui attend 1 frame.
        if (alivePlayers.Count <= 1)
        {
            StartCoroutine(CheckEndGameRoutine());
        }
    }
    IEnumerator CheckEndGameRoutine()
    {
        // On attend la prochaine frame, pour laisser le temps à tous les autres joueurs de mourir "en même temps".     
        yield return null;

        // Si la partie a déjà été arrêtée par une autre mort, on ne fait rien
        if (!gameStarted) yield break;

        if (alivePlayers.Count == 1)
        {
            // Victoire claire
            EndMiniGame(alivePlayers[0]);
        }
        else if (alivePlayers.Count == 0)
        {
            // Égalité (aucun survivant)
            EndMiniGame(null);
        }
    }
    void EndMiniGame(PlayerControllerSwat winner)
    {
        gameStarted = false;
        swatControllerer.StopAttacking();

        // On désactive les mouvements des joueurs qui restent
        foreach (var p in allPlayers)
        {
            p.OnEnableMovement(false);
            
        }

        PlayerControllerSwat finalWinner = winner;

        if (finalWinner != null)
        {
            Debug.Log($"Le gagnant est : {winner.name}");
        }
        else
        {
            Debug.Log("Égalité parfaite ! Aucun gagnant.");
        }
        // TODO: C'est ici que j'appel le "Grand Game Manager" pour lui dire qui a gagné (finalWinner peut être null)
    }

    // Fonction pour trouver le gagnant aux points
    private PlayerControllerSwat GetWinnerFromPoint()
    {
        // S'il n'y a aucun point, on retourne null
        if (playerPoints.Values.Sum() == 0)
        {          
            return null;
        }

        var sortedPoints = playerPoints.OrderByDescending(pair => pair.Value);

        var topPlayer = sortedPoints.First();

        int topScore = topPlayer.Value;
        int playersWithTopScore = playerPoints.Values.Count(score => score == topScore);

        if (playersWithTopScore > 1)
        {
            return null;
        }
        return topPlayer.Key;
    }
    
}
