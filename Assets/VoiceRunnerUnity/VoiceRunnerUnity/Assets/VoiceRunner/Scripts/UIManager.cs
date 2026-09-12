using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VoiceRunner
{
    /// <summary>
    /// Wires up the four screens (start, calibrating, HUD, game over) to GameManager and
    /// MicrophoneInputManager. Assign every field in the Inspector — see the setup guide
    /// in README.md for the exact Canvas hierarchy this expects.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private GameObject startScreen;
        [SerializeField] private GameObject calibratingScreen;
        [SerializeField] private GameObject hud;
        [SerializeField] private GameObject gameOverScreen;
        [SerializeField] private GameObject fallbackControls;

        [Header("Start Screen")]
        [SerializeField] private Button micButton;
        [SerializeField] private Button noMicButton;
        [SerializeField] private TMP_Text micStatusText;

        [Header("HUD")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Image volumeMeterFill;
        [SerializeField] private TMP_Text levelToastText;
        [SerializeField] private CanvasGroup levelToastGroup;

        [Header("Game Over")]
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text finalLevelText;
        [SerializeField] private Button retryButton;

        [Header("Fallback Controls")]
        [SerializeField] private Button shoutButton; // "hold to talk" uses HoldToTalkButton.cs instead

        [Header("Level")]
        [SerializeField] private ObstacleSpawner obstacleSpawner;

        private Coroutine toastRoutine;

        void Awake()
        {
            micButton.onClick.AddListener(OnMicButton);
            noMicButton.onClick.AddListener(OnNoMicButton);
            retryButton.onClick.AddListener(OnRetry);
            if (shoutButton != null)
                shoutButton.onClick.AddListener(() => MicrophoneInputManager.Instance.QueueManualSpike());

            GameManager.Instance.OnLevelChanged += ShowLevelToast;
            GameManager.Instance.OnCaught += ShowGameOver;

            hud.SetActive(false);
            calibratingScreen.SetActive(false);
            gameOverScreen.SetActive(false);
            fallbackControls.SetActive(false);
            if (levelToastGroup != null) levelToastGroup.alpha = 0f;
        }

        private void OnMicButton()
        {
            micButton.interactable = false;
            micStatusText.text = "Requesting microphone…";
            StartCoroutine(SetupMicThenPlay());
        }

        private IEnumerator SetupMicThenPlay()
        {
            yield return StartCoroutine(MicrophoneInputManager.Instance.InitializeMicrophone());

            if (!MicrophoneInputManager.Instance.MicAvailable)
            {
                micStatusText.text = "Couldn't access the mic — check your permissions, or play without one below.";
                fallbackControls.SetActive(true);
                micButton.interactable = true;
                yield break;
            }

            startScreen.SetActive(false);
            calibratingScreen.SetActive(true);
            GameManager.Instance.SetCalibrating();
            yield return StartCoroutine(MicrophoneInputManager.Instance.Calibrate(BeginPlay));
        }

        private void OnNoMicButton()
        {
            fallbackControls.SetActive(true);
            startScreen.SetActive(false);
            BeginPlay();
        }

        private void BeginPlay()
        {
            calibratingScreen.SetActive(false);
            gameOverScreen.SetActive(false);
            hud.SetActive(true);
            GameManager.Instance.BeginRun();
        }

        private void OnRetry()
        {
            if (obstacleSpawner != null) obstacleSpawner.ClearAll();
            BeginPlay();
        }

        private void ShowLevelToast(int level)
        {
            if (levelToastText == null || levelToastGroup == null) return;
            levelToastText.text = "Level " + level;
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine());
        }

        private IEnumerator ToastRoutine()
        {
            levelToastGroup.alpha = 1f;
            yield return new WaitForSeconds(1.6f);
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                levelToastGroup.alpha = 1f - t / 0.4f;
                yield return null;
            }
            levelToastGroup.alpha = 0f;
        }

        private void ShowGameOver()
        {
            finalScoreText.text = Mathf.FloorToInt(GameManager.Instance.Distance).ToString();
            finalLevelText.text = GameManager.Instance.Level.ToString();
            gameOverScreen.SetActive(true);
        }

        void Update()
        {
            if (GameManager.Instance.State != GameState.Playing) return;

            if (scoreText != null)
                scoreText.text = Mathf.FloorToInt(GameManager.Instance.Distance).ToString();

            if (volumeMeterFill != null && MicrophoneInputManager.Instance != null)
                volumeMeterFill.fillAmount = Mathf.Clamp01(MicrophoneInputManager.Instance.CurrentVolume * 6f);
        }
    }
}
