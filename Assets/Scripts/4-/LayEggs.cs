
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class LayEggs : MonoBehaviour
{
    public GameObject eggsGO;
    [SerializeField] private int eggCount;
    [SerializeField] private Vector2 whereShouldEggBe = new Vector2(10, 10);
    public Transform wherePlayerIs;

    public void LayEggsInput(InputAction.CallbackContext ctx)
    {
        Debug.Log($"[INPUT] P{wherePlayerIs.GetComponent<PlayerData>().myDeviceIdPV} LAY EGG");
        Instantiate(eggsGO, wherePlayerIs.position + (Vector3)whereShouldEggBe, Quaternion.identity, transform);
        eggCount++;
    }
}
