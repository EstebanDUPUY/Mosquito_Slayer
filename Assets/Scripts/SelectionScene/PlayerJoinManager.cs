
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField] private Color[] playerColors = new Color[4]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow
    }; // tu peux changer dans l'Inspector

    [SerializeField] private Color neutralColor = Color.white * 0.4f;

    // --- NEW : on garde les slots déjà verrouillés pour éviter de re-sélectionner visuellement
    private HashSet<CharacterSlotUI> lockedSlots = new HashSet<CharacterSlotUI>();

    [SerializeField] private CharacterSlotUI[] allSlots;
    [SerializeField] private EventSystem eventSystem;

    private CharacterSlotUI focusedSlot; // le slot actuellement highlighté (focus UI)
    public void SetFocusedSlot(CharacterSlotUI slot)
    {
        focusedSlot = slot;
    }

    // NEW: Input action pour "valider"
    [Header("Input Validation")]
    [SerializeField] private InputActionReference confirmAction;


    private void OnEnable()
    {
        if (confirmAction != null && confirmAction.action != null)
        {
            confirmAction.action.performed += OnConfirmPerformed;
            confirmAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (confirmAction != null && confirmAction.action != null)
        {
            confirmAction.action.performed -= OnConfirmPerformed;
            confirmAction.action.Disable();
        }
    }

    private void Start()
    {
        playerId = new int[4];
        pIMVar = GetComponent<PlayerInputManager>();
        // Create takenSkins list
        takenSkins = new List<int>();
        joinedPlayers = new List<PlayerData>();

        // après avoir fait takenSkins = new List<int>(); etc...

        // NEW: au lancement, on setup la couleur du joueur actuel (P1 au début)
        RefreshButtonColorsForCurrentPlayer();

        // Donner le focus au premier slot si possible
        if (eventSystem != null && allSlots != null && allSlots.Length > 0 && allSlots[0] != null)
        {
            // On cherche un Selectable (genre Button) sur le slot
            var firstSelectable = allSlots[0].GetComponent<Selectable>();
            if (firstSelectable != null)
            {
                eventSystem.SetSelectedGameObject(firstSelectable.gameObject);
            }
        }
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
        currentPlayer.playerCamera = playerInput.camera;

        // NEW: au lancement, on setup la couleur du joueur actuel (P1 au début)
        RefreshButtonColorsForCurrentPlayer();
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

        // NEW: au lancement, on setup la couleur du joueur actuel (P1 au début)
        RefreshButtonColorsForCurrentPlayer();
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

    // Appelé dans Start() et à chaque changement de joueur
    void RefreshButtonColorsForCurrentPlayer()
    {
        // couleur qui doit apparaître en "Selected"
        Color c = HasCurrentPlayer() ? GetCurrentPlayerColor() : neutralColor;

        if (allSlots == null) return;

        foreach (var slot in allSlots)
        {
            if (slot == null) continue;

            // va récupérer le script CharacterSlotButtonColor sur le même objet que le bouton
            var colorCtrl = slot.GetComponent<CharacterButtonSlotColor>();
            if (colorCtrl == null) continue;

            if (HasCurrentPlayer())
            {
                colorCtrl.SetSelectedColor(c);
            }
            else
            {
                // personne ne choisit en ce moment
                colorCtrl.ResetColors(neutralColor);
            }
        }
    }

    // Permet à un slot de demander "est-ce que je suis déjà pris ?"
    public bool IsSkinAlreadyTaken(int skinIndex)
    {
        return takenSkins.Contains(skinIndex);
    }

    private void OnConfirmPerformed(InputAction.CallbackContext ctx)
    {
        // si personne n'est en train de choisir → rien
        if (!HasCurrentPlayer()) return;

        // si aucun slot n'est focus → rien
        if (focusedSlot == null) return;

        // essaie de valider le slot actuel
        focusedSlot.TrySelectMe();
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

    public void OnValidateCurrentSelection(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        // Pas de joueur en train de choisir ? => rien
        if (currentPlayer == null) return;

        // Pas de slot focus ? => rien
        if (focusedSlot == null) return;

        // Essaye de sélectionner ce slot
        TrySelectSlot(focusedSlot);
    }

}

