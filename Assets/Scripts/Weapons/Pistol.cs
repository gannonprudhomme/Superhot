using UnityEngine;

#nullable enable

public interface Weapon {
    public ThrowableObject? ThrowablePrefab { get; }
    
    // Muzzle is really just where the bullets fire from
    // It's not actually the muzzle of the gun
    //
    // Returns true if we successfully fired
    public bool FirePressed(Transform muzzle);
    public void FireReleased();
    
    public bool RequiresReload { get; }
    public float ReloadDuration { get; }
}

public sealed class Pistol : MonoBehaviour, Weapon {
    [Header("References")]
    [Tooltip("The prefab for the bullet projectile")]
    public GameObject? BulletPrefab;
    
    [Tooltip("Prefab for the throwable object that's used when we throw this")]
    public ThrowableObject? _ThrowablePrefab;
    
    public ThrowableObject? ThrowablePrefab => _ThrowablePrefab!;

    private const float StartingAmmo = 5;
    private float ammoCount = StartingAmmo;
    
    public float ReloadDuration => 1f; // 1 shot per second
    public bool RequiresReload => true;

    private float lastTimeFired = Mathf.NegativeInfinity;
    
    public bool IsOutOfAmmo => ammoCount <= 0;
    
    public bool FirePressed(Transform muzzle) {
        if (IsOutOfAmmo) {
            Debug.LogError("Calling fire when we're out of ammo - this shouldn't ever happen");
            return false;
        }
        
        // Give enemies infinite ammo / only subtract for the player
        // though idk if they actually have infinite ammo
        ammoCount -= 1;
        return FireIfPossible(muzzle.position, muzzle.forward);
    }
    
    public void FireReleased() { }

    public bool FireIfPossible(Vector3 spawnPoint, Vector3 direction) {
        // Spawn the BulletPrefab and rotate it correctly
        // and we should be good to go tbh, it should handle the rest
        if (Time.time - lastTimeFired < ReloadDuration) {
            return false;
        }
        
        lastTimeFired = Time.time;
        
        GameObject bullet = Instantiate(BulletPrefab!, spawnPoint, Quaternion.LookRotation(direction));

        return true;
    }
}
