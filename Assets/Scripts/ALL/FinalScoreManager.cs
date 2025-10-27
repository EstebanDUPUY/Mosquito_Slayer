using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;

public class FinalScoreManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text winnerText;
    public TMP_Text[] scoreLines; // 4 textes : un par joueur

    void Start()
    {
        DisplayScores();
    }

    void DisplayScores()
    {
        var players = GameManager.instance.players.OrderByDescending(p => p.scorePV).ToArray();

        // Affiche le vainqueur
        winnerText.text = $"{players[0].name} GAGNE LA PARTIE !";

        // Affiche les scores ligne par ligne
        for (int i = 0; i < players.Length; i++)
        {
            scoreLines[i].text = $"{i + 1}. {players[i].name} — {players[i].scorePV} victoires";
        }
    }

    public void Replay()
    {
        GameManager.instance.AvengersStartGame();
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
