using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AlarmLight : MonoBehaviour
{
    [Header("Target Lights")]
    [Tooltip("Optional: a GameObject whose child Light components will be used. If set, this takes priority over the explicit Lights array.")]
    public GameObject lightsContainer;

    [Tooltip("Lights to control. If empty and 'includeChildren' is true, child Light components will be used.")]
    public Light[] lights;

    [Tooltip("If true and 'lights' is empty and lightsContainer is null, include Lights from children.")]
    public bool includeChildren = true;

    [Header("Alarm Appearance")]
    public Color alarmColor = Color.red;
    [Tooltip("Relative intensity multiplier when at peak of flash (multiplies the original intensity).")]
    public float peakIntensityMultiplier = 2f;

    [Header("Timing (either set Frequency or Period)")]
    [Tooltip("How many flash cycles per second. Ignored if flashPeriod > 0.")]
    public float flashFrequency = 0.25f; // lower default frequency (slower) if period not used
    [Tooltip("Seconds per full flash cycle. If > 0 this overrides flashFrequency (more intuitive for slow flashing).")]
    public float flashPeriod = 4f; // default to 4 seconds per cycle for a slower alarm

    [Tooltip("Smoothness of color/intensity interpolation (0..1). 1 = immediate, 0 = very smooth")]
    [Range(0f,1f)]
    public float sharpness = 0.8f;

    [Header("Behavior")]
    [Tooltip("Start the alarm automatically on Play.")]
    public bool startOnAwake = false;
    [Tooltip("If > 0 the alarm will automatically stop after this many seconds.")]
    public float autoStopAfter = 0f;

    // runtime
    private List<Color> _originalColors = new List<Color>();
    private List<float> _originalIntensity = new List<float>();
    private List<bool> _originalEnabled = new List<bool>();
    private Coroutine _alarmRoutine;

    void Awake()
    {
        EnsureLights();
        CacheOriginals();
        if (startOnAwake)
            StartAlarm(autoStopAfter);
    }

    void OnValidate()
    {
        // Keep values sane in inspector
        if (flashFrequency < 0f) flashFrequency = 0f;
        if (flashPeriod < 0f) flashPeriod = 0f;
        if (peakIntensityMultiplier < 0f) peakIntensityMultiplier = 0f;
        if (sharpness < 0f) sharpness = 0f;
        if (sharpness > 1f) sharpness = 1f;
    }

    void EnsureLights()
    {
        // If a container was provided, gather lights from it
        if (lightsContainer != null)
        {
            var found = lightsContainer.GetComponentsInChildren<Light>(true);
            lights = found;
            return;
        }

        // otherwise keep explicit array if set
        if (lights != null && lights.Length > 0)
            return;

        // fallback: collect child lights of this GameObject if requested
        if (includeChildren)
        {
            lights = GetComponentsInChildren<Light>(true);
        }
    }

    void CacheOriginals()
    {
        _originalColors.Clear();
        _originalIntensity.Clear();
        _originalEnabled.Clear();

        if (lights == null) return;
        foreach (var l in lights)
        {
            if (l == null) continue;
            _originalColors.Add(l.color);
            _originalIntensity.Add(l.intensity);
            _originalEnabled.Add(l.enabled);
        }
    }

    /// <summary>
    /// Start the alarm. Pass duration <= 0 for indefinite until StopAlarm() is called.
    /// </summary>
    public void StartAlarm(float duration = 0f)
    {
        EnsureLights();
        if (lights == null || lights.Length == 0)
        {
            Debug.LogWarning("[AlarmLight] No lights assigned or found to alarm.", this);
            return;
        }

        // if already running, restart with new duration
        if (_alarmRoutine != null)
            StopCoroutine(_alarmRoutine);

        CacheOriginals();
        _alarmRoutine = StartCoroutine(AlarmRoutine(duration));
    }

    /// <summary>
    /// Stop the alarm and restore original light colors/intensities.
    /// </summary>
    public void StopAlarm()
    {
        if (_alarmRoutine != null)
        {
            StopCoroutine(_alarmRoutine);
            _alarmRoutine = null;
        }
        RestoreOriginals();
    }

    private IEnumerator AlarmRoutine(float duration)
    {
        float elapsed = 0f;

        // compute angular velocity (omega) using period if provided, otherwise frequency
        float omega;
        if (flashPeriod > 0f)
            omega = Mathf.PI * 2f / Mathf.Max(0.0001f, flashPeriod); // period -> omega
        else
            omega = Mathf.PI * 2f * Mathf.Max(0.0001f, flashFrequency); // frequency -> omega

        // ensure lights enabled
        foreach (var l in lights)
            if (l != null) l.enabled = true;

        while (duration <= 0f || elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float raw = Mathf.Sin(elapsed * omega) * 0.5f + 0.5f; // 0..1
            // sharpen the curve toward peaks based on sharpness parameter
            float t = Mathf.Lerp(Mathf.SmoothStep(0f,1f,raw), raw, Mathf.Clamp01(sharpness));

            for (int i = 0; i < lights.Length; i++)
            {
                var l = lights[i];
                if (l == null) continue;

                Color orig = (i < _originalColors.Count) ? _originalColors[i] : Color.white;
                float origI = (i < _originalIntensity.Count) ? _originalIntensity[i] : l.intensity;

                l.color = Color.Lerp(orig, alarmColor, t);
                l.intensity = Mathf.Lerp(origI, origI * peakIntensityMultiplier, t);
            }

            yield return null;
        }

        _alarmRoutine = null;
        RestoreOriginals();
    }

    void RestoreOriginals()
    {
        if (lights == null) return;
        for (int i = 0; i < lights.Length; i++)
        {
            var l = lights[i];
            if (l == null) continue;
            if (i < _originalColors.Count) l.color = _originalColors[i];
            if (i < _originalIntensity.Count) l.intensity = _originalIntensity[i];
            if (i < _originalEnabled.Count) l.enabled = _originalEnabled[i];
        }
    }

    // Editor / quick test helpers
    [ContextMenu("Trigger Alarm (indefinite)")]
    void ContextStart() => StartAlarm(0f);

    [ContextMenu("Trigger Alarm (3s)")]
    void ContextStart3() => StartAlarm(3f);

    [ContextMenu("Stop Alarm")]
    void ContextStop() => StopAlarm();
}