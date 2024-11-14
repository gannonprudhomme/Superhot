using UnityEngine;
using UnityEngine.InputSystem;

#nullable enable

[RequireComponent(
    typeof(PlayerMovementController),
    typeof(Target)
)]
public class PlayerController : MonoBehaviour {
    [Header("References")]
    public Pistol? EquippedWeapon; // this will be private & handled by picking up weapons later
    
    private InputAction? fireAction;
    private InputAction? throwAction;

    private void Awake() {
        // TODO: Remove this duh
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        fireAction = InputSystem.actions.FindAction("Attack");
        throwAction = InputSystem.actions.FindAction("Throw");
    }

    private void Update() {
        HandleInputs();
    }

    private void HandleInputs() {
        if (fireAction!.WasPressedThisFrame()) {
            EquippedWeapon!.FirePressed();
        }

        if (fireAction!.WasReleasedThisFrame()) {
            EquippedWeapon!.FireReleased();
        }
    }
}
