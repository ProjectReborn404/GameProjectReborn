# Tipos de Interacción - Documentación

## 🎯 Sistema Recomendado: UnifiedInteractable

**¡IMPORTANTE!** Ahora existe **UnifiedInteractable.cs** que combina TODOS los tipos de interacción en un solo script.

### 🌟 ¿Cuál Usar?

| Opción | Cuándo Usarlo |
|--------|---------------|
| **UnifiedInteractable** ⭐ | **RECOMENDADO** - Para proyectos nuevos y flexibilidad máxima |
| Scripts Individuales | Solo si ya los estás usando o necesitas algo muy específico |

📖 **Ver guía completa**: `UNIFIED_INTERACTABLE_GUIDE.md`

---

## Resumen de Scripts de Interacción

Tu proyecto cuenta con **2 opciones**:

### Opción 1: UnifiedInteractable (TODO-EN-UNO) ⭐
- ✅ **1 solo script** para todo
- ✅ Combina Manual, Trigger, PressurePlate
- ✅ ColorChange, ColorCycle, MaterialSwap, PressAnimation
- ✅ Máxima flexibilidad

### Opción 2: Scripts Individuales (Legacy)
1. **SimpleColorInteractable** - Requiere presionar tecla (E), cambia colores
2. **TutorialInteractable** - Requiere presionar tecla (E), notifica al tutorial
3. **TriggerInteractable** - Activación automática al contacto
4. **PressurePlate** - Placa de presión que se hunde

---

## 🆕 TriggerInteractable.cs

### Descripción
Objeto que se activa **automáticamente** cuando el jugador entra en contacto con él, sin necesidad de presionar ninguna tecla. Ideal para zonas de activación, portales, trampas, etc.

### Características Principales

#### 🎯 Modos de Trigger
- **OnEnter**: Se activa cuando el jugador entra
- **OnStay**: Se activa continuamente mientras está dentro
- **OnExit**: Se activa cuando el jugador sale
- **OnEnterAndExit**: Se activa al entrar Y al salir

#### 🔄 Tipos de Activación
- **Once**: Solo se activa una vez (para eventos únicos)
- **EveryTime**: Se activa cada vez (con cooldown configurable)
- **Toggleable**: Alterna entre on/off cada vez
- **WhileInside**: Permanece activo solo mientras el jugador está dentro

#### 🎮 Características Especiales
- ✅ Opción "Require Standing On Top" - Solo activa si el jugador está ENCIMA
- ✅ Tiempo de espera requerido antes de activar
- ✅ Cooldown entre activaciones
- ✅ Feedback visual (colores o materiales)
- ✅ Feedback de audio (sonidos de activación/desactivación)
- ✅ Integración automática con TutorialManager
- ✅ Eventos Unity para lógica personalizada

### Configuración Paso a Paso

1. **Crear el objeto**:
   - Crea un GameObject (Cube, Plane, etc.)
   - Agrega el componente `TriggerInteractable`
   - Asegúrate de que tenga un **Collider** (se marcará automáticamente como Trigger)

2. **Configurar el Trigger Mode**:
   ```
   Trigger Mode: OnEnter (o el que necesites)
   Activation Type: Once (o el que necesites)
   ```

3. **Configurar Detección**:
   ```
   Activator Tag: Player
   Require Standing On Top: ☑️ si quieres que solo active al pararse encima
   Standing Check Distance: 0.5 (distancia para verificar)
   ```

4. **Configurar Tiempos** (opcional):
   ```
   Required Stay Time: 0 (instantáneo) o 2 (espera 2 segundos)
   Cooldown Time: 1 (1 segundo entre activaciones)
   ```

5. **Configurar Visuales**:
   ```
   Target Renderer: (arrastra el Renderer del objeto)
   Use Colors: ☑️
   Inactive Color: Gris
   Active Color: Verde
   ```

6. **Configurar Audio** (opcional):
   ```
   Activation Sound: (arrastra tu AudioClip)
   Deactivation Sound: (arrastra tu AudioClip)
   ```

7. **Integración con Tutorial**:
   ```
   Notify Tutorial Manager: ☑️
   ```

8. **Eventos**:
   - `On Activate`: Qué sucede al activarse
   - `On Deactivate`: Qué sucede al desactivarse
   - `On Stay Inside`: Qué sucede mientras el jugador está dentro

### Casos de Uso

#### Ejemplo 1: Portal de Teletransporte
```
Trigger Mode: OnEnter
Activation Type: EveryTime
Require Standing On Top: ❌
Cooldown Time: 2

En OnActivate: Llamar a función de teletransporte
```

#### Ejemplo 2: Trampa de Púas
```
Trigger Mode: OnStay
Activation Type: WhileInside
Require Standing On Top: ❌

En OnStayInside: Llamar a función que quita vida continuamente
```

#### Ejemplo 3: Plataforma de Checkpoint
```
Trigger Mode: OnEnter
Activation Type: Once
Require Standing On Top: ☑️

En OnActivate: Guardar checkpoint
```

#### Ejemplo 4: Puerta Automática
```
Trigger Mode: OnEnterAndExit
Activation Type: EveryTime
Require Standing On Top: ❌

En OnActivate: Abrir puerta
En OnDeactivate: Cerrar puerta
```

---

## 🆕 PressurePlate.cs

### Descripción
Placa de presión física que se **hunde** cuando el jugador se para encima. Versión simplificada y optimizada específicamente para placas de presión.

### Características Principales
- ✅ Animación de hundimiento suave
- ✅ Se desactiva automáticamente cuando el jugador se baja
- ✅ Opción de uso único
- ✅ Cambio de color visual
- ✅ Sonidos de presión y liberación
- ✅ Integración con TutorialManager
- ✅ Cuenta cuántos objetos están sobre la placa

### Configuración Paso a Paso

1. **Crear la placa**:
   - Crea un GameObject para la base (Cube escalado: 2x0.1x2)
   - Crea un child GameObject para el pad que se hundirá (Cube escalado: 1.8x0.05x1.8)
   - Agrega `PressurePlate` a la base

2. **Configurar componente**:
   ```
   Activator Tag: Player
   Deactivate On Exit: ☑️ (se libera al bajarse)
   One Time Use: ❌ (puede usarse varias veces)
   ```

3. **Configurar Animación**:
   ```
   Pressure Pad: (arrastra el child GameObject que se hundirá)
   Press Depth: 0.1 (cuánto se hunde)
   Press Speed: 5 (velocidad de la animación)
   ```

4. **Configurar Visuales**:
   ```
   Plate Renderer: (Renderer del pad)
   Inactive Color: Rojo
   Active Color: Verde
   ```

5. **Configurar Sonidos**:
   ```
   Press Sound: (sonido de "click" al presionar)
   Release Sound: (sonido al liberar)
   ```

6. **Eventos**:
   - `On Pressed`: Abrir puerta, activar plataforma, etc.
   - `On Released`: Cerrar puerta, desactivar plataforma, etc.

### Casos de Uso

#### Ejemplo 1: Abrir Puerta
```
One Time Use: ❌
Deactivate On Exit: ☑️

En OnPressed: Llamar a Puerta.Abrir()
En OnReleased: Llamar a Puerta.Cerrar()
```

#### Ejemplo 2: Activar Plataforma Permanentemente
```
One Time Use: ☑️
Deactivate On Exit: ❌

En OnPressed: Llamar a Plataforma.Activar()
```

#### Ejemplo 3: Puzzle de Múltiples Placas
```
One Time Use: ❌
Deactivate On Exit: ☑️

Crear un script PuzzleManager que verifica si todas las placas están presionadas
```

---

## Comparación de Scripts

| Característica | SimpleColorInteractable | TutorialInteractable | TriggerInteractable | PressurePlate |
|----------------|------------------------|---------------------|---------------------|---------------|
| **Activación** | Presionar E | Presionar E | Automática | Automática |
| **Uso Principal** | Cambio de color | Tutorial general | Zonas/Triggers | Placas físicas |
| **Animación** | ❌ | ❌ | ❌ | ✅ Hundimiento |
| **Múltiples Modos** | ❌ | ❌ | ✅ 4 modos | ❌ |
| **Highlight** | ❌ | ✅ Opcional | ✅ | ✅ |
| **Tutorial Integration** | ❌ | ✅ | ✅ | ✅ |
| **Eventos Unity** | ✅ | ✅ | ✅✅✅ | ✅✅ |
| **Sonido** | ❌ | ❌ | ✅ | ✅ |
| **One-Time Use** | ❌ | ✅ | ✅ | ✅ |

---

## Métodos Públicos Útiles

### TriggerInteractable
```csharp
// Forzar activación
triggerInteractable.ForceActivate();

// Forzar desactivación
triggerInteractable.ForceDeactivate();

// Resetear estado
triggerInteractable.ResetState();

// Verificar si está activo
bool isActive = triggerInteractable.IsActive();
```

### PressurePlate
```csharp
// Resetear la placa
pressurePlate.Reset();

// Verificar si está presionada
bool isPressed = pressurePlate.IsPressed();
```

---

## Tips y Consejos

### 🎯 Para Zonas de Activación
- Usa `TriggerInteractable` con `Trigger Mode: OnEnter`
- Si es un checkpoint, usa `Activation Type: Once`
- Usa un BoxCollider grande para el área

### 🚪 Para Puertas Automáticas
- Usa `TriggerInteractable` con `Trigger Mode: OnEnterAndExit`
- `Activation Type: EveryTime`
- Conecta OnActivate a abrir puerta, OnDeactivate a cerrar

### 🎮 Para Placas de Presión
- Usa `PressurePlate` para placas físicas visuales
- Usa `TriggerInteractable` con `Require Standing On Top` para placas invisibles/lógicas

### ⏱️ Para Zonas Cronometradas
- Usa `TriggerInteractable` con `Required Stay Time > 0`
- Ejemplo: "Permanece 5 segundos en la zona para capturarla"

### 🔄 Para Interruptores de Palanca
- Usa `TriggerInteractable` con `Activation Type: Toggleable`
- Cada vez que el jugador entra, cambia de estado

### 🎯 Para Combinar con Tutorial
- Marca `Notify Tutorial Manager` en cualquiera
- Asegúrate de asignar el TutorialManager (o se buscará automáticamente)
- Configura la fase del tutorial para detectar esta interacción

---

## Debugging

### Visualización en Scene View
- **TriggerInteractable**: Muestra el volumen del collider con el color activo/inactivo
- **PressurePlate**: Muestra la posición de la placa

### Logs de Debug
- En `TriggerInteractable`, activa `Show Debug Logs` para ver en consola:
  - Cuándo entra/sale el jugador
  - Cuándo se activa/desactiva
  - Estado actual

### Problemas Comunes

**No se activa al entrar:**
- ✅ Verifica que el Collider sea Trigger (Is Trigger ☑️)
- ✅ Verifica que el jugador tenga el tag correcto
- ✅ Si usas "Require Standing On Top", asegúrate de que el jugador esté ENCIMA

**Se activa demasiadas veces:**
- ✅ Usa `Activation Type: Once` o configura un `Cooldown Time`

**No se ve la animación:**
- ✅ En PressurePlate, asegúrate de asignar el `Pressure Pad`
- ✅ Verifica que el Pressure Pad sea un hijo del objeto principal

**No cambia de color:**
- ✅ Verifica que Target Renderer esté asignado
- ✅ Si usas URP, puede que necesites usar `_BaseColor` en lugar de `_Color`

---

## Ejemplo Completo: Puzzle de 3 Placas

### Setup
1. Crea 3 GameObjects con `PressurePlate`
2. Crea una puerta (GameObject con animación)
3. Crea un script `ThreePlatesPuzzle.cs`:

```csharp
using UnityEngine;

public class ThreePlatesPuzzle : MonoBehaviour
{
    public PressurePlate plate1;
    public PressurePlate plate2;
    public PressurePlate plate3;
    public Animator doorAnimator;
    
    void Update()
    {
        if (plate1.IsPressed() && plate2.IsPressed() && plate3.IsPressed())
        {
            doorAnimator.SetTrigger("Open");
        }
    }
}
```

¡Listo! Ahora tienes múltiples formas de crear interacciones en tu juego. 🎮

---

## 📊 Comparación Final: UnifiedInteractable vs Scripts Individuales

### 🌟 UnifiedInteractable (RECOMENDADO)
```
PROS:
✅ 1 solo script para aprender
✅ Cambiar comportamiento sin cambiar componente
✅ Proyecto más limpio y organizado
✅ Todas las funcionalidades disponibles simultáneamente
✅ Fácil experimentación
✅ Menor tamaño de proyecto
✅ Un solo lugar para bugs/mejoras
✅ Puedes combinar comportamientos (Hybrid mode)

CONTRAS:
⚠️ Más opciones en Inspector (pero bien organizadas)
⚠️ Script ligeramente más grande (~600 líneas)
```

### 📦 Scripts Individuales
```
PROS:
✅ Scripts más simples individualmente
✅ Solo ves las opciones relevantes
✅ Más fácil para principiantes (menos opciones)
✅ Código más específico por tipo

CONTRAS:
❌ 4 scripts diferentes (SimpleColor, Tutorial, Trigger, Pressure)
❌ Necesitas saber cuál usar para cada caso
❌ Cambiar tipo = eliminar componente y agregar otro
❌ 4 lugares diferentes para bugs/mejoras
❌ No puedes combinar comportamientos
❌ Más archivos en el proyecto
```

---

## 🎯 Recomendación Final

### ✨ Para NUEVOS proyectos o features:
➡️ **Usa UnifiedInteractable** - Es más flexible, profesional y moderno

### 🔄 Si ya tienes scripts individuales:
➡️ Mantén lo que funciona, o migra gradualmente según necesites

### 📚 Para aprender Unity:
➡️ Empieza con **UnifiedInteractable** - Un solo script con todas las opciones

### 🎮 Para prototipar rápido:
➡️ **UnifiedInteractable** - Cambiar comportamiento es solo cambiar un dropdown

---

## 📖 Documentación Adicional

- **UNIFIED_INTERACTABLE_GUIDE.md** - Guía completa del sistema unificado
- **TUTORIAL_README.md** - Guía del sistema de tutorial
- Este archivo - Comparación y referencia rápida

---

## 🎓 Resumen Visual

```
┌─────────────────────────────────────────────────────────┐
│                 SISTEMA DE INTERACCIONES                │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌───────────────────┐      ┌────────────────────────┐ │
│  │ UnifiedInteractable│ ⭐   │  Scripts Individuales  │ │
│  ├───────────────────┤      ├────────────────────────┤ │
│  │ • Manual          │      │ • SimpleColor          │ │
│  │ • Trigger         │      │ • TutorialInteractable │ │
│  │ • PressurePlate   │      │ • TriggerInteractable  │ │
│  │ • Hybrid          │      │ • PressurePlate        │ │
│  │                   │      │                        │ │
│  │ + ColorChange     │      │ (funciones separadas)  │ │
│  │ + ColorCycle      │      │                        │ │
│  │ + MaterialSwap    │      │                        │ │
│  │ + PressAnimation  │      │                        │ │
│  └───────────────────┘      └────────────────────────┘ │
│                                                          │
│  RECOMENDADO ✓              Legacy / Específico         │
└─────────────────────────────────────────────────────────┘
```

---

## Créditos

Sistema de Tutorial e Interacciones creado para GameProjectReborn  
Compatible con Unity Input System y TutorialCamera  
Versión Unificada - Octubre 2025

````
