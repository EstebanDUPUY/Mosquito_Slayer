using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class AimMiniGame : MonoBehaviour
{
    [Header("Références")]
    public Transform zoneTopLeft;
    public Transform zoneBottomRight;
    public Transform crosshair;       // Le viseur du joueur
    public TMP_Text infoText;         // Texte d'information ou de score

    [Header("Paramètres de mouvement")]
    public float moveSpeed = 3f;      // Vitesse de déplacement du viseur
    public float moveInterval = 1.2f; // Temps avant de choisir une nouvelle destination
    public float roundDuration = 5f;  // Durée totale de la manche

    private MoskilltoControls controls;
    private bool hasShot = false;
    private bool isPerturbed = false;
    private float playerScore = 0;

    // Déplacement fluide
    private Vector3 currentTarget;
    private float moveTimer = 0f;

    private void Awake()
    {
        controls = new MoskilltoControls();
    }

    private void OnEnable()
    {
        controls.MiniGame_Aim.Enable();
        controls.MiniGame_Aim.Shoot.performed += OnShoot;
        controls.MiniGame_Aim.Sabotage.performed += OnSabotage;
    }

    private void OnDisable()
    {
        controls.MiniGame_Aim.Shoot.performed -= OnShoot;
        controls.MiniGame_Aim.Sabotage.performed -= OnSabotage;
        controls.MiniGame_Aim.Disable();
    }

    private void Start()
    {
        currentTarget = GetRandomPointInZone();
        StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        if (infoText) infoText.text = "Prépare-toi...";
        yield return new WaitForSeconds(1f);
        if (infoText) infoText.text = "";

        float t = 0f;
        hasShot = false;
        playerScore = 0;
        moveTimer = 0f;

        while (t < roundDuration)
        {
            t += Time.deltaTime;
            moveTimer -= Time.deltaTime;

            // Changer régulièrement de direction
            if (moveTimer <= 0f)
            {
                currentTarget = GetRandomPointInZone();
                moveTimer = moveInterval;
            }

            // Déplacement fluide vers la cible
            Vector3 nextPos = Vector3.Lerp(crosshair.position, currentTarget, Time.deltaTime * moveSpeed);

            // Si le joueur est perturbé : petit tremblement
            if (isPerturbed)
                nextPos += (Vector3)Random.insideUnitCircle * 0.1f;

            // Limite du cadre
            float minX = zoneTopLeft.position.x;
            float maxX = zoneBottomRight.position.x;
            float maxY = zoneTopLeft.position.y;
            float minY = zoneBottomRight.position.y;

            nextPos.x = Mathf.Clamp(nextPos.x, minX, maxX);
            nextPos.y = Mathf.Clamp(nextPos.y, minY, maxY);

            crosshair.position = nextPos;

            yield return null;
        }

        if (infoText)
            infoText.text = $"Score final : {playerScore}";
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        if (hasShot) return;
        hasShot = true;

        Vector2 origin = crosshair.position;

        // Vérifie s'il y a une cible directement sous le viseur
        Collider2D hit = Physics2D.OverlapPoint(origin);

        if (hit != null)
        {
            TargetZone zone = hit.GetComponent<TargetZone>();
            if (zone != null)
            {
                int pts = zone.GetScore();
                playerScore += pts;

                if (infoText)
                    infoText.text = $"Touché : {zone.zoneName} (+{pts})";
            }
            else
            {
                if (infoText)
                    infoText.text = "Raté !";
            }
        }
        else
        {
            if (infoText)
                infoText.text = "Raté !";
        }
    }

    private void OnSabotage(InputAction.CallbackContext ctx)
    {
        if (!isPerturbed)
            StartCoroutine(Perturbation());
    }

    private IEnumerator Perturbation()
    {
        isPerturbed = true;
        yield return new WaitForSeconds(1.5f);
        isPerturbed = false;
    }

    private Vector3 GetRandomPointInZone()
    {
        float minX = zoneTopLeft.position.x;
        float maxX = zoneBottomRight.position.x;
        float maxY = zoneTopLeft.position.y;
        float minY = zoneBottomRight.position.y;

        float x = Random.Range(minX, maxX);
        float y = Random.Range(minY, maxY);
        return new Vector3(x, y, crosshair.position.z);
    }
}
