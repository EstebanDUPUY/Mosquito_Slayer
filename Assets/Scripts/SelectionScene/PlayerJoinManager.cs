
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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

    // --- NEW : couleurs d'identification des joueurs (ordre d'arrivée) ---
    [SerializeField]
    private Color[] playerColors = new Color[4]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow
    }; // tu peux changer dans l'Inspector

    // --- NEW : on garde les slots déjà verrouillés pour éviter de re-sélectionner visuellement
    private HashSet<CharacterSlotUI> lockedSlots = new HashSet<CharacterSlotUI>();


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
        // NEW : on mémorise l'ordre d'arrivée pour ce joueur (0,1,2,3...)
        currentPlayer.joinOrderPV = playerIndex; // <- ajoute ce champ dans PlayerData (int)
        playerIndex++;
        currentPlayer.myDeviceIdPV = playerInput.devices[0].deviceId;
    }

    public void ApplySkin(int skinIndex, CharacterSlotUI slot = null)
    {
        if (currentPlayer == null) return;
        if (takenSkins.Contains(skinIndex)) return;

        // 
        currentPlayer.playerSpritePV = playerSkin[skinIndex];
        takenSkins.Add(skinIndex);

        //
        currentPlayer.spriteRendererRef.sprite = currentPlayer.playerSpritePV;

        // NEW : on "lock" le slot visuellement (tache de sang ON)
        if (slot != null)
        {
            slot.LockThisChoice();       // active la tache sang, désactive le contour
            lockedSlots.Add(slot);       // on retient qu'il est pris
        }

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

    // --------- NEW : helpers pour l'UI ---------

    // Est-ce qu'on a un joueur en train de choisir ?
    public bool HasCurrentPlayer()
    {
        return currentPlayer != null;
    }

    // Donne la couleur du contour à afficher pour ce joueur en train de choisir
    public Color GetCurrentPlayerColor()
    {
        if (currentPlayer == null) return Color.white;

        int idx = currentPlayer.joinOrderPV;
        if (idx < 0 || idx >= playerColors.Length)
            return Color.white;

        return playerColors[idx];
    }

    // Permet à un slot de demander "est-ce que je suis déjà pris ?"
    public bool IsSkinAlreadyTaken(int skinIndex)
    {
        return takenSkins.Contains(skinIndex);
    }

    // Permet à un slot de dire "je tente d'être choisi"
    // (il appelle en gros ApplySkin en lui passant son index et lui-même)
    public bool TrySelectSlot(CharacterSlotUI slot)
    {
        if (slot == null) return false;
        if (currentPlayer == null) return false;
        if (IsSkinAlreadyTaken(slot.skinIndex)) return false;
        if (lockedSlots.Contains(slot)) return false;

        ApplySkin(slot.skinIndex, slot); // ← garde toute ta logique ApplySkin
        return true;
    }

}

