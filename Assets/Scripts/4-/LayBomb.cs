
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LayBomb : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private float minExplosionTime = 5f;
    [SerializeField] private float maxExplosionTime = 15f;
    [SerializeField] private float passInputDelay = 0.5f; // Prevent instant re-passing

    [Header("Visual References")]
    [SerializeField] private GameObject bombVisualPrefab; // Visual representation of bomb
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Canvas gameCanvas;

    [Header("Audio")]
    [SerializeField] private AudioSource tickSound;
    [SerializeField] private AudioSource explosionSound;
    [SerializeField] private AudioSource passSound;

    private List<LayEggs> players;
    private LayEggs currentBombHolder;
    private GameObject bombVisual;
    private float explosionCountdown;
    private float maxCountdown;
    private bool gameStarted = false;
    public bool bombIsActive = false;
    private bool canPassBomb = false;
    private int eliminatedPlayerIndex = -1;

    //private void Start()
    //{
    //    // Get all players from their persistent objects
    //    FindPlayers();

    //    if (players.Count < 2)
    //    {
    //        Debug.LogError("Not enough players for Tic Tac Boum!");
    //        return;
    //    }

    //    StartCoroutine(StartGameSequence());
    //}

    public void SetUpBomb()
    {
        players = new List<LayEggs>();
        LayEggs[] foundPlayers = FindObjectsOfType<LayEggs>();

        for (int i = 0; i < foundPlayers.Length; i++)
        {
            if (foundPlayers[i].linkZelda != null)
                players.Add(foundPlayers[i]);
        }

        StartCoroutine(StartGameSequence());

    }

    private IEnumerator StartGameSequence()
    {
        // Show instructions
        if (instructionText != null)
        {
            instructionText.text = "Get ready! Someone will get the bomb...";
        }

        yield return new WaitForSeconds(2f);

        // Randomly assign bomb to a player
        AssignBombToRandomPlayer();

        if (instructionText != null)
        {
            instructionText.text = "Press your action button to KEEP the bomb or PASS it!";
        }

        yield return new WaitForSeconds(2f);

        if (instructionText != null)
        {
            instructionText.text = "";
        }

        gameStarted = true;
    }

    private void AssignBombToRandomPlayer()
    {
        int randomIndex = Random.Range(0, players.Count);
        GiveBombToPlayer(players[randomIndex]);
    }

    private void GiveBombToPlayer(LayEggs player)
    {
        currentBombHolder = player;
        player.hasBomb = true;

        // Position bomb visual near player
        if (bombVisualPrefab != null && bombVisual == null)
        {
            bombVisual = Instantiate(bombVisualPrefab);
        }

        if (bombVisual != null)
        {
            bombVisual.transform.position = player.transform.position + Vector3.up * 1.5f;
            bombVisual.transform.SetParent(player.transform);
        }

        // Reset pass delay
        canPassBomb = false;
        StartCoroutine(EnablePassingAfterDelay());
    }

    private IEnumerator EnablePassingAfterDelay()
    {
        yield return new WaitForSeconds(passInputDelay);
        canPassBomb = true;
    }

    private void Update()
    {
        if (!gameStarted || currentBombHolder == null) return;

        // Handle bomb activation and passing
        //HandlePlayerInput();

        // Update timer if bomb is active
        if (bombIsActive)
        {
            UpdateBombTimer();
        }
    }

    //private void HandlePlayerInput()
    //{
    //    // Check if current bomb holder presses their action button
    //    if (currentBombHolder.playerInputPV != null)
    //    {
    //        var inputAction = currentBombHolder.playerInputPV.actions["Fire"];

    //        if (inputAction != null && inputAction.triggered)
    //        {
    //            if (!bombIsActive)
    //            {
    //                // First press: Activate the bomb
    //                ActivateBomb();
    //            }
    //            else if (canPassBomb)
    //            {
    //                // Subsequent presses: Pass the bomb
    //                PassBombToNextPlayer();
    //            }
    //        }
    //    }
    //}

    public void ActivateBomb()
    {
        bombIsActive = true;

        // Set random countdown
        maxCountdown = Random.Range(minExplosionTime, maxExplosionTime);
        explosionCountdown = maxCountdown;

        if (instructionText != null)
        {
            instructionText.text = "BOMB IS LIVE! Pass it with action button!";
        }

        if (tickSound != null)
        {
            tickSound.Play();
        }
    }

    public void PassBombToNextPlayer()
    {
        if (!canPassBomb) return;

        // Find next player (not the current holder)
        List<LayEggs> availablePlayers = new List<LayEggs>();

        foreach (LayEggs player in players)
        {
            if (player != currentBombHolder)
            {
                availablePlayers.Add(player);
            }
        }

        if (availablePlayers.Count > 0)
        {
            int randomIndex = Random.Range(0, availablePlayers.Count);
            GiveBombToPlayer(availablePlayers[randomIndex]);

            if (passSound != null)
            {
                passSound.Play();
            }
        }
    }

    private void UpdateBombTimer()
    {
        explosionCountdown -= Time.deltaTime;

        // Update timer display
        if (timerText != null)
        {
            timerText.text = $"Time: {explosionCountdown:F1}s";

            // Change color as time runs out
            float timeRatio = explosionCountdown / maxCountdown;
            if (timeRatio < 0.3f)
            {
                timerText.color = Color.red;
            }
            else if (timeRatio < 0.6f)
            {
                timerText.color = Color.yellow;
            }
        }

        // Speed up ticking sound as time runs out
        if (tickSound != null && explosionCountdown < 3f)
        {
            tickSound.pitch = 1.5f + (3f - explosionCountdown) * 0.5f;
        }

        // Check if bomb should explode
        if (explosionCountdown <= 0f)
        {
            ExplodeBomb();
        }
    }

    private void ExplodeBomb()
    {
        bombIsActive = false;
        gameStarted = false;

        // Play explosion
        if (explosionSound != null)
        {
            explosionSound.Play();
        }

        if (tickSound != null)
        {
            tickSound.Stop();
        }

        // Show explosion effect on bomb holder
        if (instructionText != null)
        {
            instructionText.text = $"BOOM! {currentBombHolder.gameObject.name} exploded!";
        }

        // Store eliminated player index
        eliminatedPlayerIndex = players.IndexOf(currentBombHolder);



        // Destroy bomb visual
        if (bombVisual != null)
        {
            Destroy(bombVisual);
        }

        // End game after delay
        StartCoroutine(EndGameAfterDelay());
    }


    private IEnumerator EndGameAfterDelay()
    {
        yield return new WaitForSeconds(3f);

        // Return to game manager to load next minigame
        GameManager.instance.NextMiniGame();
    }

    // Optional: Visualize bomb holder in Scene view
    private void OnDrawGizmos()
    {
        if (currentBombHolder != null && bombIsActive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentBombHolder.transform.position, 1f);
        }
    }
}

