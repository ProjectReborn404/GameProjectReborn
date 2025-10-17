using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;              // el personaje a seguir
    public Vector3 offset = new Vector3(0f, 1.6f, -3.5f);
    public Vector3 pivotOffset = new Vector3(0f, 1.2f, 0f); // punto desde donde se realiza el spherecast (aprox pecho)
    public float minDistance = 0.7f; // distancia mínima de cámara respecto al pivot
    public bool lockCursor = true; // bloquear y ocultar el cursor al iniciar

    [Header("Rotación")]
    public float mouseSens = 150f;      // sensibilidad del ratón/joystick
    public float minPitch = -30f;
    public float maxPitch = 60f;
    [Header("Input Actions (opcional)")]
#if ENABLE_INPUT_SYSTEM
    public UnityEngine.InputSystem.InputActionReference lookAction; // Vector2
    public UnityEngine.InputSystem.InputActionReference toggleLockAction; // Button
#endif

    [Header("Suavizado y colisión")]
    public float smoothTime = 0.08f;      // suavizado de posición
    public float collisionRadius = 0.3f;  // radio para spherecast anti-clipping
    [Tooltip("Tiempo de suavizado de rotación (SmoothDampAngle)")]
    public float rotationSmoothTime = 0.06f;
    [Tooltip("Suavizado para la entrada de look (valores pequeños hacen la entrada más suave)")]
    public float inputSmoothing = 0.03f;
    [Tooltip("Velocidad de interpolación para la rotación hacia el objetivo (mayor = más rápida)")]
    public float rotationLerpSpeed = 10f;
    [Tooltip("Si está activado, la cámara centra inmediatamente al jugador al mirar (menos smoothing en rotación)")]
    public bool alwaysCenterTarget = false;

    private float yaw;    // rotación Y
    private float pitch;  // rotación X
    private Vector3 currentVelocity;
    private float targetYaw;
    private float targetPitch;
    // smoothing helpers
    private Vector2 currentLook; // smoothed input
    private Vector2 lookSmoothVelocity;
    private float yawVelocity;
    private float pitchVelocity;

    void Start()
    {
        if (target == null && Camera.main != null)
        {
            // intentar encontrar al jugador por tag 'Player'
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }

        Vector3 angles = transform.eulerAngles;
        yaw = targetYaw = angles.y;
        pitch = targetPitch = angles.x;

        // Bloquear cursor al inicio si está activado
        ApplyCursorLock(lockCursor);
    }

#if ENABLE_INPUT_SYSTEM
    void OnEnable()
    {
        if (toggleLockAction != null && toggleLockAction.action != null)
        {
            toggleLockAction.action.performed += OnToggleLockPerformed;
            // Ensure action is enabled so callbacks fire
            if (!toggleLockAction.action.enabled)
                toggleLockAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (toggleLockAction != null && toggleLockAction.action != null)
            toggleLockAction.action.performed -= OnToggleLockPerformed;
    }

    void OnToggleLockPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        lockCursor = !lockCursor;
        ApplyCursorLock(lockCursor);
    }
#endif

    void LateUpdate()
    {
        if (target == null) return;

        // Toggle cursor lock: Escape para liberar, click izquierdo para bloquear (compatibilidad con Input System y legacy)
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            lockCursor = false;
            ApplyCursorLock(false);
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            lockCursor = true;
            ApplyCursorLock(true);
        }
#else
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            lockCursor = false;
            ApplyCursorLock(false);
        }
        if (Input.GetMouseButtonDown(0))
        {
            lockCursor = true;
            ApplyCursorLock(true);
        }
#endif

        // Lectura de entrada de rotación (ratón o gamepad)
        float inputX = 0f;
        float inputY = 0f;

#if ENABLE_INPUT_SYSTEM
        // Si se asignó una InputActionReference para 'look', usarla
        if (lookAction != null && lookAction.action != null)
        {
            var v = lookAction.action.ReadValue<Vector2>();
            inputX = v.x;
            inputY = v.y;
        }
        else
        {
            // Fallback a lectura directa de Mouse/Gamepad
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                var delta = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();
                inputX += delta.x * 0.02f; // escala para que sea similar al legacy
                inputY += delta.y * 0.02f;
            }
            if (UnityEngine.InputSystem.Gamepad.current != null)
            {
                var r = UnityEngine.InputSystem.Gamepad.current.rightStick.ReadValue();
                inputX += r.x;
                inputY += r.y;
            }
        }
#else
        inputX = Input.GetAxis("Mouse X");
        inputY = Input.GetAxis("Mouse Y");
#endif

        // Interpretar la entrada: diferenciar entre delta de ratón (pixeles/frame) y stick normalizado
        Vector2 rawLook = new Vector2(inputX, inputY);
        bool isDeltaInput = false;
#if ENABLE_INPUT_SYSTEM
        // Si usamos lookAction y su valor es grande (>1.5) lo tratamos como delta (mouse), si es pequeño lo tratamos como stick
        if (lookAction != null && lookAction.action != null)
        {
            var v = lookAction.action.ReadValue<Vector2>();
            rawLook = v;
            isDeltaInput = Mathf.Abs(v.x) > 1.5f || Mathf.Abs(v.y) > 1.5f;
        }
#endif

        // Escalado distinto según tipo de input
        Vector2 desiredLook;
        if (isDeltaInput)
        {
            // Mouse delta: ya es en unidades por frame — aplicar factor de sensibilidad pero NO multiplicar por Time.deltaTime
            desiredLook = rawLook * (mouseSens * 0.02f);
        }
        else
        {
            // Stick / normalized input: escala por sensibilidad y por deltaTime para que sea suave y frame-rate independent
            desiredLook = rawLook * (mouseSens * Time.deltaTime);
        }

        // Smooth input to avoid spikes
        currentLook = Vector2.SmoothDamp(currentLook, desiredLook, ref lookSmoothVelocity, inputSmoothing);

        // Acumular delta en variables objetivo usando la entrada suavizada
        targetYaw += currentLook.x;
        targetPitch -= currentLook.y;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        // Suavizar rotación con SmoothDampAngle (evita tirones)
        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, rotationSmoothTime);
        pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref pitchVelocity, rotationSmoothTime);

    Quaternion camRot = Quaternion.Euler(pitch, yaw, 0f);

    // Definir pivot (centro de rotación) y posición objetivo antes de colisión: la cámara orbita alrededor del pivot
    Vector3 pivot = target.position + camRot * pivotOffset;
    Vector3 desiredPosition = pivot + camRot * offset;
        Vector3 dir = (desiredPosition - pivot);
        float maxDist = dir.magnitude;
        dir = dir.normalized;

        Vector3 finalPos = desiredPosition;
        RaycastHit[] hits = Physics.SphereCastAll(pivot, collisionRadius, dir, maxDist);
        float closestHitDist = float.MaxValue;
        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            // Ignorar colisionadores que pertenezcan al target (para evitar autocolisión)
            if (h.collider.transform.IsChildOf(target) || h.collider.transform == target)
                continue;

            if (h.distance < closestHitDist)
                closestHitDist = h.distance;
        }

        if (closestHitDist != float.MaxValue)
        {
            Vector3 hitPos = pivot + dir * (closestHitDist - collisionRadius);
            finalPos = hitPos;
        }

        // Forzar distancia mínima respecto al pivot
        float distFromPivot = Vector3.Distance(pivot, finalPos);
        if (distFromPivot < minDistance)
        {
            finalPos = pivot + (finalPos - pivot).normalized * minDistance;
        }

    // Suavizar movimiento
    transform.position = Vector3.SmoothDamp(transform.position, finalPos, ref currentVelocity, smoothTime);

        // Hacer que la cámara mire al pivot
        Quaternion lookRot = Quaternion.LookRotation((pivot - transform.position).normalized, Vector3.up);
        if (alwaysCenterTarget)
        {
            // Rápido o inmediato para mantener al jugador centrado
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 1f - Mathf.Exp(- (rotationLerpSpeed * 5f) * Time.deltaTime));
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime));
        }
    }

    // Método utilitario para asignar target desde código
    public void SetTarget(Transform t)
    {
        target = t;
    }

    void ApplyCursorLock(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
