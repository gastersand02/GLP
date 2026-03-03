using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    GameManager gameManager;

    [Header("Audio")]
    [Tooltip("Clip played when health is picked up.")]
    public AudioClip pickupClip;
    [Range(0f, 1f)]
    public float pickupVolume = 1f;

    // Start is called before the first frame update
    private void Start()
    {
        var gm = GameObject.FindGameObjectWithTag("GameManager");
        if (gm != null)
            gameManager = gm.GetComponent<GameManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (gameManager != null)
            gameManager.Heal(50f);

        if (pickupClip != null)
        {
            Vector3 playPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(pickupClip, playPos, Mathf.Clamp01(pickupVolume));
        }

        Destroy(gameObject);
    }
}
