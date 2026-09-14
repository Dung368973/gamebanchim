#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ShootEmUp.Editor
{
    /// <summary>
    /// Milestone 5 Scene Integration Helper.
    /// Automates complete assembly of SampleScene.unity with all game systems,
    /// managers, player, enemies, parallax background, and mobile UI canvas.
    /// Supports both interactive Editor menu item and headless CLI batchmode execution.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneSetupHelper
    {
        public const string ScenePath = "Assets/Scenes/SampleScene.unity";
        public const string PrefabDir = "Assets/Prefabs";
        public const string SpritesDir = "Assets/Sprites";

        [MenuItem("Tools/Build Main Scene")]
        public static void BuildMainSceneMenu()
        {
            BuildMainScene();
        }

        /// <summary>
        /// Assembles, wires, and saves SampleScene.unity in headless or interactive mode.
        /// </summary>
        public static void BuildMainScene()
        {
            Debug.Log("<color=cyan>[SceneSetupHelper]</color> Starting complete scene assembly for SampleScene.unity...");

            EnsureDirectories();

            // 1. Ensure required sprites exist
            EnsureSprites();

            // 2. Create and save reusable prefabs
            Bullet bulletPrefab = CreateBulletPrefab();
            PowerUp powerUpPrefab = CreatePowerUpPrefab();
            BasicBird basicBirdPrefab = CreateBasicBirdPrefab();
            FastBird fastBirdPrefab = CreateFastBirdPrefab();
            TankBird tankBirdPrefab = CreateTankBirdPrefab(powerUpPrefab != null ? powerUpPrefab.gameObject : null);

            // 3. Create fresh new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 4. Setup Main Camera
            Camera mainCam = SetupMainCamera();

            // 5. Setup Core Singletons
            GameObject coreSystems = SetupCoreSingletons();

            // 6. Setup Parallax Background
            GameObject environment = SetupParallaxBackground();

            // 7. Setup Wave Spawner
            GameObject spawnerObj = SetupWaveSpawner(basicBirdPrefab, fastBirdPrefab, tankBirdPrefab);

            // 8. Setup Player
            GameObject playerObj = SetupPlayer(mainCam, bulletPrefab);

            // 9. Setup Responsive UI Canvas & HUD
            GameObject canvasObj = SetupUICanvas(mainCam);

            // 10. Setup EventSystem
            SetupEventSystem();

            // 11. Mark dirty and save scene
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (saved)
            {
                Debug.Log("<color=green>[SceneSetupHelper]</color> SampleScene.unity assembled and saved successfully at: " + ScenePath);
            }
            else
            {
                Debug.LogError("[SceneSetupHelper] Failed to save scene to: " + ScenePath);
            }
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(PrefabDir))
            {
                Directory.CreateDirectory(PrefabDir);
                AssetDatabase.Refresh();
            }
        }

        private static void EnsureSprites()
        {
            string starsPath = $"{SpritesDir}/bg_stars.png";
            string cloudsPath = $"{SpritesDir}/bg_clouds.png";
            string mountainsPath = $"{SpritesDir}/bg_mountains.png";

            if (!File.Exists(starsPath) || !File.Exists(cloudsPath) || !File.Exists(mountainsPath))
            {
                ProceduralSpriteGenerator.GenerateAllSprites();
            }
        }

        #region Camera Setup
        private static Camera SetupMainCamera()
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0f, 0f, -10f);

            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.0f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x0A, 0x0E, 0x1A, 0xFF); // #0A0E1A
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            cam.depth = -1f;

            camObj.AddComponent<AudioListener>();
            return cam;
        }
        #endregion

        #region Core Singletons Setup
        private static GameObject SetupCoreSingletons()
        {
            GameObject root = new GameObject("CoreSystems");

            // GameManager
            var gm = root.AddComponent<GameManager>();

            // ScoreManager
            var sm = root.AddComponent<ScoreManager>();

            // AudioManager with AudioSources
            var audioMgr = root.AddComponent<AudioManager>();
            AudioSource bgmSource = root.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = 0.6f;

            AudioSource sfxSource = root.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = 0.85f;

            SetSerializedField(audioMgr, "bgmSource", bgmSource);
            SetSerializedField(audioMgr, "sfxSource", sfxSource);

            // FXManager
            var fxMgr = root.AddComponent<FXManager>();

            // BoundaryCleaner Trigger Box
            var cleanerObj = new GameObject("BoundaryCleanerZone");
            cleanerObj.transform.SetParent(root.transform);
            var cleaner = cleanerObj.AddComponent<BoundaryCleaner>();
            cleaner.MinY = -8.5f;
            cleaner.MaxY = 8.5f;
            cleaner.MinX = -25.0f;
            cleaner.MaxX = 25.0f;
            SetSerializedField(cleaner, "cleanOnUpdate", false);
            SetSerializedField(cleaner, "cleanOnTriggerExit", true);

            var boxCol = cleanerObj.AddComponent<BoxCollider2D>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector2(60f, 26f);

            return root;
        }
        #endregion

        #region Parallax Background Setup
        private static GameObject SetupParallaxBackground()
        {
            GameObject envObj = new GameObject("Environment");
            var parallax = envObj.AddComponent<ParallaxBackground>();

            Sprite starsSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/bg_stars.png");
            if (starsSprite == null) starsSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/bg_sky_base.png");

            Sprite cloudsSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/bg_clouds.png");
            Sprite mountainsSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/bg_mountains.png");

            var layerStars = new ParallaxBackground.LayerConfig
            {
                name = "DistantStars",
                sprite = starsSprite,
                scrollSpeed = 0.8f,
                sortingOrder = -10,
                scale = new Vector2(8.0f, 1.25f),
                height = 10.24f * 1.25f,
                tintColor = Color.white
            };

            var layerClouds = new ParallaxBackground.LayerConfig
            {
                name = "MidClouds",
                sprite = cloudsSprite,
                scrollSpeed = 1.4f,
                sortingOrder = -5,
                scale = new Vector2(8.0f, 1.25f),
                height = 10.24f * 1.25f,
                tintColor = new Color(1f, 1f, 1f, 0.45f)
            };

            var layerMountains = new ParallaxBackground.LayerConfig
            {
                name = "NearMountains",
                sprite = mountainsSprite,
                scrollSpeed = 2.6f,
                sortingOrder = -1,
                scale = new Vector2(8.0f, 1.25f),
                height = 10.24f * 1.25f,
                tintColor = new Color(0.85f, 0.95f, 1f, 0.70f)
            };

            SetSerializedField(parallax, "layers", new ParallaxBackground.LayerConfig[] { layerStars, layerClouds, layerMountains });
            SetSerializedField(parallax, "autoInitialize", true);

            return envObj;
        }
        #endregion

        #region Wave Spawner Setup
        private static GameObject SetupWaveSpawner(BasicBird basic, FastBird fast, TankBird tank)
        {
            GameObject spawnerObj = new GameObject("WaveSpawner");
            var spawner = spawnerObj.AddComponent<WaveSpawner>();

            SetSerializedField(spawner, "basicBirdPrefab", basic);
            SetSerializedField(spawner, "fastBirdPrefab", fast);
            SetSerializedField(spawner, "tankBirdPrefab", tank);
            SetSerializedField(spawner, "autoStart", true);
            SetSerializedField(spawner, "spawnY", 6.8f);
            SetSerializedField(spawner, "spawnMinX", -8.0f);
            SetSerializedField(spawner, "spawnMaxX", 8.0f);

            return spawnerObj;
        }
        #endregion

        #region Player Setup
        private static GameObject SetupPlayer(Camera cam, Bullet bulletPrefab)
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, -3.5f, 0f);

            // SpriteRenderer
            var sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/player_ship.png");
            sr.sortingOrder = 10;

            // CircleCollider2D
            var col = player.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;

            // Rigidbody2D
            var rb = player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true;

            // PlayerHealth
            var health = player.AddComponent<PlayerHealth>();
            SetSerializedField(health, "maxHealth", 3);
            SetSerializedField(health, "currentHealth", 3);
            SetSerializedField(health, "iFrameDuration", 2.0f);
            SetSerializedField(health, "spriteRenderer", sr);
            SetSerializedField(health, "playerCollider", col);

            // PlayerController
            var controller = player.AddComponent<PlayerController>();
            SetSerializedField(controller, "targetCamera", cam);
            SetSerializedField(controller, "playerSpeed", 8.0f);
            SetSerializedField(controller, "smoothingLambda", 25.0f);
            SetSerializedField(controller, "useSmoothing", true);
            SetSerializedField(controller, "useErgonomicLift", true);
            SetSerializedField(controller, "ergonomicLift", 0.6f);
            SetSerializedField(controller, "defaultSpawnPosition", new Vector2(0f, -3.5f));

            // PlayerShooter
            var shooter = player.AddComponent<PlayerShooter>();
            SetSerializedField(shooter, "bulletPrefab", bulletPrefab);
            SetSerializedField(shooter, "bulletSpeed", 14.0f);
            SetSerializedField(shooter, "baseFireInterval", 0.20f);
            SetSerializedField(shooter, "autoFireEnabled", true);
            SetSerializedField(shooter, "defaultMuzzleOffset", new Vector2(0f, 0.65f));

            return player;
        }
        #endregion

        #region UI Canvas Setup
        private static GameObject SetupUICanvas(Camera cam)
        {
            GameObject canvasObj = new GameObject("UICanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 100;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 1. Safe Area Container
            GameObject safeAreaObj = new GameObject("SafeAreaContainer");
            safeAreaObj.transform.SetParent(canvasObj.transform, false);
            RectTransform safeRect = safeAreaObj.AddComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;

            var safeHandler = safeAreaObj.AddComponent<SafeAreaHandler>();
            SetSerializedField(safeHandler, "targetRect", safeRect);

            // 2. HUD Controller
            var hud = safeAreaObj.AddComponent<HUDController>();

            // Top Header: Score & Wave
            GameObject topBar = CreateUIObject("TopBar", safeAreaObj.transform, new Vector2(0f, 0.92f), new Vector2(1f, 1f));

            Text scoreText = CreateText("ScoreText", topBar.transform, "SCORE: 0", 34, TextAnchor.MiddleLeft, new Vector2(0.04f, 0.5f), new Vector2(0.42f, 0.98f));
            scoreText.fontStyle = FontStyle.Bold;
            scoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
            scoreText.verticalOverflow = VerticalWrapMode.Overflow;

            Text highText = CreateText("HighScoreText", topBar.transform, "BEST: 0", 24, TextAnchor.MiddleLeft, new Vector2(0.04f, 0.02f), new Vector2(0.42f, 0.5f));
            highText.fontStyle = FontStyle.Bold;
            highText.color = new Color(1f, 0.85f, 0.2f, 1f);
            highText.horizontalOverflow = HorizontalWrapMode.Overflow;
            highText.verticalOverflow = VerticalWrapMode.Overflow;

            Text waveText = CreateText("WaveText", topBar.transform, "WAVE 1", 38, TextAnchor.MiddleCenter, new Vector2(0.42f, 0.1f), new Vector2(0.76f, 0.9f));

            // Pause Button in TopBar
            GameObject pauseBtnObj = CreateUIObject("PauseButton", topBar.transform, new Vector2(0.78f, 0.15f), new Vector2(0.96f, 0.85f));
            Image pauseBtnImg = pauseBtnObj.AddComponent<Image>();
            pauseBtnImg.color = new Color(0.12f, 0.22f, 0.35f, 0.90f);
            Button pauseBtn = pauseBtnObj.AddComponent<Button>();
            Text pauseBtnText = CreateText("PauseBtnText", pauseBtnObj.transform, "❚❚", 30, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            pauseBtnText.color = new Color(0.85f, 0.95f, 1.0f, 1f);

            SetSerializedField(hud, "scoreText", scoreText);
            SetSerializedField(hud, "highScoreText", highText);
            SetSerializedField(hud, "waveText", waveText);

            // Combo Indicator
            GameObject comboRoot = CreateUIObject("ComboRoot", safeAreaObj.transform, new Vector2(0.2f, 0.82f), new Vector2(0.8f, 0.90f));
            Text comboText = CreateText("ComboText", comboRoot.transform, "2x COMBO!", 36, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            Image comboFill = CreateImage("ComboFillBar", comboRoot.transform, new Vector2(0.1f, 0f), new Vector2(0.9f, 0.15f), new Color(1f, 0.8f, 0.1f, 0.8f));
            comboFill.type = Image.Type.Filled;
            comboFill.fillMethod = Image.FillMethod.Horizontal;
            comboRoot.SetActive(false);

            SetSerializedField(hud, "comboRoot", comboRoot);
            SetSerializedField(hud, "comboText", comboText);
            SetSerializedField(hud, "comboFillBar", comboFill);
            SetSerializedField(hud, "comboScaleTarget", comboRoot.transform);

            // Bottom Bar: Health Hearts
            GameObject bottomBar = CreateUIObject("BottomBar", safeAreaObj.transform, new Vector2(0f, 0f), new Vector2(1f, 0.08f));
            Image[] hearts = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                float xMin = 0.05f + (i * 0.08f);
                float xMax = xMin + 0.07f;
                hearts[i] = CreateImage($"Heart_{i}", bottomBar.transform, new Vector2(xMin, 0.15f), new Vector2(xMax, 0.85f), new Color(1f, 0.2f, 0.3f, 1f));
            }
            SetSerializedField(hud, "heartImages", hearts);

            // Shield Gauge
            GameObject shieldRoot = CreateUIObject("ShieldRoot", bottomBar.transform, new Vector2(0.40f, 0.25f), new Vector2(0.95f, 0.75f));
            Image shieldFill = CreateImage("ShieldFillBar", shieldRoot.transform, Vector2.zero, Vector2.one, new Color(0.2f, 0.8f, 1f, 0.85f));
            shieldFill.type = Image.Type.Filled;
            shieldFill.fillMethod = Image.FillMethod.Horizontal;
            shieldRoot.SetActive(false);
            SetSerializedField(hud, "shieldRoot", shieldRoot);
            SetSerializedField(hud, "shieldFillBar", shieldFill);

            // 3. Game Over UI
            var gameOverUI = safeAreaObj.AddComponent<GameOverUI>();
            GameObject modalPanel = CreateUIObject("GameOverModal", safeAreaObj.transform, new Vector2(0.1f, 0.25f), new Vector2(0.9f, 0.75f));
            Image modalBg = modalPanel.AddComponent<Image>();
            modalBg.color = new Color(0.04f, 0.06f, 0.12f, 0.92f);

            Text titleText = CreateText("TitleText", modalPanel.transform, "GAME OVER", 54, TextAnchor.MiddleCenter, new Vector2(0f, 0.75f), new Vector2(1f, 0.95f));
            titleText.color = new Color(1f, 0.3f, 0.3f, 1f);

            Text finalScore = CreateText("FinalScoreText", modalPanel.transform, "SCORE: 0", 38, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.72f));
            Text bestScore = CreateText("HighScoreText", modalPanel.transform, "BEST: 0", 32, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.40f), new Vector2(0.9f, 0.55f));

            GameObject newRecordBadge = CreateUIObject("NewRecordBadge", modalPanel.transform, new Vector2(0.2f, 0.30f), new Vector2(0.8f, 0.38f));
            Text recordText = CreateText("RecordText", newRecordBadge.transform, "★ NEW RECORD! ★", 26, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            recordText.color = new Color(1f, 0.85f, 0.2f, 1f);
            newRecordBadge.SetActive(false);

            // Restart Button
            GameObject btnObj = CreateUIObject("RestartButton", modalPanel.transform, new Vector2(0.2f, 0.10f), new Vector2(0.8f, 0.25f));
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.15f, 0.75f, 0.45f, 1f);
            Button restartBtn = btnObj.AddComponent<Button>();
            CreateText("BtnText", btnObj.transform, "RESTART", 36, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);

            SetSerializedField(gameOverUI, "modalPanel", modalPanel);
            SetSerializedField(gameOverUI, "finalScoreText", finalScore);
            SetSerializedField(gameOverUI, "highScoreText", bestScore);
            SetSerializedField(gameOverUI, "newBestBadge", newRecordBadge);
            SetSerializedField(gameOverUI, "restartButton", restartBtn);

            modalPanel.SetActive(false);

            // 4. Pause UI
            var pauseUI = safeAreaObj.AddComponent<PauseUI>();
            GameObject pauseModal = CreateUIObject("PauseModal", safeAreaObj.transform, new Vector2(0.12f, 0.28f), new Vector2(0.88f, 0.72f));
            Image pauseModalBg = pauseModal.AddComponent<Image>();
            pauseModalBg.color = new Color(0.04f, 0.06f, 0.12f, 0.96f);

            Text pauseTitle = CreateText("PauseTitleText", pauseModal.transform, "GAME PAUSED", 50, TextAnchor.MiddleCenter, new Vector2(0f, 0.70f), new Vector2(1f, 0.92f));
            pauseTitle.color = new Color(0.3f, 0.85f, 1.0f, 1f);

            // Resume Button
            GameObject resumeBtnObj = CreateUIObject("ResumeButton", pauseModal.transform, new Vector2(0.18f, 0.42f), new Vector2(0.82f, 0.58f));
            Image resumeBtnImg = resumeBtnObj.AddComponent<Image>();
            resumeBtnImg.color = new Color(0.15f, 0.75f, 0.45f, 1f);
            Button resumeBtn = resumeBtnObj.AddComponent<Button>();
            CreateText("ResumeBtnText", resumeBtnObj.transform, "RESUME", 34, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);

            // Restart Button in Pause Modal
            GameObject pauseRestartBtnObj = CreateUIObject("PauseRestartButton", pauseModal.transform, new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.34f));
            Image pauseRestartBtnImg = pauseRestartBtnObj.AddComponent<Image>();
            pauseRestartBtnImg.color = new Color(0.85f, 0.32f, 0.25f, 1f);
            Button pauseRestartBtn = pauseRestartBtnObj.AddComponent<Button>();
            CreateText("PauseRestartBtnText", pauseRestartBtnObj.transform, "RESTART", 34, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);

            SetSerializedField(pauseUI, "pauseButton", pauseBtn);
            SetSerializedField(pauseUI, "pauseModal", pauseModal);
            SetSerializedField(pauseUI, "resumeButton", resumeBtn);
            SetSerializedField(pauseUI, "restartButton", pauseRestartBtn);
            pauseModal.SetActive(false);

            return canvasObj;
        }

        private static void SetupEventSystem()
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        #endregion

        #region Prefab Creation Helpers
        private static Bullet CreateBulletPrefab()
        {
            string path = $"{PrefabDir}/Bullet.prefab";
            GameObject obj = new GameObject("Bullet");

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/bullet_standard.png");
            sr.sortingOrder = 8;

            var col = obj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.15f;

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var bullet = obj.AddComponent<Bullet>();
            SetSerializedField(bullet, "speed", 14.0f);
            SetSerializedField(bullet, "damage", 1);
            SetSerializedField(bullet, "isPlayerBullet", true);

            var cleaner = obj.AddComponent<BoundaryCleaner>();
            cleaner.MinY = -8.5f;
            cleaner.MaxY = 8.5f;
            cleaner.MinX = -25.0f;
            cleaner.MaxX = 25.0f;

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(obj, path);
            UnityEngine.Object.DestroyImmediate(obj);
            return prefabAsset.GetComponent<Bullet>();
        }

        private static PowerUp CreatePowerUpPrefab()
        {
            string path = $"{PrefabDir}/PowerUp.prefab";
            GameObject obj = new GameObject("PowerUp");

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/powerup_spread.png");
            sr.sortingOrder = 9;

            var col = obj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var pu = obj.AddComponent<PowerUp>();
            var cleaner = obj.AddComponent<BoundaryCleaner>();
            cleaner.MinY = -8.5f;
            cleaner.MaxY = 8.5f;
            cleaner.MinX = -25.0f;
            cleaner.MaxX = 25.0f;

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(obj, path);
            UnityEngine.Object.DestroyImmediate(obj);
            return prefabAsset.GetComponent<PowerUp>();
        }

        private static BasicBird CreateBasicBirdPrefab()
        {
            string path = $"{PrefabDir}/BasicBird.prefab";
            GameObject obj = new GameObject("BasicBird");

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/bird_basic.png");
            sr.sortingOrder = 5;

            var col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.8f);

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var bird = obj.AddComponent<BasicBird>();
            var cleaner = obj.AddComponent<BoundaryCleaner>();
            cleaner.MinY = -8.5f;
            cleaner.MaxY = 8.5f;
            cleaner.MinX = -25.0f;
            cleaner.MaxX = 25.0f;

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(obj, path);
            UnityEngine.Object.DestroyImmediate(obj);
            return prefabAsset.GetComponent<BasicBird>();
        }

        private static FastBird CreateFastBirdPrefab()
        {
            string path = $"{PrefabDir}/FastBird.prefab";
            GameObject obj = new GameObject("FastBird");

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/bird_fast.png");
            sr.sortingOrder = 5;

            var col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.7f, 0.7f);

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var bird = obj.AddComponent<FastBird>();
            var cleaner = obj.AddComponent<BoundaryCleaner>();
            cleaner.MinY = -8.5f;
            cleaner.MaxY = 8.5f;
            cleaner.MinX = -25.0f;
            cleaner.MaxX = 25.0f;

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(obj, path);
            UnityEngine.Object.DestroyImmediate(obj);
            return prefabAsset.GetComponent<FastBird>();
        }

        private static TankBird CreateTankBirdPrefab(GameObject dropPrefab)
        {
            string path = $"{PrefabDir}/TankBird.prefab";
            GameObject obj = new GameObject("TankBird");

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/bird_boss.png");
            sr.sortingOrder = 6;

            var col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.4f, 1.4f);

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var bird = obj.AddComponent<TankBird>();
            SetSerializedField(bird, "powerUpDropPrefab", dropPrefab);

            var cleaner = obj.AddComponent<BoundaryCleaner>();
            cleaner.MinY = -8.5f;
            cleaner.MaxY = 8.5f;
            cleaner.MinX = -25.0f;
            cleaner.MaxX = 25.0f;

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(obj, path);
            UnityEngine.Object.DestroyImmediate(obj);
            return prefabAsset.GetComponent<TankBird>();
        }
        #endregion

        #region UI Creation Helpers
        private static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return obj;
        }

        private static Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject obj = CreateUIObject(name, parent, anchorMin, anchorMax);
            Text t = obj.AddComponent<Text>();
            t.text = content;
            t.fontSize = fontSize;
            t.alignment = alignment;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject obj = CreateUIObject(name, parent, anchorMin, anchorMax);
            Image img = obj.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static void SetSerializedField(object target, string fieldName, object value)
        {
            if (target == null) return;
            Type type = target.GetType();
            FieldInfo field = null;

            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                type = type.BaseType;
            }

            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
        #endregion
    }
}
#endif
