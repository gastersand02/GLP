using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Collider))]
public class ObjectiveCircle : MonoBehaviour
{
    [Header("Fill settings")]
    [Tooltip("Seconds the player must stay inside to complete this circle.")]
    public float fillDuration = 3f;

    [Tooltip("If true the fill is shown as a slider UI (assign below).")]
    public bool showProgressUI = true;

    [Tooltip("Optional: Slider to show progress (0..1). Assign a UI Slider in the prefab.")]
    public Slider progressBar;

    [Tooltip("Optional small label above slider (TMP).")]
    public TMP_Text progressLabel;

    [Header("Feedback")]
    [Tooltip("Clip to play when this circle completes (manager will also play global clip).")]
    public AudioClip completeClip;
    [Range(0f, 1f)]
    public float completeVolume = 1f;

    [Tooltip("Destroy this GameObject when completed.")]
    public bool destroyOnComplete = true;

    private bool isPlayerInside;
    private float elapsed;
    private bool completed;

    // Tracks whether the progress UI has been shown after the player started progress
    private bool progressVisible = false;

    void Start()
    {
        // ensure collider is trigger
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("[ObjectiveCircle] Collider is not set to isTrigger — setting isTrigger = true.", this);
            col.isTrigger = true;
        }

        // register with manager if present
        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.Register(this);

        // initialize UI: start hidden until the player actually begins progress
        if (showProgressUI)
        {
            SetProgressUIActive(false);
        }

        UpdateUI(0f);
    }

    void Update()
    {
        if (completed) return;

        if (isPlayerInside)
        {
            // advance progress
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fillDuration);

            // show the progress UI only after progress has started (small threshold to avoid instant show on enter if desired)
            if (!progressVisible && elapsed > 0.0001f)
            {
                progressVisible = true;
                if (showProgressUI)
                    SetProgressUIActive(true);
            }

            UpdateUI(t);

            if (t >= 1f)
            {
                OnCompleted();
            }
        }
    }

    private void UpdateUI(float normalized)
    {
        if (showProgressUI && progressBar != null)
        {
            progressBar.value = normalized;
        }

        if (progressLabel != null)
            progressLabel.text = $"{Mathf.RoundToInt(normalized * 100f)}%";
    }

    private void SetProgressUIActive(bool active)
    {
        if (progressBar != null && progressBar.gameObject.activeSelf != active)
            progressBar.gameObject.SetActive(active);
        if (progressLabel != null && progressLabel.gameObject.activeSelf != active)
            progressLabel.gameObject.SetActive(active);
    }

    private void OnCompleted()
    {
        if (completed) return;
        completed = true;
        UpdateUI(1f);

        // Ensure UI shows completion state
        if (showProgressUI && !progressVisible)
        {
            progressVisible = true;
            SetProgressUIActive(true);
        }

        // play local clip
        if (completeClip != null)
            AudioSource.PlayClipAtPoint(completeClip, transform.position, completeVolume);

        // notify manager
        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.NotifyCompleted(this);

        // destroy self (after tiny delay to allow sound)
        if (destroyOnComplete)
            Destroy(gameObject, 0.1f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (completed) return;
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            // do NOT show UI immediately here — it will appear once progress actually starts (elapsed > 0)
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (completed) return;
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            // keep the progress UI visible if player has already started progress (so they can see progress when they return)
            // If you instead want to hide the UI on exit, uncomment the following:
            //
            // if (!completed && progressVisible)
            // {
            //     progressVisible = false;
            //     SetProgressUIActive(false);
            // }
        }
    }

    // External helper to force-complete (optional)
    public void ForceComplete()
    {
        elapsed = fillDuration;
        OnCompleted();
    }
}
