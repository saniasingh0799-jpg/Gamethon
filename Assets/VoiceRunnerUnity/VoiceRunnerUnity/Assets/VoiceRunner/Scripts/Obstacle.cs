using UnityEngine;

namespace VoiceRunner
{
    /// <summary>
    /// Put on every obstacle prefab. Collider2D must be set to "Is Trigger".
    /// Only registers one hit per obstacle instance, whether or not the hit
    /// actually costs the player anything (GameManager decides that based on
    /// whether the player was airborne/invulnerable at the time).
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";
        private bool hit;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (hit) return;
            if (!other.CompareTag(playerTag)) return;

            hit = true;
            GameManager.Instance.RegisterObstacleHit();
        }
    }
}
