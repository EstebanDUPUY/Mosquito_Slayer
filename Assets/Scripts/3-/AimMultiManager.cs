using UnityEngine;
using System.Collections.Generic;

public class AimMultiManager : MonoBehaviour
{
    [Header("Références")]
    public GameObject miniGamePrefab;
    public Camera cameraPrefab; // petite caméra 2D simple
    public Transform[] spawnPositions; // 4 points différents dans la scène

    private void Start()
    {
        List<PlayerData> players = new List<PlayerData>(FindObjectsOfType<PlayerData>());
        if (players.Count == 0)
        {
            Debug.LogError("Aucun joueur trouvé depuis PlayerData !");
            return;
        }

        int total = Mathf.Min(players.Count, 4);
        for (int i = 0; i < total; i++)
        {
            CreateMiniGameForPlayer(players[i], i, total);
        }
    }

    private void CreateMiniGameForPlayer(PlayerData player, int index, int totalPlayers)
    {
        // Instancier le prefab mini-jeu
        GameObject instance = Instantiate(miniGamePrefab, spawnPositions[index].position, Quaternion.identity);
        AimMiniGame aim = instance.GetComponentInChildren<AimMiniGame>();

        // Assignation du PlayerInput à ce mini-jeu
        aim.GetComponent<AimMiniGame>().enabled = true;
        player.playerInputPV.actions.FindActionMap("MiniGame_Aim").Enable();

        // Créer la caméra du joueur
        Camera cam = Instantiate(cameraPrefab);
        cam.transform.position = new Vector3(0, 0, -10);
        cam.transform.SetParent(instance.transform);

        // Split screen selon le nombre de joueurs
        cam.rect = GetViewportRect(index, totalPlayers);

        // Optionnel : colorer le crosshair selon le joueur
        var sr = aim.crosshair.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = GetPlayerColor(index);

        Debug.Log($"Mini-jeu instancié pour {player.name} (viewport {cam.rect})");
    }

    private Rect GetViewportRect(int index, int total)
    {
        if (total == 1) return new Rect(0, 0, 1, 1);
        if (total == 2)
            return (index == 0) ? new Rect(0, 0, 0.5f, 1) : new Rect(0.5f, 0, 0.5f, 1);
        if (total == 3 || total == 4)
        {
            float w = 0.5f, h = 0.5f;
            return index switch
            {
                0 => new Rect(0, 0.5f, w, h),
                1 => new Rect(0.5f, 0.5f, w, h),
                2 => new Rect(0, 0, w, h),
                _ => new Rect(0.5f, 0, w, h)
            };
        }
        return new Rect(0, 0, 1, 1);
    }

    private Color GetPlayerColor(int index)
    {
        return index switch
        {
            0 => Color.green,
            1 => Color.yellow,
            2 => Color.red,
            3 => Color.blue,
            _ => Color.white
        };
    }
}
