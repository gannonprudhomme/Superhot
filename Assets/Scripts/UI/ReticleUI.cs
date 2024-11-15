using UnityEngine;
using UnityEngine.UI;

#nullable enable

public class ReticleUI : MonoBehaviour  {
    [Tooltip("Image that is shown when we're not hovering")]
    public Image? ReticleImage;
    
    [Tooltip("Image that is shown when we're hovering")]
    public Image? HoveringImage;

    [Tooltip("Event we subscribe to when we're hovering over a pickup")]
    public PickupHoveringEvent? PickupHoveringEvent;
    
    private void Start() {
        ReticleImage!.enabled = true;
        HoveringImage!.enabled = false;
        
        PickupHoveringEvent!.OnHovering += OnHover;
        PickupHoveringEvent!.OnNotHovering += OnNotHover;
    }

    private void OnHover() {
        ReticleImage!.enabled = false;
        HoveringImage!.enabled = true;
    }
    
    private void OnNotHover() {
        ReticleImage!.enabled = true;
        HoveringImage!.enabled = false;
    }
}
