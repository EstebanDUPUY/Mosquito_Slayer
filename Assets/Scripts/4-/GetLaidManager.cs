using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GetLaidManager : MonoBehaviour
{
    [SerializeField] private float gameDuration = 10f;
    [SerializeField] private LayEggs[] playerSpawnPoints;
    public InstructionCountdownIntro instructionCountdownIntro;

    void Start()
    {
        PlayerSetup();
        StartCoroutine(StartGame());
    }

    void Update()
    {
        
    }

    public void OnTimerEnd()
    {
        PlayerData[] playersInGame = GameManager.instance.players;
        for (int i = 0; i < playersInGame.Length; i++)
        {
            PlayerData currentPlayer = playersInGame[i];
            currentPlayer.playerInputPV.actions["Action"].canceled -= playerSpawnPoints[i].LayEggsInput;
        }

        //GameManager.instance.AddScoreToPlayer(GetComponent<GetLaidWinner>().DetermineWinner());

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
        }
    }

    private IEnumerator StartGame()
    {
        yield return instructionCountdownIntro.StartCoroutine("Flow");
        yield return new WaitForSeconds(gameDuration);
        OnTimerEnd();
    }

}
