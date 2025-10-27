
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public PlayerData[] players;
    private int miniGamesPlayed;
    private List<int> playedGames;
    [SerializeField] private List<int> nonPlayedGames;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (instance  == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void AvengersStartGame()
    {
        for (int i = 0; i < players.Length; i++)
        {
            SetPlayerScore(players[i], 0);
        }
        playedGames = new List<int>();
        miniGamesPlayed = 0;
        NextMiniGame();
    }

    private void AvengersEndGame()
    {
        ChangeScene("FinalScore");
    }

    private void AvengersLateGame()
    {
       for (int j = players.Length - 1; j > 0; j--)
       {
          Destroy(players[j]);
       }
        players = new PlayerData[0];
    }
    public void SetPlayer(List<PlayerData> avengersAssemble)
    {
        players = avengersAssemble.ToArray();
    }
    private void SetPlayerScore(PlayerData player, int score)
    {
        player.scorePV = score;
    }
    public void AddScoreToPlayer(PlayerData winner)
    {
        if (winner != null) winner.scorePV++;
    }
    private void ChangeScene(int nextScene)
    {
        SceneManager.LoadScene(nextScene);
    }

    private void ChangeScene(string nextScene)
    {
        SceneManager.LoadScene(nextScene);
    }
    public void NextMiniGame()
    {
        if (miniGamesPlayed < 4)
        {
            miniGamesPlayed++;
            int gameIndex = Random.Range(0, nonPlayedGames.Count);

            while (playedGames.Contains(gameIndex))
            {
                gameIndex = Random.Range(0, nonPlayedGames.Count);
            }
            ChangeScene(nonPlayedGames[gameIndex]);
            playedGames.Add(gameIndex); 
           
        }
        else
        {
            AvengersEndGame();
        }
    }
}

