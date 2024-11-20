using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

#nullable enable

struct AnimationStates {
    public const string Idle = "Base Layer.Idle";
    public const string PunchLeft = "Base Layer.Punch Left";
    public const string PunchRight = "Base Layer.Punch Right";
    public const string ThrowObject = "Base Layer.Throw Object";
    public const string ShootPistol = "Base Layer.Shoot Pistol";
    public const string PickupObject = "Base Layer.Pickup Object";
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

    private bool useLeftPunch = false; // Used to alternate between left & right arms
    private const float PunchCooldown = 0.2f;
    private float timeOfLastPunch = Mathf.NegativeInfinity;

    public void OnUpdate(PlayerController playerController) {
        CheckIfAimingAtObjectOrEnemy(playerController);

        if (attackAction.WasPressedThisFrame()) {
            if (currentAimedAtThrowableObject != null) {
                playerController.ChangeState(new PickupThrowableObjectState(currentAimedAtThrowableObject));
                return;
            } else if (currentAimedAtCollidable != null) {
                // TODO: This should just be its own state probably
                // well actually it would need to be able to interrupt itself, hrmmmmmm
                AttemptPunch(currentAimedAtCollidable, playerController);
            }
        }
    }

    public void OnExit(PlayerController playerController) {
        playerController.PickupHoveringEvent!.OnNotHovering?.Invoke();
    }

    private void AttemptPunch(Collidable enemyCollidable, PlayerController playerController) {
        if (Time.time - timeOfLastPunch < (PunchCooldown)) {
            return;
        }
        
        playerController.timeDilation!.ForceTimeDilation();

        timeOfLastPunch = Time.time;
        
        // Damage the collidable (enemy) we're aiming at *immediately*
        enemyCollidable.OnHit?.Invoke(new Collidable.Parameters() { hitPoint = aimedAtPoint, isLethal = false });

        // Create the force area
        Object.Instantiate(playerController.ForceAreaPrefab!, aimedAtPoint, playerController.Muzzle!.rotation);

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
        playerController.animator!.Play(AnimationStates.PickupObject); // This will be an actual pickup animation soon
    }
    
    private IEnumerator PickupWeapon(PlayerController playerController) {
        if (objectToPickUp.IsBeingPickedUp) {
            Debug.LogError("Almost picked it up twice!");
            // TODO: I think we need to move back to UnarmedState in this case
            yield break;
        }
        
        playerController.timeDilation!.ForceTimeDilation();
        
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
        
        // TODO: Based on what type of object this is, move to the according state
        playerController.ChangeState(new GunEquippedState(weapon));
    }
} 

class GunEquippedState : PlayerState {
    private readonly Pistol weapon;
    private readonly InputAction attackAction;
    private readonly InputAction throwAction;
    
    public GunEquippedState(Pistol weapon) {
        this.weapon = weapon;
        attackAction = InputSystem.actions.FindAction("Attack");
        throwAction = InputSystem.actions.FindAction("Throw");
    }
    
    public void OnUpdate(PlayerController playerController) {
        if (attackAction.WasPressedThisFrame()) {
            if (weapon.IsOutOfAmmo) {
                // TODO: Show the "YOU'RE OUT" text (it changes)
                // TODO: I think play a half-shoot animation?
                // then on next fire (LMB pressed, not RMB), throw the weapon
                // For now I'm just going to throw it immediately
                playerController.ChangeState(new ThrowObjectState(weapon.ThrowablePrefab!, playerController.Muzzle!.rotation, weapon.gameObject));
                return;
            }
            
            bool didFire = weapon.FirePressed(playerController.Muzzle!);

            if (didFire) {
                playerController.timeDilation!.ForceTimeDilation();
                
                // This normalizedTime: 0 is important - without it, if this was the last animation that played, it wouldn't play it again
                playerController!.animator!.Play(AnimationStates.ShootPistol, -1, 0f);
                
                // Not all weapons require a reload
                if (weapon.RequiresReload) {
                    playerController.ReloadEvent!.ReloadStart?.Invoke(weapon.ReloadDuration);
                }
            }
        }
        
        // Should this be an else-if?
        if (throwAction.WasPressedThisFrame()) {
            playerController.ChangeState(new ThrowObjectState(weapon.ThrowablePrefab!, playerController.Muzzle!.rotation, weapon.gameObject));
        }
    }
}

class ThrowObjectState : PlayerState {
    private readonly Quaternion initialRotation;
    private readonly ThrowableObject throwableObjectPrefab;
    private readonly GameObject originalWeapon;
    
    private const float ThrowForce = 8_000f * 2f; // This makes no sense, why is it so high
    
    public ThrowObjectState(ThrowableObject throwableObjectPrefab, Quaternion initialRotation, GameObject originalWeapon) {
        this.throwableObjectPrefab = throwableObjectPrefab;
        this.initialRotation = initialRotation;
        this.originalWeapon = originalWeapon;
    }
    
    public void OnEnter(PlayerController playerController) {
        Object.Destroy(originalWeapon);
        
        // Play the throw animation
        // This normalizedTime: 0 is important
        // without it, if this was the last animation that played, it wouldn't play it again
        playerController.animator!.Play(AnimationStates.ThrowObject, -1, 0f);
        
        playerController.timeDilation!.ForceTimeDilation();
        
        CreateAndThrowObject(playerController);
    }

    public void OnUpdate(PlayerController playerController) {
        // It doesn't like us changing states in OnEnter, so have to do it here bleh
        // TODO: Figure out why
        playerController.ChangeState(new UnarmedState());
    }

    private void CreateAndThrowObject(PlayerController playerController) {
        Transform muzzle = playerController.Muzzle!;
        
        // Spawn a throwable variation of it
        ThrowableObject throwableInstance = Object.Instantiate(
            throwableObjectPrefab!,
            muzzle!.position + (muzzle!.forward * 0.5f), // Move it slightly forward so it doesn't collide w/ the camera
            initialRotation
        );
        
        throwableInstance.OnThrown();
        
        Vector3 force = muzzle!.forward * ThrowForce; // TODO: This should incorporate the player's velocity

        throwableInstance.Rigidbody!.AddForce(force, ForceMode.Acceleration);
    }
}

[RequireComponent(
    typeof(PlayerMovementController),
    typeof(Target),
    typeof(Animator)
)]
[RequireComponent(typeof(TimeDilation))]
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

    public ForceArea? ForceAreaPrefab;
    
    // TODO: Honestly this should just be an enum?
    // though I don't think we ever start with a weapon?
    public Pistol? StartingWeapon;
    
    [Header("Events")]
    public PickupHoveringEvent? PickupHoveringEvent;
    public ReloadEvent? ReloadEvent;

    public Animator? animator { get; private set; }
    public TimeDilation? timeDilation { get; private set; }
    
    private InputAction? fireAction;
    private InputAction? throwAction;
    private InputAction? pickupAction;

    #nullable disable // We have to initialize this in Awake due to the InputSystem calls
    private PlayerState playerState;
    #nullable enable

    private void Awake() {
        animator = GetComponent<Animator>();
        timeDilation = GetComponent<TimeDilation>();
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
        // Debug.Log($"Changing player state from {playerState} to {newState}");
        playerState.OnExit(this);
        newState.OnEnter(this);
        playerState = newState;
    }
}
