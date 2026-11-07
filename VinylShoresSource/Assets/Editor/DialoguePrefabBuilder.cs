using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VinylShores.Dialogue;

namespace VinylShores.EditorTools
{
    /// <summary>
    /// Builds the reusable dialogue prefabs from the Unity editor so Unity
    /// generates their YAML, GUIDs and metas. The NPC prefab leaves the
    /// player and UI references empty on purpose: DialogueInteractable
    /// resolves them at runtime, which keeps the prefab drop-in.
    /// </summary>
    public static class DialoguePrefabBuilder
    {
        public const string PrefabFolder = "Assets/Prefabs/Dialogue";
        public const string NpcPrefabPath = PrefabFolder + "/DialogueNpc.prefab";
        public const string CanvasPrefabPath = PrefabFolder + "/DialogueCanvas.prefab";

        private const string PanelObjectName = "DialoguePanel";
        private const string PortraitObjectName = "Portrait";
        private const string SpeakerObjectName = "SpeakerText";
        private const string BodyObjectName = "BodyText";
        private const string HintObjectName = "HintText";
        private const string PromptObjectName = "DialoguePrompt";

        [MenuItem("Tools/Vinyl Shores/Build Dialogue Prefabs")]
        public static void BuildDialoguePrefabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play mode before building the dialogue prefabs.");
                return;
            }

            DialogueArtLibrary.EnsureFolder("Assets/Prefabs");
            DialogueArtLibrary.EnsureFolder(PrefabFolder);

            Sprite npcSprite = DialogueArtLibrary.LoadSpriteFrame(
                DialogueArtLibrary.NpcSpritePath,
                DialogueArtLibrary.NpcSpriteFrame);
            if (npcSprite == null)
            {
                Debug.LogWarning("NPC sprite not found. Using the placeholder sprite.");
                npcSprite = DialogueArtLibrary.GetOrCreatePlaceholderSprite(
                    DialogueArtLibrary.NpcPlaceholderPath,
                    new Color(0.95f, 0.55f, 0.20f, 1f),
                    new Color(0.38f, 0.18f, 0.05f, 1f));
            }

            Sprite npcPortrait = DialogueArtLibrary.LoadSpriteFrame(
                DialogueArtLibrary.NpcPortraitPath,
                DialogueArtLibrary.NpcPortraitFrame);
            if (npcPortrait == null)
            {
                Debug.LogWarning("NPC portrait not found. The dialogue will show no portrait.");
            }

            if (npcSprite == null)
            {
                Debug.LogError("Could not create the NPC sprite. Prefab generation aborted.");
                return;
            }

            BuildNpcPrefab(npcSprite, npcPortrait);
            BuildCanvasPrefab();

            AssetDatabase.SaveAssets();
            Debug.Log("Dialogue prefabs created at " + PrefabFolder + ".");
        }

        /// <summary>
        /// Creates the prefabs only when they are missing. Used by the scene
        /// builder when it does not want to overwrite hand-edited prefabs.
        /// </summary>
        public static void EnsureDialoguePrefabs()
        {
            bool npcMissing = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath) == null;
            bool canvasMissing = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPrefabPath) == null;
            if (npcMissing || canvasMissing)
            {
                BuildDialoguePrefabs();
            }
        }

        private static void BuildNpcPrefab(Sprite sprite, Sprite portrait)
        {
            GameObject npc = new GameObject("DialogueNpc");

            BoxCollider2D collider = npc.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.6f, 0.9f);
            collider.offset = new Vector2(0f, 0.45f);

            DialogueArtLibrary.CreateVisualChild(npc, sprite, 1);

            DialogueInteractable interactable = npc.AddComponent<DialogueInteractable>();

            SerializedObject serializedNpc = new SerializedObject(interactable);
            serializedNpc.FindProperty("interactionRadius").floatValue = 1.5f;

            SerializedProperty lines = serializedNpc.FindProperty("lines");
            lines.arraySize = 3;
            DialogueArtLibrary.SetLine(
                lines.GetArrayElementAtIndex(0),
                "Old Spin",
                "The sea keeps its own tempo, kid.",
                portrait);
            DialogueArtLibrary.SetLine(
                lines.GetArrayElementAtIndex(1),
                "Old Spin",
                "Find the record by the dock, and we'll see if you can keep up.",
                portrait);
            DialogueArtLibrary.SetLine(
                lines.GetArrayElementAtIndex(2),
                "Old Spin",
                "Come back when you're ready.",
                portrait);

            serializedNpc.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(npc, NpcPrefabPath);
            Object.DestroyImmediate(npc);
        }

        private static void BuildCanvasPrefab()
        {
            GameObject canvasObject = new GameObject(
                "DialogueCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.layer = DialogueArtLibrary.UiLayer;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            DialogueUI dialogueUI = canvasObject.AddComponent<DialogueUI>();
            dialogueUI.BuildUIIfNeeded();

            Transform panel = canvasObject.transform.Find(PanelObjectName);
            SerializedObject serializedUI = new SerializedObject(dialogueUI);

            if (panel != null)
            {
                DialogueArtLibrary.SetObjectReference(serializedUI, "panel", panel.gameObject);
                DialogueArtLibrary.SetObjectReference(
                    serializedUI,
                    "portraitImage",
                    DialogueArtLibrary.FindImage(panel, PortraitObjectName));
                DialogueArtLibrary.SetObjectReference(
                    serializedUI,
                    "speakerText",
                    DialogueArtLibrary.FindText(panel, SpeakerObjectName));
                DialogueArtLibrary.SetObjectReference(
                    serializedUI,
                    "bodyText",
                    DialogueArtLibrary.FindText(panel, BodyObjectName));
                DialogueArtLibrary.SetObjectReference(
                    serializedUI,
                    "hintText",
                    DialogueArtLibrary.FindText(panel, HintObjectName));
            }
            else
            {
                Debug.LogError("DialogueUI did not create its " + PanelObjectName + " object.");
            }

            Transform prompt = canvasObject.transform.Find(PromptObjectName);
            if (prompt != null)
            {
                DialogueArtLibrary.SetObjectReference(serializedUI, "promptRoot", prompt.gameObject);
                DialogueArtLibrary.SetObjectReference(
                    serializedUI,
                    "promptText",
                    prompt.GetComponentInChildren<Text>(true));
            }
            else
            {
                Debug.LogError("DialogueUI did not create its " + PromptObjectName + " object.");
            }

            serializedUI.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(canvasObject, CanvasPrefabPath);
            Object.DestroyImmediate(canvasObject);
        }
    }
}
