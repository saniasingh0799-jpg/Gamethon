using UnityEngine;

namespace VoiceRunner
{
    /// <summary>
    /// Player stays at a fixed X position; the world scrolls past it (see ObstacleSpawner /
    /// ParallaxLayer, which read GameManager.CurrentSpeed). This script only handles the
    /// jump: a shout/spike from the mic (or fallback) arcs the visual up for a fixed
    /// duration, during which obstacle collisions are ignored — timing the shout is the skill.
    /// Requires a Rigidbody2D (Body Type: Kinematic) on this object so 2D trigger
    /// events fire against obstacles.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlayerRunner : MonoBehaviour
    {
        [SerializeField] private float jumpDuration = 0.5f;
        [SerializeField] private float jumpHeight = 1.6f;
        [Tooltip("The sprite/visual to bob up and down. Leave empty to move this transform.")]
        [SerializeField] private Transform visualRoot;
        [Tooltip("Optional: simple leg/run animation driven by GameManager.CurrentSpeed.")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedAnimatorParam = "Speed01";

        private float airTimer;
        private float startY;

        void Start()
        {
            var t = visualRoot != null ? visualRoot : transform;
            startY = t.localPosition.y;
        }

        void OnEnable()
        {
            if (MicrophoneInputManager.Instance != null)
                MicrophoneInputManager.Instance.OnSpike += HandleSpike;
        }

        void OnDisable()
        {
            if (MicrophoneInputManager.Instance != null)
                MicrophoneInputManager.Instance.OnSpike -= HandleSpike;
        }

        private void HandleSpike()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (airTimer <= 0f) Jump();
        }

        public void Jump()
        {
            airTimer = jumpDuration;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (airTimer > 0f)
            {
                airTimer -= Time.deltaTime;
                float progress = 1f - Mathf.Clamp01(airTimer / jumpDuration);
                float h = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
                SetVisualY(startY + h);
                gm.IsPlayerAirborne = true;
                if (airTimer < 0f) airTimer = 0f;
            }
            else
            {
                SetVisualY(startY);
                gm.IsPlayerAirborne = false;
            }

            if (animator != null && gm.MaxSpeed > 0f)
                animator.SetFloat(speedAnimatorParam, Mathf.Clamp01(gm.CurrentSpeed / gm.MaxSpeed));
        }

        private void SetVisualY(float y)
        {
            var t = visualRoot != null ? visualRoot : transform;
            var p = t.localPosition;
            p.y = y;
            t.localPosition = p;
        }
    }
}
