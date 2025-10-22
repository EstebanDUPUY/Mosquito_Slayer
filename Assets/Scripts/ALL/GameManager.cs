using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    private PlayerData[] players;
    private PlayerData currentPlayer;
    private int miniGamesPlayed;
    private List<int> playedGames;
    private List<int> nonPlayedGames;

    private void Start()
    {
        currentPlayer.scorePV = 0;
    }

    private void SetPlayer()
    {
        
    }
    private void AddScoreToPlayer(PlayerData winner)
    {
        if (winner != null /*win*/) winner.scorePV++;
    }
    private void ChangeScene(int nextScene)
    {
        SceneManager.LoadScene(nextScene);
    }
    private void NextMiniGame()
    {
        if (miniGamesPlayed < 4)
        {
            miniGamesPlayed++;
            int gameIndex = Random.Range(0,nonPlayedGames.Count);
        }
        else 
        {
            ChangeScene(5);
        }
    }
}
