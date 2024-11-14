using UnityEngine;
using UnityEngine.Events;

#nullable enable

// might want to add callback to this or something
public class Collidable : MonoBehaviour  {
    public UnityAction<Vector3>? OnHit;
}
