using UnityEngine;
using UnityEngine.Events;

#nullable enable

// TODO: Explain this & why I did it, cause honestly after a few days I'm confused
// might want to add callback to this or something
public class Collidable : MonoBehaviour  {
    public struct Parameters {
        public Vector3 hitPoint;
        public bool isLethal;
    }
    
    public UnityAction<Parameters>? OnHit;
}
