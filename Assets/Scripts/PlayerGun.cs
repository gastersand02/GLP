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

    [Header("Audio")]
    [Tooltip("Sound played when a shot is fired.")]
    public AudioClip shootClip;
    [Tooltip("Optional local AudioSource. If not assigned or present, PlayClipAtPoint is used.")]
    public AudioSource audioSource;

    [Header("Rapid fire")]
    [Tooltip("Rate used by rapid fire (seconds between shots).")]
    public float rapidFiringRate = 0.1f;

    public static PlayerGun Instance;

    public float LastShot = 0;
    public float delayTime = 2.0f;

    GameManager gameManager;

    // Start is called before the first frame update
    private void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        // try to auto-assign AudioSource if not set in inspector
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
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

            PlayShootSound();
        }
    }

    public void RapidShooting()
    {
        if (LastShot + rapidFiringRate <= Time.time)
        {
            LastShot = Time.time;
            Instantiate(bulletPrefab, firingPoint.position, firingPoint.rotation);
            gameManager.AmmoCount -= 1;

            PlayShootSound();
        }
    }

    void PlayShootSound()
    {
        if (shootClip == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(shootClip);
        }
        else if (Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(shootClip, Camera.main.transform.position, 1f);
        }
        else
        {
            AudioSource.PlayClipAtPoint(shootClip, firingPoint != null ? firingPoint.position : transform.position, 1f);
        }
    }
}
