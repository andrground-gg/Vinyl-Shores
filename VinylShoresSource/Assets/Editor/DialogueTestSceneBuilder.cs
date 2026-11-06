using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VinylShores.Dialogue;

namespace VinylShores.EditorTools
{
    /// <summary>
    /// Builds the standalone dialogue test scene from the Unity editor. Keeping
    /// scene creation here lets Unity generate the scene YAML and metas.
    /// </summary>
    public static class DialogueTestSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/DialogueTest.unity";
        private const string PlaceholderFolder = "Assets/Art/Placeholders";
        private const string PlayerSpritePath = PlaceholderFolder + "/PlayerPlaceholder.png";
        private const string NpcSpritePath = PlaceholderFolder + "/NpcPlaceholder.png";

        private const string PanelObjectName = "DialoguePanel";
        private const string SpeakerObjectName = "SpeakerText";
        private const string BodyObjectName = "BodyText";
        private const string HintObjectName = "HintText";
        private const string PromptObjectName = "DialoguePrompt";

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
                bool overwrite = EditorUtility.DisplayDialog(
                    "Dialogue Test Scene",
                    ScenePath + " already exists. Overwrite it?",
                    "Overwrite",
                    "Cancel");

                if (!overwrite)
                {
                    return;
                }
            }

            EnsureFolder("Assets/Art");
            EnsureFolder(PlaceholderFolder);

            Sprite playerSprite = GetOrCreatePlaceholderSprite(
                PlayerSpritePath,
                new Color(0.20f, 0.55f, 1.00f, 1f),
                new Color(0.06f, 0.18f, 0.42f, 1f));

            Sprite npcSprite = GetOrCreatePlaceholderSprite(
                NpcSpritePath,
                new Color(0.95f, 0.55f, 0.20f, 1f),
                new Color(0.38f, 0.18f, 0.05f, 1f));

            if (playerSprite == null || npcSprite == null)
            {
                Debug.LogError("Could not create the placeholder sprites. Scene generation aborted.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateGlobalLight();
            GameObject player = CreatePlayer(playerSprite);

            DialogueUI dialogueUI;
            CreateDialogueCanvas(out dialogueUI);

            GameObject npc = CreateNpc(npcSprite, player.transform, dialogueUI);

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

            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 1;

            player.AddComponent<DialogueTestPlayerMover>();
            return player;
        }

        private static GameObject CreateDialogueCanvas(out DialogueUI dialogueUI)
        {
            GameObject canvasObject = new GameObject(
                "DialogueCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.layer = UiLayer;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            dialogueUI = canvasObject.AddComponent<DialogueUI>();
            dialogueUI.BuildUIIfNeeded();

            Transform panel = canvasObject.transform.Find(PanelObjectName);
            SerializedObject serializedUI = new SerializedObject(dialogueUI);

            if (panel != null)
            {
                SetObjectReference(serializedUI, "panel", panel.gameObject);
                SetObjectReference(serializedUI, "speakerText", FindText(panel, SpeakerObjectName));
                SetObjectReference(serializedUI, "bodyText", FindText(panel, BodyObjectName));
                SetObjectReference(serializedUI, "hintText", FindText(panel, HintObjectName));
            }
            else
            {
                Debug.LogError("DialogueUI did not create its " + PanelObjectName + " object.");
            }

            Transform prompt = canvasObject.transform.Find(PromptObjectName);
            if (prompt != null)
            {
                SetObjectReference(serializedUI, "promptRoot", prompt.gameObject);
                SetObjectReference(serializedUI, "promptText", prompt.GetComponentInChildren<Text>(true));
            }
            else
            {
                Debug.LogError("DialogueUI did not create its " + PromptObjectName + " object.");
            }

            serializedUI.ApplyModifiedPropertiesWithoutUndo();
            return canvasObject;
        }

        private static GameObject CreateNpc(Sprite sprite, Transform player, DialogueUI dialogueUI)
        {
            GameObject npc = new GameObject("NPC_OldSpin");
            npc.transform.position = new Vector3(2f, 0f, 0f);

            SpriteRenderer renderer = npc.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 1;

            DialogueInteractable interactable = npc.AddComponent<DialogueInteractable>();

            SerializedObject serializedNpc = new SerializedObject(interactable);
            serializedNpc.FindProperty("interactionRadius").floatValue = 1.5f;
            serializedNpc.FindProperty("player").objectReferenceValue = player;
            serializedNpc.FindProperty("ui").objectReferenceValue = dialogueUI;

            SerializedProperty lines = serializedNpc.FindProperty("lines");
            lines.arraySize = 3;
            SetLine(lines.GetArrayElementAtIndex(0), "Old Spin", "The sea keeps its own tempo, kid.");
            SetLine(lines.GetArrayElementAtIndex(1), "Old Spin", "Find the record by the dock, and we'll see if you can keep up.");
            SetLine(lines.GetArrayElementAtIndex(2), "Old Spin", "Come back when you're ready.");

            serializedNpc.ApplyModifiedPropertiesWithoutUndo();
            return npc;
        }

        private static void SetLine(SerializedProperty line, string speaker, string text)
        {
            line.FindPropertyRelative("speaker").stringValue = speaker;
            line.FindPropertyRelative("text").stringValue = text;
        }

        private static Text FindText(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static void SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning("Serialized property '" + propertyName + "' was not found.");
                return;
            }

            property.objectReferenceValue = value;
        }

        private static Sprite GetOrCreatePlaceholderSprite(string path, Color fill, Color border)
        {
            string absolutePath = ToAbsolutePath(path);
            if (!File.Exists(absolutePath))
            {
                WritePlaceholderTexture(absolutePath, fill, border);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }

            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            else
            {
                Debug.LogError("Could not configure the placeholder sprite at " + path + ".");
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogError("Could not load the placeholder sprite at " + path + ".");
            }

            return sprite;
        }

        private static void WritePlaceholderTexture(string absolutePath, Color fill, Color border)
        {
            const int size = 16;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    bool isEye = (x == 4 || x == 5 || x == 10 || x == 11) && (y == 8 || y == 9);
                    pixels[(y * size) + x] = isBorder ? border : (isEye ? Color.white : fill);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);

            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(absolutePath, png);
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath);
            string folderName = Path.GetFileName(folderPath);

            if (!string.IsNullOrEmpty(parent))
            {
                parent = parent.Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    EnsureFolder(parent);
                }
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static int UiLayer
        {
            get
            {
                int layer = LayerMask.NameToLayer("UI");
                return layer >= 0 ? layer : 5;
            }
        }
    }
}
