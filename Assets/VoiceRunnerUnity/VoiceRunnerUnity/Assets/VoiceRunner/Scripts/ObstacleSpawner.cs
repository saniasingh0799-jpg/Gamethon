using System.Collections.Generic;
using UnityEngine;

namespace VoiceRunner
{
    [System.Serializable]
    public class ObstaclePrefabEntry
    {
        public GameObject prefab;
        [Tooltip("Lowest level this obstacle type can appear at.")]
        public int unlockLevel = 1;
        [Tooltip("Relative spawn weight among currently-unlocked types.")]
        [Range(0.1f, 3f)] public float weight = 1f;
    }

    /// <summary>
    /// This is the "AI level creation" piece: rather than hand-authored levels, obstacles
    /// are picked from a weighted, level-gated pool and spaced by a gap that shrinks as
    /// the run goes on, so the track keeps generating itself and getting harder forever.
    /// Moves + despawns everything at GameManager.CurrentSpeed, so the whole world freezes
    /// in place if the player goes silent.
    /// </summary>
    public class ObstacleSpawner : MonoBehaviour
    {
        [SerializeField] private List<ObstaclePrefabEntry> obstaclePool;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float despawnX = -12f;
        [SerializeField] private Vector2 baseGapRangeMeters = new Vector2(18f, 28f);
        [SerializeField] private float gapShrinkPerLevel = 1.3f;
        [SerializeField] private float minGapMeters = 9f;
        [SerializeField] private float firstSpawnAtDistance = 14f;

        private readonly List<GameObject> active = new List<GameObject>();
        private float nextSpawnAtDistance;

        void OnEnable()
        {
            nextSpawnAtDistance = firstSpawnAtDistance;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;

            float speed = gm.CurrentSpeed;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var go = active[i];
                if (go == null) { active.RemoveAt(i); continue; }

                go.transform.position += Vector3.left * speed * Time.deltaTime;
                if (go.transform.position.x < despawnX)
                {
                    Destroy(go);
                    active.RemoveAt(i);
                }
            }

            if (gm.Distance >= nextSpawnAtDistance)
                Spawn(gm.Level);
        }

        private void Spawn(int level)
        {
            var candidates = new List<ObstaclePrefabEntry>();
            float totalWeight = 0f;
            foreach (var e in obstaclePool)
            {
                if (e.prefab == null) continue;
                if (e.unlockLevel <= level) { candidates.Add(e); totalWeight += e.weight; }
            }
            if (candidates.Count == 0) return;

            float r = Random.value * totalWeight;
            GameObject chosen = candidates[0].prefab;
            foreach (var e in candidates)
            {
                r -= e.weight;
                if (r <= 0f) { chosen = e.prefab; break; }
            }

            var go = Instantiate(chosen, spawnPoint.position, Quaternion.identity);
            active.Add(go);

            float gapMin = Mathf.Max(minGapMeters, baseGapRangeMeters.x - level * gapShrinkPerLevel);
            float gapMax = Mathf.Max(gapMin + 2f, baseGapRangeMeters.y - level * gapShrinkPerLevel);
            nextSpawnAtDistance = GameManager.Instance.Distance + Random.Range(gapMin, gapMax);
        }

        /// <summary>Call when restarting a run so old obstacles don't linger.</summary>
        public void ClearAll()
        {
            foreach (var go in active) if (go != null) Destroy(go);
            active.Clear();
            nextSpawnAtDistance = firstSpawnAtDistance;
        }
    }
}
