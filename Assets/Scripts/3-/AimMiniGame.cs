using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class AimMiniGame : MonoBehaviour
{
    [Header("Références multi")]
    public PlayerData linkedPlayer; // Le joueur associé à cette instance

    [Header("Références UI & zone")]
    public Transform zoneTopLeft;
    public Transform zoneBottomRight;
    public Transform crosshair;
    public HumanTarget human;
    public TMP_Text infoText;
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public TMP_Text shotsText;

    [Header("UI Visuelle des piqûres")]
    public GameObject mosquitoIconPrefab;
    public Transform bitesContainer;
    public int maxIcons = 3;
    private List<GameObject> mosquitoIcons = new List<GameObject>();

    [Header("Paramètres de jeu")]
    public float moveSpeed = 3f;
    public float moveInterval = 1.2f;
    public float roundDuration = 5f;
    public int totalRounds = 3;
    public int maxShotsPerRound = 3;
    public float shootCooldown = 0.5f;

    [Header("Zones piquables")]
    public List<TargetZone> targetZones = new List<TargetZone>();
    [Range(0f, 1f)] public float chanceToGoNearTarget = 0.75f;
    public float offsetAroundTarget = 0.5f;

    private bool isPerturbed = false;
    private bool canShoot = true;
    private float playerScore = 0;

    private Vector3 currentTarget;
    private float moveTimer = 0f;
    private int shotsRemaining;
    private int currentRound = 1;
    private float lastShootTime = -999f;

    private SpriteRenderer crosshairRenderer;
    private Color crosshairBaseColor;

    private PlayerInput playerInput; // Référence au PlayerInput du joueur
    private InputAction shootAction;
    private InputAction sabotageAction;

    private void Start()
    {
        if (linkedPlayer == null)
        {
            Debug.LogError("Aucun PlayerData lié à ce mini-jeu !");
            enabled = false;
            return;
        }

        // Associer le PlayerInput du joueur
        playerInput = linkedPlayer.playerInputPV;
        var map = playerInput.actions.FindActionMap("MiniGame_Aim", true);
        map.Enable();

        shootAction = map.FindAction("Shoot", true);
        sabotageAction = map.FindAction("Sabotage", true);

        shootAction.performed += OnShoot;
        sabotageAction.performed += OnSabotage;

        crosshairRenderer = crosshair.GetComponent<SpriteRenderer>();
        if (crosshairRenderer != null)
            crosshairBaseColor = crosshairRenderer.color;

        SetupMosquitoIcons();
        StartCoroutine(GameLoop());
    }

    private void OnDestroy()
    {
        if (shootAction != null) shootAction.performed -= OnShoot;
        if (sabotageAction != null) sabotageAction.performed -= OnSabotage;
    }

    // ------------------------ GAMEPLAY ------------------------

    private void SetupMosquitoIcons()
    {
        if (mosquitoIconPrefab == null || bitesContainer == null) return;

        for (int i = 0; i < maxIcons; i++)
        {
            GameObject icon = Instantiate(mosquitoIconPrefab, bitesContainer);
            mosquitoIcons.Add(icon);
        }
    }

    private void UpdateBiteIcons()
    {
        for (int i = 0; i < mosquitoIcons.Count; i++)
            mosquitoIcons[i].SetActive(i < shotsRemaining);
    }

    private IEnumerator GameLoop()
    {
        if (infoText) infoText.text = $"{linkedPlayer.name} se prépare...";
        yield return new WaitForSeconds(1.2f);
        if (infoText) infoText.text = "";

        for (currentRound = 1; currentRound <= totalRounds; currentRound++)
        {
            yield return StartCoroutine(PlayRound(currentRound));
            yield return new WaitForSeconds(1f);
        }

        if (infoText)
            infoText.text = $"Fin ! Score final : {playerScore}";
        if (scoreText)
            scoreText.text = $"Score : {playerScore}";

        yield return new WaitForSeconds(2f);

        // Envoie du score global au GameManager
        GameManager.instance.AddScoreToPlayer(linkedPlayer);
        GameManager.instance.NextMiniGame();
    }

    private IEnumerator PlayRound(int round)
    {
        shotsRemaining = maxShotsPerRound;
        canShoot = true;
        moveTimer = 0f;
        float t = 0f;

        if (crosshair) crosshair.gameObject.SetActive(true);
        if (infoText) infoText.text = $"Manche {round}";
        UpdateShotsUI();
        UpdateBiteIcons();

        currentTarget = GetRandomPointInZone();

        if (human != null)
            human.StartJumpsForRound(roundDuration);

        while (t < roundDuration)
        {
            t += Time.deltaTime;
            moveTimer -= Time.deltaTime;

            if (timerText)
                timerText.text = $"{Mathf.Ceil(roundDuration - t)} sec";

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

    // ------------------------ ACTIONS ------------------------

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        if (!canShoot || shotsRemaining <= 0) return;

        if (Time.time - lastShootTime < shootCooldown)
        {
            if (infoText) infoText.text = "Recharge...";
            return;
        }

        lastShootTime = Time.time;
        shotsRemaining--;
        UpdateShotsUI();
        UpdateBiteIcons();

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

                StartCoroutine(FlashCrosshair(Color.red));
            }
            else
            {
                if (infoText) infoText.text = "Raté !";
                StartCoroutine(FlashCrosshair(Color.gray));
            }
        }
        else
        {
            if (infoText) infoText.text = "Raté !";
            StartCoroutine(FlashCrosshair(Color.gray));
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

    private IEnumerator FlashCrosshair(Color flashColor, float duration = 0.15f)
    {
        if (crosshairRenderer == null) yield break;
        crosshairRenderer.color = flashColor;
        yield return new WaitForSeconds(duration);
        crosshairRenderer.color = crosshairBaseColor;
    }

    private void UpdateShotsUI()
    {
        if (shotsText)
            shotsText.text = $"{shotsRemaining} piqûres";
    }

    private IEnumerator ReloadNextRound()
    {
        if (infoText) infoText.text = "Préparation...";
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
