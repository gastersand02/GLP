using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DialogueTrigger : MonoBehaviour
{
    [Tooltip("Dialogue asset to play when player triggers.")]
    public DialogueData dialogue;

    [Tooltip("Play when player with 'Player' tag enters. If false, you must call Trigger() manually.")]
    public bool triggerOnEnter = true;

    [Header("Auto-destroy")]
    [Tooltip("If true the DialogueTrigger GameObject will be destroyed after the dialogue finishes.")]
    public bool destroyAfterDialogue = false;
    [Tooltip("Delay in seconds before destroying the trigger object after dialogue finishes. 0 = immediate next frame.")]
    public float destroyDelay = 0f;

    private Coroutine _destroyRoutine;

    void Start()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnEnter) return;
        if (other.CompareTag("Player"))
            Trigger();
    }

    public void Trigger()
    {
        if (dialogue != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(dialogue);

            if (destroyAfterDialogue)
            {
                if (_destroyRoutine != null)
                    StopCoroutine(_destroyRoutine);
                _destroyRoutine = StartCoroutine(DestroyAfterDialogueCoroutine());
            }
        }
    }

    private IEnumerator DestroyAfterDialogueCoroutine()
    {
        // Wait for the DialogueManager instance to be present
        while (DialogueManager.Instance == null)
            yield return null;

        // Wait until the dialogue starts (IsActive true) — defensive in case StartDialogue schedules start
        while (!DialogueManager.Instance.IsActive)
            yield return null;

        // Wait until the dialogue finishes
        while (DialogueManager.Instance.IsActive)
            yield return null;

        // Destroy after optional delay
        if (destroyDelay <= 0f)
            Destroy(gameObject);
        else
            Destroy(gameObject, destroyDelay);
    }

    private void OnDestroy()
    {
        if (_destroyRoutine != null)
            StopCoroutine(_destroyRoutine);
    }
}