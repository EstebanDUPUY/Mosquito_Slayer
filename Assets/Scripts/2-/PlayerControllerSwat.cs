using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerSwat : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    public bool isAlive = true;
    private SpriteRenderer sr;
    public event Action<PlayerControllerSwat> OnPlayerDied;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }
    

    // Update is called once per frame
    void FixedUpdate()
    {
       if (!isAlive) return;

        Vector2 move = moveInput * moveSpeed;
        rb.linearVelocity = move;
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnEnableMovement(bool state)
    {
        rb.simulated = state;

        // Si on désactive le mouvement, on stoppe aussi la vélocité.
        if (state == false)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void Kill()
    {
        if (!isAlive) return;

        isAlive = false;
        OnEnableMovement(false);

        // Changement visuel rapide
        if (sr != null)
        {
            sr.color = Color.gray;
        }
            

        // Hurle qu'ont est mort
        OnPlayerDied?.Invoke(this);
    }
}
