# Sistema de Tutorial - Documentación

## Descripción General

Este sistema de tutorial proporciona un asistente completo para manejar escenas de tutorial en Unity. Permite gestionar fases, aparición de objetos, verificación de completitud, y posicionamiento del jugador y cámara.

## Componentes del Sistema

### 1. **TutoriaManager.cs** (Script Principal)
El cerebro del sistema de tutorial. Gestiona todas las fases y coordina los elementos.

### 2. **TutorialInteractable.cs**
Componente para objetos que pueden ser interactuados por el jugador.

### 3. **TutorialZone.cs**
Componente para zonas que detectan cuando el jugador las alcanza.

---

## Configuración Inicial

### Paso 1: Configurar el TutorialManager

1. Crea un **GameObject vacío** en tu escena de tutorial
2. Nómbralo `TutorialManager`
3. Agrega el componente `TutoriaManager`
4. Configura las referencias:
   - **Player Transform**: Arrastra el GameObject del jugador
   - **Player Movement**: Se asignará automáticamente si el jugador tiene el componente `Movement`
   - **Tutorial Camera**: Arrastra la cámara con el componente `TutorialCamera`

### Paso 2: Configurar el Tag del Jugador

Asegúrate de que tu GameObject del jugador tenga el tag `Player`:
1. Selecciona el GameObject del jugador
2. En el Inspector, en la parte superior, cambia el Tag a `Player`

### Paso 3: Crear Fases del Tutorial

En el TutorialManager, en la sección "Fases del Tutorial":

1. Define el número de fases (por ejemplo, 5)
2. Para cada fase configura:

#### Identificación
- **Phase Name**: Nombre descriptivo (ej: "Aprende a Moverte")
- **Description**: Instrucciones para el jugador (ej: "Usa WASD para moverte")

#### Objetos a Activar
- **Objects To Spawn**: Objetos que aparecerán al iniciar esta fase
- **Objects To Hide**: Objetos que se ocultarán

#### Posicionamiento
- **Reposition Player**: ✓ Marcar si quieres teletransportar al jugador
- **Player Start Position**: Posición inicial (X, Y, Z)
- **Player Start Rotation**: Rotación inicial (en grados Euler)

#### Configuración de Cámara
- **Use Camera Height**: ✓ Marcar para establecer altura específica
- **Camera Height**: Altura de la cámara para esta fase
- **Camera Limit Colliders**: Colliders que limitan el área de la cámara
- **Camera Limit Tag**: Alternativamente, un tag para buscar colliders

#### Condiciones de Completitud
- **Completion Type**: Tipo de condición para completar la fase
  - `Interaction`: Interactuar con un objeto específico
  - `ReachZone`: Llegar a una zona determinada
  - `Timer`: Esperar cierto tiempo
  - `CollectMultiple`: Recolectar/interactuar con múltiples objetos
  - `Manual`: Completar manualmente desde código

- **Target Interactable**: (Si Completion Type = Interaction) Objeto a interactuar
- **Target Zone**: (Si Completion Type = ReachZone) Zona a alcanzar
- **Timer Duration**: (Si Completion Type = Timer) Tiempo en segundos
- **Items To Collect**: (Si Completion Type = CollectMultiple) Cantidad de objetos

#### Habilidades
- **Unlock Double Jump**: ✓ Marcar para desbloquear doble salto al completar

#### Eventos
- **On Phase Start**: Eventos que se ejecutan al iniciar la fase
- **On Phase Complete**: Eventos que se ejecutan al completar la fase

---

## Configuración de Objetos Interactuables

### Método 1: Usar TutorialInteractable (Recomendado)

1. Selecciona el objeto que será interactuable
2. Agrega el componente `TutorialInteractable`
3. Asegúrate de que tenga un **Collider**
4. El tag "interactive" se asignará automáticamente
5. Configuración opcional:
   - **Disable After Interaction**: Se desactiva tras ser usado
   - **Can Interact Multiple Times**: Puede usarse varias veces
   - **Highlight Material**: Material para resaltar cuando el jugador lo mira
   - **On Interact**: Eventos personalizados al interactuar

### Método 2: Usar tu propio script

Si ya tienes tu propio script interactuable, simplemente llama:
```csharp
FindObjectOfType<TutoriaManager>().NotifyInteraction(gameObject);
```

---

## Configuración de Zonas de Objetivo

1. Crea un **GameObject vacío** donde quieras la zona
2. Nómbralo descriptivamente (ej: "ZonaObjetivo_Fase1")
3. Agrega un **Collider** (BoxCollider, SphereCollider, etc.)
4. Marca el Collider como **Trigger** (✓ Is Trigger)
5. Agrega el componente `TutorialZone`
6. Configuración:
   - **Disable After Reached**: Se desactiva al ser alcanzada
   - **Can Trigger Multiple Times**: Puede activarse varias veces
   - **Show In Editor**: Mostrar en el editor (útil para visualizar)
   - **Gizmo Color**: Color de visualización

---

## Ejemplo de Configuración: Tutorial Básico de Movimiento

### Fase 1: Aprender a Moverse
- **Name**: "Movimiento Básico"
- **Description**: "Usa WASD para moverte. Llega a la zona marcada."
- **Completion Type**: ReachZone
- **Target Zone**: Zona1 (con TutorialZone)
- **Objects To Spawn**: Plataforma1, Zona1

### Fase 2: Aprender a Saltar
- **Name**: "Salto"
- **Description**: "Presiona ESPACIO para saltar. Salta sobre el obstáculo."
- **Completion Type**: ReachZone
- **Target Zone**: Zona2
- **Objects To Spawn**: Obstáculo1, Zona2
- **Reposition Player**: ✓
- **Player Start Position**: (10, 0, 0)

### Fase 3: Desbloquear Doble Salto
- **Name**: "Poder: Doble Salto"
- **Description**: "¡Has obtenido el doble salto! Presiona ESPACIO dos veces."
- **Completion Type**: Interaction
- **Target Interactable**: CristalPoder (con TutorialInteractable)
- **Unlock Double Jump**: ✓

### Fase 4: Practicar Doble Salto
- **Name**: "Práctica de Doble Salto"
- **Description**: "Usa el doble salto para alcanzar la plataforma alta."
- **Completion Type**: ReachZone
- **Target Zone**: PlataformaAlta
- **Objects To Spawn**: PlataformaAlta, ZonaAlta

### Fase 5: Completar Tutorial
- **Name**: "¡Felicidades!"
- **Description**: "Has completado el tutorial."
- **Completion Type**: Timer
- **Timer Duration**: 3

---

## Métodos Públicos Útiles

### En TutoriaManager:

```csharp
// Saltar a una fase específica
tutorialManager.JumpToPhase(2);

// Completar la fase actual manualmente
tutorialManager.ManualCompletePhase();

// Reiniciar el tutorial
tutorialManager.RestartTutorial();

// Obtener la fase actual
TutorialPhase currentPhase = tutorialManager.GetCurrentPhase();

// Verificar si el tutorial está completo
bool isComplete = tutorialManager.IsTutorialComplete();

// Notificar interacción (desde scripts externos)
tutorialManager.NotifyInteraction(gameObject);

// Notificar entrada a zona (desde scripts externos)
tutorialManager.NotifyZoneEntered(collider);
```

---

## Integración con UI (Opcional)

Para mostrar información del tutorial en pantalla:

1. Crea un Canvas en tu escena
2. Agrega dos elementos **Text** (o TextMeshPro):
   - Uno para el nombre de la fase
   - Otro para las instrucciones
3. En TutorialManager, arrastra estos textos a:
   - **Phase Name Text**
   - **Instruction Text**

---

## Consejos y Buenas Prácticas

1. **Organización de Jerarquía**: Crea carpetas en la jerarquía:
   ```
   Tutorial
   ├── TutorialManager
   ├── Fase1
   │   ├── Plataforma1
   │   ├── Zona1
   │   └── Objetos...
   ├── Fase2
   │   ├── Obstáculo1
   │   └── Objetos...
   └── etc...
   ```

2. **Visualización**: Activa los Gizmos en la vista Scene para ver:
   - Posiciones de inicio de cada fase (esferas amarillas/verdes)
   - Zonas objetivo (cubos semi-transparentes)
   - Direcciones de rotación del jugador

3. **Debug Mode**: Mantén activo el Debug Mode en TutorialManager durante desarrollo para ver logs detallados

4. **Límites de Cámara**: Usa colliders invisibles con el tag configurado para limitar el área de la cámara por fase

5. **Eventos Unity**: Usa los eventos OnPhaseStart y OnPhaseComplete para:
   - Activar animaciones
   - Reproducir sonidos
   - Mostrar diálogos
   - Etc.

---

## Solución de Problemas

### El jugador no se teletransporta
- Verifica que "Reposition Player" esté marcado
- Asegúrate de que Player Transform esté asignado

### Los objetos no aparecen
- Verifica que estén en "Objects To Spawn"
- Asegúrate de que estén desactivados al inicio

### La fase no se completa
- Verifica el Completion Type
- Para Interaction: asegúrate de que el Target Interactable tenga TutorialInteractable
- Para ReachZone: verifica que el collider sea Trigger y esté asignado
- Para CollectMultiple: cuenta bien los Items To Collect

### El doble salto no se desbloquea
- Marca "Unlock Double Jump" en la fase correcta
- Verifica que Player Movement esté asignado en TutorialManager

---

## Extensiones Futuras

Puedes extender el sistema agregando:
- Nuevos tipos de completitud personalizados
- Sistema de puntuación
- Tiempo límite por fase
- Pistas visuales (flechas, marcadores)
- Sistema de diálogos integrado
- Grabación de estadísticas

---

## Créditos

Sistema de Tutorial creado para GameProjectReborn
Compatible con Unity Input System y TutorialCamera
