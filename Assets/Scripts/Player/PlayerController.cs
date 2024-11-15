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
            } else if (currentAimedAtThrowableObject != null) {
                EquippedWeapon = Instantiate(
                    currentAimedAtThrowableObject.WeaponPrefab!,
                    WeaponSpawnPoint // We want it to be under the Weapon Spawn Point
                );

                EquippedWeapon!.transform.position = WeaponSpawnPoint!.TransformPoint(Vector3.zero);
                EquippedWeapon!.transform.localEulerAngles = Vector3.zero;

                Destroy(currentAimedAtThrowableObject!.gameObject);
                currentAimedAtThrowableObject = null;
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
            Muzzle!.position,
            EquippedWeapon!.transform.rotation
        );
        
        Destroy(EquippedWeapon.gameObject);
        EquippedWeapon = null;

        Vector3 force = Muzzle!.forward * throwForce; // TODO: This should incorporate the player's velocity

        throwable.Rigidbody!.AddForce(force, ForceMode.Acceleration);
    }

    private void CheckIfAimingAtPickup() {
        // We do more than 1 as the pickups have different colliders - one-to-many for actual physics / RigidBody
        // and one for the hover hitbox (the trigger)
        RaycastHit[] hits = new RaycastHit[5]; // I doubt it will ever be more than 5

        Physics.RaycastNonAlloc(
            origin: Camera!.transform.position,
            direction: Camera!.transform.forward,
            results: hits,
            maxDistance: 5f
        );
        
        ThrowableObject? hoveringThrowable = null;

        foreach(RaycastHit hit in hits) {
            if (hit.collider == null) {
                continue;
            }

            if (hit.collider.gameObject.TryGetComponent(out ThrowableObject throwableObject)) {
                hoveringThrowable = throwableObject;
            } else if (hit.collider.gameObject.TryGetComponent(out ThrowableParentPointer parentPointer)) {
                hoveringThrowable = parentPointer.Parent;
            }
            
            // Found one!
            if (hoveringThrowable != null) {
                break;
            }
        }

        if (hoveringThrowable != null) {
            currentAimedAtThrowableObject = hoveringThrowable;
            
            PickupHoveringEvent!.OnHovering?.Invoke();
        } else {
            currentAimedAtThrowableObject = null;
            
            PickupHoveringEvent!.OnNotHovering?.Invoke();
        }
    }
}
