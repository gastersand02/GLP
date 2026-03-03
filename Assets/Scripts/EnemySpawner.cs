using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefab / Pool")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Spawn Area / Points")]
    [Tooltip("If set, these transforms are used as spawn positions.")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("When true and spawnPoints are set, a random point from the array is used.")]
    [SerializeField] private bool useRandomSpawnPoint = true;
    [Tooltip("When no spawn points are provided, a random position inside this object's XZ circle is used.")]
    [SerializeField] private float spawnAreaRadius = 5f;

    [Header("Timing / Limits")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxAlive = 6;
    [SerializeField] private int initialSpawn = 0;

    [Header("Spawn Safety")]
    [Tooltip("Layers considered blockers for spawn position (e.g. walls, other objects).")]
    [SerializeField] private LayerMask spawnBlockerLayers = 0;
    [SerializeField] private float spawnCheckRadius = 0.5f;
    [SerializeField] private int spawnCheckAttempts = 6;

    [Header("NavMesh")]
    [Tooltip("Max distance to sample the NavMesh from desired spawn position.")]
    [SerializeField] private float navMeshSampleDistance = 2f;

    [Header("Control")]
    [Tooltip("If false, spawner will not spawn until unlocked at runtime.")]
    [SerializeField] private bool unlocked = true;
    [Tooltip("Optional: assign player so spawned enemies can be given a direct reference (EnemyAI will auto-find if null).")]
    [SerializeField] private Transform playerReference;

    [Header("Kill requirement (optional)")]
    [Tooltip("If > 0 the spawner will count enemy deaths and stop/destroy itself when this many have been killed.")]
    [SerializeField] private int requiredKillCount = 0;
    [Tooltip("If true the spawner GameObject will be destroyed when the required kill count is reached.")]
    [SerializeField] private bool destroySpawnerOnComplete = true;
    [Tooltip("Delay in seconds before destroying the spawner GameObject after requirement is met.")]
    [SerializeField] private float destroySpawnerDelay = 0f;

    [Header("UI (optional)")]
    [Tooltip("Optional TMP_Text to display currentKills/requiredKills (assign in Inspector).")]
    public TMP_Text killCounterText;
    [Tooltip("Format used to display kills. {0}=current, {1}=required")]
    public string killTextFormat = "{0}/{1}";

    private readonly List<GameObject> _alive = new List<GameObject>();
    private Coroutine _spawnRoutine;

    // runtime tracking of kills
    private int _killCount = 0;

    void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] enemyPrefab is not assigned. Spawner disabled.", this);
            enabled = false;
            return;
        }

        UpdateKillUI();

        if (spawnOnStart && unlocked)
        {
            // spawn initial burst
            for (int i = 0; i < initialSpawn; i++)
                TrySpawn();

            _spawnRoutine = StartCoroutine(SpawnLoop());
        }
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            PruneDead();

            // If a required kill count exists and already reached, do not spawn anymore
            if (requiredKillCount > 0 && _killCount >= requiredKillCount)
            {
                // stop spawning loop
                _spawnRoutine = null;
                yield break;
            }

            if (unlocked && _alive.Count < maxAlive)
            {
                TrySpawn();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // Attempt to spawn one enemy, returns true if spawned
    public bool TrySpawn()
    {
        if (enemyPrefab == null) return false;

        PruneDead();
        if (_alive.Count >= maxAlive) return false;

        Vector3 spawnPos;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            if (useRandomSpawnPoint)
                spawnPos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
            else
            {
                // deterministic: cycle through points by choosing index = mod of current alive count
                int idx = _alive.Count % spawnPoints.Length;
                spawnPos = spawnPoints[idx].position;
            }
        }
        else
        {
            // random point inside spawner's XZ circle
            spawnPos = transform.position + Random.insideUnitSphere * spawnAreaRadius;
            spawnPos.y = transform.position.y;
        }

        // Sample the NavMesh to snap spawn position onto walkable NavMesh
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(spawnPos, out navHit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            spawnPos = navHit.position;
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] Could not find nearby NavMesh for spawn position, skipping spawn.", this);
            return false;
        }

        // verify position isn't blocked (optional)
        bool good = false;
        for (int i = 0; i < spawnCheckAttempts; i++)
        {
            Vector3 checkPos = spawnPos;
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                // jitter attempts inside radius to try to find valid spot
                checkPos = transform.position + Random.insideUnitSphere * spawnAreaRadius;
                checkPos.y = transform.position.y;

                // try to snap jittered pos to navmesh too
                if (NavMesh.SamplePosition(checkPos, out navHit, navMeshSampleDistance, NavMesh.AllAreas))
                    checkPos = navHit.position;
            }

            Collider[] hits = Physics.OverlapSphere(checkPos, spawnCheckRadius, spawnBlockerLayers);
            if (hits.Length == 0)
            {
                spawnPos = checkPos;
                good = true;
                break;
            }
        }

        if (!good && spawnBlockerLayers != 0)
        {
            // Could not find an unblocked point
            Debug.LogWarning("[EnemySpawner] Could not find valid spawn position, skipping spawn.", this);
            return false;
        }

        // Instantiate and register
        var go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        _alive.Add(go);
        go.transform.SetParent(transform); // keep hierarchy tidy

        // Try to assign player reference to spawned EnemyAI if available
        var enemyAI = go.GetComponent<EnemyAI>();
        if (enemyAI != null && playerReference != null)
        {
            if (enemyAI.player == null)
                enemyAI.player = playerReference;
        }

        // Optional: automatically prune when object is destroyed (best-effort)
        var tracker = go.AddComponent<SpawnerChildTracker>();
        tracker.Initialize(this, go);

        return true;
    }

    public void StartSpawning()
    {
        if (_spawnRoutine == null)
            _spawnRoutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }

    public void Unlock() => unlocked = true;
    public void Lock()
    {
        unlocked = false;
    }

    public int GetAliveCount()
    {
        PruneDead();
        return _alive.Count;
    }

    // Called by child tracker when destroyed
    internal void Unregister(GameObject child)
    {
        _alive.Remove(child);

        // Count the death toward requiredKillCount if configured
        if (requiredKillCount > 0)
        {
            _killCount++;
            Debug.Log($"[EnemySpawner] Registered kill {_killCount}/{requiredKillCount}", this);

            UpdateKillUI();

            if (_killCount >= requiredKillCount)
            {
                Debug.Log("[EnemySpawner] Required kill count reached. Stopping spawner.", this);
                StopSpawning();
                // Optionally destroy the spawner GameObject after a delay
                if (destroySpawnerOnComplete)
                {
                    // destroy the optional killCounterText GameObject as well (if assigned)
                    if (killCounterText != null)
                    {
                        if (destroySpawnerDelay <= 0f)
                            Destroy(killCounterText.gameObject);
                        else
                            Destroy(killCounterText.gameObject, destroySpawnerDelay);
                    }

                    if (destroySpawnerDelay <= 0f)
                        Destroy(this.gameObject);
                    else
                        Destroy(this.gameObject, destroySpawnerDelay);
                }
            }
        }
        else
        {
            // still update UI to show current kills even if required not set
            UpdateKillUI();
        }
    }

    private void UpdateKillUI()
    {
        if (killCounterText == null) return;
        int req = Mathf.Max(0, requiredKillCount);
        killCounterText.text = string.Format(killTextFormat, _killCount, req);
    }

    private void PruneDead()
    {
        _alive.RemoveAll(item => item == null);
    }

    // Editor helper
    void OnValidate()
    {
        if (spawnInterval < 0.1f) spawnInterval = 0.1f;
        if (spawnAreaRadius < 0f) spawnAreaRadius = 0f;
        if (maxAlive < 1) maxAlive = 1;
        if (spawnCheckRadius < 0f) spawnCheckRadius = 0f;
        if (spawnCheckAttempts < 1) spawnCheckAttempts = 1;
        if (requiredKillCount < 0) requiredKillCount = 0;
        if (destroySpawnerDelay < 0f) destroySpawnerDelay = 0f;
    }

    // Small helper component attached to spawned enemies to inform the spawner when they are destroyed.
    private class SpawnerChildTracker : MonoBehaviour
    {
        private EnemySpawner _parent;
        private GameObject _me;

        public void Initialize(EnemySpawner parent, GameObject me)
        {
            _parent = parent;
            _me = me;
        }

        void OnDestroy()
        {
            if (_parent != null && _me != null)
                _parent.Unregister(_me);
        }
    }
}
