using UnityEngine;

public class ControlParticles : MonoBehaviour
{
    public ParticleSystem particleSystemToToggle;

    private bool isPlaying = false;

    void Start()
    {
        if (particleSystemToToggle == null)
            particleSystemToToggle = GetComponent<ParticleSystem>();

        // Ensure it's off at the start
        particleSystemToToggle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        isPlaying = false;
    }

    public void ToggleParticles()
    {
        if (isPlaying)
        {
            particleSystemToToggle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            isPlaying = false;
        }
        else
        {
            particleSystemToToggle.Play(); // Starts emission
            isPlaying = true;
        }
    }
}
