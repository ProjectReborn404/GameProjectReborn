using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// PlayerInteraction: detecta objetos etiquetados como "interactive" mediante un rayo
/// y, al pulsar la tecla configurada, llama a la lógica de interacción del objeto.
/// Requiere (opcional) IInteractable.cs y/o SimpleColorInteractable.cs para comportamientos concretos.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    public enum OriginMode { Camera, Player, Custom }

    [Header("Detección")]
    public float interactDistance = 3f;
    public LayerMask interactLayerMask = ~0;
    public string interactiveTag = "interactive";

    [Header("Origen del Rayo")]
    public OriginMode originMode = OriginMode.Player;
    public Transform playerOrigin;
    public Transform customOrigin;

    [Header("Input")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Offsets")]
    public Vector3 originOffset = Vector3.zero;
    public Vector3 directionEulerOffset = Vector3.zero;

    [Header("Visualización")]
    public bool drawRayInPlay = true;
    public bool drawRayInEditor = true;
    public Color rayColor = Color.green;

    [Header("Raycast Pressure Plate (vertical)")]
    public bool enablePressureRaycast = true;
    public float pressureRaycastDistance = 2f;
    public Vector3 pressureRaycastOffset = new Vector3(0, 0.5f, 0);
    public Color pressureRayColor = Color.yellow;

    [Header("Debug (solo dibujado, sin logs repetitivos)")]
    public bool showExtraDebug = false; // por si quieres activar futuras ayudas

    GameObject currentDetected;
    GameObject previousDetected;
    bool legacyInputAvailable = false;

    void Start()
    {
        if (originMode == OriginMode.Player && playerOrigin == null)
            playerOrigin = this.transform;

        // Probar si Input.GetKeyDown funciona (evitar InvalidOperationException si solo está activo el nuevo Input System)
        try
        {
            Input.GetKeyDown(KeyCode.None);
            legacyInputAvailable = true;
        }
        catch (InvalidOperationException)
        {
            legacyInputAvailable = false;
        }

        // Mensaje inicial (solo una vez)
        Debug.Log("No hay objeto interactuable");
    }

    void Update()
    {
        DetectInteractiveObject();
        if (enablePressureRaycast)
            DetectPressurePlateRaycast();

        // Solo un log cuando cambia el estado de detection (null <-> objeto)
        if (currentDetected != previousDetected)
        {
            if (currentDetected == null)
                Debug.Log("No hay objeto interactuable");
            else
                Debug.Log($"Objeto interactuable detectado: {currentDetected.name}");

            previousDetected = currentDetected;
        }

        if (IsInteractPressedThisFrame())
        {
            if (currentDetected != null)
            {
                // Antes de interactuar, validar modo de interacción según el tipo de objeto
                var ui = currentDetected.GetComponentInParent<UnifiedInteractable>();
                if (ui != null)
                {
                    // Manual raycast solo interactúa con Manual o Hybrid
                    if (ui.interactionMode == UnifiedInteractable.InteractionMode.Manual ||
                        ui.interactionMode == UnifiedInteractable.InteractionMode.Hybrid)
                    {
                        try { ui.Interact(this.gameObject); } catch (Exception e) { Debug.LogError($"Error en UnifiedInteractable.Interact: {e}"); }
                    }
                    // Si está en PressurePlate, ignorar la interacción manual
                    return;
                }

                // Evitar activar PressurePlate con interacción manual
                var plate = currentDetected.GetComponentInParent<PressurePlate>();
                if (plate != null)
                {
                    return;
                }

                // 1) Intentar IInteractable (otros casos manuales)
                var inter = currentDetected.GetComponent<IInteractable>();
                if (inter != null)
                {
                    try { inter.Interact(this.gameObject); } catch (Exception e) { Debug.LogError($"Error al ejecutar Interact en {currentDetected.name}: {e}"); }
                    return;
                }

                // 2) Fallback: SimpleColorInteractable si existe (interacción visual simple)
                var sc = currentDetected.GetComponent<SimpleColorInteractable>();
                if (sc != null)
                {
                    try { sc.Interact(this.gameObject); } catch (Exception e) { Debug.LogError($"Error al ejecutar SimpleColorInteractable.Interact en {currentDetected.name}: {e}"); }
                    return;
                }
            }
            // si no hay objeto detectado, no imprimimos nada (evita spam)
        }
    }

    Transform GetReferenceTransform()
    {
        switch (originMode)
        {
            case OriginMode.Camera:
                return Camera.main != null ? Camera.main.transform : null;
            case OriginMode.Player:
                return playerOrigin != null ? playerOrigin : this.transform;
            case OriginMode.Custom:
                return customOrigin;
            default:
                return this.transform;
        }
    }

    void DetectInteractiveObject()
    {
        Transform refT = GetReferenceTransform();

        Vector3 origin;
        Vector3 direction;

        if (refT != null)
        {
            origin = refT.TransformPoint(originOffset);
            direction = refT.forward;
            direction = Quaternion.Euler(directionEulerOffset) * direction;
        }
        else
        {
            origin = transform.TransformPoint(originOffset + Vector3.up * 1.5f);
            direction = transform.forward;
            direction = Quaternion.Euler(directionEulerOffset) * direction;
        }

        Ray ray = new Ray(origin, direction);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, interactLayerMask, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Reset detection (no logs aquí)
        currentDetected = null;

        if (hits.Length == 0)
        {
            if (drawRayInPlay && Application.isPlaying)
                Debug.DrawRay(origin, direction * interactDistance, Color.red);
            return;
        }

        if (drawRayInPlay && Application.isPlaying)
            Debug.DrawRay(origin, direction * Mathf.Min(interactDistance, hits[hits.Length - 1].distance), rayColor);

        // Tomar el primer hit válido (ignorando self/children)
        foreach (var h in hits)
        {
            var go = h.collider?.gameObject;
            if (go == null) continue;

            if (go == this.gameObject || go.transform.IsChildOf(this.transform))
                continue;

            if (go.CompareTag(interactiveTag))
            {
                currentDetected = go;
                break;
            }
            // si no es interactivo, se sigue buscando
        }
    }

    void DetectPressurePlateRaycast()
    {
        Transform refT = GetReferenceTransform();
        Vector3 origin = (refT != null ? refT.position : transform.position) + pressureRaycastOffset;
        Vector3 direction = Vector3.down;
        float distance = pressureRaycastDistance;

        Ray ray = new Ray(origin, direction);
        RaycastHit hit;

        if (drawRayInPlay && Application.isPlaying)
            Debug.DrawRay(origin, direction * distance, pressureRayColor);

        if (Physics.Raycast(ray, out hit, distance, interactLayerMask, QueryTriggerInteraction.Collide))
        {
            GameObject go = hit.collider?.gameObject;
            if (go == null) return;

            // Buscar componentes en el objeto impactado o sus padres (muchas placas usan colliders hijos)
            var ui = go.GetComponentInParent<UnifiedInteractable>();
            if (ui != null && ui.interactionMode == UnifiedInteractable.InteractionMode.PressurePlate)
            {
                if (!ui.IsActive())
                {
                    ui.ForceActivate();
                }
                return;
            }

            var plate = go.GetComponentInParent<PressurePlate>();
            if (plate != null)
            {
                if (!plate.IsPressed())
                {
                    plate.Interact(this.gameObject);
                }
                return;
            }

            // Solo Pressure: no hacer fallback genérico aquí
            // Si no encontramos nada relacionado a Pressure, no hacemos nada
        }
    } // --- FIN DetectPressurePlateRaycast ---

    bool IsInteractPressedThisFrame()
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (interactKey == KeyCode.E) pressed = Keyboard.current.eKey != null && Keyboard.current.eKey.wasPressedThisFrame;
            else if (interactKey == KeyCode.Space) pressed = Keyboard.current.spaceKey != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            else if (interactKey == KeyCode.F) pressed = Keyboard.current.fKey != null && Keyboard.current.fKey.wasPressedThisFrame;
            else if (interactKey == KeyCode.Mouse0 && Mouse.current != null) pressed = Mouse.current.leftButton.wasPressedThisFrame;
            else
            {
                if (Enum.TryParse<UnityEngine.InputSystem.Key>(interactKey.ToString(), out var parsedKey))
                {
                    var control = Keyboard.current[parsedKey];
                    if (control != null)
                        pressed = control.wasPressedThisFrame;
                }
            }
        }
#else
        // Input legacy (Unity antes de 2019.1)
        if (legacyInputAvailable)
        {
            if (interactKey == KeyCode.E) pressed = Input.GetKeyDown(KeyCode.E);
            else if (interactKey == KeyCode.Space) pressed = Input.GetKeyDown(KeyCode.Space);
            else if (interactKey == KeyCode.F) pressed = Input.GetKeyDown(KeyCode.F);
            else if (interactKey == KeyCode.Mouse0) pressed = Input.GetMouseButtonDown(0);
        }
#endif

        return pressed;
    }

    private void OnDrawGizmos()
    {
        if (drawRayInEditor)
        {
            Transform refT = GetReferenceTransform();

            Vector3 origin;
            Vector3 direction;

            if (refT != null)
            {
                origin = refT.TransformPoint(originOffset);
                direction = refT.forward;
                direction = Quaternion.Euler(directionEulerOffset) * direction;
            }
            else
            {
                origin = transform.TransformPoint(originOffset + Vector3.up * 1.5f);
                direction = transform.forward;
                direction = Quaternion.Euler(directionEulerOffset) * direction;
            }

            Gizmos.color = rayColor;
            Gizmos.DrawLine(origin, origin + direction * interactDistance);

            if (enablePressureRaycast)
            {
                Vector3 pressureOrigin = (refT != null ? refT.position : transform.position) + pressureRaycastOffset;
                Vector3 pressureDirection = Vector3.down;
                float pressureDistance = pressureRaycastDistance;

                Gizmos.color = pressureRayColor;
                Gizmos.DrawLine(pressureOrigin, pressureOrigin + pressureDirection * pressureDistance);
            }
        }
    }
}
