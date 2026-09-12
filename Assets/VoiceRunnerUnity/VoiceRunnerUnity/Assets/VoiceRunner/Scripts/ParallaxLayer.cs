using UnityEngine;

namespace VoiceRunner
{
    /// <summary>
    /// Scrolls a repeating background layer at a fraction of the world speed for depth.
    /// Put this on each background layer (distant hills slower, near hills faster), with
    /// a wide tiling sprite/texture so the wrap-around isn't obvious.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Range(0f, 1f)] [SerializeField] private float speedMultiplier = 0.3f;
        [Tooltip("Width of one repeating tile, in world units, for the wrap-around reset.")]
        [SerializeField] private float tileWidth = 20f;

        private Vector3 startPos;

        void Start() => startPos = transform.position;

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;

            transform.position += Vector3.left * gm.CurrentSpeed * speedMultiplier * Time.deltaTime;
            if (transform.position.x < startPos.x - tileWidth)
                transform.position += Vector3.right * tileWidth;
        }
    }
}
