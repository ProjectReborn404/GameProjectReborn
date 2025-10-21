# UnifiedInteractable - Guía Completa

## 🎯 Descripción

**UnifiedInteractable.cs** es un script **TODO-EN-UNO** que combina todas las funcionalidades de interacción en un solo componente modular. Reemplaza y mejora:

- ✅ SimpleColorInteractable
- ✅ TutorialInteractable  
- ✅ TriggerInteractable
- ✅ PressurePlate

## 🌟 Ventajas del Sistema Unificado

| Ventaja | Descripción |
|---------|-------------|
| 🎯 **Un Solo Script** | No necesitas decidir cuál script usar |
| 🔧 **Auto-Configuración** | El collider se configura automáticamente según el modo |
| 🔄 **Flexible** | Cambia el comportamiento sin cambiar de componente |
| 🎨 **Modular** | Activa/desactiva funcionalidades según necesites |
| 🧹 **Más Limpio** | Menos scripts = proyecto más organizado |
| 🔧 **Fácil Mantenimiento** | Un solo lugar para bugs y mejoras |
| 💾 **Eficiente** | Código optimizado y reutilizable |

---

## ⚠️ Nota Importante sobre Colliders

**El script configura automáticamente el collider según el modo de interacción:**

| Modo | Collider es Trigger | Comportamiento |
|------|-------------------|----------------|
| **Manual** | ❌ NO (sólido) | El jugador puede acercarse sin atravesarlo |
| **Trigger** | ✅ SÍ | El jugador lo atraviesa, se activa automáticamente |
| **PressurePlate** | ✅ SÍ | Detecta cuando está encima |
| **Hybrid** | ✅ SÍ | Combina detección + manual |

💡 **Si necesitas una placa sólida:** Agrega un segundo collider NO-trigger para la física.  
📖 **Más detalles:** Ver `UNIFIEDINTERACTABLE_FIX_COLLIDER.md`

---

## 📋 Configuración Rápida

### 1️⃣ Objeto Interactuable Manual (Presionar E)

```
Interaction Mode: Manual
Activation Type: Once
Feedback Type: ColorChange
  └─ Inactive Color: Gris
  └─ Active Color: Verde
Notify Tutorial Manager: ✓
```

**Uso:** Palancas, botones, objetos para recolectar

---

### 2️⃣ Trigger Automático (Portal, Checkpoint)

```
Interaction Mode: Trigger
Trigger Activation: OnEnter
Activation Type: Once
Feedback Type: ColorChange
  └─ Inactive Color: Azul claro
  └─ Active Color: Verde brillante
```

**Uso:** Portales, checkpoints, zonas de activación

---

### 3️⃣ Placa de Presión

```
Interaction Mode: PressurePlate
Trigger Activation: OnEnter
Activation Type: Toggleable
Feedback Type: PressAnimation
  └─ Press Depth: 0.1
  └─ Press Speed: 5
  └─ Inactive Color: Rojo
  └─ Active Color: Verde
Deactivate On Exit: ✓
```

**Uso:** Placas de presión físicas, interruptores de piso

---

### 4️⃣ Puerta Automática

```
Interaction Mode: Trigger
Trigger Activation: OnEnterAndExit
Activation Type: EveryTime
Deactivate On Exit: ✓

Eventos:
  OnActivate: Llamar a Puerta.Abrir()
  OnDeactivate: Llamar a Puerta.Cerrar()
```

**Uso:** Puertas que se abren/cierran automáticamente

---

### 5️⃣ Ciclo de Colores (Objeto Puzzle)

```
Interaction Mode: Manual
Activation Type: EveryTime
Feedback Type: ColorCycle
  └─ Color Cycle: [Rojo, Verde, Azul, Amarillo]
  └─ Loop Colors: ✓
```

**Uso:** Puzzles de colores, objetos que cambian de estado

---

### 6️⃣ Zona de Daño Continuo

```
Interaction Mode: Trigger
Trigger Activation: OnStay
Activation Type: WhileInside

Eventos:
  OnStayInside: Llamar a Player.TakeDamage(1)
```

**Uso:** Lava, ácido, zonas de daño

---

### 7️⃣ Objeto Híbrido (Manual Y Automático)

```
Interaction Mode: Hybrid
Trigger Activation: OnEnter
Activation Type: EveryTime
```

**Uso:** Objeto que se puede activar con E o automáticamente al entrar

---

## 🎮 Modos de Interacción Detallados

### Manual
- Requiere presionar tecla (E)
- Compatible con PlayerInteraction
- Ideal para objetos tradicionales

### Trigger
- Activación automática por trigger
- Sin necesidad de presionar tecla
- Ideal para zonas y áreas

### PressurePlate
- Placa de presión específica
- Incluye animación de hundimiento
- Se activa al pararse encima

### Hybrid
- Combina Manual + Trigger
- Máxima flexibilidad
- Se activa con E o automáticamente

---

## 🔄 Tipos de Activación

| Tipo | Comportamiento | Uso Común |
|------|----------------|-----------|
| **Once** | Solo una vez | Eventos únicos, recolectables |
| **EveryTime** | Cada vez (con cooldown) | Puertas, interruptores |
| **Toggleable** | Alterna on/off | Interruptores de palanca |
| **WhileInside** | Solo mientras está dentro | Zonas de daño, buffs temporales |

---

## 🎨 Tipos de Feedback Visual

### None
Sin feedback visual (solo lógica)

### ColorChange
Cambia entre dos colores: inactivo ↔ activo

### ColorCycle
Cicla por una lista de colores en cada activación

### MaterialSwap
Cambia entre dos materiales completos

### PressAnimation
Anima el objeto hundiéndose (placa de presión)

---

## ⚙️ Opciones Avanzadas

### Require Standing On Top
Solo se activa si el objeto está **específicamente encima** del trigger.

```
✓ Require Standing On Top
Standing Check Distance: 0.5
```

### Required Stay Time
Tiempo que debe permanecer dentro antes de activarse.

```
Required Stay Time: 3.0  // 3 segundos
```

### Cooldown Time
Tiempo mínimo entre activaciones (para EveryTime).

```
Cooldown Time: 1.5  // 1.5 segundos
```

### Deactivate On Exit
Se desactiva automáticamente cuando sale del trigger.

```
✓ Deactivate On Exit
```

---

## 🎯 Ejemplos Prácticos Completos

### Ejemplo 1: Sistema de Checkpoint

```
=== CONFIGURACIÓN ===
Interaction Mode: Trigger
Trigger Activation: OnEnter
Activation Type: Once
Feedback Type: ColorChange
  Inactive Color: Azul (0.3, 0.3, 1, 1)
  Active Color: Verde (0, 1, 0, 1)
Activation Sound: checkpoint_sound.wav
Notify Tutorial Manager: ✓

=== EVENTOS ===
OnActivate:
  - GameManager.SetCheckpoint(transform.position)
  - UI.ShowMessage("¡Checkpoint guardado!")
```

---

### Ejemplo 2: Puzzle de 4 Colores

```
=== CONFIGURACIÓN ===
Interaction Mode: Manual
Activation Type: EveryTime
Feedback Type: ColorCycle
  Color Cycle: [Rojo, Verde, Azul, Amarillo]
  Loop Colors: ✓
Cooldown Time: 0.5

=== SCRIPT EXTERNO ===
public class ColorPuzzleManager : MonoBehaviour
{
    public UnifiedInteractable[] colorPillars;
    public Color solutionColor = Color.Blue;
    
    void Update()
    {
        // Verificar si todos están en el color correcto
        bool allCorrect = true;
        foreach (var pillar in colorPillars)
        {
            // Verificar color actual del renderer
            if (/* color no coincide */)
                allCorrect = false;
        }
        
        if (allCorrect)
            OpenDoor();
    }
}
```

---

### Ejemplo 3: Placa de Presión con Puerta

```
=== PLACA ===
Interaction Mode: PressurePlate
Trigger Activation: OnEnter
Activation Type: Toggleable
Feedback Type: PressAnimation
  Pressure Pad: (hijo "PlacaPad")
  Press Depth: 0.15
  Press Speed: 8
  Inactive Color: Rojo
  Active Color: Verde
Press Sound: plate_press.wav
Release Sound: plate_release.wav
Deactivate On Exit: ✓

=== EVENTOS ===
OnActivate:
  - Door.Open()
  - ParticleSystem.Play()
  
OnDeactivate:
  - Door.Close()
  - ParticleSystem.Stop()
```

---

### Ejemplo 4: Portal Bidireccional

```
=== PORTAL A ===
Interaction Mode: Trigger
Trigger Activation: OnEnter
Activation Type: EveryTime
Cooldown Time: 2
Notify Tutorial Manager: ❌

OnActivate:
  - TeleportPlayer(PortalB.position)
  - PlayEffect()

=== PORTAL B ===
(Configuración idéntica pero teletransporta a Portal A)
```

---

### Ejemplo 5: Zona de Lava (Daño Continuo)

```
=== CONFIGURACIÓN ===
Interaction Mode: Trigger
Trigger Activation: OnStay
Activation Type: WhileInside
Feedback Type: None
Activator Tag: Player

=== EVENTOS ===
OnStayInside:
  - PlayerHealth.TakeDamage(5 * Time.deltaTime)
  
OnActivate:
  - PlayerEffects.StartBurning()
  
OnDeactivate:
  - PlayerEffects.StopBurning()
```

---

## 🔌 Integración con Tutorial

```csharp
// El UnifiedInteractable notifica automáticamente
Notify Tutorial Manager: ✓

// En TutorialManager, configura la fase:
Completion Type: Interaction
Target Interactable: (arrastra el UnifiedInteractable)
```

---

## 📞 Métodos Públicos

```csharp
// Forzar activación
unifiedInteractable.ForceActivate();

// Forzar desactivación
unifiedInteractable.ForceDeactivate();

// Resetear estado completo
unifiedInteractable.ResetState();

// Verificar si está activo
bool isActive = unifiedInteractable.IsActive();

// Activar/desactivar highlight (modo Manual)
unifiedInteractable.SetHighlight(true);
```

---

## 🎨 Uso desde Código Externo

### Activar desde otro script
```csharp
public class PuzzleManager : MonoBehaviour
{
    public UnifiedInteractable lever;
    
    void OnSomeCondition()
    {
        lever.ForceActivate();
    }
}
```

### Detectar estado
```csharp
public class Door : MonoBehaviour
{
    public UnifiedInteractable pressurePlate;
    
    void Update()
    {
        if (pressurePlate.IsActive())
        {
            // Mantener puerta abierta
            OpenDoor();
        }
        else
        {
            CloseDoor();
        }
    }
}
```

### Puzzle de múltiples objetos
```csharp
public class MultiPlatePuzzle : MonoBehaviour
{
    public UnifiedInteractable[] plates;
    public GameObject door;
    
    void Update()
    {
        bool allPressed = true;
        
        foreach (var plate in plates)
        {
            if (!plate.IsActive())
            {
                allPressed = false;
                break;
            }
        }
        
        door.SetActive(allPressed);
    }
}
```

---

## 🐛 Solución de Problemas

### No se activa con trigger
✅ Verifica que Interaction Mode sea Trigger/Hybrid/PressurePlate  
✅ Asegúrate de que el Collider sea Trigger  
✅ Verifica el Activator Tag (normalmente "Player")

### La animación de presión no funciona
✅ Asigna Pressure Pad (el objeto que se hundirá)  
✅ Verifica que Feedback Type sea PressAnimation  
✅ Ajusta Press Depth y Press Speed

### El color no cambia
✅ Asigna Target Renderer  
✅ Verifica que Feedback Type sea ColorChange/ColorCycle  
✅ Si usas URP, verifica la propiedad del shader (_BaseColor)

### No notifica al TutorialManager
✅ Marca "Notify Tutorial Manager"  
✅ Asigna TutorialManager (o déjalo encontrarlo automáticamente)  
✅ En el tutorial, usa este objeto como Target Interactable

---

## 🔄 Migración desde Scripts Antiguos

### SimpleColorInteractable → UnifiedInteractable
```
Interaction Mode: Manual
Feedback Type: ColorCycle
Color Cycle: (copia tus colores)
Loop Colors: ✓
```

### TutorialInteractable → UnifiedInteractable
```
Interaction Mode: Manual
Activation Type: Once (o EveryTime)
Notify Tutorial Manager: ✓
```

### TriggerInteractable → UnifiedInteractable
```
Interaction Mode: Trigger
(copia toda la configuración de trigger)
```

### PressurePlate → UnifiedInteractable
```
Interaction Mode: PressurePlate
Feedback Type: PressAnimation
(copia la configuración de presión)
```

---

## 📊 Comparación: Scripts Separados vs Unificado

| Aspecto | Scripts Separados | UnifiedInteractable |
|---------|-------------------|---------------------|
| Scripts totales | 4 scripts | 1 script |
| Cambiar tipo | Eliminar/agregar componente | Cambiar dropdown |
| Combinaciones | Limitadas | Infinitas |
| Mantenimiento | 4 archivos | 1 archivo |
| Tamaño proyecto | +KB | Optimizado |
| Curva aprendizaje | 4 documentaciones | 1 documentación |

---

## 💡 Tips y Mejores Prácticas

1. **Empieza Simple**: Usa los presets recomendados
2. **Experimenta**: Cambia Interaction Mode para ver qué funciona mejor
3. **Usa Eventos**: Conecta OnActivate/OnDeactivate en el Inspector
4. **Debug Mode**: Activa "Show Debug Logs" durante desarrollo
5. **Visuales**: Los Gizmos muestran el área de activación
6. **Nombra Bien**: Usa nombres descriptivos: "Palanca_PuertaPrincipal"
7. **Documenta**: Usa el campo Description en el Inspector (si agregas uno)

---

## 🎓 Conclusión

**UnifiedInteractable** simplifica radicalmente tu flujo de trabajo:

- ✅ **1 script** para todas las interacciones
- ✅ **Configuración visual** en el Inspector
- ✅ **Fácil de entender** y mantener
- ✅ **Potente y flexible**
- ✅ **Listo para producción**

**¡Ya no necesitas decidir entre múltiples scripts!** 🎉
