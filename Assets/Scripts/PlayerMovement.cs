using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    Rigidbody rb;
    [SerializeField] private float movementSpeed;
    [SerializeField] private float rotationSpeed = 10f;

    [SerializeField]
    GameObject muzzlePrefab;

    private Animator animator;

    GameManager gameManager;
    // Start is called before the first frame update
    void Start()
    {
        animator = gameObject.GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive)
        {
            // keep rotating if you want the player to still face cursor; remove Rotation() call if you want rotation blocked too.
            Rotation();

            // stop movement and shooting animations / muzzle
            movementSpeed = 0f;

        }

        Movement();
        Rotation();
        Shooting();
        
    }

    void Movement()
    {

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(horizontal, 0, vertical);
        

        if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
        {
            rb.velocity = movement * movementSpeed;
            animator.SetBool("IsRunning", true);
        }
        else
        {
            rb.velocity = Vector3.zero;
            animator.SetBool("IsRunning", false);
        }
    }

    void Rotation()
    {
        // Use a horizontal plane at the player's position for robust cursor-to-world mapping.
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 direction = hitPoint - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }

    void Shooting()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && gameManager.AmmoCount > 0)
        {
            if (rb.velocity != Vector3.zero)
            {
                PlayerGun.Instance.Shooting();
                movementSpeed = 4f;

                animator.SetBool("IsShooting", true);
                muzzlePrefab.SetActive(true);

                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");

                if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
                {
                    animator.SetBool("IsShooting", false);
                    animator.SetBool("IsRunningShoot", true);
                    animator.SetBool("IsRunning", false);
                    movementSpeed = 4f;
                }
            }
        }

        else
        {
            animator.SetBool("IsShooting", false);
            animator.SetBool("IsRunningShoot", false);
            movementSpeed = 8f;
        }

        if (Input.GetKey(KeyCode.Mouse0) && gameManager.AmmoCount > 0)
        {
            {
                PlayerGun.Instance.RapidShooting();
                movementSpeed = 4f;

                animator.SetBool("IsRapid", true);
                muzzlePrefab.SetActive(true);

                if (rb.velocity != Vector3.zero)
                {
                    animator.SetBool("IsRapid", false);
                    animator.SetBool("IsRunningRapid", true);
                    animator.SetBool("IsRunning", false);
                    movementSpeed = 4f;
                }
            }
        }


        else
        {
            animator.SetBool("IsRunningRapid", false);
            animator.SetBool("IsRapid", false);
            movementSpeed = 8f;
        }

        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            muzzlePrefab.SetActive(false);
        }
    }

    public void Hit()
    {
        EnemyAI enemy = GameObject.FindGameObjectWithTag("Enemy").GetComponent<EnemyAI>();

        gameManager.TakeDamage(Time.deltaTime * enemy.damagePerSecond);
    }



}
