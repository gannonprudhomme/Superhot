using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

#nullable enable

[RequireComponent(typeof(PlayerMovementController))]
public class TimeDilation : MonoBehaviour  {
    public AnimationCurve TimeDilationCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    private float unscaledTimeOfLastForcedTimeDilation = Mathf.NegativeInfinity;
    private float currentForcedTimeDilationDuration = Mathf.NegativeInfinity;
    private const float ForcedTimeDilationValue = 0.75f;
    private float currForcedTimeDilation = 0;
    
    private const float MinTimeDilation = 0.05f;
    
    private PlayerMovementController movementController;
    private Vector3 velocity => movementController!.Velocity;
    
    private float forcedTimeDilation {
        get {
            // We really shouldn't do this every time
            if (Time.unscaledTime - unscaledTimeOfLastForcedTimeDilation < currentForcedTimeDilationDuration) {
                return ForcedTimeDilationValue;
            }
            return 0;
        }
    }

    public float TimeDilationValue {
        get {
            // TODO: This is bugging out blehhh
            // idk what changed
            /*
            if (!movementController!.characterController!.isGrounded) {
                Debug.Log("Not grounded");
                return 1f; // Force real-time when in the air
            } else {
                Debug.Log("Grounded");
            }
            */
            
            float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            
            float percentSpeed = horizontalSpeed / PlayerMovementController.MaxSpeed; // curve's t value, [0, 1]
            
            float dilation = Mathf.Max(TimeDilationCurve.Evaluate(percentSpeed), MinTimeDilation);
            dilation = Mathf.Max(dilation, forcedTimeDilation);

            return Mathf.Min(dilation, 1f); // Don't let it go above 1
        }
    }

    private void Awake() {
        // Dynamically changing this makes the forces different based on the time dilation
        // so it seems to need to be fixed
        Time.fixedDeltaTime = 0.02F * MinTimeDilation;
        VFXManager.fixedTimeStep = 0.02F * MinTimeDilation;
        
        movementController = GetComponent<PlayerMovementController>();
    }

    private void Update() {
        Time.timeScale = TimeDilationValue;
    }

    // force time dilation to speed up
    public void ForceTimeDilation(float unscaledDuration = 0.25f) {
        unscaledTimeOfLastForcedTimeDilation = Time.unscaledTime;
        currentForcedTimeDilationDuration = unscaledDuration;
    }
}
