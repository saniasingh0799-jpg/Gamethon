using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoiceRunner
{
    /// <summary>
    /// Wraps Unity's built-in Microphone API, exposes a smoothed volume value each frame,
    /// auto-calibrates against the player's ambient noise floor, and raises OnSpike when
    /// a sudden loud burst (shout/clap) is detected. Also supports a manual fallback
    /// (hold button / keyboard) for devices or environments where mic access isn't available.
    /// One instance should live in the scene for the whole session (DontDestroyOnLoad-style singleton).
    /// </summary>
    public class MicrophoneInputManager : MonoBehaviour
    {
        public static MicrophoneInputManager Instance { get; private set; }

        [Header("Mic capture")]
        [SerializeField] private int sampleWindow = 1024;
        [SerializeField] private int recordSeconds = 10;

        [Header("Spike detection")]
        [SerializeField] private float spikeDelta = 0.07f;
        [SerializeField] private float spikeCooldown = 0.32f;
        [SerializeField] private int historyLength = 10;

        public float CurrentVolume { get; private set; }
        public bool MicAvailable { get; private set; }
        public bool IsCalibrated { get; private set; }
        public float NoiseFloor { get; private set; } = 0.01f;
        public float RunThreshold { get; private set; } = 0.03f;
        public float SpikeAbsoluteMin { get; private set; } = 0.07f;

        /// <summary>Fired the frame a shout/clap/spike is detected (from mic or fallback).</summary>
        public event System.Action OnSpike;

        // Fallback input, used when there's no mic or the user opts out of it.
        public bool ManualHold { get; set; }
        public void QueueManualSpike() => manualSpikeQueued = true;
        private bool manualSpikeQueued;

        private string deviceName;
        private AudioClip micClip;
        private float[] volHistory;
        private int volHistoryIndex;
        private int volHistoryCount;
        private float lastSpikeTime = -999f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            volHistory = new float[Mathf.Max(2, historyLength)];
        }

        /// <summary>
        /// Requests mic permission (mobile) and starts the microphone. Safe to call once at
        /// game start. Check MicAvailable afterwards — if false, fall back to manual controls.
        /// </summary>
        public IEnumerator InitializeMicrophone()
        {
            MicAvailable = false;

#if PLATFORM_ANDROID || PLATFORM_IOS
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
            }
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                yield break;
            }
#endif

            if (Microphone.devices.Length == 0)
                yield break;

            deviceName = Microphone.devices[0];
            micClip = Microphone.Start(deviceName, true, recordSeconds, AudioSettings.outputSampleRate);

            float t0 = Time.realtimeSinceStartup;
            while (Microphone.GetPosition(deviceName) <= 0)
            {
                if (Time.realtimeSinceStartup - t0 > 3f) yield break; // mic never came online
                yield return null;
            }

            MicAvailable = true;
        }

        /// <summary>
        /// Samples ambient noise for ~1.1s to set thresholds relative to the room/device,
        /// since raw mic sensitivity varies a lot. Calls onDone when finished (even if no mic).
        /// </summary>
        public IEnumerator Calibrate(System.Action onDone)
        {
            if (!MicAvailable)
            {
                IsCalibrated = true;
                onDone?.Invoke();
                yield break;
            }

            var samples = new List<float>();
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 1.1f)
            {
                samples.Add(SampleRawVolume());
                yield return null;
            }

            samples.Sort();
            NoiseFloor = samples.Count > 0 ? samples[samples.Count / 2] : 0.01f;
            RunThreshold = Mathf.Max(NoiseFloor + 0.018f, 0.028f);
            SpikeAbsoluteMin = Mathf.Max(NoiseFloor + 0.05f, 0.065f);
            IsCalibrated = true;
            onDone?.Invoke();
        }

        private float SampleRawVolume()
        {
            if (!MicAvailable || micClip == null) return 0f;

            int micPos = Microphone.GetPosition(deviceName) - sampleWindow;
            if (micPos < 0) return 0f;

            var samples = new float[sampleWindow];
            micClip.GetData(samples, micPos);

            float sumSq = 0f;
            for (int i = 0; i < samples.Length; i++) sumSq += samples[i] * samples[i];
            return Mathf.Sqrt(sumSq / samples.Length);
        }

        void Update()
        {
            float rms = SampleRawVolume();
            if (ManualHold) rms = Mathf.Max(rms, 0.09f);

            float prevAvg = volHistoryCount > 0 ? HistoryAverage() : rms;
            PushHistory(rms);

            bool spiked = false;
            if (manualSpikeQueued)
            {
                spiked = true;
                manualSpikeQueued = false;
            }
            else if (MicAvailable && rms > SpikeAbsoluteMin && (rms - prevAvg) > spikeDelta
                     && Time.time - lastSpikeTime > spikeCooldown)
            {
                spiked = true;
            }

            if (spiked)
            {
                lastSpikeTime = Time.time;
                OnSpike?.Invoke();
            }

            CurrentVolume = rms;
        }

        private void PushHistory(float v)
        {
            volHistory[volHistoryIndex] = v;
            volHistoryIndex = (volHistoryIndex + 1) % volHistory.Length;
            volHistoryCount = Mathf.Min(volHistoryCount + 1, volHistory.Length);
        }

        private float HistoryAverage()
        {
            float sum = 0f;
            for (int i = 0; i < volHistoryCount; i++) sum += volHistory[i];
            return volHistoryCount > 0 ? sum / volHistoryCount : 0f;
        }

        void OnDestroy()
        {
            if (MicAvailable && !string.IsNullOrEmpty(deviceName) && Microphone.IsRecording(deviceName))
                Microphone.End(deviceName);
        }
    }
}
