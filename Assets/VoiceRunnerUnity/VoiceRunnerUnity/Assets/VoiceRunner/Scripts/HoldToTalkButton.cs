using UnityEngine;
using UnityEngine.EventSystems;

namespace VoiceRunner
{
    /// <summary>
    /// Put on the "Hold to run" fallback button alongside its Button/Image components.
    /// Sets ManualHold true while pressed, false on release — this feeds the same
    /// volume pipeline the mic uses, so no other script needs to know which input is active.
    /// </summary>
    public class HoldToTalkButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            if (MicrophoneInputManager.Instance != null)
                MicrophoneInputManager.Instance.ManualHold = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (MicrophoneInputManager.Instance != null)
                MicrophoneInputManager.Instance.ManualHold = false;
        }
    }
}
