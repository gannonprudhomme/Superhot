using UnityEngine;

#nullable enable

public interface Weapon {
    public ThrowableObject? ThrowablePrefab { get; }
    
    // Muzzle is really just where the bullets fire from
    // It's not actually the muzzle of the gun
    public void FirePressed(Transform muzzle);
    public void FireReleased();
}

public sealed class Pistol : MonoBehaviour, Weapon {
    [Header("References")]
    [Tooltip("The prefab for the bullet projectile")]
    public GameObject? BulletPrefab;
    
    [Tooltip("Prefab for the throwable object that's used when we throw this")]
    public ThrowableObject? _ThrowablePrefab;
    
    public ThrowableObject? ThrowablePrefab => _ThrowablePrefab!;

    private float lastTimeFired = Mathf.NegativeInfinity;
    private const float fireRate = 1f; // 1 shots per second
    
    public void FirePressed(Transform muzzle) {
        // Spawn the BulletPrefab and rotate it correctly
        // and we should be good to go tbh, it should handle the rest
        if (Time.time - lastTimeFired < fireRate) {
            Debug.Log("Cooling down!");
            return;
        }
        
        lastTimeFired = Time.time;
        
        GameObject bullet = Instantiate(BulletPrefab!, muzzle!.position, muzzle!.rotation);
    }
    
    public void FireReleased() { }
}
