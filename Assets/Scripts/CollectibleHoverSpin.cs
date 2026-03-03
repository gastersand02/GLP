using UnityEngine;

[DisallowMultipleComponent]
public class CollectibleHoverSpin : MonoBehaviour
{
    [Header("Rotation")]
    [Tooltip("Axis to rotate around (local).")]
    public Vector3 rotationAxis = Vector3.up;
    [Tooltip("Degrees per second.")]
    public float rotationSpeed = 90f;

    [Header("Hover (bob)")]
    [Tooltip("Maximum vertical offset from the start position (meters).")]
    public float hoverAmplitude = 0.25f;
    [Tooltip("Cycles per second.")]
    public float hoverFrequency = 0.75f;
    [Tooltip("Use localPosition for hover (true) or world position (false).")]
    public bool useLocalPosition = true;
    [Tooltip("Randomize the starting phase so many collectibles don't bob in sync.")]
    public bool randomizePhase = true;

    [Header("Performance / Visibility")]
    [Tooltip("If true the effect runs only while a Renderer on this object is visible to any camera.")]
    public bool onlyWhenVisible = false;
    [Tooltip("If true the rotation uses local space; otherwise world space.")]
    public bool rotateInLocalSpace = true;

    // Internal
    private Vector3 _initialLocalPos;
    private Vector3 _initialWorldPos;
    private float _phaseOffset;
    private Renderer _renderer;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        _initialLocalPos = transform.localPosition;
        _initialWorldPos = transform.position;
        _phaseOffset = randomizePhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    void OnEnable()
    {
        // re-sync initial positions in case prefab/parent moved while disabled
        _initialLocalPos = transform.localPosition;
        _initialWorldPos = transform.position;
    }

    void Update()
    {
        if (onlyWhenVisible && _renderer != null && !_renderer.isVisible)
            return;

        // Rotation
        if (rotationSpeed != 0f)
        {
            Vector3 axis = rotationAxis.normalized;
            float angle = rotationSpeed * Time.deltaTime;
            if (rotateInLocalSpace)
                transform.Rotate(axis, angle, Space.Self);
            else
                transform.Rotate(axis, angle, Space.World);
        }

        // Hover
        if (hoverAmplitude > 0f && hoverFrequency > 0f)
        {
            float t = Time.time * hoverFrequency * Mathf.PI * 2f + _phaseOffset;
            float yOffset = Mathf.Sin(t) * hoverAmplitude;

            if (useLocalPosition)
            {
                Vector3 p = _initialLocalPos;
                p.y += yOffset;
                transform.localPosition = p;
            }
            else
            {
                Vector3 p = _initialWorldPos;
                p.y += yOffset;
                transform.position = p;
            }
        }
    }

    void OnValidate()
    {
        if (hoverAmplitude < 0f) hoverAmplitude = 0f;
        if (hoverFrequency < 0f) hoverFrequency = 0f;
    }

    void OnDrawGizmosSelected()
    {
        // draw hover extents
        Gizmos.color = Color.cyan;
        if (useLocalPosition)
        {
            Vector3 a = transform.TransformPoint(_initialLocalPos + Vector3.up * -hoverAmplitude);
            Vector3 b = transform.TransformPoint(_initialLocalPos + Vector3.up * hoverAmplitude);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawWireSphere(a, 0.05f);
            Gizmos.DrawWireSphere(b, 0.05f);
        }
        else
        {
            Vector3 a = _initialWorldPos + Vector3.up * -hoverAmplitude;
            Vector3 b = _initialWorldPos + Vector3.up * hoverAmplitude;
            Gizmos.DrawLine(a, b);
            Gizmos.DrawWireSphere(a, 0.05f);
            Gizmos.DrawWireSphere(b, 0.05f);
        }
    }
}