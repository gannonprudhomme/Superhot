using UnityEngine;
using UnityEngine.InputSystem;

#nullable enable

public class DebugSpawner : MonoBehaviour {
    [Tooltip("Array of prefabs, corresponds to keys 1-9. Index 0 in this is Key 1. Key 0 is not used.")]
    public GameObject[]? Prefabs = new GameObject[1];

    private InputAction?[] spawnInputs;

    private void Awake() {
        spawnInputs = new InputAction[Prefabs!.Length];
        
        for (int i = 0; i < Prefabs.Length; i++) {
            spawnInputs[i] = InputSystem.actions.FindAction($"Spawn{i+1}");
        }
    }

    private void Update() {
        for (int i = 0; i < spawnInputs.Length; i++) {
            if (spawnInputs[i]!.WasPressedThisFrame()) {
                Instantiate(Prefabs![i], GetLookPosition(), Quaternion.identity);
            }
        }
    }
    
    private Vector3 GetLookPosition() {
        Ray ray = Camera.main!.ScreenPointToRay(Mouse.current.position.ReadValue());
        
        if (Physics.Raycast(ray, out RaycastHit hit)) {
            return hit.point;
        }
        
        return ray.GetPoint(10f);
    }
}
