using UnityEngine;
using UnityEngine.UI;  // Needed for the UI Slider

public class HingeMotorController : MonoBehaviour
{
    public GameObject Cam;
    private HingeJoint hinge;       // Reference to hinge joint
    public Slider speedSlider;      // Assign in Inspector

    public void StartMotor()
    {
        // Find the hinge joint on this object
        hinge = Cam.GetComponent<HingeJoint>();

        if (hinge != null)
        {
            hinge.useMotor = true;

            // Initialize motor settings
            JointMotor motor = hinge.motor; 
            motor.targetVelocity = (speedSlider != null ? speedSlider.value : 0f) * 1000f;
            hinge.motor = motor;
        }

        // Attach listener to the slider
        if (speedSlider != null)
        {
            speedSlider.onValueChanged.AddListener(SetMotorSpeed);
        }
    }

    // Function to set motor speed from the slider
    public void SetMotorSpeed(float speed)
    {
        if (hinge != null)
        {
            //Debug.LogError("Speed is set to " + speed*100);

            // Always pull the current motor struct, edit, and re-apply
            JointMotor motor = hinge.motor;
            motor.targetVelocity = speed*1000;
            hinge.motor = motor;
        }
    }

    public void StopMotor()
    {
        hinge = Cam.GetComponent<HingeJoint>();
        if (hinge != null)
        {
            hinge.useMotor = false;
        }
    }
}
