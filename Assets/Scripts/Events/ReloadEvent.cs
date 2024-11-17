using UnityEngine;
using UnityEngine.Events;

// Called whenever we're "reloading" and can't fire
// Not all guns need this - e.g. the machine gone doesn't, but the pistol does
[CreateAssetMenu(menuName = "ScriptableObjects/ReloadEvent")]
public class ReloadEvent : ScriptableObject {
    public UnityAction<float> ReloadStart;
}
