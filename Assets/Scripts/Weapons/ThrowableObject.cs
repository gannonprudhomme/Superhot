using System;
using System.Collections;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

#nullable enable

[RequireComponent(
    typeof(Rigidbody)
)]
public class ThrowableObject : MonoBehaviour  {
    public Rigidbody? Rigidbody { get; private set; }

    [Tooltip("Weapon prefab which is created whenever this is picked up")]
    public Pistol? WeaponPrefab;

    [Tooltip("Prefab that is created when this object is destroyed")]
    public GameObject? DestructedPrefab;
    
    public bool IsBeingPickedUp => moveTween != null || rotateTween != null;

    private TweenerCore<Vector3, Vector3, VectorOptions>? moveTween;
    private TweenerCore<Quaternion, Vector3, QuaternionOptions>? rotateTween;

    private bool hasCollided = false;

    private void Awake() {
        Rigidbody = GetComponent<Rigidbody>();
    }

    private void OnDestroy() {
        moveTween?.Kill();
        rotateTween?.Kill();
    }


    private void OnCollisionEnter(Collision hitCollider) {
        if (hasCollided) return;
        
        hasCollided = true;
        
        if (hitCollider.gameObject.TryGetComponent(out Collidable collidable)) {   
            collidable.OnHit?.Invoke(new Collidable.Parameters {
                hitPoint = hitCollider.GetContact(0).point, // TODO: Idk which we should do, maybe the least one?
                isLethal = false
            });
            
            GameObject destructedObject = Instantiate(DestructedPrefab!, transform.position, transform.rotation);
            Destroy(destructedObject, 5f);
            
            Destroy(gameObject);
        }
    }

    public IEnumerator Pickup(
        Transform goalTransform
    ) {
        // This is better represented as speed
        const float moveSpeed = 40f; // in seconds...?
        const float rotationSpeed = 2000f;
        
        // Sequence sequence = DOTween.Sequence();
        moveTween = transform
            .DOMove(goalTransform.position, duration: moveSpeed, snapping: false)
            .SetSpeedBased(true)
            .SetEase(Ease.Linear)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true); // ignore timescale

        rotateTween = transform
            .DORotate(goalTransform.rotation.eulerAngles, duration: rotationSpeed)
            .SetSpeedBased(true)
            .SetEase(Ease.Linear)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true); // ignore timescale

        rotateTween.onUpdate = () => {
            // We don't need to stop this because we don't await the rotation tween's completion
            // though idk if that's necessarily a good idea
            rotateTween.ChangeEndValue(goalTransform.rotation.eulerAngles, snapStartValue: true);
        };
        
        moveTween.onUpdate = () => {
            const float completionRadius = 1f; // 0.5f means we can run away from it
            float distance = Vector3.Distance(transform.position, goalTransform.position);
            
            // This happens basically every update
            if (distance > completionRadius) {
                moveTween.ChangeEndValue(goalTransform.position, true);
            }
        };

        yield return moveTween.WaitForCompletion();
    } 
}
