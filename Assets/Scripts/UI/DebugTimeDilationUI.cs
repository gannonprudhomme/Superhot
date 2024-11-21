using TMPro;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

public class DebugTimeDilationUI : MonoBehaviour {
    public Image? FillImage;
    public TextMeshProUGUI? Text;
    
    private TimeDilation? timeDilation;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start() {
        // this is obviously bad, but it's just for debugging
        timeDilation = FindFirstObjectByType<TimeDilation>();
    }

    private void Update() {
        if (timeDilation == null) {
            // Handle level loading (since the player gets recreated)
            timeDilation = FindFirstObjectByType<TimeDilation>();
        }
        
        // If it's still null, we're probably loading
        if (timeDilation == null) {
            return;
        }
        
        FillImage!.fillAmount = timeDilation!.TimeDilationValue;
        Text!.text = $"{timeDilation!.TimeDilationValue:F2}";
    }
}
