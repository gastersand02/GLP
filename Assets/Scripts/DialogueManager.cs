using System;
using System.Collections;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI")]
    public DialogueUI ui;

    [Header("Typing")]
    [Tooltip("Seconds per character. Lower = faster.")]
    public float charDelay = 0.02f;
    [Tooltip("Key used to advance dialogue / skip typing.")]
    public KeyCode advanceKey = KeyCode.Space;

    [Header("Audio")]
    public AudioClip advanceClick;

    [Header("Typing SFX (Undertale-style)")]
    [Tooltip("Per-character blip sound.")]
    public AudioClip charBlip;
    [Range(0f, 1f)]
    public float charBlipVolume = 0.8f;
    [Tooltip("Random pitch variation applied to each blip.")]
    public float charPitchVariance = 0.08f;
    [Tooltip("Minimum time between blips (avoid over-triggering for very fast charDelay).")]
    public float minBlipInterval = 0.03f;

    private Coroutine _runner;
    private bool _isTyping;
    private bool _skipTyping;

    private AudioSource _audioSource;
    private float _lastBlipTime;

    // Public read-only accessor so other systems can check if dialogue is currently running
    public bool IsActive => _runner != null;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;

        // ensure audio source available for SFX
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D
        }
    }

    public void StartDialogue(DialogueData data)
    {
        if (data == null || ui == null)
        {
            Debug.LogWarning("[DialogueManager] Cannot start dialogue - data or ui is null.");
            return;
        }

        StopDialogue();

        Debug.Log("[DialogueManager] Starting dialogue.");
        ui.Show();
        if (ui.panel != null && !ui.panel.activeSelf) ui.panel.SetActive(true);

        _runner = StartCoroutine(RunDialogue(data));
    }

    public void StopDialogue()
    {
        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }

        _isTyping = false;
        _skipTyping = false;

        if (ui != null)
        {
            ui.ClearChoices();
            if (ui.panel != null && ui.panel.activeSelf)
            {
                ui.panel.SetActive(false);
                Debug.Log("[DialogueManager] StopDialogue: panel SetActive(false).");
            }
            ui.SetContent("");
            ui.SetName("");
            ui.SetPortrait(null);
        }

        Debug.Log("[DialogueManager] Dialogue stopped / reset.");
    }

    IEnumerator RunDialogue(DialogueData data)
    {
        if (ui != null)
        {
            ui.Show();
            if (ui.panel != null && !ui.panel.activeSelf) ui.panel.SetActive(true);
        }

        int index = 0;
        int steps = 0;
        int maxSteps = Mathf.Max(200, (data.lines != null ? data.lines.Length * 10 : 200)); // safety cap

        try
        {
            while (index >= 0 && data.lines != null && index < data.lines.Length)
            {
                // safety watchdog
                steps++;
                if (steps > maxSteps)
                {
                    Debug.LogError($"[DialogueManager] Dialogue appears to be stuck (index={index}). Breaking after {steps} steps. Check nextLineIndex/choices for loops.", this);
                    break;
                }

                var line = data.lines[index];

                if (ui != null)
                {
                    ui.SetName(line.speakerName);
                    ui.SetPortrait(line.portrait);
                }

                // Typewriter
                _isTyping = true;
                _skipTyping = false;
                yield return StartCoroutine(TypeText(line.text));

                // If there are choices present: present and wait for selection
                if (line.choices != null && line.choices.Length > 0)
                {
                    if (ui != null) ui.ClearChoices();
                    int chosen = -1;
                    for (int i = 0; i < line.choices.Length; i++)
                    {
                        int copy = i;
                        var btn = ui != null ? ui.CreateChoice(line.choices[i].text) : null;
                        if (btn == null) continue;
                        btn.onClick.AddListener(() =>
                        {
                            chosen = copy;
                        });
                    }

                    // wait until a choice made
                    while (chosen < 0)
                        yield return null;

                    // invoke choice event (if any)
                    var choice = line.choices[chosen];
                    if (choice.onSelect != null) choice.onSelect.Invoke();

                    if (ui != null) ui.ClearChoices();

                    // determine next index
                    int prev = index;
                    index = choice.nextLineIndex >= 0 ? choice.nextLineIndex : index + 1;

                    if (advanceClick != null) AudioSource.PlayClipAtPoint(advanceClick, Camera.main != null ? Camera.main.transform.position : Vector3.zero, 1f);

                    // if choice didn't advance and points to same line -> detect possible loop next iteration by steps counter
                    if (index == prev)
                    {
                        Debug.LogWarning($"[DialogueManager] Choice led to same index {index}. This may cause a loop.", this);
                    }

                    continue;
                }

                // No choices: wait for advance key
                bool advanced = false;
                while (!advanced)
                {
                    if (!_isTyping)
                    {
                        if (Input.GetKeyDown(advanceKey))
                            advanced = true;
                    }
                    else
                    {
                        if (Input.GetKeyDown(advanceKey))
                        {
                            _skipTyping = true;
                        }
                    }
                    yield return null;
                }

                int prevIndex = index;
                // If this line set a forced next index use it
                if (line.nextLineIndex >= 0) index = line.nextLineIndex;
                else index++;

                // NEW: if the line's nextLineIndex points to itself, force advance and log error
                if (index == prevIndex)
                {
                    
                    index = prevIndex + 1;
                }

                if (advanceClick != null) AudioSource.PlayClipAtPoint(advanceClick, Camera.main != null ? Camera.main.transform.position : Vector3.zero, 1f);

                if (index == prevIndex)
                {
                    Debug.LogWarning($"[DialogueManager] Line {index} did not advance to a new index (nextLineIndex == current). This may cause a loop.", this);
                }
            }
        }
        finally
        {
            // Ensure UI is always cleaned up when dialogue ends or coroutine is stopped
            _runner = null;
            _isTyping = false;
            _skipTyping = false;
            if (ui != null)
            {
                ui.ClearChoices();
                ui.SetContent("");
                ui.SetName("");
                ui.SetPortrait(null);

                if (ui.panel != null)
                {
                    ui.panel.SetActive(false);
                    Debug.Log("[DialogueManager] Dialogue finished: panel SetActive(false).");
                }
                else
                {
                    ui.Hide();
                    Debug.Log("[DialogueManager] Dialogue finished: ui.Hide() called.");
                }
            }
        }
    }

    IEnumerator TypeText(string full)
    {
        if (ui == null)
        {
            _isTyping = false;
            yield break;
        }

        ui.SetContent("");
        if (string.IsNullOrEmpty(full))
        {
            _isTyping = false;
            yield break;
        }

        int i = 0;
        while (i < full.Length)
        {
            if (_skipTyping)
            {
                ui.SetContent(full);
                // play blips for remaining visible chars (optional) - quick pass
                TryPlayBlipForRemaining(full, i);
                break;
            }

            ui.SetContent(full.Substring(0, i + 1));

            // play blip for this character (skip whitespace)
            if (charBlip != null && !char.IsWhiteSpace(full[i]) && Time.time - _lastBlipTime >= minBlipInterval)
            {
                float basePitch = 1f;
                float variance = charPitchVariance;
                float pitch = basePitch + UnityEngine.Random.Range(-variance, variance);
                _audioSource.pitch = pitch;
                _audioSource.PlayOneShot(charBlip, charBlipVolume);
                _lastBlipTime = Time.time;
            }

            i++;
            yield return new WaitForSeconds(charDelay);
        }

        _isTyping = false;
        _skipTyping = false;
    }

    private void TryPlayBlipForRemaining(string full, int startIndex)
    {
        if (charBlip == null) return;
        for (int j = startIndex; j < full.Length; j++)
        {
            if (!char.IsWhiteSpace(full[j]) && Time.time - _lastBlipTime >= minBlipInterval)
            {
                float pitch = 1f + UnityEngine.Random.Range(-charPitchVariance, charPitchVariance);
                _audioSource.pitch = pitch;
                _audioSource.PlayOneShot(charBlip, charBlipVolume);
                _lastBlipTime = Time.time;
            }
        }
    }
}