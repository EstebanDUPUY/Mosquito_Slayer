using UnityEngine;
using System.Collections;

public class HumanAttack : MonoBehaviour
{
    // Garantir un seul état visuel à la fois
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
    [SerializeField] SuccManager manager; //on récupère le chef d'orchestre

    // Garde-fous anti chevauchement
    public bool IsBusy { get; private set; } // séquence en cours
    public bool Cooldown { get; private set; } // en récupération
    private int _seqToken = 0;                 // jeton pour invalider proprement une ancienne séquence

    Coroutine seqCo;

    [SerializeField] LaserAttack laser;  // glisse ton GO LaserAttack


    void Start()
    {
        SetState(VisState.Idle);
        if (!manager) manager = FindAnyObjectByType<SuccManager>();
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


        SetState(VisState.Idle);

        // 1) petit délai avant de montrer l'alerte
        yield return new WaitForSeconds(Random.Range(preAlertWait.x, preAlertWait.y));

        // 2) ALERTE (montre "Human will attack")
        SetState(VisState.Alert);
        float alertDur = Random.Range(alertTime.x, alertTime.y);
        yield return new WaitForSeconds(alertDur);

        // feinte : revenir Idle sans attaquer
        if (Random.value < feintChance)
        {
            SetState(VisState.Idle);
            yield return StartCooldown();
            IsBusy = false; seqCo = null;
            yield break;
        }

        // 4) ATTAQUE
        SetState(VisState.Attack);

        if (laser != null && manager != null && targetIndex >= 0 && targetIndex < manager.players.Length)
        {
            PlayerSucc tgt = manager.players[targetIndex];
            if (tgt != null)
            {
                float xSnap = tgt.transform.position.x; // c'est la position X du joueur visé

                // Si on a  bien la méthode dans LaserAttack :
                float travel = laser.EstimateTravelTimeFromCurrentStartY();

                laser.FireAtX(xSnap);  //sert à tirer dans l'axe X du joueur
                yield return new WaitForSeconds(travel + 0.05f);
            }
        }
        else
        {
            yield return new WaitForSeconds(Random.Range(fireTime.x, fireTime.y));
        }

        // sécurité
        if (laser && laser.Active) laser.StopNow(); //on coupe le laser par sécurité

        SetState(VisState.Idle);
        yield return StartCooldown();
        IsBusy = false; seqCo = null;
    }

    IEnumerator StartCooldown() //on met en repos, puis on réattaque après la fin du cooldown
    {
        Cooldown = true;
        yield return new WaitForSeconds(Random.Range(cooldownTime.x, cooldownTime.y));
        Cooldown = false;
    }

    // ————— Helpers —————

    bool IsCurrent(int token) => token == _seqToken; //on vérifie que la séquence est toujours valide, sinon on annule

    void SetState(VisState st)
    {
        //Une seule image active à la fois
        if (idleImage) idleImage.SetActive(st == VisState.Idle);
        if (alertImage) alertImage.SetActive(st == VisState.Alert);
        if (attackImage) attackImage.SetActive(st == VisState.Attack);
    }
}
