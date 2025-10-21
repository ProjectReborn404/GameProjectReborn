using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// UnifiedInteractable: Sistema unificado de interacciones que combina todos los tipos de interacción
/// en un solo script modular. Soporta interacción manual (presionar tecla), automática (trigger),
/// placas de presión, cambios de color, y más.
/// </summary>
[RequireComponent(typeof(Collider))]
public class UnifiedInteractable : MonoBehaviour, IInteractable
{
    #region Enums
    
    [System.Serializable]
    public enum InteractionMode
    {
        Manual,             // Requiere presionar tecla (E) - como TutorialInteractable
        Trigger,            // Activación automática por trigger - como TriggerInteractable
        PressurePlate,      // Placa de presión con animación - como PressurePlate
        Hybrid              // Ambos: manual Y trigger
    }
    
    [System.Serializable]
    public enum TriggerActivation
    {
        OnEnter,            // Se activa al entrar
        OnStay,             // Se activa mientras está dentro
        OnExit,             // Se activa al salir
        OnEnterAndExit      // Se activa al entrar Y al salir
    }
    
    [System.Serializable]
    public enum ActivationType
    {
        Once,               // Solo se activa una vez
        EveryTime,          // Se activa cada vez (con cooldown)
        Toggleable,         // Alterna entre on/off
        WhileInside         // Permanece activo mientras está dentro (solo Trigger)
    }
    
    [System.Serializable]
    public enum FeedbackType
    {
        None,               // Sin feedback visual
        ColorChange,        // Cambio de color simple
        ColorCycle,         // Ciclo de colores - como SimpleColorInteractable
        MaterialSwap,       // Cambio de material
        PressAnimation      // Animación de presión - como PressurePlate
    }
    
    #endregion
    
    #region Inspector Fields
    
    [Header("=== MODO DE INTERACCIÓN ===")]
    [Tooltip("Cómo se activa este objeto")]
    public InteractionMode interactionMode = InteractionMode.Manual;
    
    [Header("=== CONFIGURACIÓN DE TRIGGER ===")]
    [Tooltip("Cuándo se activa el trigger (solo para modos Trigger/Hybrid/PressurePlate)")]
    public TriggerActivation triggerActivation = TriggerActivation.OnEnter;
    
    [Tooltip("Tag del objeto que puede activar (normalmente 'Player')")]
    public string activatorTag = "Player";
    
    [Tooltip("Solo activar si el objeto está específicamente ENCIMA")]
    public bool requireStandingOnTop = false;
    
    [Tooltip("Distancia del raycast para verificar si está encima")]
    public float standingCheckDistance = 0.5f;
    
    [Header("=== TIPO DE ACTIVACIÓN ===")]
    [Tooltip("Comportamiento de activación")]
    public ActivationType activationType = ActivationType.Once;
    
    [Tooltip("Tiempo mínimo dentro del trigger para activar (0 = instantáneo)")]
    public float requiredStayTime = 0f;
    
    [Tooltip("Cooldown entre activaciones (para EveryTime)")]
    public float cooldownTime = 1f;
    
    [Tooltip("Desactivar automáticamente al salir del trigger")]
    public bool deactivateOnExit = true;
    
    [Header("=== FEEDBACK VISUAL ===")]
    [Tooltip("Tipo de feedback visual")]
    public FeedbackType feedbackType = FeedbackType.ColorChange;
    
    [Tooltip("Renderer del objeto")]
    public Renderer targetRenderer;
    
    [Header("Color Change / Color Cycle")]
    [Tooltip("Color cuando está inactivo")]
    public Color inactiveColor = Color.gray;
    
    [Tooltip("Color cuando está activo")]
    public Color activeColor = Color.green;
    
    [Tooltip("Colores para ciclar (solo ColorCycle)")]
    public Color[] colorCycle = new Color[] { Color.yellow, Color.red, Color.green, Color.blue };
    
    [Tooltip("Hacer loop en los colores (solo ColorCycle)")]
    public bool loopColors = true;
    
    [Header("Material Swap")]
    [Tooltip("Material cuando está inactivo")]
    public Material inactiveMaterial;
    
    [Tooltip("Material cuando está activo")]
    public Material activeMaterial;
    
    [Header("Press Animation")]
    [Tooltip("Transform que se hundirá (puede ser este mismo objeto)")]
    public Transform pressurePad;
    
    [Tooltip("Distancia que se hunde")]
    public float pressDepth = 0.1f;
    
    [Tooltip("Velocidad de la animación")]
    public float pressSpeed = 5f;
    
    [Header("=== HIGHLIGHT (Manual Mode) ===")]
    [Tooltip("Material para resaltar cuando el jugador lo mira")]
    public Material highlightMaterial;
    
    [Tooltip("Activar highlight automático")]
    public bool useHighlight = false;
    
    [Header("=== AUDIO ===")]
    [Tooltip("Sonido al activarse")]
    public AudioClip activationSound;
    
    [Tooltip("Sonido al desactivarse")]
    public AudioClip deactivationSound;
    
    [Tooltip("AudioSource (se creará automáticamente si no existe)")]
    public AudioSource audioSource;
    
    [Header("=== INTEGRACIÓN CON TUTORIAL ===")]
    [Tooltip("Notificar al TutorialManager al activarse")]
    public bool notifyTutorialManager = false;
    
    [Tooltip("Referencia al TutorialManager")]
    public TutoriaManager tutorialManager;
    
    [Header("=== COMPORTAMIENTO ===")]
    [Tooltip("Desactivar el GameObject tras ser activado")]
    public bool disableAfterActivation = false;
    
    [Tooltip("Revertir a estado original al desactivar el objeto")]
    public bool revertOnDisable = true;
    
    [Header("=== EVENTOS ===")]
    [Tooltip("Evento al activarse")]
    public UnityEvent onActivate;
    
    [Tooltip("Evento al desactivarse")]
    public UnityEvent onDeactivate;
    
    [Tooltip("Evento mientras está dentro (Trigger mode)")]
    public UnityEvent onStayInside;
    
    [Header("=== DEBUG ===")]
    [Tooltip("Mostrar logs en consola")]
    public bool showDebugLogs = false;
    
    [Header("=== FÍSICA ===")]
    [Tooltip("¿El objeto debe tener gravedad?")]
    public bool useGravity = false;
    
    [Header("=== PRESSURE PLATE ===")]
    [Tooltip("¿La placa de presión debe ser trigger?")]
    public bool pressurePlateIsTrigger = false;
    
    #endregion
    
    #region Private Fields
    
    private bool isActive = false;
    private bool hasBeenActivated = false;
    private bool playerIsInside = false;
    private int objectsOnPlate = 0;
    private float timeInside = 0f;
    private float lastActivationTime = -999f;
    
    // Color cycle
    private int currentColorIndex = 0;
    private Color originalColor;
    private bool originalColorCaptured = false;
    
    // Materials
    private Material originalMaterial;
    
    // Press animation
    private Vector3 originalPosition;
    private Vector3 pressedPosition;
    
    // Collider
    private Collider triggerCollider;
    
    #endregion
    
    #region Unity Callbacks
    
    void Awake()
    {
        InitializeComponents();
        CaptureOriginalState();
        ConfigureCollider();
        ConfigureGravity();
    }
    
    void Start()
    {
        FindTutorialManager();
        UpdateVisuals();
    }
    
    void Update()
    {
        // Update para trigger mode y press animation
        if (interactionMode == InteractionMode.Trigger || 
            interactionMode == InteractionMode.Hybrid || 
            interactionMode == InteractionMode.PressurePlate)
        {
            HandleTriggerUpdate();
        }
        
        // Detección de PressurePlate con raycast (cuando no es trigger)
        if (interactionMode == InteractionMode.PressurePlate && !pressurePlateIsTrigger)
        {
            CheckPressurePlateRaycast();
        }
        
        // Animación de presión
        if (feedbackType == FeedbackType.PressAnimation)
        {
            AnimatePressure();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!ShouldRespondToTrigger())
            return;
        
        if (!other.CompareTag(activatorTag))
            return;
        
        if (requireStandingOnTop && !IsStandingOnTop(other))
        {
            DebugLog($"Objeto no está encima de {gameObject.name}");
            return;
        }
        
        objectsOnPlate++;
        
        if (!playerIsInside)
        {
            playerIsInside = true;
            timeInside = 0f;
            DebugLog($"Objeto entró en {gameObject.name}");
        }
        
        // Activar según configuración
        if (triggerActivation == TriggerActivation.OnEnter || 
            triggerActivation == TriggerActivation.OnEnterAndExit)
        {
            if (requiredStayTime <= 0)
            {
                TryActivate();
            }
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        if (!ShouldRespondToTrigger())
            return;
        
        if (!other.CompareTag(activatorTag))
            return;
        
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
        if (!ShouldRespondToTrigger())
            return;
        
        if (!other.CompareTag(activatorTag))
            return;
        
        objectsOnPlate = Mathf.Max(0, objectsOnPlate - 1);
        
        if (objectsOnPlate <= 0)
        {
            playerIsInside = false;
            timeInside = 0f;
            DebugLog($"Objeto salió de {gameObject.name}");
            
            // Activar según configuración
            if (triggerActivation == TriggerActivation.OnExit || 
                triggerActivation == TriggerActivation.OnEnterAndExit)
            {
                TryActivate();
            }
            
            // Desactivar si corresponde
            if (deactivateOnExit && isActive)
            {
                Deactivate();
            }
        }
    }
    
    void OnDisable()
    {
        if (revertOnDisable)
        {
            RevertToOriginalState();
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (interactionMode == InteractionMode.PressurePlate && !pressurePlateIsTrigger)
        {
            if (!collision.collider.CompareTag(activatorTag))
                return;
            
            // Verificar que la colisión sea desde arriba
            if (!IsCollisionFromAbove(collision))
            {
                DebugLog($"Colisión lateral ignorada en {gameObject.name}");
                return;
            }
            
            objectsOnPlate++;
            
            if (!playerIsInside)
            {
                playerIsInside = true;
                timeInside = 0f;
                DebugLog($"Objeto colisionó desde arriba con {gameObject.name}");
            }
            
            // Activar según configuración
            if (triggerActivation == TriggerActivation.OnEnter || 
                triggerActivation == TriggerActivation.OnEnterAndExit)
            {
                if (requiredStayTime <= 0)
                {
                    TryActivate();
                }
            }
        }
    }
    
    void OnCollisionStay(Collision collision)
    {
        if (interactionMode == InteractionMode.PressurePlate && !pressurePlateIsTrigger)
        {
            if (!collision.collider.CompareTag(activatorTag))
                return;
            
            // Verificar que sigue estando encima
            if (!IsCollisionFromAbove(collision))
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
            
            // Invocar evento de stay
            onStayInside?.Invoke();
        }
    }
    
    void OnCollisionExit(Collision collision)
    {
        if (interactionMode == InteractionMode.PressurePlate && !pressurePlateIsTrigger)
        {
            if (!collision.collider.CompareTag(activatorTag))
                return;
            
            objectsOnPlate = Mathf.Max(0, objectsOnPlate - 1);
            
            if (objectsOnPlate <= 0)
            {
                playerIsInside = false;
                timeInside = 0f;
                DebugLog($"Objeto dejó de colisionar con {gameObject.name}");
                
                // Activar según configuración
                if (triggerActivation == TriggerActivation.OnExit || 
                    triggerActivation == TriggerActivation.OnEnterAndExit)
                {
                    TryActivate();
                }
                
                // Desactivar si corresponde
                if (deactivateOnExit && isActive)
                {
                    Deactivate();
                }
            }
        }
    }
    
    // --- Agregado: Detección de PressurePlate con raycast desde el jugador ---
    void CheckPressurePlateRaycast()
    {
        // Buscar el jugador por tag
        GameObject player = GameObject.FindGameObjectWithTag(activatorTag);
        if (player == null)
            return;

        // Origen y dirección del raycast (igual que PlayerInteraction)
        Vector3 origin = player.transform.position + Vector3.up * 0.5f; // Ajusta el offset si tu pivot está en los pies
        Vector3 direction = Vector3.down;
        float rayDistance = standingCheckDistance + 0.5f;

        // Visualización del raycast (como PlayerInteraction)
        if (Application.isPlaying)
            Debug.DrawRay(origin, direction * rayDistance, Color.yellow);

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, rayDistance))
        {
            if (hit.collider == triggerCollider)
            {
                if (!playerIsInside)
                {
                    playerIsInside = true;
                    timeInside = 0f;
                    DebugLog($"Jugador detectado encima de {gameObject.name}");
                    if (triggerActivation == TriggerActivation.OnEnter || 
                        triggerActivation == TriggerActivation.OnEnterAndExit)
                    {
                        if (requiredStayTime <= 0)
                        {
                            TryActivate();
                        }
                    }
                }
            }
            else
            {
                if (playerIsInside)
                {
                    playerIsInside = false;
                    timeInside = 0f;
                    DebugLog($"Jugador salió de {gameObject.name}");
                    if (deactivateOnExit && isActive)
                    {
                        Deactivate();
                    }
                }
            }
        }
        else
        {
            if (playerIsInside)
            {
                playerIsInside = false;
                timeInside = 0f;
                if (deactivateOnExit && isActive)
                {
                    Deactivate();
                }
            }
        }
    }
    
    #endregion
    
    #region Initialization
    
    void InitializeComponents()
    {
        triggerCollider = GetComponent<Collider>();
        
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && (activationSound != null || deactivationSound != null))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
        
        if (pressurePad == null)
            pressurePad = transform;
    }
    
    void CaptureOriginalState()
    {
        // Capturar color original
        if (targetRenderer != null)
        {
            originalMaterial = targetRenderer.sharedMaterial;
            originalColorCaptured = TryGetColorFromRenderer(targetRenderer, out originalColor);
        }
        
        // Capturar posición original para press animation
        if (pressurePad != null)
        {
            originalPosition = pressurePad.localPosition;
            pressedPosition = originalPosition - Vector3.up * pressDepth;
        }
    }
    
    void ConfigureCollider()
    {
        if (triggerCollider == null)
            return;

        // Configurar como trigger según el modo y la opción de PressurePlate
        bool shouldBeTrigger = false;
        if (interactionMode == InteractionMode.PressurePlate)
        {
            shouldBeTrigger = pressurePlateIsTrigger;
        }
        else
        {
            shouldBeTrigger = (interactionMode == InteractionMode.Trigger || interactionMode == InteractionMode.Hybrid);
        }
        triggerCollider.isTrigger = shouldBeTrigger;
        DebugLog($"Collider configurado - isTrigger: {triggerCollider.isTrigger} para modo {interactionMode}");
    }
    
    void FindTutorialManager()
    {
        if (notifyTutorialManager && tutorialManager == null)
        {
            tutorialManager = FindObjectOfType<TutoriaManager>();
            if (tutorialManager == null && showDebugLogs)
            {
                Debug.LogWarning($"[UnifiedInteractable] No se encontró TutorialManager para {gameObject.name}");
            }
        }
    }
    
    void ConfigureGravity()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Solo aplicar gravedad si el collider NO es trigger
            rb.useGravity = useGravity && !triggerCollider.isTrigger;
            DebugLog($"Gravedad configurada: {rb.useGravity} en {gameObject.name}");
        }
    }
    
    #endregion
    
    #region Interaction Logic
    
    bool ShouldRespondToTrigger()
    {
        return interactionMode == InteractionMode.Trigger || 
               interactionMode == InteractionMode.Hybrid || 
               interactionMode == InteractionMode.PressurePlate;
    }
    
    void HandleTriggerUpdate()
    {
        if (playerIsInside)
        {
            timeInside += Time.deltaTime;
            
            // Verificar activación por tiempo
            if (!isActive && requiredStayTime > 0 && timeInside >= requiredStayTime)
            {
                if (triggerActivation == TriggerActivation.OnStay || 
                    triggerActivation == TriggerActivation.OnEnter)
                {
                    TryActivate();
                }
            }
            
            // Activación continua WhileInside
            if (activationType == ActivationType.WhileInside && !isActive)
            {
                Activate();
            }
            
            // Evento de stay
            onStayInside?.Invoke();
        }
        else
        {
            // Desactivar WhileInside
            if (activationType == ActivationType.WhileInside && isActive)
            {
                Deactivate();
            }
        }
    }
    
    void TryActivate()
    {
        // Verificar condiciones de activación
        switch (activationType)
        {
            case ActivationType.Once:
                if (!hasBeenActivated)
                    Activate();
                break;
                
            case ActivationType.EveryTime:
                // Para PressurePlate no aplicar cooldown: reactivar inmediatamente
                if (interactionMode == InteractionMode.PressurePlate)
                {
                    Activate();
                }
                else
                {
                    if (Time.time - lastActivationTime >= cooldownTime)
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
        
        // Aplicar feedback visual
        ApplyFeedback();
        
        // Reproducir sonido
        PlaySound(activationSound);
        
        // Notificar tutorial
        if (notifyTutorialManager && tutorialManager != null)
        {
            tutorialManager.NotifyInteraction(gameObject);
        }
        
        // Ejecutar eventos
        onActivate?.Invoke();
        
        // Desactivar GameObject si corresponde
        if (disableAfterActivation)
        {
            gameObject.SetActive(false);
        }
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
    
    #endregion
    
    #region Visual Feedback
    
    void ApplyFeedback()
    {
        switch (feedbackType)
        {
            case FeedbackType.ColorChange:
                ApplyColor(activeColor);
                break;
                
            case FeedbackType.ColorCycle:
                ApplyColorCycle();
                break;
                
            case FeedbackType.MaterialSwap:
                ApplyMaterial(activeMaterial);
                break;
                
            case FeedbackType.PressAnimation:
                // La animación se maneja en Update
                break;
        }
    }
    
    void UpdateVisuals()
    {
        switch (feedbackType)
        {
            case FeedbackType.ColorChange:
                ApplyColor(isActive ? activeColor : inactiveColor);
                break;
                
            case FeedbackType.MaterialSwap:
                ApplyMaterial(isActive ? activeMaterial : inactiveMaterial);
                break;
                
            case FeedbackType.PressAnimation:
                // La animación se maneja en Update
                break;
        }
    }
    
    void ApplyColorCycle()
    {
        if (colorCycle == null || colorCycle.Length == 0)
            return;
        
        ApplyColor(colorCycle[currentColorIndex]);
        
        currentColorIndex++;
        if (currentColorIndex >= colorCycle.Length)
        {
            currentColorIndex = loopColors ? 0 : colorCycle.Length - 1;
        }
    }
    
    void ApplyColor(Color color)
    {
        if (targetRenderer == null)
            return;
        
        var mpb = new MaterialPropertyBlock();
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetColor("_Color", color);
        mpb.SetColor("_BaseColor", color);
        mpb.SetColor("_EmissionColor", color * 0.5f);
        targetRenderer.SetPropertyBlock(mpb);
    }
    
    void ApplyMaterial(Material material)
    {
        if (targetRenderer == null || material == null)
            return;
        
        targetRenderer.material = material;
    }
    
    void AnimatePressure()
    {
        if (pressurePad == null)
            return;
        
        Vector3 targetPos = isActive ? pressedPosition : originalPosition;
        pressurePad.localPosition = Vector3.Lerp(
            pressurePad.localPosition,
            targetPos,
            pressSpeed * Time.deltaTime
        );
    }
    
    void RevertToOriginalState()
    {
        if (feedbackType == FeedbackType.ColorChange || feedbackType == FeedbackType.ColorCycle)
        {
            if (originalColorCaptured)
                ApplyColor(originalColor);
        }
        else if (feedbackType == FeedbackType.MaterialSwap)
        {
            if (originalMaterial != null)
                ApplyMaterial(originalMaterial);
        }
        else if (feedbackType == FeedbackType.PressAnimation)
        {
            if (pressurePad != null)
                pressurePad.localPosition = originalPosition;
        }
    }
    
    bool TryGetColorFromRenderer(Renderer r, out Color c)
    {
        c = Color.white;
        if (r == null)
            return false;
        
        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        
        if (mpb != null && !mpb.isEmpty)
        {
            if (mpb.GetVector("_Color") != default)
            {
                c = mpb.GetColor("_Color");
                return true;
            }
            if (mpb.GetVector("_BaseColor") != default)
            {
                c = mpb.GetColor("_BaseColor");
                return true;
            }
        }
        
        if (r.sharedMaterial != null)
        {
            if (r.sharedMaterial.HasProperty("_Color"))
            {
                c = r.sharedMaterial.GetColor("_Color");
                return true;
            }
            if (r.sharedMaterial.HasProperty("_BaseColor"))
            {
                c = r.sharedMaterial.GetColor("_BaseColor");
                return true;
            }
        }
        
        return false;
    }
    
    #endregion
    
    #region Utility
    
    bool IsStandingOnTop(Collider other)
    {
        Vector3 rayOrigin = other.transform.position;
        RaycastHit hit;
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, standingCheckDistance))
        {
            return hit.collider == triggerCollider;
        }
        
        return false;
    }
    
    bool IsCollisionFromAbove(Collision collision)
    {
        // Verificar si la mayoría de los puntos de contacto están en la parte superior
        if (collision.contactCount == 0)
            return false;
        
        int topContacts = 0;
        foreach (ContactPoint contact in collision.contacts)
        {
            // Verificar si la normal del contacto apunta hacia arriba (el jugador está presionando desde arriba)
            if (contact.normal.y > 0.5f)
            {
                topContacts++;
            }
        }
        
        // Si la mayoría de los contactos son desde arriba, está encima
        return topContacts > collision.contactCount / 2;
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
            Debug.Log($"[UnifiedInteractable] {message}");
        }
    }
    
    #endregion
    
    #region IInteractable Implementation
    
    /// <summary>
    /// Implementación de IInteractable - para interacción manual con tecla E
    /// </summary>
    public void Interact(GameObject interactor)
    {
        if (interactionMode == InteractionMode.Manual || interactionMode == InteractionMode.Hybrid)
        {
            DebugLog($"Interacción manual con {gameObject.name}");
            TryActivate();
        }
    }
    
    #endregion
    
    #region Public Methods
    
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
    /// Resetear completamente el estado
    /// </summary>
    public void ResetState()
    {
        isActive = false;
        hasBeenActivated = false;
        playerIsInside = false;
        objectsOnPlate = 0;
        timeInside = 0f;
        lastActivationTime = -999f;
        currentColorIndex = 0;
        
        RevertToOriginalState();
        UpdateVisuals();
    }
    
    /// <summary>
    /// Verificar si está actualmente activo
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }
    
    /// <summary>
    /// Activar/desactivar highlight (para modo Manual)
    /// </summary>
    public void SetHighlight(bool active)
    {
        if (!useHighlight || targetRenderer == null || highlightMaterial == null)
            return;
        
        if (active)
        {
            targetRenderer.material = highlightMaterial;
        }
        else
        {
            targetRenderer.material = isActive && activeMaterial != null ? activeMaterial : 
                                    (!isActive && inactiveMaterial != null ? inactiveMaterial : originalMaterial);
        }
    }
    
    #endregion
    
    #region Editor Gizmos
    
#if UNITY_EDITOR
    void OnValidate()
    {
        // Reconfigurar collider y gravedad cuando se cambia el modo en el Inspector
        ConfigureCollider();
        ConfigureGravity();
    }
    
    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
            return;
        
        Color gizmoColor = isActive ? activeColor : inactiveColor;
        gizmoColor.a = 0.3f;
        Gizmos.color = gizmoColor;
        
        if (col is BoxCollider boxCol)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(boxCol.center, boxCol.size);
            gizmoColor.a = 1f;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(boxCol.center, boxCol.size);
        }
        else if (col is SphereCollider sphereCol)
        {
            Gizmos.DrawSphere(transform.position + sphereCol.center, sphereCol.radius * transform.lossyScale.x);
        }
        
        Gizmos.matrix = Matrix4x4.identity;
    }
    
    void OnDrawGizmosSelected()
    {
        if (requireStandingOnTop)
        {
            Gizmos.color = Color.yellow;
            Vector3 top = transform.position + Vector3.up * standingCheckDistance;
            Gizmos.DrawLine(transform.position, top);
            Gizmos.DrawWireSphere(top, 0.1f);
        }
    }
#endif
    
    #endregion
}