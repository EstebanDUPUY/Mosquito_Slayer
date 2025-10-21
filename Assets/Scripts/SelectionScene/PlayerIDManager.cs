using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.iOS;

public class PlayerIDManager : MonoBehaviour
{
    private PlayerInputManager pIMVar;
    [SerializeField] private int[] playerId;
    private int playerIndex;


    private void Start()
    {
        playerId = new int[4];
    }

    public void OnPlayerJoinedEvent(PlayerInput playerInput)
    {
        playerId[playerIndex] = playerInput.devices[0].deviceId;
        playerIndex++;
    }
}
