using System;
using UnityEngine;

#nullable enable

[RequireComponent(
    typeof(Rigidbody)
)]
public class ThrowableObject : MonoBehaviour  {
    public Rigidbody? Rigidbody { get; private set; }

    [Tooltip("Weapon prefab which is created whenever this is picked up")]
    public Pistol? WeaponPrefab;

    private void Awake() {
        Rigidbody = GetComponent<Rigidbody>();
    }
}
