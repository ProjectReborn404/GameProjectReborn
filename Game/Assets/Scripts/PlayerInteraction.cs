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
                Debug.Log($"Interacción con {currentDetected.name}");

                // 1) Intentar IInteractable (preferido)
                var inter = currentDetected.GetComponent<IInteractable>();
                if (inter != null)
                {
                    try
                    {
                        inter.Interact(this.gameObject);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error al ejecutar Interact en {currentDetected.name}: {e}");
                    }
                }
                else
                {
                    // 2) Fallback: SimpleColorInteractable si existe (interacción visual simple)
                    var sc = currentDetected.GetComponent<SimpleColorInteractable>();
                    if (sc != null)
                    {
                        try
                        {
                            sc.Interact(this.gameObject);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error al ejecutar SimpleColorInteractable.Interact en {currentDetected.name}: {e}");
                        }
                    }
                    else
                    {
                        // 3) Ninguna implementación encontrada: aviso (puedes quitar esto si lo deseas)
                        Debug.Log($"El objeto '{currentDetected.name}' no implementa IInteractable ni SimpleColorInteractable.");
                    }
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
                    if (control != null) pressed = control.wasPressedThisFrame;
                }
            }
        }
#endif

        if (!pressed && legacyInputAvailable)
        {
            pressed = Input.GetKeyDown(interactKey);
        }

        return pressed;
    }

    void OnDrawGizmos()
    {
        if (!drawRayInEditor) return;

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
        Gizmos.DrawSphere(origin + direction * 0.05f, interactDistance * 0.01f);
    }
}
