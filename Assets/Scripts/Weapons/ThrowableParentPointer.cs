using UnityEngine;

#nullable enable

// Placed on the collider of a ThrowableObject so we can easily get the parent object when we RaycastHit the collider
// Or more specifically, placed on the Hover Hitbox of the ThrowableObject, which should be larger than the collider used for the RigidBody
public class ThrowableParentPointer : MonoBehaviour {
    public ThrowableObject? Parent;
}
