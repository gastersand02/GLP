using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGun : MonoBehaviour
{
    [SerializeField]
    Transform firingPoint;

    [SerializeField]
    GameObject bulletPrefab;

    [SerializeField]
    float firingRate;

    public static PlayerGun Instance;

    public float LastShot = 0;
    public float delayTime = 2.0f;

    GameManager gameManager;

    // Start is called before the first frame update
    private void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }
    void Awake()
    {
        Instance = GetComponent<PlayerGun>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Shooting()
    {
        if (LastShot + firingRate <= Time.time)
        {
            LastShot = Time.time;
            Instantiate(bulletPrefab, firingPoint.position, firingPoint.rotation);
            gameManager.AmmoCount -= 1;
        }
    }

    public void RapidShooting()
    {
        if (LastShot + firingRate <= Time.time)
        {
            firingRate = 0.1f;
            LastShot = Time.time;
            Instantiate(bulletPrefab, firingPoint.position, firingPoint.rotation);
            gameManager.AmmoCount -= 1;
        }
    }
}
