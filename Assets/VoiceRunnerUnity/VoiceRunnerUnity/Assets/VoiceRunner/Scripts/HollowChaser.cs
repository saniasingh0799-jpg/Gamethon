using UnityEngine;

namespace VoiceRunner
{
    /// <summary>
    /// Visualizes the chasing NPC as a shadow/fog that creeps in from the left edge of the
    /// world. Its reach is driven directly by GameManager.Hollow (100 = safe/off-screen,
    /// 0 = it has reached the player = caught). Attach to a left-pivoted sprite/quad that
    /// sits behind everything else, scaled on X to represent how far it's crept.
    /// </summary>
    public class HollowChaser : MonoBehaviour
    {
        [Tooltip("The left-anchored sprite/quad stretched on X to show how far the Hollow has reached.")]
        [SerializeField] private Transform fogVisual;
        [SerializeField] private SpriteRenderer fogRenderer;
        [Tooltip("World-space X position of the player (fixed, since the player doesn't move).")]
        [SerializeField] private float playerX = 0f;
        [SerializeField] private float maxReachPastPlayer = 1.2f;
        [SerializeField] private Color safeColor = new Color(0.10f, 0.05f, 0.20f, 0.85f);
        [SerializeField] private Color urgentColor = new Color(0.55f, 0.10f, 0.15f, 0.92f);

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || fogVisual == null) return;

            float reach = (1f - gm.Hollow / 100f) * (playerX + maxReachPastPlayer);
            var scale = fogVisual.localScale;
            scale.x = Mathf.Max(0.001f, reach);
            fogVisual.localScale = scale;

            if (fogRenderer != null)
            {
                float urgency = 1f - Mathf.Clamp01(gm.Hollow / 40f);
                fogRenderer.color = Color.Lerp(safeColor, urgentColor, urgency);
            }
        }
    }
}
