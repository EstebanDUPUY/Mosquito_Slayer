using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerData : MonoBehaviour
{
    public Sprite playerSpritePV;
    public PlayerInput playerInputPV; // PV = Public Variable
    public int myDeviceIdPV;
    public SpriteRenderer spriteRendererRef;
    public int scorePV;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
        playerInputPV = GetComponent<PlayerInput>(); 
        spriteRendererRef = GetComponent<SpriteRenderer>();
    }
}
