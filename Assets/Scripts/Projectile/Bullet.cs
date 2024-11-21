using System;
using UnityEngine;

#nullable enable

public class Bullet : MonoBehaviour {
    // This is the parent object basically
    [Tooltip("Transform representing the root of the projectile (used for accurate collision detection")]
    public Transform? Root;

    [Tooltip("Transform representing the tip of the projectile (used for accurate collision detection")]
    public Transform? Tip;

    public float Radius = 2f;

    [Tooltip("Layers this projectile can hit")] // Should be all for now
    public LayerMask HittableLayers = -1;

    public ForceArea? ForceArePrefab;
    
    private Vector3 lastRootPosition = Vector3.negativeInfinity;

    private const float speed = 20f; 
    private Vector3 velocity = Vector3.zero;

    private float timeOfStart = Mathf.Infinity;
    private const float Lifetime = 2f; // This shouldn't really matter when we have levels.
    
    private void Start() {
        timeOfStart = Time.time;
        velocity = transform.forward * speed;
    }

    private void Update() {
        if (Time.time - timeOfStart > Lifetime) {
            Destroy(gameObject);
            return;
        }
        
        HitCheck();
        
        transform.position += velocity * Time.deltaTime;
    }

    private void HitCheck() {
        RaycastHit closestHit = new();
        closestHit.distance = Mathf.Infinity;
        bool foundHit = false;

        // TODO: I wonder if technically I should use nonalloc & reuse the same array for this (allocate once in Start)
        // Sphere cast (should it be a capsule cast? Probably, but depends on projectile)
        Vector3 displacementSinceLastFrame = Tip!.position - lastRootPosition;
        RaycastHit[] hits = Physics.SphereCastAll(
            lastRootPosition,
            Radius,
            displacementSinceLastFrame.normalized,
            displacementSinceLastFrame.magnitude,
            HittableLayers,
            QueryTriggerInteraction.Ignore
        );

        lastRootPosition = Root!.position;

        foreach(var hit in hits) {
            if (hit.distance < closestHit.distance) {
                foundHit = true;
                closestHit = hit;
            }
        }

        if (!foundHit) {
            return;
        }
        // Handle case of casting while already inside a collider
        // when tf does this happen?
        if (closestHit.distance <= 0f) {
            Debug.LogError($"Ack, for {closestHit.collider.gameObject.name}");
            closestHit.point = Root!.position;
            closestHit.normal = -transform.forward;
        }

        OnHit(closestHit.point, closestHit.normal, closestHit.collider);
        
        if (ForceArePrefab != null) {
            Instantiate(ForceArePrefab, closestHit.point, transform.rotation);
        }
    }

    private void OnHit(Vector3 point, Vector3 normal, Collider hitCollider) {
        if (hitCollider.gameObject.TryGetComponent(out Collidable collidable)) {
            collidable.OnHit?.Invoke(new Collidable.Parameters() { hitPoint = point, isLethal = true });
        } else if (hitCollider.gameObject.TryGetComponent(out Rigidbody rigidbody)) {
            float force = 100f;
            rigidbody.AddForceAtPosition(velocity.normalized * force, point, ForceMode.Impulse);
        }
        
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Root!.position, Radius);
    }
}
