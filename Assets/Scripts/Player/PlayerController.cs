using UnityEngine;
using UnityEngine.InputSystem;

#nullable enable

[RequireComponent(
    typeof(PlayerMovementController),
    typeof(Target)
)]
public class PlayerController : MonoBehaviour {
    [Header("References")]
    [Tooltip("Transform for where we throw objects & fire bullets from")]
    public Transform? Muzzle;

    [Tooltip("The transform which we place the equipped weapon under")]
    public Transform? WeaponSpawnPoint;

    [Tooltip("The player / first-person camera, which we use to check if we're aiming at an interactable")]
    public Camera? Camera;
    
    [Tooltip("LayerMask we use to ignore collisions for the pickups, so we only get the hover hitbox")]
    public LayerMask IgnoreHoverCollisionsLayerMask;
    
    public Pistol? EquippedWeapon; // this will be private & handled by picking up weapons later

    public PickupHoveringEvent? PickupHoveringEvent;
    
    private InputAction? fireAction;
    private InputAction? throwAction;
    private InputAction? pickupAction;

    private ThrowableObject? currentAimedAtThrowableObject = null;

    private void Awake() {
        // TODO: Remove this duh
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        fireAction = InputSystem.actions.FindAction("Attack");
        throwAction = InputSystem.actions.FindAction("Throw");
    }

    private void Update() {
        CheckIfAimingAtPickup(); // We intentionally do this before HandleInputs for the pickup
        
        HandleInputs();
    }

    private void HandleInputs() {
        if (fireAction!.WasPressedThisFrame()) {
            if (EquippedWeapon != null) {
                EquippedWeapon.FirePressed(muzzle: Muzzle!);
            } else if (currentAimedAtThrowableObject != null) { // Pick up weapon
                StartCoroutine(PickupObject(currentAimedAtThrowableObject!));
            }
        }

        if (fireAction!.WasReleasedThisFrame()) {
            if (EquippedWeapon != null) {
                EquippedWeapon.FireReleased();
            }
        }
        
        if (throwAction!.WasPressedThisFrame()) {
            AttemptThrowWeapon();
        }
    }

    public const float throwForce = 5000f; // This makes no sense, why is it so high
    
    private void AttemptThrowWeapon() {
        if (EquippedWeapon == null) {
            return;
        }

        // Spawn a throwable variation of it
        ThrowableObject throwable = Instantiate(
            EquippedWeapon.ThrowablePrefab!,
            Muzzle!.position + (Muzzle!.forward * 0.5f), // Move it slightly forward so it doesn't collide w/ the camera
            EquippedWeapon!.transform.rotation
        );
        
        Destroy(EquippedWeapon.gameObject);
        EquippedWeapon = null;

        Vector3 force = Muzzle!.forward * throwForce; // TODO: This should incorporate the player's velocity

        throwable.Rigidbody!.AddForce(force, ForceMode.Acceleration);
    }

    private void CheckIfAimingAtPickup() {
        if (!Physics.Raycast(
            origin: Camera!.transform.position,
            direction: Camera!.transform.forward,
            out RaycastHit hit,
            maxDistance: 5f,
            layerMask: ~IgnoreHoverCollisionsLayerMask
        )) {
            currentAimedAtThrowableObject = null;
            PickupHoveringEvent!.OnNotHovering?.Invoke();
            return;
        }

        ThrowableObject? throwableHit = null;

        if (hit.collider.gameObject.TryGetComponent(out ThrowableObject throwableObject)) {
            throwableHit = throwableObject;
        } else if (hit.collider.gameObject.TryGetComponent(out ThrowableParentPointer parentPointer)) {
            throwableHit = parentPointer.Parent;
        }
        
        if (throwableHit != null) {
            currentAimedAtThrowableObject = throwableHit;
            
            PickupHoveringEvent!.OnHovering?.Invoke();
        } else {
            currentAimedAtThrowableObject = null;
            
            PickupHoveringEvent!.OnNotHovering?.Invoke();
        }
    }

    private IEnumerator PickupObject(ThrowableObject throwableObject) {
        if (throwableObject.IsBeingPickedUp) {
            Debug.Log("Almost picked it up twice!");
            yield break;
        }
        
        // Animate it coming to the hand
        yield return throwableObject.Pickup(
            goalTransform: WeaponSpawnPoint!
        );
        
        EquippedWeapon = Instantiate(
            throwableObject.WeaponPrefab!,
            WeaponSpawnPoint
        );

        EquippedWeapon!.transform.position = WeaponSpawnPoint!.TransformPoint(Vector3.zero);
        EquippedWeapon!.transform.localEulerAngles = Vector3.zero;

        Destroy(throwableObject.gameObject);
        currentAimedAtThrowableObject = null;
    }
}
