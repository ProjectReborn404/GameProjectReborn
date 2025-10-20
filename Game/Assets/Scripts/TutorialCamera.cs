using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cámara sencilla para tutorial: mantiene una altura fija (Y) y sigue al jugador lateralmente (X/Z).
/// Ajustable: altura fija, suavizado, offsets y ejes a seguir.
/// Uso: asignar el Transform del jugador a `target` en el inspector.
/// </summary>
public class TutorialCamera : MonoBehaviour
{
    [Tooltip("Transform del jugador a seguir")]
    public Transform target;

    [Header("Altura")]
    [Tooltip("Altura fija de la cámara en el eje Y. Si `useInitialHeight` está activado, se usará la altura inicial de la cámara en escena.")]
    public float fixedHeight = 5f;
    [Tooltip("Si está activado, la cámara usará su Y inicial como altura fija al iniciar.")]
    public bool useInitialHeight = true;

    [Header("Movimiento")]
    [Tooltip("Offset aplicado a la posición del target (X,Z). Y del offset se ignora, la altura se controla por fixedHeight.")]
    public Vector3 offset = Vector3.zero;
    
    [Header("Control del cursor")]
    [Tooltip("Si está activado, el cursor se bloqueará al iniciar")]
    public bool lockCursorOnStart = true;
    [Tooltip("Velocidad de suavizado. Valores altos hacen que la cámara siga más rápido.")]
    public float smoothSpeed = 8f;
    [Tooltip("Seguir en X (horizontal)")]
    public bool followX = true;
    [Tooltip("Seguir en Z (profundidad). Desactivar si quieres sólo seguimiento horizontal en X.")]
    public bool followZ = true;

    float initialHeight;
    [Header("Límites de cámara")]
    [Tooltip("Si se activa, la cámara se limitará dentro de los colliders especificados.")]
    public bool useLimits = false;
    [Tooltip("Lista manual de colliders que definen el área navegable/visible para la cámara. Se combinarán sus bounds.")]
    public Collider[] limitColliders;
    [Tooltip("Alternativamente, si 'useCollidersByTag' está activado, todos los colliders con este tag serán usados como límites.")]
    public bool useCollidersByTag = false;
    [Tooltip("Tag usado para buscar colliders que actúen como límite (por ejemplo: 'CameraLimit').")]
    public string limitTag = "CameraLimit";
    [Tooltip("Padding que se aplica al bounds combinado para dar espacio extra antes de clamping.")]
    public Vector2 boundsPadding = Vector2.zero; // x = padding en X, y = padding en Z

    // Bounds combinados calculados a partir de colliders
    Bounds combinedBounds;
    bool boundsValid = false;

    void Start()
    {
        initialHeight = transform.position.y;
        if (useInitialHeight)
        {
            fixedHeight = initialHeight;
        }
        // Inicializar bounds si corresponde
        if (useLimits)
        {
            RecalculateBounds();
        }
        
        // Bloquear el cursor si está configurado
        if (lockCursorOnStart)
        {
            LockCursor();
        }
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Usar LateUpdate para seguir al objetivo después de que éste se haya movido en Update
    void OnEnable()
    {
        // Suscribirse al evento de la tecla Escape
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.actions["Cancel"].performed += OnCancelAction;
        }
    }

    void OnDisable()
    {
        // Desuscribirse del evento
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.actions["Cancel"].performed -= OnCancelAction;
        }
    }

    void OnCancelAction(InputAction.CallbackContext context)
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            UnlockCursor();
        }
        else
        {
            LockCursor();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Posición objetivo combinando offset
        Vector3 targetPos = target.position + offset;

        // Empezamos desde la posición actual y cambiamos sólo los ejes que queremos seguir
        Vector3 desired = transform.position;
        if (followX) desired.x = targetPos.x;
        if (followZ) desired.z = targetPos.z;

        // Forzamos la altura fija
        desired.y = fixedHeight;

        // Si usamos límites, recalcular si es necesario y aplicar clamping en X/Z
        if (useLimits)
        {
            if (useCollidersByTag)
            {
                // Recalcular cada frame por seguridad si el usuario quiere dinamismo
                RecalculateBoundsByTag();
            }
            if (boundsValid)
            {
                // Aplicar padding
                float minX = combinedBounds.min.x + boundsPadding.x;
                float maxX = combinedBounds.max.x - boundsPadding.x;
                float minZ = combinedBounds.min.z + boundsPadding.y;
                float maxZ = combinedBounds.max.z - boundsPadding.y;

                if (followX) desired.x = Mathf.Clamp(desired.x, minX, maxX);
                if (followZ) desired.z = Mathf.Clamp(desired.z, minZ, maxZ);
            }
        }

        // Suavizado exponencial (frame-rate independiente)
        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);
    }

    // Recalcula combinedBounds a partir de `limitColliders` si están asignados
    void RecalculateBounds()
    {
        boundsValid = false;
        if (limitColliders == null || limitColliders.Length == 0) return;

        bool first = true;
        Bounds b = new Bounds();
        foreach (var col in limitColliders)
        {
            if (col == null) continue;
            if (first)
            {
                b = col.bounds;
                first = false;
            }
            else
            {
                b.Encapsulate(col.bounds);
            }
        }
        combinedBounds = b;
        boundsValid = !first;
    }

    // Recalcula combinedBounds buscando colliders por tag
    void RecalculateBoundsByTag()
    {
    var colliders = GameObject.FindObjectsByType<Collider>(FindObjectsSortMode.None);
    limitColliders = System.Array.FindAll(colliders, c => c != null && c.CompareTag(limitTag));
        RecalculateBounds();
    }
}
