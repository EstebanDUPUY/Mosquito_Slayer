
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class LayEggs : MonoBehaviour
{
    public GameObject eggsGO;
    [SerializeField] private int eggCount;
    [SerializeField] private Vector2 whereShouldEggBe = new Vector2(10, 10);
    public Transform wherePlayerIs;
    public bool hasBomb = false;
    public LayBomb throwBombMdr;

    public void LayEggsInput(InputAction.CallbackContext ctx)
    {
        Debug.Log($"[INPUT] P{wherePlayerIs.GetComponent<PlayerData>().myDeviceIdPV} LAY EGG");
        Instantiate(eggsGO, wherePlayerIs.position + (Vector3)whereShouldEggBe, Quaternion.identity, transform);
        eggCount++;
    }

    public void ThrowBomb(InputAction.CallbackContext ctx)
    { 
        if (!hasBomb) return;

        if (throwBombMdr.bombIsActive)
        {
             throwBombMdr.PassBombToNextPlayer();
        }
        else
        {
             throwBombMdr.ActivateBomb();

        }

    }
}
