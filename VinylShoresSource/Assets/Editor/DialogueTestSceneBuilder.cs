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
        private const string PlayerPlaceholderPath = PlaceholderFolder + "/PlayerPlaceholder.png";
        private const string NpcPlaceholderPath = PlaceholderFolder + "/NpcPlaceholder.png";

        private const string PlayerSpritePath =
            "Assets/Sprites/NinjaAdventure/Actor/Character/Boy/SpriteSheet.png";
        private const string NpcSpritePath =
            "Assets/Sprites/NinjaAdventure/Actor/Character/OldMan/SpriteSheet.png";
        private const string NpcPortraitPath =
            "Assets/Sprites/NinjaAdventure/Actor/Character/OldMan/Faceset.png";

        private const string PlayerSpriteFrame = "SpriteSheet_0";
        private const string NpcSpriteFrame = "SpriteSheet_0";
        private const string NpcPortraitFrame = "Faceset_0";

        private const string PanelObjectName = "DialoguePanel";
        private const string PortraitObjectName = "Portrait";
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

            EnsureFolder("Assets/Art");
            EnsureFolder(PlaceholderFolder);

            Sprite playerSprite = LoadSpriteFrame(PlayerSpritePath, PlayerSpriteFrame);
            if (playerSprite == null)
            {
                Debug.LogWarning("Player sprite not found. Using the placeholder sprite.");
                playerSprite = GetOrCreatePlaceholderSprite(
                    PlayerPlaceholderPath,
                    new Color(0.20f, 0.55f, 1.00f, 1f),
                    new Color(0.06f, 0.18f, 0.42f, 1f));
            }

            Sprite npcSprite = LoadSpriteFrame(NpcSpritePath, NpcSpriteFrame);
            if (npcSprite == null)
            {
                Debug.LogWarning("NPC sprite not found. Using the placeholder sprite.");
                npcSprite = GetOrCreatePlaceholderSprite(
                    NpcPlaceholderPath,
                    new Color(0.95f, 0.55f, 0.20f, 1f),
                    new Color(0.38f, 0.18f, 0.05f, 1f));
            }

            Sprite npcPortrait = LoadSpriteFrame(NpcPortraitPath, NpcPortraitFrame);
            if (npcPortrait == null)
            {
                Debug.LogWarning("NPC portrait not found. The dialogue will show no portrait.");
            }

            if (playerSprite == null || npcSprite == null)
            {
                Debug.LogError("Could not create the character sprites. Scene generation aborted.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateGlobalLight();
            GameObject player = CreatePlayer(playerSprite);

            DialogueUI dialogueUI;
            CreateDialogueCanvas(out dialogueUI);

            GameObject npc = CreateNpc(npcSprite, npcPortrait, player.transform, dialogueUI);

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

            CreateVisualChild(player, sprite, 2);
            player.AddComponent<DialogueTestPlayerMover>();
            return player;
        }

        private static GameObject CreateVisualChild(GameObject parent, Sprite sprite, int sortingOrder)
        {
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(parent.transform, false);

            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            if (sprite != null && sprite.pixelsPerUnit > 0f && sprite.rect.height > 0f)
            {
                // The pack sprites are 16 px tall at 100 PPU. Scale them to
                // roughly one world unit so they match the test scene layout.
                float scale = 1f / (sprite.rect.height / sprite.pixelsPerUnit);
                float width = sprite.rect.width / sprite.pixelsPerUnit * scale;
                visual.transform.localScale = new Vector3(scale, scale, 1f);
                visual.transform.localPosition = new Vector3(-width * 0.5f, 0f, 0f);
            }

            return visual;
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
                SetObjectReference(serializedUI, "portraitImage", FindImage(panel, PortraitObjectName));
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

        private static GameObject CreateNpc(
            Sprite sprite,
            Sprite portrait,
            Transform player,
            DialogueUI dialogueUI)
        {
            GameObject npc = new GameObject("NPC_OldSpin");
            npc.transform.position = new Vector3(2f, 0f, 0f);

            BoxCollider2D collider = npc.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.6f, 0.9f);
            collider.offset = new Vector2(0f, 0.45f);

            CreateVisualChild(npc, sprite, 1);

            DialogueInteractable interactable = npc.AddComponent<DialogueInteractable>();

            SerializedObject serializedNpc = new SerializedObject(interactable);
            serializedNpc.FindProperty("interactionRadius").floatValue = 1.5f;
            serializedNpc.FindProperty("player").objectReferenceValue = player;
            serializedNpc.FindProperty("ui").objectReferenceValue = dialogueUI;

            SerializedProperty lines = serializedNpc.FindProperty("lines");
            lines.arraySize = 3;
            SetLine(lines.GetArrayElementAtIndex(0), "Old Spin", "The sea keeps its own tempo, kid.", portrait);
            SetLine(
                lines.GetArrayElementAtIndex(1),
                "Old Spin",
                "Find the record by the dock, and we'll see if you can keep up.",
                portrait);
            SetLine(lines.GetArrayElementAtIndex(2), "Old Spin", "Come back when you're ready.", portrait);

            serializedNpc.ApplyModifiedPropertiesWithoutUndo();
            return npc;
        }

        private static void SetLine(SerializedProperty line, string speaker, string text, Sprite portrait)
        {
            line.FindPropertyRelative("speaker").stringValue = speaker;
            line.FindPropertyRelative("portrait").objectReferenceValue = portrait;
            line.FindPropertyRelative("text").stringValue = text;
        }

        private static Sprite LoadSpriteFrame(string path, string frameName)
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
            {
                return null;
            }

            ConfigurePixelArtTexture(path);

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                Sprite sprite = assets[i] as Sprite;
                if (sprite != null && sprite.name == frameName)
                {
                    return sprite;
                }
            }

            Debug.LogWarning("Sprite frame '" + frameName + "' was not found in " + path + ".");
            return null;
        }

        private static void ConfigurePixelArtTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool changed = false;
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static Text FindText(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static Image FindImage(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
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
