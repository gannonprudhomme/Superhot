using UnityEngine;

#nullable enable

public interface Weapon {
    public void FirePressed();
    public void FireReleased();
}

public sealed class Pistol : MonoBehaviour, Weapon {
    [Header("References")]
    public Transform? Muzzle; // is there a better name?
    
    [Tooltip("The prefab for the bullet projectile")]
    public GameObject? BulletPrefab;

    private float lastTimeFired = Mathf.NegativeInfinity;
    private const float fireRate = 1f; // 1 shots per second

    private void Update() {
    }
    
    public void FirePressed() {
        // Spawn the BulletPrefab and rotate it correctly
        // and we should be good to go tbh, it should handle the rest
        if (Time.time - lastTimeFired < fireRate) {
            Debug.Log("Cooling down!");
            return;
        }
        
        lastTimeFired = Time.time;
        
    }
    
    public void FireReleased() { }
}
