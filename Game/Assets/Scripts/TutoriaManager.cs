using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TutorialManager: Asistente completo para manejar la escena del tutorial.
/// Gestiona fases, aparición de objetos, verificación de completitud,
/// y posicionamiento del jugador y cámara según el progreso.
/// </summary>
public class TutoriaManager : MonoBehaviour
{
    #region Nested Classes
    
    [System.Serializable]
    public class TutorialPhase
    {
        [Header("Identificación")]
        public string phaseName = "Fase 1";
        [TextArea(2, 4)]
        public string description = "Descripción de la fase del tutorial";
        
        [Header("Objetos a Activar")]
        [Tooltip("Objetos que aparecerán al iniciar esta fase")]
        public GameObject[] objectsToSpawn;
        
        [Tooltip("Objetos que se desactivarán al iniciar esta fase")]
        public GameObject[] objectsToHide;
        
        [Header("Posicionamiento")]
        [Tooltip("Posición donde se colocará el jugador al iniciar esta fase (dejar en 0,0,0 para no mover)")]
        public Vector3 playerStartPosition;
        
        [Tooltip("Rotación del jugador (en grados Euler)")]
        public Vector3 playerStartRotation;
        
        [Tooltip("Si está marcado, el jugador será teletransportado a la posición especificada")]
        public bool repositionPlayer = false;
        
        [Header("Configuración de Cámara")]
        [Tooltip("Altura de la cámara para esta fase (si useCameraHeight está activo)")]
        public float cameraHeight = 5f;
        
        [Tooltip("Aplicar altura de cámara específica para esta fase")]
        public bool useCameraHeight = false;
        
        [Tooltip("Colliders que actuarán como límites de cámara para esta fase")]
        public Collider[] cameraLimitColliders;
        
        [Tooltip("Tag para buscar colliders de límite de cámara (alternativa a asignar manualmente)")]
        public string cameraLimitTag = "";
        
        [Header("Condiciones de Completitud")]
        [Tooltip("Tipo de condición para completar esta fase")]
        public CompletionType completionType = CompletionType.Interaction;
        
        [Tooltip("Objeto que debe ser interactuado (si completionType es Interaction)")]
        public GameObject targetInteractable;
        
        [Tooltip("Zona que debe ser alcanzada (si completionType es ReachZone)")]
        public Collider targetZone;
        
        [Tooltip("Tiempo en segundos que debe pasar (si completionType es Timer)")]
        public float timerDuration = 5f;
        
        [Tooltip("Número de objetos que deben ser interactuados (si completionType es CollectMultiple)")]
        public int itemsToCollect = 3;
        
        [Header("Habilidades a Desbloquear")]
        [Tooltip("¿Desbloquear doble salto al completar esta fase?")]
        public bool unlockDoubleJump = false;
        
        [Header("Eventos")]
        public UnityEvent onPhaseStart;
        public UnityEvent onPhaseComplete;
        
        [HideInInspector] public bool isComplete = false;
        [HideInInspector] public int currentItemsCollected = 0;
        [HideInInspector] public float elapsedTime = 0f;
    }
    
    public enum CompletionType
    {
        Interaction,      // Interactuar con un objeto específico
        ReachZone,        // Llegar a una zona determinada
        Timer,            // Esperar cierto tiempo
        CollectMultiple,  // Recolectar/interactuar con múltiples objetos
        Manual            // Completar manualmente desde código
    }
    
    #endregion
    
    #region Inspector Fields
    
    [Header("Referencias")]
    [Tooltip("Transform del jugador")]
    public Transform playerTransform;
    
    [Tooltip("Script de movimiento del jugador")]
    public Movement playerMovement;
    
    [Tooltip("Cámara del tutorial")]
    public TutorialCamera tutorialCamera;
    
    [Header("Fases del Tutorial")]
    [Tooltip("Lista de fases del tutorial en orden")]
    public List<TutorialPhase> phases = new List<TutorialPhase>();
    
    [Header("Configuración General")]
    [Tooltip("Índice de la fase inicial (normalmente 0)")]
    public int startingPhaseIndex = 0;
    
    [Tooltip("Activar logs de debug en consola")]
    public bool debugMode = true;
    
    [Tooltip("Tiempo de transición entre fases (en segundos)")]
    public float transitionDelay = 0.5f;
    
    [Header("UI (Opcional)")]
    [Tooltip("Texto UI para mostrar instrucciones de la fase actual")]
    public UnityEngine.UI.Text instructionText;
    
    [Tooltip("Texto UI para mostrar el nombre de la fase")]
    public UnityEngine.UI.Text phaseNameText;
    
    #endregion
    
    #region Private Fields
    
    private int currentPhaseIndex = -1;
    private TutorialPhase currentPhase;
    private bool isTransitioning = false;
    private HashSet<GameObject> interactedObjects = new HashSet<GameObject>();
    
    #endregion
    
    #region Unity Callbacks
    
    void Start()
    {
        ValidateReferences();
        InitializeTutorial();
    }
    
    void Update()
    {
        if (currentPhase == null || currentPhase.isComplete || isTransitioning)
            return;
        
        CheckPhaseCompletion();
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Para detectar cuando el jugador entra en zonas de completitud
        if (currentPhase != null && !currentPhase.isComplete)
        {
            if (currentPhase.completionType == CompletionType.ReachZone)
            {
                if (currentPhase.targetZone == other && other.CompareTag("Player"))
                {
                    CompleteCurrentPhase();
                }
            }
        }
    }
    
    #endregion
    
    #region Initialization
    
    void ValidateReferences()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
            else
                Debug.LogError("[TutorialManager] No se encontró el Transform del jugador. Asigna manualmente.");
        }
        
        if (playerMovement == null && playerTransform != null)
        {
            playerMovement = playerTransform.GetComponent<Movement>();
            if (playerMovement == null)
                Debug.LogWarning("[TutorialManager] No se encontró el componente Movement en el jugador.");
        }
        
        if (tutorialCamera == null)
        {
            tutorialCamera = FindObjectOfType<TutorialCamera>();
            if (tutorialCamera == null)
                Debug.LogWarning("[TutorialManager] No se encontró TutorialCamera en la escena.");
        }
    }
    
    void InitializeTutorial()
    {
        // Desactivar todos los objetos de todas las fases al inicio
        foreach (var phase in phases)
        {
            foreach (var obj in phase.objectsToSpawn)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }
        
        // Iniciar la primera fase
        StartPhase(startingPhaseIndex);
    }
    
    #endregion
    
    #region Phase Management
    
    public void StartPhase(int phaseIndex)
    {
        if (phaseIndex < 0 || phaseIndex >= phases.Count)
        {
            LogDebug($"Índice de fase inválido: {phaseIndex}");
            return;
        }
        
        StartCoroutine(TransitionToPhase(phaseIndex));
    }
    
    IEnumerator TransitionToPhase(int phaseIndex)
    {
        isTransitioning = true;
        
        // Desactivar objetos de la fase anterior
        if (currentPhase != null)
        {
            foreach (var obj in currentPhase.objectsToSpawn)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }
        
        yield return new WaitForSeconds(transitionDelay);
        
        currentPhaseIndex = phaseIndex;
        currentPhase = phases[phaseIndex];
        currentPhase.isComplete = false;
        currentPhase.currentItemsCollected = 0;
        currentPhase.elapsedTime = 0f;
        
        LogDebug($"Iniciando fase {phaseIndex}: {currentPhase.phaseName}");
        
        // Activar objetos de la nueva fase
        foreach (var obj in currentPhase.objectsToSpawn)
        {
            if (obj != null)
                obj.SetActive(true);
        }
        
        // Desactivar objetos específicos
        foreach (var obj in currentPhase.objectsToHide)
        {
            if (obj != null)
                obj.SetActive(false);
        }
        
        // Posicionar jugador
        if (currentPhase.repositionPlayer && playerTransform != null)
        {
            RepositionPlayer(currentPhase.playerStartPosition, currentPhase.playerStartRotation);
        }
        
        // Configurar cámara
        ConfigureCamera();
        
        // Actualizar UI
        UpdateUI();
        
        // Ejecutar eventos de inicio de fase
        currentPhase.onPhaseStart?.Invoke();
        
        isTransitioning = false;
    }
    
    void CheckPhaseCompletion()
    {
        if (currentPhase == null || currentPhase.isComplete)
            return;
        
        bool shouldComplete = false;
        
        switch (currentPhase.completionType)
        {
            case CompletionType.Timer:
                currentPhase.elapsedTime += Time.deltaTime;
                if (currentPhase.elapsedTime >= currentPhase.timerDuration)
                    shouldComplete = true;
                break;
                
            case CompletionType.CollectMultiple:
                if (currentPhase.currentItemsCollected >= currentPhase.itemsToCollect)
                    shouldComplete = true;
                break;
                
            case CompletionType.Interaction:
                // Se verifica mediante el método NotifyInteraction llamado externamente
                break;
                
            case CompletionType.ReachZone:
                // Se verifica en OnTriggerEnter
                if (currentPhase.targetZone != null && playerTransform != null)
                {
                    if (currentPhase.targetZone.bounds.Contains(playerTransform.position))
                        shouldComplete = true;
                }
                break;
                
            case CompletionType.Manual:
                // Se completa manualmente llamando a CompleteCurrentPhase()
                break;
        }
        
        if (shouldComplete)
        {
            CompleteCurrentPhase();
        }
    }
    
    void CompleteCurrentPhase()
    {
        if (currentPhase == null || currentPhase.isComplete)
            return;
        
        currentPhase.isComplete = true;
        LogDebug($"Fase completada: {currentPhase.phaseName}");
        
        // Desbloquear habilidades
        if (currentPhase.unlockDoubleJump && playerMovement != null)
        {
            playerMovement.UnlockDoubleJump();
            LogDebug("¡Doble salto desbloqueado!");
        }
        
        // Ejecutar eventos de completitud
        currentPhase.onPhaseComplete?.Invoke();
        
        // Avanzar a la siguiente fase automáticamente
        if (currentPhaseIndex + 1 < phases.Count)
        {
            StartPhase(currentPhaseIndex + 1);
        }
        else
        {
            LogDebug("¡Tutorial completado!");
            OnTutorialComplete();
        }
    }
    
    #endregion
    
    #region Player & Camera Control
    
    void RepositionPlayer(Vector3 position, Vector3 rotation)
    {
        if (playerTransform == null)
            return;
        
        // Desactivar CharacterController temporalmente para permitir teletransporte
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;
        
        playerTransform.position = position;
        playerTransform.rotation = Quaternion.Euler(rotation);
        
        if (cc != null)
            cc.enabled = true;
        
        LogDebug($"Jugador reposicionado a {position}");
    }
    
    void ConfigureCamera()
    {
        if (tutorialCamera == null)
            return;
        
        // Configurar altura de cámara
        if (currentPhase.useCameraHeight)
        {
            tutorialCamera.fixedHeight = currentPhase.cameraHeight;
            tutorialCamera.useInitialHeight = false;
        }
        
        // Configurar límites de cámara
        if (currentPhase.cameraLimitColliders != null && currentPhase.cameraLimitColliders.Length > 0)
        {
            tutorialCamera.useLimits = true;
            tutorialCamera.limitColliders = currentPhase.cameraLimitColliders;
        }
        else if (!string.IsNullOrEmpty(currentPhase.cameraLimitTag))
        {
            tutorialCamera.useLimits = true;
            tutorialCamera.useCollidersByTag = true;
            tutorialCamera.limitTag = currentPhase.cameraLimitTag;
        }
        else
        {
            tutorialCamera.useLimits = false;
        }
        
        LogDebug("Cámara configurada para la fase actual");
    }
    
    #endregion
    
    #region Public Methods - External Notifications
    
    /// <summary>
    /// Llamar este método desde objetos interactuables cuando sean interactuados
    /// </summary>
    public void NotifyInteraction(GameObject interactedObject)
    {
        if (currentPhase == null || currentPhase.isComplete)
            return;
        
        if (interactedObjects.Contains(interactedObject))
            return; // Ya fue interactuado anteriormente
        
        interactedObjects.Add(interactedObject);
        
        switch (currentPhase.completionType)
        {
            case CompletionType.Interaction:
                if (currentPhase.targetInteractable == interactedObject)
                {
                    LogDebug($"Objeto objetivo interactuado: {interactedObject.name}");
                    CompleteCurrentPhase();
                }
                break;
                
            case CompletionType.CollectMultiple:
                currentPhase.currentItemsCollected++;
                LogDebug($"Objetos recolectados: {currentPhase.currentItemsCollected}/{currentPhase.itemsToCollect}");
                break;
        }
    }
    
    /// <summary>
    /// Notificar que el jugador ha entrado en una zona
    /// </summary>
    public void NotifyZoneEntered(Collider zone)
    {
        if (currentPhase == null || currentPhase.isComplete)
            return;
        
        if (currentPhase.completionType == CompletionType.ReachZone)
        {
            if (currentPhase.targetZone == zone)
            {
                LogDebug($"Zona objetivo alcanzada: {zone.name}");
                CompleteCurrentPhase();
            }
        }
    }
    
    /// <summary>
    /// Completar manualmente la fase actual
    /// </summary>
    public void ManualCompletePhase()
    {
        if (currentPhase != null && currentPhase.completionType == CompletionType.Manual)
        {
            CompleteCurrentPhase();
        }
    }
    
    /// <summary>
    /// Saltar a una fase específica
    /// </summary>
    public void JumpToPhase(int phaseIndex)
    {
        StartPhase(phaseIndex);
    }
    
    /// <summary>
    /// Reiniciar el tutorial desde el inicio
    /// </summary>
    public void RestartTutorial()
    {
        interactedObjects.Clear();
        
        foreach (var phase in phases)
        {
            phase.isComplete = false;
            phase.currentItemsCollected = 0;
            phase.elapsedTime = 0f;
        }
        
        StartPhase(startingPhaseIndex);
    }
    
    #endregion
    
    #region UI Updates
    
    void UpdateUI()
    {
        if (currentPhase == null)
            return;
        
        if (phaseNameText != null)
        {
            phaseNameText.text = currentPhase.phaseName;
        }
        
        if (instructionText != null)
        {
            instructionText.text = currentPhase.description;
        }
    }
    
    #endregion
    
    #region Tutorial Complete
    
    void OnTutorialComplete()
    {
        LogDebug("=== TUTORIAL FINALIZADO ===");
        // Aquí puedes agregar lógica adicional:
        // - Cargar siguiente escena
        // - Mostrar pantalla de resumen
        // - Guardar progreso
        // - etc.
    }
    
    #endregion
    
    #region Utility
    
    void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[TutorialManager] {message}");
        }
    }
    
    /// <summary>
    /// Obtener la fase actual
    /// </summary>
    public TutorialPhase GetCurrentPhase()
    {
        return currentPhase;
    }
    
    /// <summary>
    /// Obtener el índice de la fase actual
    /// </summary>
    public int GetCurrentPhaseIndex()
    {
        return currentPhaseIndex;
    }
    
    /// <summary>
    /// Verificar si el tutorial está completo
    /// </summary>
    public bool IsTutorialComplete()
    {
        return currentPhaseIndex >= phases.Count - 1 && 
               (currentPhase != null && currentPhase.isComplete);
    }
    
    #endregion
    
    #region Editor Helpers
    
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (phases == null || phases.Count == 0)
            return;
        
        // Dibujar posiciones de inicio de cada fase
        for (int i = 0; i < phases.Count; i++)
        {
            var phase = phases[i];
            if (phase.repositionPlayer)
            {
                Gizmos.color = i == currentPhaseIndex ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(phase.playerStartPosition, 0.5f);
                
                // Dibujar dirección de rotación
                Vector3 forward = Quaternion.Euler(phase.playerStartRotation) * Vector3.forward;
                Gizmos.DrawRay(phase.playerStartPosition, forward * 2f);
            }
            
            // Dibujar zonas objetivo
            if (phase.completionType == CompletionType.ReachZone && phase.targetZone != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(phase.targetZone.bounds.center, phase.targetZone.bounds.size);
            }
        }
    }
#endif
    
    #endregion
}
