using UnityEngine;
using UnityEngine.InputSystem;

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
    private CharacterController? characterController;
    
    private const float MaxSpeed = 10f;
    private const float MovementSharpness = 5f;
    
    private float verticalCameraAngle = 0f;
    private Vector3 velocity = Vector3.zero;

    private const float DownForce = 55f;
    private const float TerminalVelocity = -10f;
    private const float JumpForce = 20f;
    
    private const float MinTimeDilation = 0.1f;
    public float TimeDilation {
        get {
            // When we're in the air it's always 1
            // (note the time dilation logic doesn't work when it's in the air, but it's fine cause we do this)
            if (!characterController!.isGrounded) {
                return 1f;
            }
            
            float currSpeed = velocity.magnitude;
            
            // Prevent it from going below minTimeDilation
            float dilation = Mathf.Max(currSpeed / MaxSpeed, MinTimeDilation);
            
            // Prevent it from being above 1
            return Mathf.Min(dilation, 1f);
        }
    }

    private void Awake() {
        characterController = GetComponent<CharacterController>();

        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        jumpAction = InputSystem.actions.FindAction("Jump");
        
        // Dynamically changing this makes the forces different based on the time dilation
        // so it seems to need to be fixed
        Time.fixedDeltaTime = 0.02F * MinTimeDilation;
    }

    // Update is called once per frame
    private void Update() {
        HandleMovement();
        HandleCameraMovement();
        
        Time.timeScale = TimeDilation;
    }

    private void HandleMovement() {
        // Get horizontal / vertical input
        var movement = moveAction!.ReadValue<Vector2>();

        // Move the player
        Vector3 moveDirection = new(movement.x, 0, movement.y);
        
        Vector3 worldSpaceMove = transform.TransformVector(moveDirection);
        
        // TODO: Handle when in the air (I think there's a jump?)

        if (characterController!.isGrounded) {
            Vector3 targetVelocity = worldSpaceMove * MaxSpeed;

            velocity = Vector3.Lerp(
                velocity,
                targetVelocity,
                MovementSharpness * Time.deltaTime
            );
        } else {
            Vector3 targetVelocity = worldSpaceMove * MaxSpeed;

            // Treat only horizontal movement like we do with ground movement
            float x = Mathf.Lerp(
                velocity.x,
                targetVelocity.x,
                MovementSharpness * Time.unscaledDeltaTime
            );

            float z = Mathf.Lerp(
                velocity.z,
                targetVelocity.z,
                MovementSharpness * Time.unscaledDeltaTime
            );

            // Handle vertical velocity (isn't limited like horizontal is)

            // Note this is negative as it's a downward force
            float y = velocity.y + (-DownForce * Time.deltaTime); // note this is actually subtracting

            // Clamp it to the terminal velocity
            y = Mathf.Max(y, TerminalVelocity);

            velocity = new Vector3(x, y, z);
        }

        if (HandleJump(velocity, out Vector3 newVelocity)) {
            Debug.Log("Jumping!");
            velocity = newVelocity;
        }

        characterController!.Move(velocity * Time.deltaTime);
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
