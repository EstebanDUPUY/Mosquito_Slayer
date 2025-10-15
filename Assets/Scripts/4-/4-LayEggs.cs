using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class LayEggs : MonoBehaviour
{
    [HideInInspector] public GameObject eggsGO;
    [SerializeField] private Vector2 whereShouldEggBe = new Vector2(10, 10);

    public void LayEggsInput(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            Instantiate(eggsGO, whereShouldEggBe, Quaternion.identity);
        }
    }
}
