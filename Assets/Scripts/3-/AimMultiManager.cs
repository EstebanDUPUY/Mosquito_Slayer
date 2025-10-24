using UnityEngine;
using System.Collections.Generic;

public class AimMultiManager : MonoBehaviour
{
    [Header("Références")]
    public GameObject miniGamePrefab;  // ton prefab complet AimMiniGamePrefab
    public Camera cameraPrefab;        // prefab d’une caméra 2D orthographique
    public Transform[] spawnPositions; // 4 positions différentes dans la scène

    private void Start()
    {
        // Récupère tous les joueurs connectés depuis le GameManager
        if (GameManager.instance == null)
        {
            Debug.LogError("Aucun GameManager trouvé dans la scène !");
            return;
        }

        // Récupère la liste des joueurs déjà enregistrés
        var playersField = typeof(GameManager).GetField("players",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (playersField == null)
        {
            Debug.LogError("Impossible d'accéder à la liste des joueurs dans GameManager !");
            return;
        }

        PlayerData[] players = playersField.GetValue(GameManager.instance) as PlayerData[];

        if (players == null || players.Length == 0)
        {
            Debug.LogError("Aucun PlayerData trouvé. Lance d’abord le jeu depuis l’écran de sélection des joueurs.");
            return;
        }

        int total = Mathf.Min(players.Length, 4);
        for (int i = 0; i < total; i++)
        {
            CreateMiniGameForPlayer(players[i], i, total);
        }
    }

    private void CreateMiniGameForPlayer(PlayerData player, int index, int totalPlayers)
    {
        if (player == null)
        {
            Debug.LogError($"Le joueur {index} est nul !");
            return;
        }

        // Instancie le prefab du mini-jeu
        Transform spawn = (spawnPositions != null && index < spawnPositions.Length)
            ? spawnPositions[index]
            : null;

        Vector3 spawnPos = spawn ? spawn.position : Vector3.zero;
        GameObject instance = Instantiate(miniGamePrefab, spawnPos, Quaternion.identity);

        // Récupère le script AimMiniGame et lie le PlayerData
        AimMiniGame aim = instance.GetComponentInChildren<AimMiniGame>();
        if (aim != null)
        {
            aim.linkedPlayer = player; // **Ligne essentielle**
        }
        else
        {
            Debug.LogError("Aucun script AimMiniGame trouvé dans le prefab !");
        }

        // Crée la caméra du joueur
        Camera cam = Instantiate(cameraPrefab);
        cam.transform.SetParent(instance.transform);
        cam.transform.localPosition = new Vector3(0, 0, -10);
        cam.rect = GetViewportRect(index, totalPlayers);

        // Optionnel : colorer le crosshair pour différencier les joueurs
        if (aim != null && aim.crosshair != null)
        {
            var sr = aim.crosshair.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = GetPlayerColor(index);
        }

        Debug.Log($"Mini-jeu instancié pour {player.name} avec viewport {cam.rect}");
    }

    private Rect GetViewportRect(int index, int total)
    {
        if (total == 1) return new Rect(0, 0, 1, 1);

        if (total == 2)
        {
            return index switch
            {
                0 => new Rect(0, 0, 0.5f, 1),
                1 => new Rect(0.5f, 0, 0.5f, 1),
                _ => new Rect(0, 0, 1, 1)
            };
        }

        if (total >= 3)
        {
            float w = 0.5f, h = 0.5f;
            return index switch
            {
                0 => new Rect(0, 0.5f, w, h),
                1 => new Rect(0.5f, 0.5f, w, h),
                2 => new Rect(0, 0, w, h),
                3 => new Rect(0.5f, 0, w, h),
                _ => new Rect(0, 0, 1, 1)
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
