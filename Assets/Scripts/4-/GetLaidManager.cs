using UnityEngine;
using UnityEngine.InputSystem;

public class GetLaidManager : MonoBehaviour
{

    [SerializeField] private LayEggs[] playerSpawnPoints;
    void Start()
    {
        PlayerSetup();
    }

    void Update()
    {
        
    }

    public void Countdown()
    {

    }
    public void ShowInstructionsPanel()
    {

    }
    public void OnTimerEnd()
    {

    }

    public void PlayerSetup()
    {
        PlayerData[] playersInGame = GameManager.instance.players;

        for (int i = 0; i < playersInGame.Length; i++)
        {
            playersInGame[i].transform.position = playerSpawnPoints[i].transform.position;

            playerSpawnPoints[i].wherePlayerIs = playersInGame[i].transform;

            //Inputs

            PlayerData currentPlayer = playersInGame[i];
            currentPlayer.playerInputPV.actions["Action"].started += playerSpawnPoints[i].LayEggsInput;
            currentPlayer.playerInputPV.actions["Action"].started += test;
        }
    }

    public void test(InputAction.CallbackContext context)
    {
        Debug.Log("Test");
    }
}
