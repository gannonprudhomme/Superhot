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
                hitPoint = hitCollider.GetContact(0).point,
                isLethal = false
            });
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
            rotateTween.ChangeEndValue(goalTransform.rotation.eulerAngles, snapStartValue: true);
            Debug.Log($"Current rotation is {transform.rotation.eulerAngles}, updating goal to {goalTransform.rotation.eulerAngles}");
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
