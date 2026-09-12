using UnityEngine;

namespace VoiceRunner
{
    /// <summary>
    /// Optional: lets you test in the Editor without a mic. Hold Space to run,
    /// press Up Arrow or J to shout/jump. Uses the legacy Input Manager
    /// (Project Settings > Player > Active Input Handling must include it).
    /// Safe to leave in a build as a quiet accessibility fallback.
    /// </summary>
    public class KeyboardFallbackInput : MonoBehaviour
    {
        void Update()
        {
            var mic = MicrophoneInputManager.Instance;
            if (mic == null) return;

            if (Input.GetKeyDown(KeyCode.Space)) mic.ManualHold = true;
            if (Input.GetKeyUp(KeyCode.Space)) mic.ManualHold = false;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.J)) mic.QueueManualSpike();
        }
    }
}
