using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AddText : MonoBehaviour
{
    public Slider slider;        // assign in inspector
    public TMP_Text valueText;   // assign in inspector

    void Start()
    {
        if (slider != null && valueText != null)
        {
            // Initialize with current slider value
            UpdateText(slider.value);

            // Subscribe to value change event
            slider.onValueChanged.AddListener(UpdateText);
        }
    }

    private void UpdateText(float value)
    {
        // Example: convert slider angle into RPS
        float valueToEnter = (value / 360f)*1000;
        valueText.text = valueToEnter.ToString("0.0") + " RPS";
    }
}
