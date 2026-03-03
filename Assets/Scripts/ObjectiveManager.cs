using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [Header("Prefab / Spawning")]
    [Tooltip("Objective circle prefab (must contain ObjectiveCircle component and a Slider assigned).")]
    public GameObject objectivePrefab;

    [Tooltip("Optional spawn points. If empty, the manager's position will be used.")]
    public Transform[] spawnPoints;

    [Tooltip("How many objective circles to spawn on Start (uses spawnPoints if provided).")]
    public int spawnCount = 0;

    [Header("Objective Requirements")]
    [Tooltip("How many completed circles required to declare objective complete.")]
    public int requiredToComplete = 1;

    [Header("UI Popup")]
    [Tooltip("Panel that contains the completion message (will be shown when required complete).")]
    public GameObject completionPanel;

    [Tooltip("Text element inside completionPanel (use TMP_Text).")]
    public TMP_Text completionText;

    [Tooltip("Optional: button inside the panel can call DismissCompletionPopup() to hide.")]
    public Button completionDismissButton;

    [Header("Audio")]
    [Tooltip("Sound to play when a circle completes.")]
    public AudioClip objectiveCompleteClip;
    [Tooltip("Volume when playing objectiveCompleteClip.")]
    [Range(0f, 1f)]
    public float objectiveCompleteVolume = 1f;

    [Header("Auto-destroy Manager (optional)")]
    [Tooltip("If true, this ObjectiveManager GameObject will be destroyed after the required objectives are completed.")]
    public bool destroyManagerOnComplete = false;
    [Tooltip("Delay in seconds before destroying the manager GameObject after completion popup shown.")]
    public float destroyManagerDelay = 0f;

    private readonly List<ObjectiveCircle> registered = new List<ObjectiveCircle>();
    private int completedCount;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this.gameObject);
        else
            Instance = this;

        if (completionPanel != null)
            completionPanel.SetActive(false);

        if (completionDismissButton != null)
            completionDismissButton.onClick.AddListener(DismissCompletionPopup);
    }

    void Start()
    {
        // Spawn prefab instances if requested
        if (objectivePrefab == null)
        {
            Debug.LogWarning("[ObjectiveManager] objectivePrefab not assigned.");
            return;
        }

        if (spawnCount > 0)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 pos;
                if (spawnPoints != null && spawnPoints.Length > 0)
                {
                    Transform t = spawnPoints[i % spawnPoints.Length];
                    pos = t.position;
                }
                else
                {
                    // spread them randomly around manager position
                    pos = transform.position + (Vector3)(Random.insideUnitCircle * 3f);
                    pos.y = transform.position.y;
                }

                // Preserve the prefab's original rotation when instantiating.
                Quaternion prefabRotation = objectivePrefab.transform.rotation;
                var go = Instantiate(objectivePrefab, pos, prefabRotation, transform);
                // manager registration is handled by ObjectiveCircle on Start()
            }
        }
    }

    // Called by ObjectiveCircle when it starts (auto registration)
    internal void Register(ObjectiveCircle circle)
    {
        if (!registered.Contains(circle))
            registered.Add(circle);
    }

    // Called by ObjectiveCircle when it is completed
    internal void NotifyCompleted(ObjectiveCircle circle)
    {
        if (registered.Contains(circle))
            registered.Remove(circle);

        completedCount++;
        // play sound
        if (objectiveCompleteClip != null)
            AudioSource.PlayClipAtPoint(objectiveCompleteClip, circle.transform.position, objectiveCompleteVolume);

        // Optionally show per-circle UI — handled by circle itself.
        Debug.Log($"[ObjectiveManager] Objective circle completed. Total completed: {completedCount}/{requiredToComplete}", this);

        if (completedCount >= requiredToComplete)
        {
            ShowCompletionPopup();

            // Optionally destroy the manager GameObject after a delay
            if (destroyManagerOnComplete)
            {
                if (destroyManagerDelay <= 0f)
                    Destroy(this.gameObject);
                else
                    Destroy(this.gameObject, destroyManagerDelay);
            }
        }
    }

    void ShowCompletionPopup()
    {
        if (completionPanel == null)
        {
            Debug.Log("[ObjectiveManager] Objective complete. No completionPanel assigned. (Set completionPanel to a UI panel in the inspector)", this);
            return;
        }

        if (completionText != null)
            completionText.text = "Objective complete! Click to dismiss.";

        completionPanel.SetActive(true);
    }

    public void DismissCompletionPopup()
    {
        if (completionPanel != null)
            completionPanel.SetActive(false);
    }

    // Optional helper to get how many are left
    public int GetRemainingToComplete()
    {
        return Mathf.Max(0, requiredToComplete - completedCount);
    }
}
