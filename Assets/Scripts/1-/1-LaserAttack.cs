using UnityEngine;
using System.Collections;

public class LaserAttack : MonoBehaviour
{
    [Header("Réfs")]
    public SuccManager manager;            // GameManager
    [SerializeField] Transform ground;     // Transform de ta GroundSuck (ou un empty à la bonne hauteur)
    [SerializeField] Transform origin;     // optionnel (tête/main). Si null => ce GO

    [Header("Mouvement")]
    [SerializeField] float fallSpeed = 10f;
    [SerializeField] float startYOffset = 0f;
    [SerializeField] float stopAboveGround = 0.05f;

    bool active;
    float stopY;
    Coroutine co;

    /// <summary>
    /// Lancer une attaque : le laser se place au X de la cible et tombe pendant 'attackDuration'.
    /// </summary>
    public void FireAt(Transform target, float attackDuration)
    {
        if (co != null) StopCoroutine(co);

        float startY = (origin ? origin.position.y : transform.position.y) + startYOffset;
        stopY = ground ? ground.position.y + stopAboveGround : startY - 5f;

        float x = target ? target.position.x : transform.position.x;
        transform.position = new Vector3(x, startY, transform.position.z);

        gameObject.SetActive(true);
        active = true;
        co = StartCoroutine(MoveAndAutoStop(attackDuration));
    }

    public void StopNow()
    {
        active = false;
        if (co != null) { StopCoroutine(co); co = null; }
        gameObject.SetActive(false);
    }

    IEnumerator MoveAndAutoStop(float dur)
    {
        float t = 0f;
        while (active && t < dur)
        {
            t += Time.deltaTime;

            var p = transform.position;
            float ny = Mathf.MoveTowards(p.y, stopY, fallSpeed * Time.deltaTime);
            transform.position = new Vector3(p.x, ny, p.z);

            if (Mathf.Abs(ny - stopY) <= 0.001f) break; // atteint le sol
            yield return null;
        }
        StopNow();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!active) return;

        var player = other.GetComponent<PlayerSucc>();
        if (player == null) return;

        // il perd seulement s’il est en train de sucer
        if (player.IsSuccing && manager != null)
        {
            manager.OnLaserHit(player); // gère mort + defeat panel + dernier survivant
            StopNow();
        }
    }
}
