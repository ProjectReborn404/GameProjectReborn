using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// PressurePlate: Placa de presión simple que se activa cuando el jugador se para encima.
/// Versión simplificada y específica para placas de presión físicas.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PressurePlate : MonoBehaviour, IInteractable
{
    [Header("Configuración")]
    [Tooltip("Tag del objeto que activa la placa (normalmente 'Player')")]
    public string activatorTag = "Player";
    
    [Tooltip("La placa se desactiva cuando el jugador se baja")]
    public bool deactivateOnExit = true;
    
    [Tooltip("Puede ser activada solo una vez")]
    public bool oneTimeUse = false;
    
    [Header("Animación de Presión")]
    [Tooltip("Transform que se hundirá al ser presionado (puede ser la misma placa)")]
    public Transform pressurePad;
    
    [Tooltip("Distancia que se hunde la placa")]
    public float pressDepth = 0.1f;
    
    [Tooltip("Velocidad de la animación de presión")]
    public float pressSpeed = 5f;
    
    [Header("Feedback Visual")]
    public Renderer plateRenderer;
    public Color inactiveColor = Color.red;
    public Color activeColor = Color.green;
    
    [Header("Sonidos")]
    public AudioClip pressSound;
    public AudioClip releaseSound;
    
    [Header("Integración con Tutorial")]
    public bool notifyTutorialManager = true;
    public TutoriaManager tutorialManager;
    
    [Header("Eventos")]
    public UnityEvent onPressed;
    public UnityEvent onReleased;
    
    // Estado
    private bool isPressed = false;
    private bool hasBeenUsed = false;
    private Vector3 originalPosition;
    private Vector3 pressedPosition;
    private AudioSource audioSource;
    private int objectsOnPlate = 0;
    
    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        
        if (pressurePad == null)
        {
            pressurePad = transform;
        }
        
        originalPosition = pressurePad.localPosition;
        pressedPosition = originalPosition - Vector3.up * pressDepth;
        
        if (plateRenderer == null)
        {
            plateRenderer = GetComponent<Renderer>();
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (pressSound != null || releaseSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        UpdateColor();
    }
    
    void Start()
    {
        if (notifyTutorialManager && tutorialManager == null)
        {
            tutorialManager = FindObjectOfType<TutoriaManager>();
        }
    }
    
    void Update()
    {
        // Animar la posición de la placa
        if (pressurePad != null)
        {
            Vector3 targetPos = isPressed ? pressedPosition : originalPosition;
            pressurePad.localPosition = Vector3.Lerp(
                pressurePad.localPosition, 
                targetPos, 
                pressSpeed * Time.deltaTime
            );
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(activatorTag))
            return;
        
        if (oneTimeUse && hasBeenUsed)
            return;
        
        objectsOnPlate++;
        
        if (!isPressed)
        {
            Press();
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(activatorTag))
            return;
        
        objectsOnPlate = Mathf.Max(0, objectsOnPlate - 1);
        
        if (objectsOnPlate <= 0 && isPressed && deactivateOnExit)
        {
            Release();
        }
    }
    
    void Press()
    {
        isPressed = true;
        hasBeenUsed = true;
        
        Debug.Log($"[PressurePlate] {gameObject.name} presionada");
        
        UpdateColor();
        PlaySound(pressSound);
        
        if (notifyTutorialManager && tutorialManager != null)
        {
            tutorialManager.NotifyInteraction(gameObject);
        }
        
        onPressed?.Invoke();
    }
    
    void Release()
    {
        isPressed = false;
        
        Debug.Log($"[PressurePlate] {gameObject.name} liberada");
        
        UpdateColor();
        PlaySound(releaseSound);
        
        onReleased?.Invoke();
    }
    
    void UpdateColor()
    {
        if (plateRenderer == null)
            return;
        
        Color targetColor = isPressed ? activeColor : inactiveColor;
        
        var mpb = new MaterialPropertyBlock();
        plateRenderer.GetPropertyBlock(mpb);
        mpb.SetColor("_Color", targetColor);
        mpb.SetColor("_BaseColor", targetColor);
        mpb.SetColor("_EmissionColor", targetColor * 0.5f);
        plateRenderer.SetPropertyBlock(mpb);
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Implementación de IInteractable (activación manual con tecla E)
    public void Interact(GameObject interactor)
    {
        if (!isPressed && (!oneTimeUse || !hasBeenUsed))
        {
            Press();
        }
    }
    
    /// <summary>
    /// Resetear la placa a su estado inicial
    /// </summary>
    public void Reset()
    {
        isPressed = false;
        hasBeenUsed = false;
        objectsOnPlate = 0;
        if (pressurePad != null)
        {
            pressurePad.localPosition = originalPosition;
        }
        UpdateColor();
    }
    
    public bool IsPressed()
    {
        return isPressed;
    }
}
