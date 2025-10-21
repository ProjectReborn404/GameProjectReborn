using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TriggerInteractable: Objeto que se activa automáticamente cuando el jugador
/// se posiciona encima o entra en contacto con él (sin necesidad de presionar tecla).
/// Útil para placas de presión, zonas de activación, portales, etc.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TriggerInteractable : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public enum TriggerMode
    {
        OnEnter,        // Se activa al entrar
        OnStay,         // Se activa continuamente mientras está dentro
        OnExit,         // Se activa al salir
        OnEnterAndExit  // Se activa tanto al entrar como al salir
    }
    
    [System.Serializable]
    public enum ActivationType
    {
        Once,           // Solo se activa una vez
        EveryTime,      // Se activa cada vez que se cumple la condición
        Toggleable,     // Alterna entre activado/desactivado
        WhileInside     // Permanece activo mientras el jugador está dentro
    }
    
    [Header("Modo de Trigger")]
    [Tooltip("Cuándo se debe activar el objeto")]
    public TriggerMode triggerMode = TriggerMode.OnEnter;
    
    [Tooltip("Tipo de activación")]
    public ActivationType activationType = ActivationType.Once;
    
    [Header("Detección")]
    [Tooltip("Tag del objeto que puede activar este trigger (normalmente 'Player')")]
    public string activatorTag = "Player";
    
    [Tooltip("Solo detectar si el jugador está específicamente ENCIMA (usa raycast hacia abajo)")]
    public bool requireStandingOnTop = false;
    
    [Tooltip("Distancia del raycast para verificar si está encima")]
    public float standingCheckDistance = 0.5f;
    
    [Header("Tiempo")]
    [Tooltip("Tiempo mínimo que debe estar dentro para activarse (0 = instantáneo)")]
    public float requiredStayTime = 0f;
    
    [Tooltip("Tiempo de cooldown entre activaciones (solo para EveryTime)")]
    public float cooldownTime = 1f;
    
    [Header("Feedback Visual")]
    [Tooltip("Renderer del objeto para cambiar apariencia")]
    public Renderer targetRenderer;
    
    [Tooltip("Material cuando está inactivo")]
    public Material inactiveMaterial;
    
    [Tooltip("Material cuando está activo")]
    public Material activeMaterial;
    
    [Tooltip("Color cuando está inactivo (si no usas materiales)")]
    public Color inactiveColor = Color.gray;
    
    [Tooltip("Color cuando está activo (si no usas materiales)")]
    public Color activeColor = Color.green;
    
    [Tooltip("Usar colores en lugar de materiales")]
    public bool useColors = true;
    
    [Header("Feedback de Audio (Opcional)")]
    [Tooltip("Sonido al activarse")]
    public AudioClip activationSound;
    
    [Tooltip("Sonido al desactivarse")]
    public AudioClip deactivationSound;
    
    [Tooltip("AudioSource para reproducir sonidos (se creará automáticamente si no existe)")]
    public AudioSource audioSource;
    
    [Header("Integración con Tutorial")]
    [Tooltip("Notificar al TutorialManager cuando se active")]
    public bool notifyTutorialManager = false;
    
    [Tooltip("Referencia al TutorialManager (se buscará automáticamente si está vacío)")]
    public TutoriaManager tutorialManager;
    
    [Header("Eventos")]
    [Tooltip("Evento cuando se activa")]
    public UnityEvent onActivate;
    
    [Tooltip("Evento cuando se desactiva")]
    public UnityEvent onDeactivate;
    
    [Tooltip("Evento mientras el jugador está dentro (se llama en Update)")]
    public UnityEvent onStayInside;
    
    [Header("Debug")]
    [Tooltip("Mostrar información de debug en consola")]
    public bool showDebugLogs = false;
    
    // Estado interno
    private bool isActive = false;
    private bool hasBeenActivated = false;
    private bool playerIsInside = false;
    private float timeInside = 0f;
    private float lastActivationTime = -999f;
    private Collider triggerCollider;
    private Material originalMaterial;
    private Color originalColor;
    
    void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
        
        // Configurar renderer
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }
        
        if (targetRenderer != null)
        {
            originalMaterial = targetRenderer.sharedMaterial;
            if (targetRenderer.sharedMaterial != null)
            {
                if (targetRenderer.sharedMaterial.HasProperty("_Color"))
                    originalColor = targetRenderer.sharedMaterial.GetColor("_Color");
                else if (targetRenderer.sharedMaterial.HasProperty("_BaseColor"))
                    originalColor = targetRenderer.sharedMaterial.GetColor("_BaseColor");
            }
        }
        
        // Configurar audio
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && (activationSound != null || deactivationSound != null))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }
    
    void Start()
    {
        // Buscar TutorialManager si es necesario
        if (notifyTutorialManager && tutorialManager == null)
        {
            tutorialManager = FindObjectOfType<TutoriaManager>();
        }
        
        // Establecer apariencia inicial
        UpdateVisuals();
    }
    
    void Update()
    {
        if (playerIsInside)
        {
            timeInside += Time.deltaTime;
            
            // Verificar si debe activarse por tiempo
            if (!isActive && requiredStayTime > 0 && timeInside >= requiredStayTime)
            {
                if (triggerMode == TriggerMode.OnStay || triggerMode == TriggerMode.OnEnter)
                {
                    Activate();
                }
            }
            
            // Activación continua mientras está dentro
            if (activationType == ActivationType.WhileInside && !isActive)
            {
                Activate();
            }
            
            // Evento OnStayInside
            onStayInside?.Invoke();
        }
        else
        {
            // Desactivar si era WhileInside
            if (activationType == ActivationType.WhileInside && isActive)
            {
                Deactivate();
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(activatorTag))
            return;
        
        // Verificar si está específicamente encima
        if (requireStandingOnTop && !IsStandingOnTop(other))
        {
            DebugLog($"Jugador no está encima de {gameObject.name}");
            return;
        }
        
        playerIsInside = true;
        timeInside = 0f;
        
        DebugLog($"Jugador entró en {gameObject.name}");
        
        // Activar según el modo
        if (triggerMode == TriggerMode.OnEnter || triggerMode == TriggerMode.OnEnterAndExit)
        {
            if (requiredStayTime <= 0) // Solo si no requiere tiempo de espera
            {
                TryActivate();
            }
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(activatorTag))
            return;
        
        // Verificar si está específicamente encima
        if (requireStandingOnTop && !IsStandingOnTop(other))
        {
            if (playerIsInside)
            {
                playerIsInside = false;
                timeInside = 0f;
            }
            return;
        }
        
        if (!playerIsInside)
        {
            playerIsInside = true;
            timeInside = 0f;
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(activatorTag))
            return;
        
        playerIsInside = false;
        timeInside = 0f;
        
        DebugLog($"Jugador salió de {gameObject.name}");
        
        // Activar según el modo
        if (triggerMode == TriggerMode.OnExit || triggerMode == TriggerMode.OnEnterAndExit)
        {
            TryActivate();
        }
    }
    
    bool IsStandingOnTop(Collider other)
    {
        // Raycast desde el jugador hacia abajo para verificar si está sobre este objeto
        Vector3 rayOrigin = other.transform.position;
        RaycastHit hit;
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, standingCheckDistance))
        {
            return hit.collider == triggerCollider;
        }
        
        return false;
    }
    
    void TryActivate()
    {
        // Verificar si puede activarse según el tipo
        switch (activationType)
        {
            case ActivationType.Once:
                if (!hasBeenActivated)
                {
                    Activate();
                }
                break;
                
            case ActivationType.EveryTime:
                if (Time.time - lastActivationTime >= cooldownTime)
                {
                    Activate();
                }
                break;
                
            case ActivationType.Toggleable:
                if (isActive)
                    Deactivate();
                else
                    Activate();
                break;
                
            case ActivationType.WhileInside:
                if (!isActive)
                    Activate();
                break;
        }
    }
    
    void Activate()
    {
        isActive = true;
        hasBeenActivated = true;
        lastActivationTime = Time.time;
        
        DebugLog($"{gameObject.name} ACTIVADO");
        
        // Actualizar visuales
        UpdateVisuals();
        
        // Reproducir sonido
        PlaySound(activationSound);
        
        // Notificar al TutorialManager
        if (notifyTutorialManager && tutorialManager != null)
        {
            tutorialManager.NotifyInteraction(gameObject);
        }
        
        // Ejecutar eventos
        onActivate?.Invoke();
    }
    
    void Deactivate()
    {
        isActive = false;
        
        DebugLog($"{gameObject.name} DESACTIVADO");
        
        // Actualizar visuales
        UpdateVisuals();
        
        // Reproducir sonido
        PlaySound(deactivationSound);
        
        // Ejecutar eventos
        onDeactivate?.Invoke();
    }
    
    void UpdateVisuals()
    {
        if (targetRenderer == null)
            return;
        
        if (useColors)
        {
            // Usar colores
            Color targetColor = isActive ? activeColor : inactiveColor;
            
            var mpb = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", targetColor);
            mpb.SetColor("_BaseColor", targetColor);
            targetRenderer.SetPropertyBlock(mpb);
        }
        else
        {
            // Usar materiales
            if (isActive && activeMaterial != null)
            {
                targetRenderer.material = activeMaterial;
            }
            else if (!isActive && inactiveMaterial != null)
            {
                targetRenderer.material = inactiveMaterial;
            }
            else
            {
                targetRenderer.material = originalMaterial;
            }
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[TriggerInteractable] {message}");
        }
    }
    
    // Implementación de IInteractable (para compatibilidad con PlayerInteraction)
    // Aunque este objeto se activa automáticamente, puede ser activado manualmente también
    public void Interact(GameObject interactor)
    {
        DebugLog($"Interacción manual con {gameObject.name}");
        TryActivate();
    }
    
    /// <summary>
    /// Forzar activación desde código externo
    /// </summary>
    public void ForceActivate()
    {
        Activate();
    }
    
    /// <summary>
    /// Forzar desactivación desde código externo
    /// </summary>
    public void ForceDeactivate()
    {
        Deactivate();
    }
    
    /// <summary>
    /// Resetear el estado del objeto
    /// </summary>
    public void ResetState()
    {
        isActive = false;
        hasBeenActivated = false;
        playerIsInside = false;
        timeInside = 0f;
        lastActivationTime = -999f;
        UpdateVisuals();
    }
    
    /// <summary>
    /// Verificar si está actualmente activo
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }
    
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Dibujar el área de trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
            return;
        
        Gizmos.color = isActive ? activeColor : inactiveColor;
        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
        
        if (col is BoxCollider boxCol)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(boxCol.center, boxCol.size);
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 1f);
            Gizmos.DrawWireCube(boxCol.center, boxCol.size);
        }
        else if (col is SphereCollider sphereCol)
        {
            Gizmos.DrawSphere(transform.position + sphereCol.center, sphereCol.radius * transform.lossyScale.x);
        }
        
        // Dibujar indicador de "standing on top" si está activado
        if (requireStandingOnTop && Application.isPlaying && playerIsInside)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * standingCheckDistance, 0.2f);
        }
        
        Gizmos.matrix = Matrix4x4.identity;
    }
    
    void OnDrawGizmosSelected()
    {
        // Dibujar información adicional cuando está seleccionado
        if (requireStandingOnTop)
        {
            Gizmos.color = Color.yellow;
            Vector3 top = transform.position + Vector3.up * standingCheckDistance;
            Gizmos.DrawLine(transform.position, top);
            Gizmos.DrawWireSphere(top, 0.1f);
        }
    }
#endif
}
