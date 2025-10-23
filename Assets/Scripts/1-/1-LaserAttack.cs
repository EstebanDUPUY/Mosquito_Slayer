using UnityEngine;

public class LaserAttack : MonoBehaviour
{
    [Header("Refs")]
    public SuccManager manager; //on récupère le chef d'orchestre
    [SerializeField] Transform ground;   // Là où le laser doit s'arrêter
    [SerializeField] Transform origin;   // Le point de départ du laser

    [Header("Move")]
    [SerializeField] float fallSpeed = 12f; //vitesse de chute, plus c'est grand, plus c'est rapide
    [SerializeField] float startYOffset = 0f; //décalage vertical de départ
    [SerializeField] float startXOffset = 0f; //décalage horizontal de départ
    [SerializeField] float stopAboveGround = 0.05f; //on s'arrête un peu au-dessus du sol

    public bool Active { get; private set; } //dit si le laser est actif ou non

    float _stopY; //la hauteur où le laser doit s'arrêter
    float _lockedX; //le X verrouillé pendant la chute du laser
    Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        }
    }

    public void FireAtX(float xSnapshot)
    {
        if (Active) return; //si le laser est activé, on ne relance pas

        transform.SetParent(null, true); //on le détache de son parent

        _lockedX = xSnapshot + startXOffset;   //  on fige le X une seule fois au moment du tir

        float startY = (origin ? origin.position.y : transform.position.y) + startYOffset; //on calcule d'où part le laser en Y, soit depuis l'origin soit depuis la position de départ avec un décalage potentiel
        _stopY = ground ? ground.position.y + stopAboveGround : startY - 5f; //on calcule où s'arrêter pour le laser (au niveau du sol + un petit offset, ou 5 unités plus bas que le départ s'il n'y a pas de ground)

        transform.position = new Vector3(_lockedX, startY, transform.position.z); //on place le laser à sa position de départ en fixant X et Y

        if (_rb)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        }

        gameObject.SetActive(true);
        Active = true;
    }


    public void StopNow() //on arrête le laser immédiatement et on le désactive
    {
        Active = false;
        if (_rb) { _rb.linearVelocity = Vector2.zero; _rb.angularVelocity = 0f; }
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!Active) return;

        var p = transform.position;
        float ny = Mathf.MoveTowards(p.y, _stopY, fallSpeed * Time.deltaTime); //on descend en Y vers la position d'arrêt à la vitesse définie

        //On force le X à rester = _lockedX ici (pas le X courant)
        transform.position = new Vector3(_lockedX, ny, p.z);

        if (Mathf.Abs(ny - _stopY) <= 0.0001f) //si on a atteint la position d'arrêt, on stoppe le laser
            StopNow();
    }


    void LateUpdate()
    {
        if (!Active) return;
        // Si on modifie le X après Update ou un autre script, on le ré-impose ici par mesure de sécurité
        var p = transform.position;
        transform.position = new Vector3(_lockedX, p.y, p.z);
    }

    //Si le laser entre en collision avec quelque chose, on vérifie si c'est un joueur
    void OnTriggerEnter2D(Collider2D o) { TryHit(o); }
    void OnTriggerStay2D(Collider2D o) { TryHit(o); }

    void TryHit(Collider2D other) //Si c'est bien un joueur et qu'il est en train de sucer, on notifie le manager pour dire qu'il a perdu et on arrête le laser
    {
        if (!Active) return;
        var p = other.GetComponent<PlayerSucc>();
        if (p == null || manager == null) return;

        if (p.IsSuccing)
        {
            manager.OnLaserHit(p);
            StopNow();
        }
    }

    public float EstimateTravelTimeFromCurrentStartY()
    {
        float startY = (origin ? origin.position.y : transform.position.y) + startYOffset;
        float stopY = ground ? ground.position.y + stopAboveGround : startY - 5f;
        float dist = Mathf.Max(0f, startY - stopY);
        return dist / Mathf.Max(0.01f, fallSpeed);
    }
}
