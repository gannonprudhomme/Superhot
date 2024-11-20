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
        FillImage!.fillAmount = timeDilation!.TimeDilationValue;
        Text!.text = $"{timeDilation!.TimeDilationValue:F2}";
    }
}
