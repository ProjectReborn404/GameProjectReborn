using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TutorialZone: Zona de trigger que notifica al TutorialManager cuando el jugador la alcanza.
/// Útil para objetivos tipo "llegar a X ubicación".
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialZone : MonoBehaviour
{
    [Header("Tutorial")]
    [Tooltip("Referencia al TutorialManager (se buscará automáticamente si no se asigna)")]
    public TutoriaManager tutorialManager;
    
    [Header("Comportamiento")]
    [Tooltip("¿Esta zona se debe desactivar tras ser alcanzada?")]
    public bool disableAfterReached = true;
    
    [Tooltip("¿Puede ser activada múltiples veces?")]
    public bool canTriggerMultipleTimes = false;
    
    [Header("Visualización")]
    [Tooltip("¿Mostrar esta zona en el editor?")]
    public bool showInEditor = true;
    
    [Tooltip("Color de visualización en el editor")]
    public Color gizmoColor = new Color(0, 1, 0, 0.3f);
    
    [Header("Eventos")]
    public UnityEvent onZoneEntered;
    public UnityEvent onZoneExited;
    
    private bool hasBeenReached = false;
    private Collider triggerCollider;
    
    void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
        else
        {
            Debug.LogError($"[TutorialZone] No se encontró Collider en {gameObject.name}");
        }
    }
    
    void Start()
    {
        // Buscar TutorialManager si no está asignado
        if (tutorialManager == null)
        {
            tutorialManager = FindObjectOfType<TutoriaManager>();
            if (tutorialManager == null)
            {
                Debug.LogWarning($"[TutorialZone] No se encontró TutorialManager en la escena. Zona: {gameObject.name}");
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Verificar que es el jugador
        if (!other.CompareTag("Player"))
            return;
        
        // Verificar si puede ser activada
        if (hasBeenReached && !canTriggerMultipleTimes)
        {
            return;
        }
        
        hasBeenReached = true;
        
        Debug.Log($"[TutorialZone] Jugador entró en la zona: {gameObject.name}");
        
        // Notificar al TutorialManager
        if (tutorialManager != null)
        {
            tutorialManager.NotifyZoneEntered(triggerCollider);
        }
        
        // Ejecutar eventos personalizados
        onZoneEntered?.Invoke();
        
        // Desactivar si corresponde
        if (disableAfterReached)
        {
            gameObject.SetActive(false);
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        
        Debug.Log($"[TutorialZone] Jugador salió de la zona: {gameObject.name}");
        
        // Ejecutar eventos de salida
        onZoneExited?.Invoke();
    }
    
    /// <summary>
    /// Reiniciar el estado de la zona (útil para reiniciar el tutorial)
    /// </summary>
    public void Reset()
    {
        hasBeenReached = false;
        gameObject.SetActive(true);
    }
    
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showInEditor)
            return;
        
        Collider col = GetComponent<Collider>();
        if (col == null)
            return;
        
        Gizmos.color = gizmoColor;
        
        // Dibujar el volumen del collider
        if (col is BoxCollider boxCol)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(boxCol.center, boxCol.size);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(boxCol.center, boxCol.size);
        }
        else if (col is SphereCollider sphereCol)
        {
            Gizmos.DrawSphere(transform.position + sphereCol.center, sphereCol.radius * transform.lossyScale.x);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireSphere(transform.position + sphereCol.center, sphereCol.radius * transform.lossyScale.x);
        }
        else if (col is CapsuleCollider capsuleCol)
        {
            // Aproximación con esfera para cápsula
            Gizmos.DrawSphere(transform.position + capsuleCol.center, capsuleCol.radius * transform.lossyScale.x);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireSphere(transform.position + capsuleCol.center, capsuleCol.radius * transform.lossyScale.x);
        }
        
        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}
