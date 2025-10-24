using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class CrosshairController : MonoBehaviour
{
    // Événements envoyés au manager
    public event Action OnShoot;
    public event Action OnSabotage;

    [Header("Réglages Input")]
    [SerializeField] private string actionMapName = "MiniGame_Aim"; // <-- Nom de ta map InputActions

    // Références injectées
    private Transform crosshair;
    private Transform zoneTL;
    private Transform zoneBR;
    private List<TargetZone> targetZones;
    private float chanceNearTarget;
    private float targetOffset;
    private float moveSpeed;
    private float moveInterval;

    // Input
    private PlayerInput playerInput;
    private InputAction shootAction;
    private InputAction sabotageAction;

    // Mouvement auto
    private Vector3 currentTarget;
    private float moveTimer;

    // Effets visuels
    private SpriteRenderer crosshairRenderer;
    private Color baseColor;
    private bool isPerturbed;
    private Coroutine perturbRoutine;

    // État
    private bool roundActive;
    private int playerIndex;

    public Vector2 CurrentPosition => crosshair ? (Vector2)crosshair.position : Vector2.zero;

    public void Init(
        int pIndex,
        Transform crosshairTransform,
        Transform zoneTL,
        Transform zoneBR,
        List<TargetZone> targetZonesRef,
        float chanceNearTarget,
        float targetOffset,
        float moveSpd,
        float moveInt)
    {
        this.playerIndex = pIndex;
        this.crosshair = crosshairTransform;
        this.zoneTL = zoneTL;
        this.zoneBR = zoneBR;
        this.targetZones = targetZonesRef;
        this.chanceNearTarget = chanceNearTarget;
        this.targetOffset = targetOffset;
        this.moveSpeed = moveSpd;
        this.moveInterval = moveInt;

        if (crosshair != null)
        {
            crosshairRenderer = crosshair.GetComponent<SpriteRenderer>();
            if (crosshairRenderer) baseColor = crosshairRenderer.color;
        }

        // Position initiale du viseur
        currentTarget = GetRandomPointInZone();
        if (crosshair) crosshair.position = currentTarget;
    }

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        // On récupère l'instance locale du contrôleur du joueur
        var localActions = playerInput.currentActionMap;

        if (localActions == null || localActions.name != actionMapName)
        {
            localActions = playerInput.actions.FindActionMap(actionMapName, true);
            playerInput.SwitchCurrentActionMap(actionMapName);
        }

        shootAction = localActions.FindAction("Shoot", true);
        sabotageAction = localActions.FindAction("Sabotage", true);

        shootAction.performed += OnShootPerformed;
        sabotageAction.performed += OnSabotagePerformed;
    }

    private void OnDisable()
    {
        if (shootAction != null) shootAction.performed -= OnShootPerformed;
        if (sabotageAction != null) sabotageAction.performed -= OnSabotagePerformed;
    }

    public void BeginRound()
    {
        roundActive = true;
        if (crosshair) crosshair.gameObject.SetActive(true);
        moveTimer = 0f;
    }

    public void EndRound()
    {
        roundActive = false;
        if (crosshair) crosshair.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!roundActive || crosshair == null) return;

        moveTimer -= Time.deltaTime;
        if (moveTimer <= 0f)
        {
            currentTarget = GetRandomPointInZone();
            moveTimer = moveInterval;
        }

        Vector3 nextPos = Vector3.Lerp(crosshair.position, currentTarget, Time.deltaTime * moveSpeed);
        if (isPerturbed) nextPos += (Vector3)UnityEngine.Random.insideUnitCircle * 0.1f;

        float minX = zoneTL.position.x;
        float maxX = zoneBR.position.x;
        float maxY = zoneTL.position.y;
        float minY = zoneBR.position.y;

        nextPos.x = Mathf.Clamp(nextPos.x, minX, maxX);
        nextPos.y = Mathf.Clamp(nextPos.y, minY, maxY);

        crosshair.position = nextPos;
    }

    private void OnShootPerformed(InputAction.CallbackContext ctx)
    {
        if (!roundActive) return;
        OnShoot?.Invoke();
    }

    private void OnSabotagePerformed(InputAction.CallbackContext ctx)
    {
        if (!roundActive) return;
        OnSabotage?.Invoke();
    }

    public void ApplyPerturbation(float duration)
    {
        if (perturbRoutine != null) StopCoroutine(perturbRoutine);
        perturbRoutine = StartCoroutine(Perturb(duration));
    }

    private IEnumerator Perturb(float duration)
    {
        isPerturbed = true;
        yield return new WaitForSeconds(duration);
        isPerturbed = false;
    }

    public IEnumerator Flash(Color c, float duration)
    {
        if (crosshairRenderer == null) yield break;
        var prev = crosshairRenderer.color;
        crosshairRenderer.color = c;
        yield return new WaitForSeconds(duration);
        crosshairRenderer.color = prev;
    }

    private Vector3 GetRandomPointInZone()
    {
        float minX = zoneTL.position.x;
        float maxX = zoneBR.position.x;
        float maxY = zoneTL.position.y;
        float minY = zoneBR.position.y;

        if (targetZones != null && targetZones.Count > 0 && UnityEngine.Random.value < chanceNearTarget)
        {
            TargetZone chosen = targetZones[UnityEngine.Random.Range(0, targetZones.Count)];
            Vector3 around = chosen.transform.position;

            float offsetX = UnityEngine.Random.Range(-targetOffset, targetOffset);
            float offsetY = UnityEngine.Random.Range(-targetOffset, targetOffset);

            Vector3 nearTarget = around + new Vector3(offsetX, offsetY, 0);
            nearTarget.x = Mathf.Clamp(nearTarget.x, minX, maxX);
            nearTarget.y = Mathf.Clamp(nearTarget.y, minY, maxY);
            return nearTarget;
        }

        float x = UnityEngine.Random.Range(minX, maxX);
        float y = UnityEngine.Random.Range(minY, maxY);
        return new Vector3(x, y, 0);
    }
}
