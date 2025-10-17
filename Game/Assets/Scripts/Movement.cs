using UnityEngine;
using UnityEngine.UI; // para Image del HUD
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class Movement : MonoBehaviour
{
    [Header("Velocidades")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 8f;
    public float acceleration = 10f;

    [Header("Salto y gravedad")]
    public float jumpHeight = 1.6f;
    public float gravity = -9.81f;
    public float groundedGravity = -2f;

    [Header("Ajustes")]
    public Transform cameraTransform;

    [Header("Doble salto (preparado)")]
    [Tooltip("Número máximo de saltos permitidos (1 = normal, 2 = doble salto)")]
    public int maxJumps = 1; // por defecto 1 -> salto simple
    [HideInInspector] public int jumpsPerformed = 0;
    [HideInInspector] public bool canDoubleJump = false; // cuando se desbloquee se pondrá true y maxJumps = 2

    // Referencia a icono UI (opcional). Asignar en Inspector si quieres mostrar HUD.
    [Header("Visualización del poder (opcional)")]
    public Image doubleJumpIconHUD; // arrastra un Image del Canvas (o déjalo null)
    public Sprite doubleJumpLockedSprite;   // sprite que muestra "bloqueado"
    public Sprite doubleJumpUnlockedSprite; // sprite que muestra "desbloqueado"

    // O bien un sprite en el mundo (opcional)
    public SpriteRenderer doubleJumpSpriteRenderer; // child encima del jugador (opcional)

    [Header("Rotación del jugador")]
    public bool rotateToMovement = true;
    public float rotationSmooth = 10f;
    public float rotationThreshold = 0.1f;

    // Estado público para debug
    [HideInInspector] public bool isGroundedPublic = false;

    // Internos
    private CharacterController cc;
    private Vector3 velocity;
    private float currentSpeed;
    private float verticalVelocity;

    // Entradas
    private float inputX;
    private float inputZ;
    private bool inputSprint;
    private bool inputJumpPressed;

#if ENABLE_INPUT_SYSTEM
    private bool jumpPressedFromAction = false;

    void OnEnable()
    {
        // Si usas InputActionReference, asigna en el Inspector
        // (no obligatorio)
    }

    void OnDisable()
    {
    }

    void OnJumpActionPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        jumpPressedFromAction = true;
    }
#endif

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        currentSpeed = walkSpeed;

        // Inicializa HUD/sprite según estado (por defecto bloqueado)
        UpdateDoubleJumpVisual();
        if (cameraTransform == null)
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // DEBUG: tecla para desbloquear en tiempo de ejecución (quita cuando lo implementes en tu progresión)
        // if (Input.GetKeyDown(KeyCode.K)) UnlockDoubleJump();

        HandleMovement();
        HandleGravityAndJump();

        cc.Move(velocity * Time.deltaTime);
    }

    void HandleMovement()
    {
#if ENABLE_INPUT_SYSTEM
        // Mismo manejo de inputs que antes (teclado/gamepad)
        inputX = 0f;
        inputZ = 0f;
        inputSprint = false;
        inputJumpPressed = false;

        if (Keyboard.current != null)
        {
            inputX += (Keyboard.current.dKey.isPressed ? 1f : 0f) + (Keyboard.current.rightArrowKey.isPressed ? 1f : 0f);
            inputX -= (Keyboard.current.aKey.isPressed ? 1f : 0f) + (Keyboard.current.leftArrowKey.isPressed ? 1f : 0f);

            inputZ += (Keyboard.current.wKey.isPressed ? 1f : 0f) + (Keyboard.current.upArrowKey.isPressed ? 1f : 0f);
            inputZ -= (Keyboard.current.sKey.isPressed ? 1f : 0f) + (Keyboard.current.downArrowKey.isPressed ? 1f : 0f);

            inputSprint |= (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            inputJumpPressed |= Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        if (Gamepad.current != null)
        {
            var stick = Gamepad.current.leftStick.ReadValue();
            inputX += stick.x;
            inputZ += stick.y;
            inputSprint |= Gamepad.current.leftShoulder.isPressed;
            inputJumpPressed |= Gamepad.current.buttonSouth.wasPressedThisFrame;
        }
#else
        inputX = Input.GetAxisRaw("Horizontal");
        inputZ = Input.GetAxisRaw("Vertical");
        inputSprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        inputJumpPressed = Input.GetButtonDown("Jump");
#endif

        Vector3 inputDir = new Vector3(inputX, 0f, inputZ);
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        Vector3 worldDir;
        if (cameraTransform != null)
        {
            Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camRight = cameraTransform.right;
            worldDir = (camForward * inputDir.z + camRight * inputDir.x).normalized;
        }
        else
        {
            worldDir = transform.TransformDirection(inputDir);
        }

        float targetSpeed = inputSprint ? sprintSpeed : walkSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        Vector3 horizontalVelocity = worldDir * currentSpeed;
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;

        if (rotateToMovement)
        {
            Vector3 moveDir = new Vector3(worldDir.x, 0f, worldDir.z);
            if (moveDir.sqrMagnitude > rotationThreshold * rotationThreshold)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmooth * Time.deltaTime);
            }
        }
    }

    void HandleGravityAndJump()
    {
        bool isGrounded = cc.isGrounded;
        isGroundedPublic = isGrounded;

        if (isGrounded)
        {
            // resetear contadores de salto al tocar suelo
            jumpsPerformed = 0;

            if (verticalVelocity < 0f)
                verticalVelocity = groundedGravity;
        }

        // Comprobar input (si usas InputSystem con InputActionReference, adapta)
#if ENABLE_INPUT_SYSTEM
        // si usas jumpAction, setea inputJumpPressed mediante su callback
        if (jumpPressedFromAction)
        {
            inputJumpPressed = true;
            jumpPressedFromAction = false;
        }
#endif

        // Si se presionó salto y tenemos saltos disponibles
        if (inputJumpPressed)
        {
            // Si estamos en suelo siempre podemos saltar
            if (isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(Mathf.Abs(gravity) * 2f * jumpHeight);
                jumpsPerformed = 1;
            }
            else
            {
                // Estamos en aire: permitimos otro salto solo si maxJumps > jumpsPerformed
                if (jumpsPerformed < maxJumps)
                {
                    verticalVelocity = Mathf.Sqrt(Mathf.Abs(gravity) * 2f * jumpHeight);
                    jumpsPerformed++;
                    PlayDoubleJumpVFX();
                }
            }
        }

        // gravedad aplicable siempre si no estamos con groundedGravity
        if (!isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        velocity.y = verticalVelocity;
    }

    // Llamar desde tu sistema de progresión para desbloquear el doble salto
    public void UnlockDoubleJump()
    {
        canDoubleJump = true;
        maxJumps = Mathf.Max(2, maxJumps);
        UpdateDoubleJumpVisual();
    }

    // Para bloquearlo si quieres
    public void LockDoubleJump()
    {
        canDoubleJump = false;
        maxJumps = Mathf.Max(1, maxJumps == 2 ? 1 : maxJumps);
        UpdateDoubleJumpVisual();
    }

    // Actualiza la UI / sprite según estado canDoubleJump
    private void UpdateDoubleJumpVisual()
    {
        if (doubleJumpIconHUD != null)
        {
            if (canDoubleJump && doubleJumpUnlockedSprite != null)
                doubleJumpIconHUD.sprite = doubleJumpUnlockedSprite;
            else if (!canDoubleJump && doubleJumpLockedSprite != null)
                doubleJumpIconHUD.sprite = doubleJumpLockedSprite;
        }

        if (doubleJumpSpriteRenderer != null)
        {
            doubleJumpSpriteRenderer.enabled = canDoubleJump;
            // opcional: cambiar spriteRenderer.sprite = ...
        }
    }

    // Hook para efectos (particle, animator). Implementa esto a tu gusto.
    private void PlayDoubleJumpVFX()
    {
        // Ejemplo: si tienes ParticleSystem, reproducir:
        // if (doubleJumpParticles != null) doubleJumpParticles.Play();

        // o disparar un trigger en Animator
        // if (animator != null) animator.SetTrigger("DoubleJump");

        // Por ahora, sólo debug:
        // Debug.Log("Double jump used. jumpsPerformed = " + jumpsPerformed);
    }

    void OnDrawGizmosSelected()
    {
        if (cc == null) cc = GetComponent<CharacterController>();
        if (cc != null && Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Vector3 start = transform.position + Vector3.up * 1f;
            Gizmos.DrawLine(start, start + new Vector3(velocity.x, 0, velocity.z));
        }
    }
}
