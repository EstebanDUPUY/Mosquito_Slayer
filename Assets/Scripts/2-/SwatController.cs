using System.Collections;
using UnityEngine;


public class SwatController : MonoBehaviour
{
    [Header("Zones")]
    public ZoneInfo[] zones;
    public Transform spawnPoint;
    public LayerMask playerLayer;

    [Header("Paramètres d'attaque")]
    public float initialWarningDelay = 4f;
    public float minWarningDelay = 1f;
    public float timeToMaxSpeed = 120f;
    public float crashSpeed = 15f;
    public float maxCrashSpeed = 30f;


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
            // --- 1. Choisir La Zone  ---
            int index = Random.Range(0, zones.Length);
            ZoneInfo zone = zones[index];

            // --- 2. Calculer La Vitesse ---
            float elapsed = gameTimeElapsed;
            float delay = Mathf.Lerp(initialWarningDelay, minWarningDelay, Mathf.Clamp01(elapsed / timeToMaxSpeed));
            float currentSpeed = Mathf.Lerp(crashSpeed, maxCrashSpeed, Mathf.Clamp01(elapsed / timeToMaxSpeed));

            // --- 3. Attaquer ---
            StartWarning(zone.transform);     
            GameManagerSwat.Instance.soundManagerSwat.PlayWarning();

            yield return new WaitForSeconds(delay);

            if (!attacking) break;
                       

            CancelWarning();


            yield return PerformCrash(zone, currentSpeed);
        }
    }

    

    void StartWarning(Transform zone)
    {
       currentWarning = Instantiate(warningPrefab, zone.position, Quaternion.identity);
    }

    void CancelWarning()
    {
        if (currentWarning != null)
        {
              Destroy(currentWarning);
        }
    }

    IEnumerator PerformCrash(ZoneInfo zone, float currentSpeed)
    {    

        transform.position = spawnPoint.position;
        Vector2 start = spawnPoint.position;
        Vector2 end = zone.transform.position;
        float travelTime = Vector2.Distance(start, end) / currentSpeed;
        float t = 0f;

        while (t < travelTime)
        {
            t += Time.deltaTime;
                     
            transform.position = Vector2.Lerp(start, end, t / travelTime);
              
            yield return null;
        }
        
        
        transform.position = end;
        

        OnImpact(end, zone.size);

       transform.position = spawnPoint.position;

        yield return new WaitForSeconds(1f); // Petite pause avant le prochain cycle

    }

    void OnImpact(Vector2 pos, Vector2 zoneSize)
    {
        GameManagerSwat.Instance.soundManagerSwat.PlayImpact();

        Collider2D[] hits = Physics2D.OverlapBoxAll(pos, zoneSize, 0f, playerLayer);

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
            foreach (ZoneInfo z in zones)
            {
                if (z != null)
                {
                    Gizmos.DrawWireCube(z.transform.position, z.size);
                }
            }
        }
    }

    

    
}
