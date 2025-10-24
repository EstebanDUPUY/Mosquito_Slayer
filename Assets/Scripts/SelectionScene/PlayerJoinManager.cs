
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerJoinManager : MonoBehaviour
{
    private PlayerInputManager pIMVar;
    public int[] playerId;
    private int playerIndex;
    private PlayerData currentPlayer;
    [SerializeField] private Sprite[] playerSkin;
    private List<int> takenSkins;
    private List<PlayerData> joinedPlayers;

    private void Start()
    {
        playerId = new int[4];
        pIMVar = GetComponent<PlayerInputManager>();
        // Create takenSkins list
        takenSkins = new List<int>();
        joinedPlayers = new List<PlayerData>();
    }

    public void OnPlayerJoinedEvent(PlayerInput playerInput)
    {
        // Stop more players from joining
        pIMVar.DisableJoining();
        // Get PlayerData script
        GameObject playerGO = playerInput.gameObject;
        PlayerData playerData = playerGO.GetComponent<PlayerData>();
        // Set Current Player
        currentPlayer = playerData;
        // Assign id to player
        playerId[playerIndex] = playerInput.devices[0].deviceId;
        playerIndex++;
        currentPlayer.myDeviceIdPV = playerInput.devices[0].deviceId;
    }

    public void ApplySkin(int skinIndex)
    {
        if (currentPlayer == null) return;
        if (takenSkins.Contains(skinIndex)) return;

        // 
        currentPlayer.playerSpritePV = playerSkin[skinIndex];
        takenSkins.Add(skinIndex);

        //
        currentPlayer.spriteRendererRef.sprite = currentPlayer.playerSpritePV;
        
        joinedPlayers.Add(currentPlayer);
        // Re-enable joining 
        currentPlayer = null;
        pIMVar.EnableJoining();
    }
    public void OnStartButtonPressed()
    {
        if (joinedPlayers.Count < 2) return;
        GameManager.instance.SetPlayer(joinedPlayers); //mettre cette formule dans tous les autres scripts pour le win de chaque mini-jeu : GameManager.instance.AddScoreToPlayer 
        GameManager.instance.AvengersStartGame();
    }

}

