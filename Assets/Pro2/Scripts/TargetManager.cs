using UnityEngine;
using System.Collections.Generic;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance;

    private class TargetData
    {
        public Renderer rend;
        public Color originalColor;
        public float hitTimer = 0f;
        public bool isGreen = false;
    }

    private Dictionary<GameObject, TargetData> targets = new Dictionary<GameObject, TargetData>();
    private HashSet<GameObject> hitThisFrame = new HashSet<GameObject>();

    private bool timerRunning = false;
    private float timer = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;

        // Register all targets automatically
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Target");
        foreach (var obj in objs)
        {
            var rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                targets[obj] = new TargetData
                {
                    rend = rend,
                    originalColor = rend.material.color
                };
            }
        }
    }

    void Update()
    {
        // Update global timer if running
        if (timerRunning)
        {
            timer += Time.deltaTime;
            Debug.Log("Timer: " + timer.ToString("F2"));

            // Check if all targets are green
            bool allGreen = true;
            foreach (var t in targets.Values)
            {
                if (!t.isGreen)
                {
                    allGreen = false;
                    break;
                }
            }

            if (allGreen)
            {
                timerRunning = false;
                Debug.Log("All targets green! Final time: " + timer.ToString("F2"));
            }
        }
    }

    public void HandleHit(GameObject obj, float deltaTime)
    {
        if (!targets.ContainsKey(obj)) return;

        TargetData t = targets[obj];

        // Mark this target as hit this frame
        hitThisFrame.Add(obj);

        // Increase hit time while spotlight is on target
        t.hitTimer += deltaTime;

        if (t.hitTimer >= 2f && !t.isGreen)
        {
            t.rend.material.color = Color.green;
            t.isGreen = true;

            if (!timerRunning)
            {
                timerRunning = true;
                timer = 0f;
                Debug.Log("Timer started!");
            }
        }

        targets[obj] = t;
    }

    void LateUpdate()
    {
        foreach (var kvp in new Dictionary<GameObject, TargetData>(targets))
        {
            TargetData t = kvp.Value;

            // If not hit this frame and not green, reset timer
            if (!hitThisFrame.Contains(kvp.Key) && !t.isGreen)
            {
                t.hitTimer = 0f;
            }

            targets[kvp.Key] = t;
        }

        // Clear hits for next frame
        hitThisFrame.Clear();
    }

    /// <summary>
    /// Reset all targets to their original color and state.
    /// </summary>
    public void ResetAllTargets()
    {
        foreach (var kvp in targets)
        {
            TargetData t = kvp.Value;
            t.rend.material.color = t.originalColor;
            t.isGreen = false;
            t.hitTimer = 0f;
            targets[kvp.Key] = t;
        }

        timerRunning = false;
        timer = 0f;

        Debug.Log("All targets reset");
    }
}
