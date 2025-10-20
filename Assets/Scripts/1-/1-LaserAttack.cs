using UnityEngine;

public class LaserAttack : MonoBehaviour
{
    [Header("Refs")]
    public SuccManager manager;
    [SerializeField] Transform ground;   // Transform du GroundSuck (Y de référence)
    [SerializeField] Transform origin;   // optionnel: point de départ visuel

    [Header("Move")]
    [SerializeField] float fallSpeed = 12f;
    [SerializeField] float startYOffset = 0f;
    [SerializeField] float startXOffset = 0f;
    [SerializeField] float stopAboveGround = 0.05f;

    public bool Active { get; private set; }

    float _stopY;
    float _lockedX;
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
        if (Active) return;

        transform.SetParent(null, true);

        _lockedX = xSnapshot + startXOffset;   // ← applique l’offset une seule fois

        float startY = (origin ? origin.position.y : transform.position.y) + startYOffset;
        _stopY = ground ? ground.position.y + stopAboveGround : startY - 5f;

        transform.position = new Vector3(_lockedX, startY, transform.position.z);

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


    public void StopNow()
    {
        Active = false;
        if (_rb) { _rb.linearVelocity = Vector2.zero; _rb.angularVelocity = 0f; }
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!Active) return;

        var p = transform.position;
        float ny = Mathf.MoveTowards(p.y, _stopY, fallSpeed * Time.deltaTime);

        // ⬇️  IMPORTANT : on impose X = _lockedX ici (pas le X courant)
        transform.position = new Vector3(_lockedX, ny, p.z);

        if (Mathf.Abs(ny - _stopY) <= 0.0001f)
            StopNow();
    }


    void LateUpdate()
    {
        if (!Active) return;
        // 🔐 verrou monde : si quelqu’un a modifié X après Update, on le ré-impose ici
        var p = transform.position;
        transform.position = new Vector3(_lockedX, p.y, p.z);
    }

    void OnTriggerEnter2D(Collider2D o) { TryHit(o); }
    void OnTriggerStay2D(Collider2D o) { TryHit(o); }

    void TryHit(Collider2D other)
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
