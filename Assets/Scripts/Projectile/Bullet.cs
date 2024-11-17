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

    [Tooltip("Layers this projectile can hit")]
    public LayerMask HittableLayers = -1;
    
    private Vector3 lastRootPosition = Vector3.negativeInfinity;

    private const float speed = 20f; 
    private Vector3 velocity = Vector3.zero;

    private void Start() {
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

        if (closestHit.distance <= 0f) {
            closestHit.point = Root!.position;
            closestHit.normal = -transform.forward;
        }

        OnHit(closestHit.point, closestHit.normal, closestHit.collider);
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

}
