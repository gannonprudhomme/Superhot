using UnityEngine;

#nullable enable

// TODO: Rename this folder lol

// There should only be one of these (the player)
public class Target : MonoBehaviour {
    public static Target instance { get; private set; }
    
    public Transform? AimPoint;
    
    private void Awake() {
        if (instance != null) {
            Debug.LogError("There should only be one Target instance!");
        }
        
        instance = this;
    }
}
