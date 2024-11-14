using System;
using UnityEngine;
using UnityEngine.AI;

#nullable enable

// I'm wondering if we need a movement state and an attack state
// but the attack state probably needs to influence the movement state
// e.g. we should charge if we have a melee weapon, but might not want to with a gun
// and if we're unarmed we need to go find a weapon (and/or charge) - this might be simpler though
public abstract class State {
    // Honestly we're just going to need the red guy's state
    protected RedGuy redGuy;
    
    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }
    public virtual void OnExit() { }
}

// Don't want this to be public? Just for this file
sealed class Unarmed : State {
    public override void OnUpdate() {
        // See if there's a weapon nearby
        
        // if there is, maybe "lean" towards it?
        // once we are ready to grab it, move to PickupWeapon()   
        GameObject weapon; // Only do this if we found one
        redGuy.ChangeState(new PickupWeapon());
    }
}

sealed class PickupWeapon: State {}

/*
sealed class GunEquipped : State {
    private Weapon gun;
    
    public GunEquipped(Weapon gun) {
        this.gun = gun;
    }
    
    public override void OnEnter() {
        // Maybe do an animation to show the gun?
    }
}

sealed class MeleeEquiped : State {
    private Weapon meleeWeapon;
    
    public MeleeEquiped(Weapon weapon) {
        this.meleeWeapon = weapon;
    }
    
    public override void OnUpdate() {
        // Charge!
    }
}
*/

sealed class FindNewPosition: State { }

sealed class Killed : State {
    private Vector3 hitPoint;
    
    public Killed(Vector3 hitPoint, RedGuy redGuy) {
        this.redGuy = redGuy; // why tf I am not enforced by the compiler to do this
        this.hitPoint = hitPoint;
    }
    
    public override void OnEnter() {
        // Start the animation
        // also ragdoll maybe?
    }
}

[RequireComponent(
    typeof(NavMeshAgent)
)]
public class RedGuy : MonoBehaviour { // TODO: I might as well just call this Enemy
    public Weapon? CurrentWeapon;
    
    public Collidable? Collidable;

    private NavMeshAgent? navMeshAgent;
    private State currentState = new Unarmed();

    private void Awake() {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }
    
    private void Start() {
        // Make sure we have the right state, we need to set it in the Editor somehow
        // since I need some form of level editor
        
        Collidable!.OnHit += OnHit;
    }

    private void Update() {
        navMeshAgent!.SetDestination(Target.instance!.AimPoint!.position);
    }

    private void OnHit(Vector3 hitPoint) {
        Destroy(this.gameObject);
    }

    public void ChangeState(State newState) {
        currentState.OnExit();
        newState.OnEnter();
        currentState = newState;
    }
}
