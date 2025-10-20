using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TutorialInteractable: Objeto interactuable que notifica al TutorialManager
/// cuando es interactuado. Implementa IInteractable para ser compatible con PlayerInteraction.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialInteractable : MonoBehaviour, IInteractable
{
    [Header("Tutorial")]
    [Tooltip("Referencia al TutorialManager (se buscará automáticamente si no se asigna)")]
    public TutoriaManager tutorialManager;
    
    [Header("Comportamiento")]
    [Tooltip("¿Este objeto se debe desactivar tras ser interactuado?")]
    public bool disableAfterInteraction = false;
    
    [Tooltip("¿Puede ser interactuado múltiples veces?")]
    public bool canInteractMultipleTimes = false;
    
    [Header("Feedback Visual (Opcional)")]
    [Tooltip("Material que se aplicará al objeto cuando sea detectado por el jugador")]
    public Material highlightMaterial;
    
    [Tooltip("Renderer del objeto para cambiar material")]
    public Renderer objectRenderer;
    
    [Header("Eventos")]
    public UnityEvent onInteract;
    
    private Material originalMaterial;
    private bool hasBeenInteracted = false;
    
    void Start()
    {
        // Buscar TutorialManager si no está asignado
        if (tutorialManager == null)
        {
            tutorialManager = FindObjectOfType<TutoriaManager>();
            if (tutorialManager == null)
            {
                Debug.LogWarning($"[TutorialInteractable] No se encontró TutorialManager en la escena. Objeto: {gameObject.name}");
            }
        }
        
        // Guardar material original
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }
        else
        {
            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null)
            {
                originalMaterial = objectRenderer.material;
            }
        }
        
        // Asegurarse de que tenga el tag correcto
        if (!gameObject.CompareTag("interactive"))
        {
            Debug.LogWarning($"[TutorialInteractable] El objeto {gameObject.name} no tiene el tag 'interactive'. Asignándolo automáticamente.");
            gameObject.tag = "interactive";
        }
    }
    
    public void Interact(GameObject interactor)
    {
        // Verificar si puede ser interactuado
        if (hasBeenInteracted && !canInteractMultipleTimes)
        {
            Debug.Log($"[TutorialInteractable] {gameObject.name} ya fue interactuado y no puede ser usado nuevamente.");
            return;
        }
        
        hasBeenInteracted = true;
        
        Debug.Log($"[TutorialInteractable] Objeto interactuado: {gameObject.name}");
        
        // Notificar al TutorialManager
        if (tutorialManager != null)
        {
            tutorialManager.NotifyInteraction(gameObject);
        }
        
        // Ejecutar eventos personalizados
        onInteract?.Invoke();
        
        // Desactivar si corresponde
        if (disableAfterInteraction)
        {
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Método opcional para resaltar el objeto cuando el jugador lo está mirando
    /// Puedes llamar esto desde PlayerInteraction si lo modificas
    /// </summary>
    public void Highlight(bool active)
    {
        if (objectRenderer == null || highlightMaterial == null)
            return;
        
        if (active)
        {
            objectRenderer.material = highlightMaterial;
        }
        else
        {
            objectRenderer.material = originalMaterial;
        }
    }
    
    /// <summary>
    /// Reiniciar el estado del objeto (útil para reiniciar el tutorial)
    /// </summary>
    public void Reset()
    {
        hasBeenInteracted = false;
        gameObject.SetActive(true);
        
        if (objectRenderer != null && originalMaterial != null)
        {
            objectRenderer.material = originalMaterial;
        }
    }
}
