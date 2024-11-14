using TMPro;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

public class DebugTimeDilationUI : MonoBehaviour {
    
    
    public Image? FillImage;
    public TextMeshProUGUI? Text;
    
    private PlayerMovementController? playerMovementController;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start() {
        // this is obviously bad, but it's just for debugging
        playerMovementController = FindFirstObjectByType<PlayerMovementController>();
    }

    // Update is called once per frame
    private void Update() {
        FillImage!.fillAmount = playerMovementController!.TimeDilation;
        Text!.text = $"{playerMovementController!.TimeDilation:F2}";
    }
}
