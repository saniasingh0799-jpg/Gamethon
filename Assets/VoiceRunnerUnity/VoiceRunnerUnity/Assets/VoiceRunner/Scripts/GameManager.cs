using UnityEngine;

namespace VoiceRunner
{
    public enum GameState { MainMenu, Calibrating, Playing, GameOver }

    /// <summary>
    /// Single source of truth for the run: converts mic volume into world speed,
    /// tracks distance/level, and runs the "Hollow" chase meter (the NPC that
    /// catches you if you stay quiet too long or clip too many obstacles).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Speed")]
        [Tooltip("World units/second the player reaches at full voice volume, at level 1.")]
        [SerializeField] private float baseMaxSpeed = 6.5f;
        [Tooltip("Extra max-speed fraction added per level, capped at level 10.")]
        [SerializeField] private float speedLevelGrowth = 0.035f;
        [SerializeField] private float speedSmoothing = 4f;

        [Header("The Hollow (chase meter, 0 = caught)")]
        [Range(0.05f, 0.9f)] [SerializeField] private float safeSpeedFraction = 0.4f;
        [SerializeField] private float hollowRecoveryRate = 16f;
        [SerializeField] private float collisionPenalty = 22f;
        [SerializeField] private float collisionInvulnDuration = 0.7f;

        [Header("Distance / Level")]
        [SerializeField] private float metersPerWorldUnit = 1f;
        [SerializeField] private float metersPerLevel = 85f;

        public GameState State { get; private set; } = GameState.MainMenu;
        public float CurrentSpeed { get; private set; }
        public float MaxSpeed { get; private set; }
        public float Distance { get; private set; }
        public int Level { get; private set; } = 1;
        public float Hollow { get; private set; } = 100f;
        public bool IsInvulnerable => invulnTimer > 0f;

        /// <summary>Set by PlayerRunner each frame — obstacle hits are ignored while airborne.</summary>
        public bool IsPlayerAirborne { get; set; }

        public System.Action<int> OnLevelChanged;
        public System.Action OnCaught;
        public System.Action OnObstacleHit;

        private float invulnTimer;
        private float volSmoothed;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void SetCalibrating() => State = GameState.Calibrating;
        public void ReturnToMenu() => State = GameState.MainMenu;

        public void BeginRun()
        {
            State = GameState.Playing;
            CurrentSpeed = 0f;
            Distance = 0f;
            Level = 1;
            Hollow = 100f;
            invulnTimer = 0f;
            volSmoothed = 0f;
            IsPlayerAirborne = false;
        }

        void Update()
        {
            if (State != GameState.Playing) return;

            var mic = MicrophoneInputManager.Instance;
            float rms = mic != null ? mic.CurrentVolume : 0f;
            float noiseFloor = mic != null ? mic.NoiseFloor : 0.01f;
            float runThreshold = mic != null ? mic.RunThreshold : 0.03f;

            volSmoothed += (rms - volSmoothed) * Mathf.Min(1f, Time.deltaTime * 10f);

            MaxSpeed = baseMaxSpeed * (1f + Mathf.Min(Level, 10) * speedLevelGrowth);
            float targetFrac = Mathf.Clamp01((volSmoothed - noiseFloor * 0.6f) / (runThreshold * 3f));
            float targetSpeed = targetFrac * MaxSpeed;
            CurrentSpeed += (targetSpeed - CurrentSpeed) * Mathf.Min(1f, Time.deltaTime * speedSmoothing);
            if (CurrentSpeed < 0.05f) CurrentSpeed = 0f;

            Distance += CurrentSpeed * metersPerWorldUnit * Time.deltaTime;
            int newLevel = 1 + Mathf.FloorToInt(Distance / metersPerLevel);
            if (newLevel != Level)
            {
                Level = newLevel;
                OnLevelChanged?.Invoke(Level);
            }

            float safeSpeed = MaxSpeed * safeSpeedFraction;
            float dev = (CurrentSpeed - safeSpeed) / safeSpeed;
            Hollow = Mathf.Clamp(Hollow + dev * hollowRecoveryRate * Time.deltaTime, 0f, 100f);

            if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;

            if (Hollow <= 0f) Catch();
        }

        /// <summary>Call from Obstacle.cs when the player's collider overlaps one.</summary>
        public void RegisterObstacleHit()
        {
            if (State != GameState.Playing) return;
            if (IsInvulnerable || IsPlayerAirborne) return;

            Hollow = Mathf.Max(0f, Hollow - collisionPenalty);
            invulnTimer = collisionInvulnDuration;
            OnObstacleHit?.Invoke();

            if (Hollow <= 0f) Catch();
        }

        private void Catch()
        {
            if (State != GameState.Playing) return;
            State = GameState.GameOver;
            OnCaught?.Invoke();
        }
    }
}
