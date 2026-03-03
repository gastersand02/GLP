using Ilumisoft.HealthSystem.UI;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class DropEntry
{
    [Tooltip("Prefab to spawn as a drop.")]
    public GameObject prefab;

    [Tooltip("Chance [0..1] this entry will drop (used as weight when choosing a single drop).")]
    [Range(0f, 1f)]
    public float chance = 0.2f;

    [Tooltip("Vertical offset applied to spawn position.")]
    public float spawnOffsetY = 0.0f;
}

public class EnemyAI : MonoBehaviour
{
    public NavMeshAgent enemy;
    public Transform player;
    public float chaseRange = 200f;
    public float attackRange = 10f;
    public float speed = 5f;
    public float damagePerSecond = 5f;
    public float enemyHealth = 100f;
    public float damage = 25f;
    public float damageHard = 50f;
    public float destroyDelay = 3f;

    private Animator animator;
    public GameObject deathFX;
    PlayerMovement PlayerMovement;
    private Rigidbody rb;

    [Header("Drops (simple single-drop)")]
    [Tooltip("Optional single prefab to drop. If assigned, this will be used instead of dropTable.")]
    public GameObject singleDropPrefab;
    [Tooltip("Chance (0..1) that the single prefab will drop.")]
    [Range(0f, 1f)]
    public float singleDropChance = 0.2f;

    [Header("Drops (weighted table fallback)")]
    [Tooltip("Configure possible drops. Exactly zero or one entry will be spawned on death.")]
    public List<DropEntry> dropTable = new List<DropEntry>();

    // internal flags to ensure exactly one drop attempt
    private bool _died = false;
    private bool _didDrop = false;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        enemy = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        // Ensure we have a player reference (falls back to tag lookup)
        if (player == null)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) player = playerGO.transform;
        }

        // Defensive checks & NavMeshAgent defaults
        if (enemy != null)
        {
            enemy.updateRotation = false;    // we rotate manually
            enemy.updatePosition = true;     // agent should still drive transform position
            enemy.speed = speed;

            // Only resume agent if it's on the NavMesh. Resuming when not on navmesh throws.
            if (enemy.isOnNavMesh)
            {
                enemy.isStopped = false;
            }
            else
            {
                enemy.isStopped = true;
                Debug.LogWarning("[EnemyAI] NavMeshAgent is not on a NavMesh. Agent kept stopped until placed on NavMesh.", this);
            }
        }

        if (rb != null)
        {
            // keep physics from interfering with NavMeshAgent movement
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }

        var pm = GameObject.FindGameObjectWithTag("Player");
        if (pm != null)
            PlayerMovement = pm.GetComponent<PlayerMovement>();

        // Quick diagnostics to help debugging in editor
        if (enemy != null)
            Debug.Log($"[EnemyAI] Start: agent.isOnNavMesh={enemy.isOnNavMesh}, speed={enemy.speed}, updatePosition={enemy.updatePosition}", this);
        if (player == null)
            Debug.LogError("[EnemyAI] Player transform not assigned - enemy won't move.", this);
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer <= chaseRange)
        {
            Chase();
        }
        else
        {
            animator.SetBool("IsRunning", false);
        }

        if (distToPlayer <= attackRange)
        {
            Attack();
        }
        else
        {
            animator.SetBool("IsAttack", false);
        }

        if (enemyHealth < 0)
        {
            Dead();
        }
    }

    void Chase()
    {
        if (enemy == null)
            return;

        if (!enemy.isOnNavMesh)
        {
            Debug.LogWarning("[EnemyAI] NavMeshAgent is not on a NavMesh. Bake NavMesh or move agent onto it.", this);
            return;
        }

        // Resume navigation
        enemy.isStopped = false;
        enemy.speed = speed;
        enemy.SetDestination(player.position);

        if (rb != null)
            rb.velocity = Vector3.zero;

        animator.SetBool("IsRunning", true);
        RotateTowards(player.position);
    }

    void RotateTowards(Vector3 target)
    {
        Vector3 direction = new Vector3(target.x - transform.position.x,
                                        0,
                                        target.z - transform.position.z);

        if (direction.sqrMagnitude > 0.001f)
            transform.forward = Vector3.Slerp(transform.forward, direction.normalized, Time.deltaTime * 10f);
    }

    private void Attack()
    {
        if (enemy != null)
        {
            // stop movement while attacking
            enemy.isStopped = true;
        }

        if (rb != null)
            rb.velocity = Vector3.zero;

        animator.SetBool("IsAttack", true);

        // Face the player while attacking
        RotateTowards(player.position);
    }

    private void Dead()
    {
        // mark as died so OnDestroy can know this was a death
        _died = true;

        animator.SetBool("IsDead", true);

        // Instantiate the death FX and destroy the INSTANCE after destroyDelay (do not destroy the prefab asset)
        if (deathFX != null)
        {
            var fx = Instantiate(deathFX, this.transform.position, this.transform.rotation);
            Destroy(fx, destroyDelay);
        }

        // schedule actual GameObject destruction (OnDestroy will run then and attempt drop once)
        Destroy(gameObject, 1f);

        if (enemy != null)
        {
            if (enemy.isOnNavMesh)
            {
                enemy.isStopped = true;
                enemy.SetDestination(transform.position);
            }
        }

        RotateTowards(transform.position);
        speed = 0f;
    }

    private void TrySpawnDrop()
    {
        if (_didDrop) return; // safety

        // If a singleDropPrefab is assigned, use simple chance
        if (singleDropPrefab != null)
        {
            if (UnityEngine.Random.value <= Mathf.Clamp01(singleDropChance))
            {
                Vector3 spawnPos = transform.position + Vector3.up * 0f;
                Instantiate(singleDropPrefab, spawnPos, Quaternion.identity);
                _didDrop = true;
            }
            return;
        }

        // Otherwise fallback to weighted table behavior (zero or one drop)
        if (dropTable == null || dropTable.Count == 0) return;

        // Build list of valid entries (prefab != null)
        List<DropEntry> valid = new List<DropEntry>();
        foreach (var e in dropTable)
            if (e != null && e.prefab != null)
                valid.Add(e);

        if (valid.Count == 0) return;

        // Sum chances as total drop probability
        float totalChance = 0f;
        foreach (var e in valid)
            totalChance += Mathf.Clamp01(e.chance);

        // Roll to determine if any drop occurs
        float roll = UnityEngine.Random.value; // 0..1
        if (totalChance <= 0f)
        {
            // No chances set -> do nothing (no drop)
            return;
        }

        float threshold = Mathf.Min(1f, totalChance);
        if (roll > threshold)
        {
            // No drop this time
            return;
        }

        // Weighted pick among valid entries using their chance as weight
        float pick = UnityEngine.Random.Range(0f, totalChance);
        float acc = 0f;
        DropEntry selection = null;
        foreach (var e in valid)
        {
            acc += Mathf.Clamp01(e.chance);
            if (pick <= acc)
            {
                selection = e;
                break;
            }
        }
        if (selection == null)
            selection = valid[valid.Count - 1];

        // Spawn exactly one of the chosen prefab
        if (selection != null && selection.prefab != null)
        {
            Vector3 basePos = transform.position;
            float radius = 0.2f;
            float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float rad = UnityEngine.Random.Range(0f, radius);
            Vector3 jitter = new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
            Vector3 spawnPos = basePos + jitter + Vector3.up * selection.spawnOffsetY;
            Instantiate(selection.prefab, spawnPos, Quaternion.identity);
            _didDrop = true;
        }
    }

    private void OnDestroy()
    {
        // Only try to spawn drop if this destroy was due to death (not scene unload, editor, etc.)
        if (_died && !Application.isEditor && !Application.isPlaying) return;

        // If died flag set, attempt one drop (guard ensures only one spawn)
        if (_died)
            TrySpawnDrop();
    }

    private void OnTriggerStay(Collider collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerMovement.Hit();
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Hit();
            return;
        }

        if (collision.CompareTag("Player"))
        {
            // When player touches the enemy, stop the agent and start attacking.
            if (enemy != null)
            {
                // Only stop if agent is valid
                if (enemy.isOnNavMesh)
                    enemy.isStopped = true;
            }

            if (rb != null)
                rb.velocity = Vector3.zero;

            animator.SetBool("IsAttack", true);
            RotateTowards(player.position);
            return;
        }

        // For other triggers, resume chasing
        Chase();
    }

    private void Hit()
    {
        // Apply damage
        enemyHealth -= damage;

        // Optional: play hit animation/feedback
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }

        // If dead, handle death
        if (enemyHealth <= 0f)
        {
            Dead();
            return;
        }

        // Clearing physics velocity to avoid sliding artifacts:
        if (rb != null)
            rb.velocity = Vector3.zero;
    }

    private IEnumerator TemporaryStun(float duration)
    {
        if (enemy == null) yield break;
        bool wasStopped = enemy.isStopped;
        enemy.isStopped = true;
        yield return new WaitForSeconds(duration);
        if (enemy != null)
            enemy.isStopped = wasStopped;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            animator.SetBool("IsAttack", false);

            // Resume navigation when player leaves
            if (enemy != null)
            {
                if (enemy.isOnNavMesh)
                    enemy.isStopped = false;
            }

            Chase();
        }
    }
}
