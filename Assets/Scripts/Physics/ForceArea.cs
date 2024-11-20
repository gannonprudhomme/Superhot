using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class ForceArea : MonoBehaviour {
    public float Force = 800f;
    
    public Vector3 ForceVector;
    
    private float lifetime = 0.1f; // it shouldn't be alive for long! This is unscaled
    private float startUnscaledTime = Mathf.NegativeInfinity;

    private HashSet<Collider> alreadyDamaged = new();

    private void Start() {
        startUnscaledTime = Time.unscaledTime;
    }

    private void OnTriggerEnter(Collider other) {
        if (alreadyDamaged.Contains(other)) {
            Debug.LogError($"Already damaged {other}");
            return;
        }

        if (!other.TryGetComponent(out Rigidbody rigidBody)) {
            return;
        }
        
        rigidBody.AddForce(transform.forward * Force, ForceMode.Force);
    }

    private void OnTriggerExit(Collider other) { }

    private void FixedUpdate() {
        if (Time.unscaledTime - startUnscaledTime > lifetime) {
            Destroy(gameObject);
            return;
        }
    }
}
