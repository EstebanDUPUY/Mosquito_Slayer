using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent (typeof(PlayerInputManager))]
public class LayLobby : MonoBehaviour
{
    private PlayerInputManager inputManager;
    [SerializeField] private Sprite[] sprites;
    private void Awake()
    {
        inputManager = GetComponent<PlayerInputManager>();
        
    }

    public void OnPlayerJoined(PlayerInput input)
    {
        var id = inputManager.playerCount - 1;
        var player = input.gameObject;
        player.transform.position = new(id * 100, 0, 0);

        var playerLay = player.GetComponent<LayPlayer>();
        if (playerLay != null)
        {
            playerLay.SetUp(id, sprites[id % sprites.Length]); 
        }
    }
}
