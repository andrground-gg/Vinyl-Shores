using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VinylShores.UI;

namespace VinylShores.EditorTools
{
    /// <summary>
    /// Builds the reusable exploration pause prefab. The prefab is a screen
    /// space canvas with the <see cref="ExplorationPauseMenu"/> component and a
    /// generated panel. The player, input map and EventSystem are resolved at
    /// runtime, so the prefab stays drop-in for the Beach scene.
    /// </summary>
    public static class PausePrefabBuilder
    {
        public const string PrefabFolder = "Assets/Prefabs/UI";
        public const string CanvasPrefabPath = PrefabFolder + "/PauseCanvas.prefab";

        [MenuItem("Tools/Vinyl Shores/Build Pause Prefab")]
        public static void BuildPausePrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play mode before building the pause prefab.");
                return;
            }

            DialogueArtLibrary.EnsureFolder("Assets/Prefabs");
            DialogueArtLibrary.EnsureFolder(PrefabFolder);

            GameObject canvasObject = new GameObject(
                "PauseCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.layer = DialogueArtLibrary.UiLayer;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Above the dialogue canvas so the pause panel blocks its clicks.
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            ExplorationPauseMenu pauseMenu = canvasObject.AddComponent<ExplorationPauseMenu>();
            pauseMenu.BuildUIIfNeeded();

            PrefabUtility.SaveAsPrefabAsset(canvasObject, CanvasPrefabPath);
            Object.DestroyImmediate(canvasObject);

            AssetDatabase.SaveAssets();
            Debug.Log("Pause prefab created at " + CanvasPrefabPath + ".");
        }
    }
}
