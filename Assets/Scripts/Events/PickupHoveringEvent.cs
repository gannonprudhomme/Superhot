using UnityEngine;
using UnityEngine.Events;

#nullable enable

// Used for communicating between the PlayerController detecting hover --> the ReticleUI
[CreateAssetMenu(menuName = "ScriptableObjects/PickupHoveringEvent")]
public class PickupHoveringEvent : ScriptableObject {
    public UnityAction? OnHovering;
    public UnityAction? OnNotHovering;
}
