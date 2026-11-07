using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using VinylShores.Dialogue;

namespace VinylShores.EditorTools
{
    /// <summary>
    /// Builds the standalone dialogue test scene from the Unity editor. The
    /// NPC and UI are instantiated from the reusable dialogue prefabs, which
    /// also exercises the runtime reference resolution used in game scenes.
    /// </summary>
    public static class DialogueTestSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/DialogueTest.unity";

        private const string NpcInstanceName = "NPC_OldSpin";
        private const string SecondNpcInstanceName = "NPC_DockWorker";
        private const string CanvasInstanceName = "DialogueCanvas";

        [MenuItem("Tools/Vinyl Shores/Build Dialogue Test Scene")]
        public static void BuildDialogueTestScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play mode before building the dialogue test scene.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                bool overwrite = Application.isBatchMode ||
                    EditorUtility.DisplayDialog(
                        "Dialogue Test Scene",
                        ScenePath + " already exists. Overwrite it?",
                        "Overwrite",
                        "Cancel");

                if (!overwrite)
                {
                    return;
                }
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            DialoguePrefabBuilder.BuildDialoguePrefabs();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(DialoguePrefabBuilder.NpcPrefabPath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(DialoguePrefabBuilder.CanvasPrefabPath) == null)
            {
                Debug.LogError("Could not create the dialogue prefabs. Scene generation aborted.");
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
            GameObject secondNpc = InstantiatePrefab(
                DialoguePrefabBuilder.NpcPrefabPath,
                SecondNpcInstanceName,
                new Vector3(4f, 0f, 0f));
            InstantiatePrefab(
                DialoguePrefabBuilder.CanvasPrefabPath,
                CanvasInstanceName,
                Vector3.zero);

            if (npc == null || secondNpc == null)
            {
                Debug.LogError("Could not instantiate the NPC prefabs. Scene generation aborted.");
                return;
            }

            ConfigureSecondNpc(secondNpc);
            AttachEventLogger(npc);
            AttachEventLogger(secondNpc);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("Could not save " + ScenePath + ".");
                return;
            }

            AssetDatabase.SaveAssets();

            Selection.activeGameObject = npc;
            EditorGUIUtility.PingObject(npc);
            Debug.Log("Dialogue test scene created at " + ScenePath + ".");
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

        private static GameObject CreatePlayer(Sprite sprite)
        {
            GameObject player = new GameObject("Player");
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
            player.AddComponent<DialogueTestPlayerMover>();
            return player;
        }

        private static void ConfigureSecondNpc(GameObject npc)
        {
            DialogueInteractable interactable = npc.GetComponent<DialogueInteractable>();
            if (interactable == null)
            {
                Debug.LogError("The second NPC is missing DialogueInteractable.", npc);
                return;
            }

            SerializedObject serializedNpc = new SerializedObject(interactable);
            SerializedProperty lines = serializedNpc.FindProperty("lines");
            lines.arraySize = 2;
            DialogueArtLibrary.SetLine(
                lines.GetArrayElementAtIndex(0),
                "Dock Worker",
                "The dock's closed until the tide turns.",
                null);
            DialogueArtLibrary.SetLine(
                lines.GetArrayElementAtIndex(1),
                "Dock Worker",
                "Come back later, kid.",
                null);
            serializedNpc.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.RecordPrefabInstancePropertyModifications(interactable);
        }

        private static void AttachEventLogger(GameObject npc)
        {
            DialogueInteractable interactable = npc.GetComponent<DialogueInteractable>();
            if (interactable == null)
            {
                return;
            }

            DialogueTestEventLogger logger = npc.AddComponent<DialogueTestEventLogger>();
            UnityEventTools.AddPersistentListener(
                interactable.onDialogueFinished,
                logger.LogFinished);
            PrefabUtility.RecordPrefabInstancePropertyModifications(interactable);
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
