
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerData : MonoBehaviour
{
    public Sprite playerSpritePV;
    public PlayerInput playerInputPV; // PV = Public Variable
    public int myDeviceIdPV;
    public SpriteRenderer spriteRendererRef;
    public int scorePV;
    // ordre dans lequel ce joueur a rejoint (0,1,2,3)
    public int joinOrderPV;
    public Camera playerCamera;

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

