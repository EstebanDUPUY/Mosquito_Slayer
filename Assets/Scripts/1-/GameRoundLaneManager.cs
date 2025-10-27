using UnityEngine;
using UnityEngine.InputSystem;

public class GameRoundLaneManager : MonoBehaviour
{
    [Header("Référence au LaneManager (drag & drop dans l'inspector)")]
    [SerializeField] private LaneManager laneManager;

    private bool roundEnded = false; // une fois qu'on a déclaré un gagnant, on ne recommence pas

    private void Update()
    {
        if (roundEnded) return;
        if (laneManager == null || laneManager.lanes == null || laneManager.lanes.Length == 0) return;

        int livingLaneCount = 0;
        SuccManager lastAliveManager = null;

        // On parcourt toutes les lanes
        foreach (var lane in laneManager.lanes)
        {
            if (lane == null || lane.manager == null) continue;

            // Est-ce qu'il reste au moins un joueur vivant dans cette lane ?
            if (lane.manager.HasAlivePlayer())
            {
                livingLaneCount++;
                lastAliveManager = lane.manager;
            }
        }

        // Cas 1 : plus personne en vie (accident chelou) -> on arrête tout sans winner
        if (livingLaneCount == 0)
        {
            roundEnded = true;
            StopAllLanes(noWinner: true);
            return;
        }

        // Cas 2 : une seule lane encore vivante -> cette lane gagne 🎉
        if (livingLaneCount == 1 && lastAliveManager != null)
        {
            roundEnded = true;

            // Le survivant gagne
            lastAliveManager.ForceWin();

            // Les autres lanes s'arrêtent et affichent rien d'autre que leur état actuel
            foreach (var lane in laneManager.lanes)
            {
                if (lane == null || lane.manager == null) continue;
                if (lane.manager == lastAliveManager) continue; // skip le gagnant
                lane.manager.ForceStopNoWin();
            }
        }
    }

    private void StopAllLanes(bool noWinner)
    {
        foreach (var lane in laneManager.lanes)
        {
            if (lane == null || lane.manager == null) continue;

            if (noWinner)
                lane.manager.ForceStopNoWin();
            else
                lane.manager.ForceWin();
        }
    }
}
