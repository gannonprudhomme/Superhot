using UnityEngine;
using UnityEngine.Events;

#nullable enable

// Used for communicating between the PlayerController detecting hover --> the ReticleUI
[CreateAssetMenu(menuName = "ScriptableObjects/PickupHoveringEvent")]
public class PickupHoveringEvent : ScriptableObject {
    public enum HoverType {
        THROWABLE, ENEMY
    }
    
    // Honestly we might as well just make this a singleton?
    
    public UnityAction<HoverType>? OnHovering;
    public UnityAction? OnNotHovering;
}
