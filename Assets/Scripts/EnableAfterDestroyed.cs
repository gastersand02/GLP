using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Watch one or more scene GameObjects and, once ALL watched objects are destroyed (or null),
/// enable a configured set of target GameObjects.
/// </summary>
public class EnableAfterDestroyed : MonoBehaviour
{
    [Header("Watched objects (destroyed)")]
    [Tooltip("Objects to wait for destruction. When all of these become null (destroyed), the targets will be enabled.")]
    public GameObject[] watchedObjects;

    [Header("Targets to enable")]
    [Tooltip("GameObjects to SetActive(true) when watched objects are destroyed.")]
    public GameObject[] objectsToEnable;

    [Header("Options")]
    [Tooltip("Start monitoring automatically on Start().")]
    public bool startOnStart = true;
    [Tooltip("Optional delay (seconds) after last watched object destroyed before enabling targets.")]
    public float delayBeforeEnable = 0.0f;
    [Tooltip("If true the watcher GameObject (this) will be destroyed after enabling targets.")]
    public bool destroyWatcherAfterEnable = false;
    [Tooltip("Poll interval in seconds while checking destruction (small values cost more CPU).")]
    public float pollInterval = 0.2f;

    [Header("Optional")]
    [Tooltip("Event invoked right after enabling targets (useful to hook other systems).")]
    public UnityEvent onEnabled;

    private Coroutine _monitorCoroutine;

    void Start()
    {
        if (startOnStart)
            StartMonitoring();
    }

    /// <summary>
    /// Begin monitoring the watchedObjects. Safe to call multiple times (will restart).
    /// </summary>
    public void StartMonitoring()
    {
        if (_monitorCoroutine != null)
            StopCoroutine(_monitorCoroutine);

        _monitorCoroutine = StartCoroutine(MonitorAndEnable());
    }

    /// <summary>
    /// Stop monitoring if running.
    /// </summary>
    public void StopMonitoring()
    {
        if (_monitorCoroutine != null)
        {
            StopCoroutine(_monitorCoroutine);
            _monitorCoroutine = null;
        }
    }

    private IEnumerator MonitorAndEnable()
    {
        // If no watched objects provided, enable immediately
        if (watchedObjects == null || watchedObjects.Length == 0)
        {
            yield return new WaitForSeconds(delayBeforeEnable);
            EnableTargets();
            yield break;
        }

        // Keep checking until all watched objects are null (destroyed)
        while (true)
        {
            bool anyAlive = false;
            for (int i = 0; i < watchedObjects.Length; i++)
            {
                var go = watchedObjects[i];
                if (go != null)
                {
                    anyAlive = true;
                    break;
                }
            }

            if (!anyAlive)
            {
                // All destroyed (null)
                if (delayBeforeEnable > 0f)
                    yield return new WaitForSeconds(delayBeforeEnable);

                EnableTargets();
                yield break;
            }

            yield return new WaitForSeconds(Mathf.Max(0.01f, pollInterval));
        }
    }

    private void EnableTargets()
    {
        if (objectsToEnable != null)
        {
            foreach (var t in objectsToEnable)
            {
                if (t != null)
                    t.SetActive(true);
            }
        }

        onEnabled?.Invoke();

        if (destroyWatcherAfterEnable)
            Destroy(this.gameObject);
    }

    // Editor helper: call from inspector context menu
    [ContextMenu("Force Enable Targets Now")]
    private void ForceEnableNow()
    {
        StopMonitoring();
        EnableTargets();
    }
}