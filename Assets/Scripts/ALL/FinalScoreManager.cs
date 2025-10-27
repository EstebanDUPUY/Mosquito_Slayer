using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;

public class FinalScoreManager : MonoBehaviour
{
    [Header("Références UI")]
    public TMP_Text winnerText;
    public Transform scoreboardParent;
    public GameObject scoreEntryPrefab; // un prefab avec 2 TMP_Text (nom + score)

    void Start()
    {
        DisplayFinalScores();
    }

    private void DisplayFinalScores()
    {
        var players = GameManager.instance.players;

        // Tri décroissant selon le score
        var sortedPlayers = players.OrderByDescending(p => p.scorePV).ToArray();

        // Afficher le vainqueur en haut
        winnerText.text = $"{sortedPlayers[0].name} remporte la partie !";

        // Générer les entrées du tableau de scores
        foreach (var p in sortedPlayers)
        {
            GameObject entry = Instantiate(scoreEntryPrefab, scoreboardParent);
            TMP_Text[] texts = entry.GetComponentsInChildren<TMP_Text>();
            texts[0].text = p.name; // nom du joueur
            texts[1].text = p.scorePV.ToString(); // score global
        }
    }

    // Boutons de fin de partie
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
