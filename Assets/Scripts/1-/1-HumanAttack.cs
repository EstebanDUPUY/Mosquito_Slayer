using UnityEngine;
using System.Collections;

public class HumanAttack : MonoBehaviour
{
    // Petit FSM pour garantir un seul état visuel à la fois
    private enum VisState { Idle, Alert, Attack }

    [Header("Images")]
    [SerializeField] GameObject idleImage;
    [SerializeField] GameObject alertImage;
    [SerializeField] GameObject attackImage;

    [Header("Timings (aléatoires)")]
    [SerializeField] Vector2 preAlertWait = new Vector2(0.15f, 0.6f);  // attente avant alerte
    [SerializeField] Vector2 alertTime = new Vector2(0.35f, 1.1f);  // durée alerte
    [SerializeField] Vector2 fireTime = new Vector2(0.15f, 0.35f); // durée attaque
    [SerializeField] Vector2 cooldownTime = new Vector2(0.4f, 0.9f);   // CD après séquence
    [SerializeField, Range(0f, 1f)] float feintChance = 0.25f;          // % de feinte (alerte puis rien)

    [Header("Gameplay (option)")]
    [SerializeField] SuccManager manager;

    // Garde-fous anti chevauchement
    public bool IsBusy { get; private set; } // séquence en cours
    public bool Cooldown { get; private set; } // en récupération
    private int _seqToken = 0;                 // jeton pour invalider proprement une ancienne séquence

    Coroutine seqCo;

    [SerializeField] LaserAttack laser;  // glisse ton GO LaserAttack


    void Start()
    {
        SetState(VisState.Idle);
        if (!manager) manager = FindObjectOfType<SuccManager>();
        if (laser) laser.gameObject.SetActive(false);

    }

    public void PlayAttackSequence(int targetIndex)
    {
        // protège d’une rafale et empêche le chevauchement des états
        if (IsBusy || Cooldown) return;

        // démarrer une nouvelle séquence annule implicitement l’ancienne via un nouveau token
        _seqToken++;
        if (seqCo != null) StopCoroutine(seqCo);
        seqCo = StartCoroutine(Sequence(_seqToken, targetIndex));
    }

    IEnumerator Sequence(int token, int targetIndex)
    {
        IsBusy = true;

        // petit pré-délai random pour casser le rythme
        yield return new WaitForSeconds(Random.Range(preAlertWait.x, preAlertWait.y));
        if (!IsCurrent(token)) yield break; // annulé pendant l’attente


        // FEINTE ?
        if (Random.value < feintChance)
        {
            SetState(VisState.Idle);
            yield return StartCooldown();
            IsBusy = false; seqCo = null;
            yield break;
        }

        SetState(VisState.Idle);

        // 1) petit délai avant de montrer l'alerte
        yield return new WaitForSeconds(Random.Range(preAlertWait.x, preAlertWait.y));

        // 2) ALERTE (montre "Human will attack")
        SetState(VisState.Alert);
        float alertDur = Random.Range(alertTime.x, alertTime.y);
        yield return new WaitForSeconds(alertDur);

        // 3) (facultatif) feinte : revenir Idle sans attaquer
        if (Random.value < feintChance)
        {
            SetState(VisState.Idle);
            yield return StartCooldown();
            IsBusy = false; seqCo = null;
            yield break;
        }

        // 4) ATTAQUE (montre image Attack) + laser qui tombe tout droit
        SetState(VisState.Attack);

        if (laser != null && manager != null && targetIndex >= 0 && targetIndex < manager.players.Length)
        {
            var target = manager.players[targetIndex].transform;
            float dur = Random.Range(fireTime.x, fireTime.y);   // fenêtre d'attaque
            laser.FireAt(target, dur);                          // ← appelle ton script unique LaserAttack
        }

        // attendre la fin de la fenêtre d’attaque
        yield return new WaitForSeconds(Random.Range(fireTime.x, fireTime.y));

        // 5) retour Idle + cooldown (le laser s’éteint seul via StopNow à la fin du FireAt)
        SetState(VisState.Idle);
        yield return StartCooldown();
        IsBusy = false; seqCo = null;

    }

    IEnumerator StartCooldown()
    {
        Cooldown = true;
        yield return new WaitForSeconds(Random.Range(cooldownTime.x, cooldownTime.y));
        Cooldown = false;
    }

    // ————— Helpers —————

    bool IsCurrent(int token) => token == _seqToken;

    void SetState(VisState st)
    {
        // Exclusivité : une seule image active à la fois
        if (idleImage) idleImage.SetActive(st == VisState.Idle);
        if (alertImage) alertImage.SetActive(st == VisState.Alert);
        if (attackImage) attackImage.SetActive(st == VisState.Attack);
    }

    // Raccourcis si tu veux garder tes anciennes méthodes
    void SetIdle() => SetState(VisState.Idle);
    void SetAlert() => SetState(VisState.Alert);
    void SetAttack() => SetState(VisState.Attack);
}
