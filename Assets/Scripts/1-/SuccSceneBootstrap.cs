using UnityEngine;

public class SuccSceneBootstrap : MonoBehaviour
{
    [SerializeField] private LaneManager laneManager; // drag depuis la scène, le GO qui porte LaneManager

    void Start()
    {
        // Sécurité
        if (!laneManager)
        {
            laneManager = FindObjectOfType<LaneManager>();
        }

        // Récupère les joueurs globaux
        PlayerData[] fromGM = null;
        if (GameManager.instance != null)
        {
            fromGM = GameManager.instance.players;
        }

        // Log pour debug
        int nb = (fromGM != null) ? fromGM.Length : 0;
        Debug.Log("[BOOT] J'appelle InitRoundFromPlayers avec " + nb + " joueurs.");

        // Lance l'init round dans le LaneManager
        laneManager.InitRoundFromPlayers(fromGM);
    }
}
