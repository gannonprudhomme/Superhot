using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

#nullable enable

struct AnimationStates {
    public const string Idle = "Base Layer.Idle";
    public const string Melee = "Base Layer.Melee";
}

// I'm wondering if we need a movement state and an attack state
// but the attack state probably needs to influence the movement state
// e.g. we should charge if we have a melee weapon, but might not want to with a gun
// and if we're unarmed we need to go find a weapon (and/or charge) - this might be simpler though
public interface State {
    public void OnEnter(RedGuy redGuy) { }
    public void OnUpdate(RedGuy redGuy) { }
    public void OnExit(RedGuy redGuy) { }
}

// Don't want this to be public? Just for this file
sealed class UnarmedChaseState : State {
    private readonly Transform fistTransform;
    private const float meleeRange = 2f;

    private readonly LayerMask pickupableLayerMask = LayerMask.GetMask("Pickupable");
    
    public UnarmedChaseState(Transform fistTransform) {
        this.fistTransform = fistTransform;
    }
    
    public void OnUpdate(RedGuy redGuy) {
        // See if there's a weapon nearby
        var nearbyWeapon = FindBestNearbyWeapon(redGuy.transform.position);

        if (nearbyWeapon != null) {
            redGuy.ChangeState(new MoveToPickupState(nearbyWeapon));
            return;
        }
        
        // Charge the player
        redGuy.SetDestination(Target.instance!.AimPoint!.position);
        
        // If we're in range & have line of sight, melee
        bool inMeleeRange = Vector3.Distance(fistTransform.position, Target.instance!.AimPoint!.position) < meleeRange;
        bool canMelee = inMeleeRange && redGuy.HasLineOfSightToTarget();
        
        if (canMelee) {
            redGuy.ChangeState(new UnarmedAttackState(fistTransform));
            return;
        }
    }

    private ThrowableObject? FindBestNearbyWeapon(Vector3 position) {
        // TOOD: We only want to pick up weapons - not basic throwables like bottles, etc

        Collider[] foundColliders = new Collider[5];
        
        int foundCount = Physics.OverlapSphereNonAlloc(
            position: position,
            radius: 5f,
            results: foundColliders,
            layerMask: pickupableLayerMask
        );
        
        if (foundCount == 0) {
            return null;
        }
        
        ThrowableObject? closestWeapon = null;
        // TODO: Really this should be how long is the path to it, rather than just the distance between the points
        // so worth considering improving this
        int closestDistance = int.MaxValue;
        
        foreach (var collider in foundColliders) {
            if (collider == null) {
                continue;
            }
            
            ThrowableObject? foundThrowableObject;
            
            if (collider.TryGetComponent<ThrowableObject>(out foundThrowableObject)) {
            } else if (collider.TryGetComponent<ThrowableParentPointer>(out var parentPointer)) {
                foundThrowableObject = parentPointer.Parent;
            }
            
            if (foundThrowableObject == null) {
                continue;
            }

            bool isActualWeapon = true; // TODO: Need to check if it's n "actual" weapon

            float dist = Vector3.Distance(position, foundThrowableObject.transform.position);

            if (dist < closestDistance) {
                closestWeapon = foundThrowableObject;
                closestDistance = (int) dist;
            }
        }
        
        return closestWeapon;
    }
}

// We're in melee range - attack!
sealed class UnarmedAttackState : State {
    // Transform of the fist we use to determine whether we hit the player or not
    private readonly Transform fistTransform;

    private const float fistRadius = 0.25f; // TODO: Visualize this
    private const float animationDuration = 0.5f;
    private const float attackCooldown = 1f; // How long we should stay on this state before transitioning away
    private float totalAttackDuration => animationDuration + attackCooldown;
    private float timeOfAttackStart;

    private readonly LayerMask playerLayerMask = LayerMask.GetMask("Player");
    
    public UnarmedAttackState(Transform fistTransform) {
        this.fistTransform = fistTransform;
    }
    
    public void OnEnter(RedGuy redGuy) {
        timeOfAttackStart = Time.time;

        // Start the animation
        // redGuy.animator!.CrossFade(AnimationStates.Melee, 0.25f); // Idk what to t this to
        redGuy.animator!.Play(AnimationStates.Melee);
    }

    public void OnUpdate(RedGuy redGuy) {
        float timeSinceStart = Time.time - timeOfAttackStart;
        if (timeSinceStart > totalAttackDuration) {
            redGuy.ChangeState(new UnarmedChaseState(fistTransform));
            return;
        }
        
        if (timeSinceStart > animationDuration) {
            // We're in the cooldown phase
            return;
        }

        // We're still in the attack animation - did we hit the player this frame?
        Collider[] collider = new Collider[1];
        int count = Physics.OverlapSphereNonAlloc(
            position: fistTransform.position,
            radius: fistRadius,
            results: collider,
            layerMask: playerLayerMask
        );

        if (count > 0) {
            Debug.Log($"Hit player '{collider[0].gameObject.name}'");
            DamagePlayer();
            return;
        }
    }

    public void OnExit(RedGuy redGuy) {
        // Go back to idle I guess? Idk
        redGuy.animator!.Play(AnimationStates.Idle);
    }

    private void DamagePlayer() {
        Debug.Log("Hit player!");
    }
}

sealed class MoveToPickupState : State {
    private readonly ThrowableObject objectToPickUp;

    public MoveToPickupState(ThrowableObject toPickUp) {
        this.objectToPickUp = toPickUp;
    }
    
    public void OnEnter(RedGuy redGuy) {
        redGuy.SetDestination(objectToPickUp.transform.position);
    }

    private const float pickupDistance = 1f;

    public void OnUpdate(RedGuy redGuy) {
        if (objectToPickUp == null) {
            Debug.Log("Can't pick up object, it was destroyed");
            // Go back, attempt to find a new one / etc
            redGuy.ChangeState(new UnarmedChaseState(redGuy.FistTransform!));
            return;
        }
        
        bool closeEnoughToPickUp = Vector3.Distance(redGuy.transform.position, objectToPickUp.transform.position) < pickupDistance;
        if (!closeEnoughToPickUp) {
            return;
        }
        
        redGuy.ChangeState(new PerformPickupState(objectToPickUp));
    }
}

sealed class PerformPickupState: State {
    private readonly ThrowableObject objectToPickUp;
    private const float animationDuration = 0.25f;
    
    private float timeOfAnimStart;
    
    public PerformPickupState(ThrowableObject toPickUp) {
        this.objectToPickUp = toPickUp;
    }
    
    public void OnEnter(RedGuy redGuy) {
        // TODO: Start the animation
        timeOfAnimStart = Time.time;
    }
    
    public void OnUpdate(RedGuy redGuy) {
        if (objectToPickUp == null) {
            Debug.Log("Can't pick up object, it was destroyed");
            // Go back, attempt to find a new one / etc
            redGuy.ChangeState(new UnarmedChaseState(redGuy.FistTransform!));
            return;
        }
        
        bool readyToPickUp = Time.time - timeOfAnimStart > animationDuration;
        if (!readyToPickUp) {
            return;
        }
        
        redGuy.EquipWeapon(objectToPickUp.WeaponPrefab!);
        
        Object.Destroy(objectToPickUp.gameObject);

        redGuy.ChangeState(new GunFindLineOfSightState(redGuy.CurrentWeapon!, Target.instance!));
    }
}

// We have a gun equipped, and have (or just had) line of sight
// so shoot!
sealed class FireGunState : State {
    private readonly Pistol gun;
    
    public FireGunState(Pistol gun) {
        this.gun = gun;
    }
    
    public void OnEnter(RedGuy redGuy) {
        // TODO: Maybe do an animation to equip the gun?
        redGuy.DisablePathfinding();
    }

    public void OnUpdate(RedGuy redGuy) {
        // TODO: Might need to make sure the pickup gun animation is done?
        
        // When they have a gun do they strafe? I don't think they do
        // For now just assume no

        if (!redGuy.HasLineOfSightToTarget()) {
            // We don't have line of sight anymore
            redGuy.ChangeState(new GunFindLineOfSightState(gun, Target.instance));
            return;
        }
        
        // Rotate towards the player
        redGuy.RotateTowards(Target.instance!.AimPoint!.position);
        
        Vector3 direction = (Target.instance!.AimPoint!.position - redGuy!.Muzzle!.position).normalized;
        
        gun.FireIfPossible(redGuy.Muzzle!.position, direction);
    }
}

sealed class StrafeAndFireGunState : State {
    private readonly Pistol gun;

    public StrafeAndFireGunState(Pistol gun) {
        this.gun = gun;
    }

    public void OnEnter(RedGuy redGuy) {
        // Find a strafe position, maybe within a 30 degree angle of between the redGuy and the player
        // and then strafe to that position
        // and then fire
        
        var target = Target.instance!;
        Vector3 directionToTarget = (target.AimPoint!.position - redGuy.transform.position).normalized;

        // get the perpendicular vector to the direction to the target
        // by rotating the vector 90 degrees around the y-axis
        // how does this work? idk
        Vector3 perpendicular = new Vector3(directionToTarget.z, 0, -directionToTarget.x).normalized;
        
        // Get a random number from 3 to 7
        float strafeDistance = Random.Range(-7f, 7f); // move left / right
        Vector3 strafePosition = redGuy.transform.position + (perpendicular * strafeDistance); // move it horizontally
        // move it forward
        strafePosition += directionToTarget * Random.Range(-2f, 4f);
        
        redGuy.DisableRotation(); // we're handling the rotation ourselves
        redGuy.SetDestination(strafePosition);
    }

    public void OnUpdate(RedGuy redGuy) {
        if (!redGuy.HasLineOfSightToTarget()) {
            // We don't have line of sight anymore, so we can't shoot
            redGuy.ChangeState(new GunFindLineOfSightState(gun, Target.instance));
            return;
        }
        
        const float remainingDistanceThreshold = 0.1f;
        if (redGuy.navMeshAgent!.remainingDistance < remainingDistanceThreshold) {
            // strafe again baby! easier this way
            redGuy.ChangeState(new StrafeAndFireGunState(gun));
            return;
        }
        
        // we're handling the rotation, so we always face the player
        // blend tree should make the animation work perfectly fine
        redGuy.RotateTowards(Target.instance!.AimPoint!.position);
        
        Vector3 direction = (Target.instance!.AimPoint!.position - redGuy!.Muzzle!.position).normalized;
        gun.FireIfPossible(redGuy.Muzzle!.position, direction);
    }
    
    public void OnExit(RedGuy redGuy) {
        redGuy.EnableRotation();
    }
}

// We have a gun equipped, and want to find line of sight as soon as we can
// once we do, we'll switch to the Fire state
sealed class GunFindLineOfSightState: State {
    private readonly Pistol gun;
    private readonly Target target;
    
    public GunFindLineOfSightState(Pistol gun, Target target) {
        this.gun = gun;
        this.target = target;
    }

    public void OnEnter(RedGuy redGuy) {
        redGuy.SetDestination(target.AimPoint!.position);
    }
    
    public void OnUpdate(RedGuy redGuy) {
        // As soon as we have line of sight, we should fire
        if (redGuy.HasLineOfSightToTarget()) {
            redGuy.ChangeState(new StrafeAndFireGunState(gun));
            return;
        }
        
        // Update the position in case the player moved
        redGuy.SetDestination(target.AimPoint!.position);
    }
}

// Transitioned to when we get hit, but not killed
sealed class InterruptedState : State {
    private const float animationDuration = 0.5f;
    private float unscaledTimeOfAnimStart;

    private Vector3 hitPoint;

    public InterruptedState(Vector3 hitPoint) {
        this.hitPoint = hitPoint;
    }
    
    public void OnEnter(RedGuy redGuy) {
        unscaledTimeOfAnimStart = Time.unscaledTime;
        
        // TODO: Play the "flinch" animation
        
        redGuy.EnablePhysics();
        
        // drop the weapon, if we have one equipped
        redGuy.DropWeapon();
        
        // Should I do the health check in here or in RedGuy?
        redGuy.TakeDamage(hitPoint);
        
        redGuy.PlayDamagedVFX(hitPoint);
    }
    
    public void OnUpdate(RedGuy redGuy) {
        bool readyToResume = Time.unscaledTime - unscaledTimeOfAnimStart > animationDuration;
        if (!readyToResume) {
            return;
        }
        
        redGuy.ChangeState(new UnarmedChaseState(redGuy.FistTransform!));
    }
    
    public void OnExit(RedGuy redGuy) {
        redGuy.DisablePhysics();
    }
}

sealed class KilledState : State {
    private readonly Vector3 hitPoint;
    
    public KilledState(Vector3 hitPoint) {
        this.hitPoint = hitPoint;
    }
    
    public void OnEnter(RedGuy redGuy) {
        // Start the animation
        // also ragdoll maybe?
        redGuy.EnemyDiedEvent!.Event?.Invoke(redGuy.gameObject.GetInstanceID());
        
        redGuy.PlayDamagedVFX(hitPoint);
        
        // Drop the weapon
        redGuy.DropWeapon();
        
        // Create the destroyed prefab
        var destroyedInstance = Object.Instantiate(redGuy.DestroyedPrefab!, redGuy.transform.position, redGuy.transform.rotation);
        Object.Destroy(destroyedInstance, 5f); // Destroy after 2 seconds

        // Delete the actual one
        Object.Destroy(redGuy.gameObject);
    }
}

[RequireComponent(
    typeof(NavMeshAgent),
    typeof(Animator),
    typeof(Rigidbody)
)]
public class RedGuy : MonoBehaviour { // TODO: I might as well just call this Enemy
    [Header("Local references")]
    public Pistol? CurrentWeapon;

    [Tooltip("The muzzle of the gun, where we fire bullets from")]
    public Transform? Muzzle;

    [Tooltip("Where we place melee weapons / ")]
    public Transform? WeaponSpawnPoint;

    [Tooltip("Reference to the leading fist transform so we can check if we hit the player")]
    public Transform? FistTransform;
    
    [Tooltip("Reference to the collidable component so we can receive hit updates from the player")]
    public Collidable? Collidable;

    [Header("Prefabs")]
    [Tooltip("Prefab we create when this dies")]
    public GameObject? DestroyedPrefab;
    [Tooltip("VFX prefab we create whenever this is hit by something")]
    public VisualEffect? OnHitVFXPrefab;

    [Header("Events")]
    public EnemyLoadedEvent? EnemyLoadedEvent;
    public EnemyDiedEvent? EnemyDiedEvent;

    // It takes 3-4 hits (melee or thrown objects) to kill
    private int health = 3;
    
    public NavMeshAgent? navMeshAgent { get; private set; }
    public Animator? animator { get; private set; }
    private Rigidbody? rigidbody;
    private State currentState;

    private void Awake() {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        rigidbody = GetComponent<Rigidbody>();

        if (CurrentWeapon != null) {
            currentState = new FireGunState(CurrentWeapon);
        } else {
            currentState = new UnarmedChaseState(FistTransform!);
        }
    }
    
    private void Start() {
        var rigidBodyCollidable = GetComponent<Collidable>();
        rigidBodyCollidable!.OnHit += OnHit;
        
        Collidable!.OnHit += OnHit;
        
        EnemyLoadedEvent!.Event?.Invoke(gameObject.GetInstanceID());
        
        // since I need some form of level editor
        currentState.OnEnter(this);
    }

    private void Update() {
        currentState.OnUpdate(this);
    }

    private void OnHit(Collidable.Parameters parameters) {
        if (parameters.isLethal) {
            ChangeState(new KilledState(parameters.hitPoint));
        } else {
            ChangeState(new InterruptedState(parameters.hitPoint));
        }
    }

    public void ChangeState(State newState) {
        currentState.OnExit(this);
        newState.OnEnter(this);
        currentState = newState;
    }

    public bool HasLineOfSightToTarget() {
        var target = Target.instance!.AimPoint!.position;
        
         Vector3 directionToTarget = (target - Muzzle!.position).normalized;
        
        const float fov = 45;
        bool isInFOV =  Vector3.Angle(Muzzle!.forward, directionToTarget) < fov;
        
        if (!isInFOV) {
            return false;
        }
        
        if (Physics.Raycast(
            origin: Muzzle!.position,
            direction: directionToTarget,
            out RaycastHit hit,
            maxDistance: 1000f, // practically infinite range (doesn't matter b/c small levels)
            layerMask: -1 // Don't think we can want to ignore anything
        )) {
            if (hit.collider == Target.instance!.TargetCollider) {
                return true;
            }
        }
        
        return false;
    }

    public void SetDestination(Vector3 destination) {
        navMeshAgent!.SetDestination(destination);
    }
    
    public void RotateTowards(Vector3 position) {
        Vector3 direction = (position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }
    
    public void DisablePathfinding() {
        // Probably just change this to:
        // navMeshAgent!.enabled = false;
        navMeshAgent!.ResetPath();;
    }

    private float angularSpeed = -1f;
    public void EnableRotation() {
        navMeshAgent!.updateRotation = true;
        navMeshAgent!.angularSpeed = angularSpeed;
    }
    
    public void DisableRotation() {
        navMeshAgent!.updateRotation = false;
        angularSpeed = navMeshAgent!.angularSpeed;
        navMeshAgent!.angularSpeed = 0;
    }

    public void EnablePhysics() {
        navMeshAgent!.enabled = false;
        rigidbody!.isKinematic = false;
    }

    public void DisablePhysics() {
        navMeshAgent!.enabled = true;
        rigidbody!.isKinematic = true;
    }

    public void EquipWeapon(Pistol weaponPrefab) {
        CurrentWeapon = Instantiate(weaponPrefab, WeaponSpawnPoint!);
        CurrentWeapon.transform.SetPositionAndRotation(WeaponSpawnPoint!.position, WeaponSpawnPoint.rotation);
    }

    public void DropWeapon() {
        if (CurrentWeapon == null) {
            return;
        }
        
        Instantiate(CurrentWeapon.ThrowablePrefab!, WeaponSpawnPoint!.position, WeaponSpawnPoint!.rotation);
        
        Destroy(CurrentWeapon.gameObject);
        CurrentWeapon = null;
    }

    public void PlayDamagedVFX(Vector3 hitPoint) {
        VisualEffect damagedVFX = Instantiate(OnHitVFXPrefab!, hitPoint, Quaternion.identity);
        
        // Destroy after 2 seconds
        Destroy(damagedVFX.gameObject, 2f);
    }

    public void TakeDamage(Vector3 hitPoint) {
        health -= 1;
        
        if (health <= 0) {
            ChangeState(new KilledState(hitPoint));
        }
    }

    /*
    public float pickupradius = 5f;
    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, pickupradius);
    }
    */
}
