using System;
using UnityEngine;
using UnityEngine.Serialization;

#nullable enable

public abstract class Weapon: MonoBehaviour {
    [Header("Weapon (Inherited)")]
    public ThrownObject? ThrownPrefab;
    
    // Muzzle is really just where the bullets fire from
    // It's not actually the muzzle of the gun
    //
    // Returns true if we successfully fired
    public virtual bool FirePressed(Transform muzzle) { return false; }
}

public abstract class Gun : Weapon {
    protected float ammoCount = 0;
    public bool IsOutOfAmmo => ammoCount <= 0;
    
    // TODO: These only apply to Guns, not generic throwables/weapons
    public abstract bool RequiresReload { get; }
    public abstract float ReloadDuration { get; }

    // What enemies use to fire weapons
    // (also what guns use internally, at least at the time of writing)
    public virtual bool FireIfPossible(Vector3 spawnPoint, Vector3 direction) { return false; }
}

public sealed class Pistol : Gun {
    [Header("References")]
    [Tooltip("The prefab for the bullet projectile")]
    public GameObject? BulletPrefab;

    private const float StartingAmmo = 5;
    
    public override float ReloadDuration => 1f; // 1 shot per second
    public override bool RequiresReload => true;

    private float lastTimeFired = Mathf.NegativeInfinity;

    private void Awake() {
        ammoCount = StartingAmmo;
    }

    public override bool FirePressed(Transform muzzle) {
        if (IsOutOfAmmo) {
            Debug.LogError("Calling fire when we're out of ammo - this shouldn't ever happen");
            return false;
        }
        
        // Give enemies infinite ammo / only subtract for the player
        // though idk if they actually have infinite ammo
        ammoCount -= 1;
        return FireIfPossible(muzzle.position, muzzle.forward);
    }

    public override bool FireIfPossible(Vector3 spawnPoint, Vector3 direction) {
        // Spawn the BulletPrefab and rotate it correctly
        // and we should be good to go tbh, it should handle the rest
        if (Time.time - lastTimeFired < ReloadDuration) {
            return false;
        }
        
        lastTimeFired = Time.time;
        
        Instantiate(BulletPrefab!, spawnPoint, Quaternion.LookRotation(direction));

        return true;
    }
}
