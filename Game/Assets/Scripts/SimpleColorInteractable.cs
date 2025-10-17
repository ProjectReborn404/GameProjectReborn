using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class SimpleColorInteractable : MonoBehaviour, IInteractable
{
    [Header("Renderer")]
    public Renderer targetRenderer; // si está vacío, usa GetComponent<Renderer>()

    [Header("Colores")]
    public Color[] colors = new Color[] { Color.yellow, Color.red, Color.green };
    public bool loopColors = true;

    [Header("Opciones")]
    public bool revertToOriginalOnDisable = true;

    [Header("Eventos (opcional)")]
    public UnityEvent onInteract; // puedes enganchar más cosas desde el inspector

    int currentIndex = 0;
    Color originalColor;
    bool originalCaptured = false;

    void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            // Intentar leer color original (no instanciamos material aquí).
            originalCaptured = TryGetColorFromRenderer(targetRenderer, out originalColor);
        }
    }

    public void Interact(GameObject interactor)
    {
        if (colors == null || colors.Length == 0)
            return;

        ApplyColor(colors[currentIndex]);

        // aumentar índice
        currentIndex++;
        if (currentIndex >= colors.Length)
            currentIndex = loopColors ? 0 : colors.Length - 1;

        onInteract?.Invoke();
    }

    void ApplyColor(Color c)
    {
        if (targetRenderer == null) return;

        // Preferimos MaterialPropertyBlock para no instanciar materiales
        var mpb = new MaterialPropertyBlock();
        targetRenderer.GetPropertyBlock(mpb);

        // Intenta propiedades comunes de color en shaders:
        if (mpb != null)
        {
            mpb.SetColor("_Color", c);      // standard shaders antiguas
            mpb.SetColor("_BaseColor", c);  // URP / HDRP newer name
            targetRenderer.SetPropertyBlock(mpb);
        }
        else
        {
            // Fallback: instanciar material (no recomendado para muchos objetos)
            try
            {
                targetRenderer.material.color = c;
            }
            catch
            {
                // nada
            }
        }
    }

    bool TryGetColorFromRenderer(Renderer r, out Color c)
    {
        c = Color.white;
        if (r == null) return false;

        // Intentar leer con MaterialPropertyBlock no siempre devuelve color si nunca fue seteado
        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        if (mpb != null && (mpb.isEmpty == false))
        {
            // intentar _Color
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

        // Fallback leer material (podría instanciar material)
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

    void OnDisable()
    {
        if (revertToOriginalOnDisable && originalCaptured && targetRenderer != null)
        {
            ApplyColor(originalColor);
        }
    }

    // Para ayudar en el editor
    void OnValidate()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
    }
}
