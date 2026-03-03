using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Vector3 firingPoint;

    [SerializeField]
    private float bulletSpeed;

    [SerializeField]
    private float maxBulletDistance;

    [SerializeField]
    private GameObject bulletHit; // assign in inspector; kept serialized for encapsulation

    EnemyAI enemy;

    // Start is called before the first frame update
    void Start()
    {
        firingPoint = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        MoveBullet();
    }

    void MoveBullet()
    {
        if (Vector3.Distance(firingPoint, transform.position) > maxBulletDistance)
        {
            Destroy(this.gameObject);
        }
        else
        {
            transform.Translate(Vector3.forward * bulletSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // Try to instantiate the assigned effect; if missing, attempt fallback from Resources or warn.
            if (bulletHit != null)
            {
                Instantiate(bulletHit, transform.position, transform.rotation);
            }
            else
            {
                Debug.LogWarning("Bullet.bulletHit is not assigned. Assign a hit prefab in the Inspector or add a 'BulletHit' prefab to a Resources folder.", this);
                GameObject fallback = Resources.Load<GameObject>("BulletHit");
                if (fallback != null)
                {
                    Instantiate(fallback, transform.position, transform.rotation);
                }
            }

            Destroy(this.gameObject);
        }
    }
}
