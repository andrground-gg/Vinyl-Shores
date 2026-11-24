using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using VinylShores.Core;
using VinylShores.Dialogue;

namespace VinylShores.EditorTools
{
    /// <summary>
    /// Builds the standalone pause test scene. It reuses the dialogue test
    /// fixtures (player, NPC and dialogue canvas) and adds the pause prefab, so
    /// pausing can be checked while walking, idle and during an open dialogue.
    /// </summary>
    public static class PauseTestSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Prototypes/PauseTest.unity";

        private const string NpcInstanceName = "NPC_OldSpin";
        private const string DialogueCanvasInstanceName = "DialogueCanvas";
        private const string PauseCanvasInstanceName = "PauseCanvas";

        [MenuItem("Tools/Vinyl Shores/Build Pause Test Scene")]
        public static void BuildPauseTestScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play mode before building the pause test scene.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !Application.isBatchMode)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Pause Test Scene",
                    ScenePath + " already exists. Overwrite it?",
                    "Overwrite",
                    "Cancel");
                if (!overwrite)
                {
                    return;
                }
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            PausePrefabBuilder.BuildPausePrefab();
            DialoguePrefabBuilder.EnsureDialoguePrefabs();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PausePrefabBuilder.CanvasPrefabPath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(DialoguePrefabBuilder.NpcPrefabPath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(DialoguePrefabBuilder.CanvasPrefabPath) == null)
            {
                Debug.LogError("Could not create the required prefabs. Scene generation aborted.");
                return;
            }

            Sprite playerSprite = DialogueArtLibrary.LoadSpriteFrame(
                DialogueArtLibrary.PlayerSpritePath,
                DialogueArtLibrary.PlayerSpriteFrame);
            if (playerSprite == null)
            {
                Debug.LogWarning("Player sprite not found. Using the placeholder sprite.");
                playerSprite = DialogueArtLibrary.GetOrCreatePlaceholderSprite(
                    DialogueArtLibrary.PlayerPlaceholderPath,
                    new Color(0.20f, 0.55f, 1.00f, 1f),
                    new Color(0.06f, 0.18f, 0.42f, 1f));
            }

            if (playerSprite == null)
            {
                Debug.LogError("Could not create the player sprite. Scene generation aborted.");
                return;
            }

            CreateCamera();
            CreateGlobalLight();
            CreatePlayer(playerSprite);

            GameObject npc = InstantiatePrefab(
                DialoguePrefabBuilder.NpcPrefabPath,
                NpcInstanceName,
                new Vector3(2f, 0f, 0f));
            InstantiatePrefab(
                DialoguePrefabBuilder.CanvasPrefabPath,
                DialogueCanvasInstanceName,
                Vector3.zero);
            InstantiatePrefab(
                PausePrefabBuilder.CanvasPrefabPath,
                PauseCanvasInstanceName,
                Vector3.zero);

            if (npc == null)
            {
                Debug.LogError("Could not instantiate the NPC prefab. Scene generation aborted.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("Could not save " + ScenePath + ".");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Pause test scene created at " + ScenePath + ".");
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.09f, 0.12f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            cameraObject.AddComponent<AudioListener>();
        }

        private static void CreateGlobalLight()
        {
            GameObject lightObject = new GameObject("Global Light 2D");
            Light2D light2D = lightObject.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = Color.white;
            light2D.intensity = 1f;
        }

        private static void CreatePlayer(Sprite sprite)
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(-3f, 0f, 0f);

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
            collider.direction = CapsuleDirection2D.Vertical;
            collider.size = new Vector2(0.5f, 0.5f);
            collider.offset = new Vector2(0f, 0.25f);

            DialogueArtLibrary.CreateVisualChild(player, sprite, 2);

            player.AddComponent<PlayerMovement>();
            player.AddComponent<PlayerMovementLock>();
        }

        private static GameObject InstantiatePrefab(string prefabPath, string instanceName, Vector3 position)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("Could not load prefab at " + prefabPath + ".");
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                Debug.LogError("Could not instantiate prefab at " + prefabPath + ".");
                return null;
            }

            instance.name = instanceName;
            instance.transform.position = position;
            return instance;
        }
    }
}
