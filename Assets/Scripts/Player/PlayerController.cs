using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

#nullable enable

struct AnimationStates {
    public const string PunchLeft = "Base Layer.Punch Left";
    public const string PunchRight = "Base Layer.Punch Right";
}

public interface PlayerState {
    public void OnEnter(PlayerController playerController) { }
    public void OnUpdate(PlayerController playerController) { }
    public void OnExit(PlayerController playerController) { }
}

// Aka melee state
class UnarmedState : PlayerState {
    private readonly InputAction attackAction = InputSystem.actions.FindAction("Attack");

    // Local state
    private Collidable? currentAimedAtCollidable = null;
    private ThrowableObject? currentAimedAtThrowableObject = null;
    private Vector3 aimedAtPoint = Vector3.zero;

    public bool useLeftPunch = false; // Used to alternate between left & right arms
    private const float punchAnimationDuration = 0.5f; // TODO: Honestly there's a chance we just ignore this & just use cooldown
    private const float punchCooldown = 0.25f;
    private float timeOfLastPunch = Mathf.NegativeInfinity;

    public void OnUpdate(PlayerController playerController) {
        CheckIfAimingAtObjectOrEnemy(playerController);

        if (attackAction.WasPressedThisFrame()) {
            if (currentAimedAtThrowableObject != null) {
                playerController.ChangeState(new PickupThrowableObjectState(currentAimedAtThrowableObject));
                return;
            } else if (currentAimedAtCollidable != null) {
                AttemptPunch(currentAimedAtCollidable, playerController);
            }
        }
    }

    public void OnExit(PlayerController playerController) {
        playerController.PickupHoveringEvent!.OnNotHovering?.Invoke();
    }

    private void AttemptPunch(Collidable enemyCollidable, PlayerController playerController) {
        if (Time.time - timeOfLastPunch < (punchCooldown + punchAnimationDuration)) {
            return;
        }

        timeOfLastPunch = Time.time;
        
        // Damage the collidable (enemy) we're aiming at *immediately*
        enemyCollidable.OnHit?.Invoke(new Collidable.Parameters() { hitPoint = aimedAtPoint, isLethal = false });

        playerController.animator!.Play(useLeftPunch ? AnimationStates.PunchLeft : AnimationStates.PunchRight);
        useLeftPunch = !useLeftPunch; // Invert it
    }

    private void CheckIfAimingAtObjectOrEnemy(PlayerController playerController) {
        // To start, null both out
        currentAimedAtCollidable = null;
        currentAimedAtThrowableObject = null;
        
        if (!Physics.Raycast(
            origin: playerController.Camera!.transform.position,
            direction: playerController.Camera!.transform.forward,
            out RaycastHit hit,
            maxDistance: 5f,
            layerMask: ~playerController.IgnoreHoverCollisionsLayerMask
        )) {
            playerController.PickupHoveringEvent!.OnNotHovering?.Invoke();
            return;
        }

        aimedAtPoint = hit.point;
        
        if (hit.collider.gameObject.TryGetComponent(out ThrowableParentPointer parentPointer)) {
            currentAimedAtThrowableObject = parentPointer.Parent;
            
            playerController.PickupHoveringEvent!.OnHovering?.Invoke(PickupHoveringEvent.HoverType.THROWABLE);
            
        } else if (hit.collider.gameObject.TryGetComponent(out Collidable collidable)) {
            // TODO: there might be collidable's that aren't enemies
            
            currentAimedAtCollidable = collidable;
            
            playerController.PickupHoveringEvent!.OnHovering?.Invoke(PickupHoveringEvent.HoverType.ENEMY);
        } else {
            playerController.PickupHoveringEvent!.OnNotHovering?.Invoke();
        }
    }
}

class PickupThrowableObjectState : PlayerState {
    private readonly ThrowableObject objectToPickUp;
    
    public PickupThrowableObjectState(ThrowableObject objectToPickUp) {
        this.objectToPickUp = objectToPickUp;
    }
    
    public void OnEnter(PlayerController playerController) {
        playerController.StartCoroutine(PickupWeapon(playerController));
    }
    
    private IEnumerator PickupWeapon(PlayerController playerController) {
        if (objectToPickUp.IsBeingPickedUp) {
            Debug.LogError("Almost picked it up twice!");
            
            yield break;
        }
        
        // Animate it coming to the hand
        yield return objectToPickUp.Pickup(
            goalTransform: playerController.WeaponSpawnPoint!
        );
        
        // It's done! Actually pick it up
        
        Pistol weapon = Object.Instantiate(
            objectToPickUp.WeaponPrefab!,
            playerController.WeaponSpawnPoint // We want it to be under the Weapon Spawn Point
        );

        weapon!.transform.position = playerController.WeaponSpawnPoint!.TransformPoint(Vector3.zero);
        weapon!.transform.localEulerAngles = Vector3.zero;

        Object.Destroy(objectToPickUp.gameObject);
        
        playerController.ChangeState(new GunEquippedState(weapon));
    }
} 

class GunEquippedState : PlayerState {
    private readonly Pistol weapon;
    private readonly InputAction attackAction;
    private readonly InputAction throwAction;
    
    private const float throwForce = 8_000f; // This makes no sense, why is it so high
    
    public GunEquippedState(Pistol weapon) {
        this.weapon = weapon;
        attackAction = InputSystem.actions.FindAction("Attack");
        throwAction = InputSystem.actions.FindAction("Throw");
    }
    
    public void OnUpdate(PlayerController playerController) {
        if (attackAction.WasPressedThisFrame()) {
            bool didFire = weapon.FirePressed(playerController.Muzzle!);
            
            if (didFire && weapon.RequiresReload) {
                playerController.ReloadEvent!.ReloadStart?.Invoke(weapon.ReloadDuration);
            }
        }
        
        if (throwAction.WasPressedThisFrame()) {
            ThrowWeapon(playerController.Muzzle!);
            playerController.ChangeState(new UnarmedState());
        }
    }

    private void ThrowWeapon(Transform muzzle) {
        // Spawn a throwable variation of it
        ThrowableObject throwable = Object.Instantiate(
            weapon.ThrowablePrefab!,
            muzzle!.position + (muzzle!.forward * 0.5f), // Move it slightly forward so it doesn't collide w/ the camera
            weapon!.transform.rotation
        );
        
        Object.Destroy(weapon.gameObject);

        Vector3 force = muzzle!.forward * throwForce; // TODO: This should incorporate the player's velocity

        throwable.Rigidbody!.AddForce(force, ForceMode.Acceleration);
    }
}

[RequireComponent(
    typeof(PlayerMovementController),
    typeof(Target),
    typeof(Animator)
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
    
    public Pistol? StartingWeapon;

    public PickupHoveringEvent? PickupHoveringEvent;
    public ReloadEvent? ReloadEvent;

    public Animator? animator { get; private set; }
    private InputAction? fireAction;
    private InputAction? throwAction;
    private InputAction? pickupAction;

    #nullable disable // We have to initialize this in Awake due to the InputSystem calls
    private PlayerState playerState;
    #nullable enable

    private void Awake() {
        animator = GetComponent<Animator>();
        fireAction = InputSystem.actions.FindAction("Attack");
        throwAction = InputSystem.actions.FindAction("Throw");

        if (StartingWeapon != null) {
            playerState = new GunEquippedState(StartingWeapon);
        } else {
            playerState = new UnarmedState();
        }
        
        playerState.OnEnter(this);
    }

    private void Update() {
        playerState.OnUpdate(this);
    }

    public void ChangeState(PlayerState newState) {
        // Debug.Log($"Changing state from {playerState} to {newState}");
        playerState.OnExit(this);
        newState.OnEnter(this);
        playerState = newState;
    }
}
