using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class SwatController : MonoBehaviour
{
    [Header("Zones")]
    public Transform[] zones;
    public Transform spawnPoint;
    public Vector2 size = new Vector2(1f, 1f);
    public LayerMask playerLayer;

    [Header("Paramètres d'attaque")]
    public float initialWarningDelay = 4f;
    public float minWarningDelay = 1f;
    public float timeToMaxSpeed = 120f;
    public float crashSpeed = 15f;


    [Header("Effets")]
    public GameObject warningPrefab;
    private GameObject currentWarning;
    private bool attacking = false;

    private float gameTimeElapsed = 0f;

    // Update is called once per frame
    void Update()
    {
        if (attacking)
        {
            gameTimeElapsed += Time.deltaTime;
        }

    }

    public void StartAttacking()
    {
        attacking = true;
        gameTimeElapsed = 0f;
        StartCoroutine(AttackLoop());
    }

    public void StopAttacking()
    {
        attacking = false;
    }

    IEnumerator AttackLoop()
    {
        

        while (attacking)
        {         

            int index = Random.Range(0, zones.Length);
            Transform zone = zones[index];

            float elapsed = gameTimeElapsed;
            float delay = Mathf.Lerp(initialWarningDelay, minWarningDelay, Mathf.Clamp01(elapsed / timeToMaxSpeed));

            StartWarning(zone);
            GameManagerSwat.Instance.soundManagerSwat.PlayWarning();

            yield return new WaitForSeconds(delay);

            if (!attacking) yield break;

            CancelWarning();
            yield return PerformCrash(zone);
        }
    }

    void StartWarning(Transform zone)
    {
       currentWarning = Instantiate(warningPrefab, zone.position, Quaternion.identity);
    }

    void CancelWarning()
    {
        if (currentWarning)
        {
              Destroy(currentWarning);
        }
    }

    IEnumerator PerformCrash(Transform zone)
    {

        transform.position = spawnPoint.position;

        Vector2 start = spawnPoint.position;
        Vector2 end = zone.position;
        float travelTime = Vector2.Distance(start, end) / crashSpeed;
        float t = 0f;

        while (t < travelTime)
        {
            t += Time.deltaTime;
            transform.position = Vector2.Lerp(start, end, t / travelTime);
            yield return null;

        }

        transform.position = end;

        OnImpact(end);
        transform.position = spawnPoint.position;
        yield return new WaitForSeconds(1f); // Petite pause avant le prochain cycle

    }

    void OnImpact(Vector2 pos)
    {
        GameManagerSwat.Instance.soundManagerSwat.PlayImpact();

        Collider2D[] hits = Physics2D.OverlapBoxAll(pos, size, playerLayer);

        foreach (var h in hits)
        {
            PlayerControllerSwat player = h.GetComponent<PlayerControllerSwat>();
            if (player != null && player.isAlive)
            {
                player.Kill();
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (zones != null)
        {
            foreach (var z in zones)
            {
                if (z) Gizmos.DrawWireCube(z.position, size);
            }
        }
    }

    

    
}
