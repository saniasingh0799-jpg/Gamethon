using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VoiceRunner.Editor
{
    public static class VoiceRunnerSceneBuilder
    {
        private const string RootPath = "Assets/VoiceRunnerUnity/VoiceRunnerUnity/Assets/VoiceRunner";
        private const string PrefabPath = RootPath + "/Prefabs";
        private const string ScenePath = RootPath + "/Scenes/OutrunTheHollow.unity";

        [MenuItem("Tools/Voice Runner/Create Demo Scene")]
        public static void CreateDemoScene()
        {
            if (!EditorUtility.DisplayDialog("Create Outrun the Hollow", "Create the complete demo scene and obstacle prefabs? Existing assets at these paths will be replaced.", "Create", "Cancel"))
                return;

            EnsureFolder(RootPath + "/Prefabs");
            EnsureFolder(RootPath + "/Scenes");
            AssetDatabase.Refresh();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateManagers();
            GameObject world = new GameObject("World");
            CreateWorld(world.transform);
            GameObject player = CreatePlayer(world.transform);
            GameObject spawner = CreateSpawner(world.transform);
            CreateCanvas(player, spawner.GetComponent<ObstacleSpawner>());
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            EditorUtility.DisplayDialog("Scene created", "Open OutrunTheHollow.unity and press Play. The obstacle pool uses generated square sprites, so no art is required.", "Done");
        }

        private static void CreateCamera()
        {
            GameObject camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            Camera component = camera.GetComponent<Camera>();
            component.orthographic = true;
            component.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 1.5f, -10f);
        }

        private static void CreateManagers()
        {
            GameObject managers = new GameObject("Managers");
            new GameObject("GameManager", typeof(GameManager)).transform.SetParent(managers.transform);
            new GameObject("MicrophoneInputManager", typeof(MicrophoneInputManager)).transform.SetParent(managers.transform);
            new GameObject("KeyboardFallbackInput", typeof(KeyboardFallbackInput)).transform.SetParent(managers.transform);
        }

        private static void CreateWorld(Transform parent)
        {
            CreateBlock("Ground", parent, new Vector3(0f, -2.5f, 1f), new Vector2(30f, 1f), new Color(0.08f, 0.12f, 0.16f));
            SetParallax(CreateBlock("HillsFar", parent, new Vector3(0f, 0.2f, 5f), new Vector2(30f, 4f), new Color(0.13f, 0.19f, 0.24f), typeof(ParallaxLayer)), 0.2f);
            SetParallax(CreateBlock("HillsNear", parent, new Vector3(0f, -0.4f, 4f), new Vector2(30f, 3f), new Color(0.10f, 0.16f, 0.19f), typeof(ParallaxLayer)), 0.4f);

            GameObject hollow = new GameObject("HollowFog");
            hollow.transform.SetParent(parent);
            hollow.transform.position = new Vector3(-10f, 0f, 2f);
            GameObject fog = CreateBlock("FogVisual", hollow.transform, Vector3.zero, new Vector2(10f, 6f), new Color(0.18f, 0.03f, 0.08f, 0.8f));
            HollowChaser chaser = hollow.AddComponent<HollowChaser>();
            SerializedObject serialized = new SerializedObject(chaser);
            serialized.FindProperty("fogVisual").objectReferenceValue = fog.transform;
            serialized.FindProperty("fogRenderer").objectReferenceValue = fog.GetComponent<SpriteRenderer>();
            serialized.FindProperty("playerX").floatValue = -3.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreatePlayer(Transform parent)
        {
            GameObject player = CreateBlock("Player", parent, new Vector3(-3.5f, -1.35f, 0f), new Vector2(0.9f, 1.5f), new Color(0.95f, 0.78f, 0.24f), typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(PlayerRunner));
            player.tag = "Player";
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts = true;
            return player;
        }

        private static GameObject CreateSpawner(Transform parent)
        {
            GameObject spawnerObject = new GameObject("ObstacleSpawner", typeof(ObstacleSpawner));
            spawnerObject.transform.SetParent(parent);
            GameObject point = new GameObject("ObstacleSpawnPoint");
            point.transform.SetParent(parent);
            point.transform.position = new Vector3(11f, -1.4f, 0f);
            ObstacleSpawner spawner = spawnerObject.GetComponent<ObstacleSpawner>();
            SerializedObject serialized = new SerializedObject(spawner);
            serialized.FindProperty("spawnPoint").objectReferenceValue = point.transform;
            serialized.FindProperty("obstaclePool").arraySize = 3;
            string[] names = { "Spike", "DoubleSpike", "Wall" };
            int[] levels = { 1, 2, 4 };
            for (int i = 0; i < names.Length; i++)
            {
                GameObject prefab = CreateObstaclePrefab(names[i], i == 2 ? new Vector2(1.3f, 2.4f) : new Vector2(i == 1 ? 1.5f : 0.8f, i == 1 ? 0.8f : 0.65f), i == 2 ? new Color(0.75f, 0.12f, 0.16f) : new Color(0.95f, 0.45f, 0.12f));
                SerializedProperty entry = serialized.FindProperty("obstaclePool").GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                entry.FindPropertyRelative("unlockLevel").intValue = levels[i];
                entry.FindPropertyRelative("weight").floatValue = i == 2 ? 0.7f : 1f;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return spawnerObject;
        }

        private static GameObject CreateObstaclePrefab(string name, Vector2 size, Color color)
        {
            GameObject obstacle = CreateBlock(name, null, new Vector3(0f, -1.3f + size.y / 2f, 0f), size, color, typeof(BoxCollider2D), typeof(Obstacle));
            obstacle.GetComponent<BoxCollider2D>().isTrigger = true;
            string path = PrefabPath + "/" + name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obstacle, path);
            Object.DestroyImmediate(obstacle);
            return prefab;
        }

        private static void CreateCanvas(GameObject player, ObstacleSpawner spawner)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);

            GameObject start = CreatePanel("StartScreen", canvasObject.transform, true);
            CreateText("Title", start.transform, "OUTRUN THE HOLLOW", 64, new Vector2(0f, 220f));
            Button mic = CreateButton("MicButton", start.transform, "Start with microphone", new Vector2(0f, 40f));
            Button noMic = CreateButton("NoMicButton", start.transform, "Play without a mic", new Vector2(0f, -60f));
            CreateText("MicStatusText", start.transform, "Voice controls are optional", 24, new Vector2(0f, -180f));

            GameObject calibrating = CreatePanel("CalibratingScreen", canvasObject.transform, false);
            CreateText("CalibratingText", calibrating.transform, "Stay quiet a second...", 42, Vector2.zero);
            GameObject hud = CreatePanel("HUD", canvasObject.transform, false);
            CreateText("ScoreText", hud.transform, "0", 40, new Vector2(-780f, 460f));
            Image meter = CreateMeter(hud.transform);
            GameObject toast = CreatePanel("LevelToast", hud.transform, true);
            CanvasGroup toastGroup = toast.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            CreateText("LevelToastText", toast.transform, "Level 1", 36, new Vector2(0f, 380f));
            GameObject fallback = CreatePanel("FallbackControls", canvasObject.transform, false);
            CreateButton("HoldToTalkButton", fallback.transform, "Hold to run", new Vector2(-300f, -420f)).gameObject.AddComponent<HoldToTalkButton>();
            Button shout = CreateButton("ShoutButton", fallback.transform, "Shout / Jump", new Vector2(300f, -420f));
            GameObject over = CreatePanel("GameOverScreen", canvasObject.transform, false);
            CreateText("FinalScoreText", over.transform, "0", 54, new Vector2(0f, 80f));
            CreateText("FinalLevelText", over.transform, "1", 34, new Vector2(0f, 0f));
            Button retry = CreateButton("RetryButton", over.transform, "Run again", new Vector2(0f, -120f));

            GameObject flow = new GameObject("UIManager", typeof(UIManager));
            SerializedObject serialized = new SerializedObject(flow.GetComponent<UIManager>());
            Assign(serialized, "startScreen", start); Assign(serialized, "calibratingScreen", calibrating); Assign(serialized, "hud", hud); Assign(serialized, "gameOverScreen", over); Assign(serialized, "fallbackControls", fallback);
            Assign(serialized, "micButton", mic); Assign(serialized, "noMicButton", noMic); Assign(serialized, "micStatusText", start.transform.Find("MicStatusText").GetComponent<TMP_Text>());
            Assign(serialized, "scoreText", hud.transform.Find("ScoreText").GetComponent<TMP_Text>()); Assign(serialized, "volumeMeterFill", meter); Assign(serialized, "levelToastText", toast.transform.Find("LevelToastText").GetComponent<TMP_Text>()); Assign(serialized, "levelToastGroup", toastGroup);
            Assign(serialized, "finalScoreText", over.transform.Find("FinalScoreText").GetComponent<TMP_Text>()); Assign(serialized, "finalLevelText", over.transform.Find("FinalLevelText").GetComponent<TMP_Text>()); Assign(serialized, "retryButton", retry); Assign(serialized, "shoutButton", shout); Assign(serialized, "obstacleSpawner", spawner);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateEventSystem() => new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        private static GameObject CreatePanel(string name, Transform parent, bool active)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false); panel.SetActive(active); panel.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.08f, 0.88f);
            RectTransform rect = panel.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; return panel;
        }
        private static TMP_Text CreateText(string name, Transform parent, string text, int size, Vector2 position)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false); TMP_Text label = go.GetComponent<TMP_Text>(); label.text = text; label.fontSize = size; label.alignment = TextAlignmentOptions.Center; label.color = Color.white; RectTransform rect = go.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(900f, 100f); rect.anchoredPosition = position; return label;
        }
        private static Button CreateButton(string name, Transform parent, string text, Vector2 position)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false); go.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.16f, 1f); RectTransform rect = go.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(460f, 76f); rect.anchoredPosition = position; CreateText("Label", go.transform, text, 28, Vector2.zero); return go.GetComponent<Button>();
        }
        private static Image CreateMeter(Transform parent) { GameObject go = new GameObject("VolumeMeter", typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false); Image image = go.GetComponent<Image>(); image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.color = new Color(0.2f, 0.85f, 0.45f); RectTransform rect = go.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(500f, 28f); rect.anchoredPosition = new Vector2(0f, 460f); return image; }
        private static GameObject CreateBlock(string name, Transform parent, Vector3 position, Vector2 size, Color color, params System.Type[] extra)
        {
            GameObject go = new GameObject(name, typeof(SpriteRenderer)); if (parent != null) go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = new Vector3(size.x, size.y, 1f); SpriteRenderer renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); renderer.color = color; foreach (System.Type type in extra) go.AddComponent(type); return go;
        }
        private static void Assign(SerializedObject serialized, string property, Object value) { serialized.FindProperty(property).objectReferenceValue = value; }
        private static void SetParallax(GameObject layer, float speed) { SerializedObject serialized = new SerializedObject(layer.GetComponent<ParallaxLayer>()); serialized.FindProperty("speedMultiplier").floatValue = speed; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        private static void EnsureFolder(string path) { string[] parts = path.Split('/'); string current = parts[0]; for (int i = 1; i < parts.Length; i++) { string next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; } }
    }
}