using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

#nullable enable

[RequireComponent(
    typeof(CharacterController),
    typeof(Target)
)]
public class PlayerController : MonoBehaviour {
    [Header("References")]
    public Pistol? EquippedWeapon; // this will be private & handled by picking up weapons later
    
    [Tooltip("The player / first-person camera")]
    public Camera? Camera;
    
    private InputAction? moveAction;
    private InputAction? lookAction;
    private InputAction? fireAction;
    private CharacterController? characterController;
    public float maxSpeed = 5f;
    public float movementSharpness = 10f;
    private Vector3 velocity = Vector3.zero;

    private const float minTimeDilation = 0.1f;
    public float TimeDilation {
        get {
            float currSpeed = velocity.magnitude;
            
            // Prevent it from going below minTimeDilation
            float dilation = Mathf.Max(currSpeed / maxSpeed, minTimeDilation);
            
            // Prevent it from being above 1
            return Mathf.Min(dilation, 1f);
        }
    }
    
    private void Awake() {
        characterController = GetComponent<CharacterController>();

        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        fireAction = InputSystem.actions.FindAction("Attack");
    }

    private void Update() {
        HandleMovement();
        HandleCameraMovement();
        HandleInputs();

        Time.timeScale = TimeDilation;
    }

    private void HandleMovement() {
        // Get horizontal / vertical input
        Vector2 movement = moveAction!.ReadValue<Vector2>();

        // Move the player
        Vector3 moveDirection = new Vector3(movement.x, 0, movement.y);
        
        Vector3 worldSpaceMove = transform.TransformVector(moveDirection);
        Vector3 targetVelocity = worldSpaceMove * maxSpeed;

        velocity = Vector3.Lerp(
            velocity,
            targetVelocity,
            movementSharpness * Time.deltaTime
        );
        
        characterController!.Move(velocity * Time.deltaTime);
    }

    // Angles per second? Idk
    public float RotationSpeed = 0.1f;

    private float verticalCameraAngle = 0f;
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

    private void HandleInputs() {
        if (fireAction!.WasPressedThisFrame()) {
            EquippedWeapon!.FirePressed();
        }

        if (fireAction!.WasReleasedThisFrame()) {
            EquippedWeapon!.FireReleased();
        }
    }
    
    // Gets a reoriented direction that is tanget to a given slope
    private static Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal, Vector3 transformUp) {
        Vector3 directionRight = Vector3.Cross(direction, transformUp);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }
}
