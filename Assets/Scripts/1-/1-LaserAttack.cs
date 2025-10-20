using UnityEngine;

public class LaserAttack : MonoBehaviour
{
    [SerializeField] float speed = 8f;
    public SuccManager manager;

    Transform _target;
    bool _active;

    public void FireAt(Transform target)
    {
        _target = target;
        _active = true;
        gameObject.SetActive(true);
    }

    public void StopLaser()
    {
        _active = false;
        gameObject.SetActive(false);
        _target = null;
    }

    void Update()
    {
        if (!_active || _target == null) return;
        transform.position = Vector3.MoveTowards(transform.position, _target.position, speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_active) return;

        // si on touche un PlayerSucc, on délègue au manager
        var p = other.GetComponent<PlayerSucc>();
        if (p != null && manager != null)
        {
            manager.OnLaserHit(p);
        }
    }
}
