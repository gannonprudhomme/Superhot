using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.VFX;

#nullable enable

[RequireComponent(
    typeof(CharacterController)
)]
public class PlayerMovementController : MonoBehaviour  {
    [Header("References")]
    [Tooltip("The player / first-person camera")]
    public Camera? Camera;
    
    [Header("Values")]
    public float RotationSpeed = 0.1f;
    
    private InputAction? moveAction;
    private InputAction? jumpAction;
    private InputAction? lookAction;
    public CharacterController? characterController;
    
    public const float MaxSpeed = 10f;
    private const float DecelerationMovementSharpness = 20f;
    private const float MovementSharpness = 2f;
    
    private float verticalCameraAngle = 0f;
    public Vector3 Velocity { get; private set; } = Vector3.zero;

    private const float DownForce = 55f;
    private const float TerminalVelocity = -10f;
    private const float JumpForce = 20f;

    private void Awake() {
        characterController = GetComponent<CharacterController>();

        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    // Update is called once per frame
    private void Update() {
        HandleMovement();
        HandleCameraMovement();
    }

    private void HandleMovement() {
        // Get horizontal / vertical input
        var movement = moveAction!.ReadValue<Vector2>();

        // Move the player
        Vector3 moveDirection = new(movement.x, 0, movement.y);
        
        Vector3 worldSpaceMove = transform.TransformVector(moveDirection);
        
        float movementSharpness = moveDirection.magnitude <= 0.01f ? DecelerationMovementSharpness : MovementSharpness;
        
        // TODO: Handle when in the air (I think there's a jump?)

        if (characterController!.isGrounded) {
            Vector3 targetVelocity = worldSpaceMove * MaxSpeed;

            // TODO? Idek if I need to do this?
            // targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, groundNormal) * targetVelocity.magnitude;

            Velocity = Vector3.Lerp(
                Velocity,
                targetVelocity,
                // Don't want timeScale to affect this - that's just another thing we'd have to factor in
                movementSharpness * Time.unscaledDeltaTime
            );
        } else {
            Vector3 targetVelocity = worldSpaceMove * MaxSpeed;

            // Treat only horizontal movement like we do with ground movement
            float x = Mathf.Lerp(
                Velocity.x,
                targetVelocity.x,
                // TODO: consider having an air movement sharpness
                movementSharpness * Time.unscaledDeltaTime
            );

            float z = Mathf.Lerp(
                Velocity.z,
                targetVelocity.z,
                // TODO: consider having an air movement sharpness
                movementSharpness * Time.unscaledDeltaTime
            );

            // Handle vertical velocity (isn't limited like horizontal is)

            // Note this is negative as it's a downward force
            float y = Velocity.y + (-DownForce * Time.deltaTime); // note this is actually subtracting

            // Clamp it to the terminal velocity
            y = Mathf.Max(y, TerminalVelocity);

            Velocity = new Vector3(x, y, z);
        }

        if (HandleJump(Velocity, out Vector3 newVelocity)) {
            Debug.Log("Jumping!");
            Velocity = newVelocity;
        }

        characterController!.Move(Velocity * Time.deltaTime);
    }
    
    // Angles per second? Idk

    private void HandleCameraMovement() {
        Vector2 look = lookAction!.ReadValue<Vector2>();

        float horizontalRotation = (look.x) * RotationSpeed;
        transform.Rotate(new Vector3(0, horizontalRotation, 0), Space.Self);

        float invertedVerticalLook = -look.y;
        float verticalMouseMovement = invertedVerticalLook * RotationSpeed;
        verticalCameraAngle += verticalMouseMovement;
        
        verticalCameraAngle = Mathf.Clamp(verticalCameraAngle, -89f, 90f);
        
        Camera!.transform.localEulerAngles = new(verticalCameraAngle, 0, 0);
    }
    
    private bool HandleJump(Vector3 inputVelocity, out Vector3 newVelocity) {
        if (!characterController!.isGrounded) {
            newVelocity = Vector3.zero;
            return false;
        }

        if (!jumpAction!.WasPressedThisFrame()) {
            newVelocity = Vector3.zero;
            return false;
        }
        
        newVelocity = inputVelocity;
        newVelocity.y = JumpForce;
        
        return true;
    }
    
    // Gets a reoriented direction that is tangent to a given slope
    private static Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal, Vector3 transformUp) {
        Vector3 directionRight = Vector3.Cross(direction, transformUp);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }
}
