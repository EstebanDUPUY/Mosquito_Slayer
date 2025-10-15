using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class AimMiniGame : MonoBehaviour
{
    [Header("Références")]
    public Transform zoneTopLeft;
    public Transform zoneBottomRight;
    public Transform crosshair;
    public HumanTarget human;
    public TMP_Text infoText;       // Texte général ("Touché !", "Raté !", etc.)
    public TMP_Text timerText;      // Affiche le timer
    public TMP_Text scoreText;      // Score total
    public TMP_Text shotsText;      // Affiche le nombre de tirs restants

    [Header("Paramètres de jeu")]
    public float moveSpeed = 3f;
    public float moveInterval = 1.2f;
    public float roundDuration = 5f;
    public int totalRounds = 3;
    public int maxShotsPerRound = 3;

    [Header("Zones piquables")]
    public List<TargetZone> targetZones = new List<TargetZone>();
    [Range(0f, 1f)]
    public float chanceToGoNearTarget = 0.75f;
    public float offsetAroundTarget = 0.5f;

    private MoskilltoControls controls;
    private bool isPerturbed = false;
    private bool canShoot = true;
    private float playerScore = 0;

    private Vector3 currentTarget;
    private float moveTimer = 0f;
    private int shotsRemaining;
    private int currentRound = 1;

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
        StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        if (infoText) infoText.text = "Prépare-toi...";
        yield return new WaitForSeconds(1.2f);
        if (infoText) infoText.text = "";

        for (currentRound = 1; currentRound <= totalRounds; currentRound++)
        {
            yield return StartCoroutine(PlayRound(currentRound));
            yield return new WaitForSeconds(1f);
        }

        if (infoText)
            infoText.text = $"Fin du mini-jeu ! Score final : {playerScore}";
        if (scoreText)
            scoreText.text = $"Score : {playerScore}";
    }

    private IEnumerator PlayRound(int round)
    {
        shotsRemaining = maxShotsPerRound;
        canShoot = true;
        moveTimer = 0f;
        float t = 0f;

        if (crosshair) crosshair.gameObject.SetActive(true);

        if (infoText)
            infoText.text = $"Manche {round}";
        UpdateShotsUI();

        currentTarget = GetRandomPointInZone();

        if (human != null)
            StartCoroutine(human.RandomJumpInRound(roundDuration));

        while (t < roundDuration)
        {
            t += Time.deltaTime;
            moveTimer -= Time.deltaTime;

            if (timerText)
                timerText.text = $"{Mathf.Ceil(roundDuration - t)}";

            if (moveTimer <= 0f)
            {
                currentTarget = GetRandomPointInZone();
                moveTimer = moveInterval;
            }

            Vector3 nextPos = Vector3.Lerp(crosshair.position, currentTarget, Time.deltaTime * moveSpeed);
            if (isPerturbed) nextPos += (Vector3)Random.insideUnitCircle * 0.1f;

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
            infoText.text = $"Manche {round} terminée !";
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        if (!canShoot || shotsRemaining <= 0) return;
        shotsRemaining--;
        UpdateShotsUI();

        Vector2 origin = crosshair.position;
        Collider2D hit = Physics2D.OverlapPoint(origin);

        if (hit != null)
        {
            TargetZone zone = hit.GetComponent<TargetZone>();
            if (zone != null)
            {
                int pts = zone.GetScore(origin);
                playerScore += pts;

                if (human != null)
                    human.OnBitten();

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

        if (scoreText)
            scoreText.text = $"Score : {playerScore}";

        if (shotsRemaining <= 0)
        {
            canShoot = false;
            if (crosshair) crosshair.gameObject.SetActive(false);
            StartCoroutine(ReloadNextRound());
        }
    }

    private void UpdateShotsUI()
    {
        if (shotsText)
            shotsText.text = $"Tirs : {shotsRemaining}/{maxShotsPerRound}";
    }

    private IEnumerator ReloadNextRound()
    {
        if (infoText)
            infoText.text = "Manche terminée, préparation...";
        yield return new WaitForSeconds(1f);
        canShoot = true;
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

        if (targetZones.Count > 0 && Random.value < chanceToGoNearTarget)
        {
            TargetZone chosen = targetZones[Random.Range(0, targetZones.Count)];
            Vector3 around = chosen.transform.position;

            float offsetX = Random.Range(-offsetAroundTarget, offsetAroundTarget);
            float offsetY = Random.Range(-offsetAroundTarget, offsetAroundTarget);

            Vector3 nearTarget = around + new Vector3(offsetX, offsetY, 0);
            nearTarget.x = Mathf.Clamp(nearTarget.x, minX, maxX);
            nearTarget.y = Mathf.Clamp(nearTarget.y, minY, maxY);
            return nearTarget;
        }

        float x = Random.Range(minX, maxX);
        float y = Random.Range(minY, maxY);
        return new Vector3(x, y, crosshair.position.z);
    }
}
