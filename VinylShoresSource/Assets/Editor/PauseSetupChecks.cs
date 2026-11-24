using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VinylShores.UI;

namespace VinylShores.EditorTools
{
    /// <summary>
    /// Lightweight validation for the pause prefab and test scene. Run it after
    /// rebuilding the prefab or before handing the prefab to the Beach owner.
    /// </summary>
    public static class PauseSetupChecks
    {
        [MenuItem("Tools/Vinyl Shores/Check Pause Setup")]
        public static void CheckPauseSetup()
        {
            int failures = 0;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PausePrefabBuilder.CanvasPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[Pause setup] Missing prefab at " + PausePrefabBuilder.CanvasPrefabPath + ".");
                return;
            }

            Check(prefab.name == "PauseCanvas", "Prefab root is named PauseCanvas.", ref failures);

            ExplorationPauseMenu menu = prefab.GetComponent<ExplorationPauseMenu>();
            Check(menu != null, "Prefab has ExplorationPauseMenu.", ref failures);

            Canvas canvas = prefab.GetComponent<Canvas>();
            Check(canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay,
                "Prefab has a screen-space overlay Canvas.", ref failures);
            Check(canvas != null && canvas.sortingOrder == 50,
                "Canvas sorting order is 50 (above the dialogue canvas).", ref failures);
            Check(prefab.GetComponent<GraphicRaycaster>() != null,
                "Prefab has a GraphicRaycaster for the Resume button.", ref failures);
            Check(prefab.GetComponent<CanvasScaler>() != null,
                "Prefab has a CanvasScaler.", ref failures);

            if (menu != null)
            {
                SerializedObject serializedMenu = new SerializedObject(menu);
                SerializedProperty panelProperty = serializedMenu.FindProperty("panel");
                SerializedProperty resumeProperty = serializedMenu.FindProperty("resumeButton");
                SerializedProperty hintProperty = serializedMenu.FindProperty("hintText");

                GameObject panel = panelProperty != null
                    ? panelProperty.objectReferenceValue as GameObject
                    : null;
                Button resume = resumeProperty != null
                    ? resumeProperty.objectReferenceValue as Button
                    : null;
                Text hint = hintProperty != null
                    ? hintProperty.objectReferenceValue as Text
                    : null;

                Check(panel != null, "Panel reference is wired.", ref failures);
                Check(resume != null, "Resume button reference is wired.", ref failures);
                Check(hint != null && !string.IsNullOrEmpty(hint.text),
                    "Controls hint reference is wired and has text.", ref failures);
                Check(panel != null && !panel.activeSelf,
                    "Panel starts hidden.", ref failures);

                if (panel != null)
                {
                    RectTransform panelRect = panel.GetComponent<RectTransform>();
                    Check(panelRect != null &&
                          panelRect.anchorMin == Vector2.zero &&
                          panelRect.anchorMax == Vector2.one,
                        "Panel covers the full screen.", ref failures);
                }
            }

            Check(AssetDatabase.LoadAssetAtPath<SceneAsset>(PauseTestSceneBuilder.ScenePath) != null,
                "Pause test scene exists.", ref failures);

            if (failures == 0)
            {
                Debug.Log("[Pause setup] All checks passed.");
            }
            else
            {
                Debug.LogError("[Pause setup] " + failures + " check(s) failed.");
            }
        }

        private static void Check(bool condition, string message, ref int failures)
        {
            if (condition)
            {
                Debug.Log("[Pause setup] OK: " + message);
            }
            else
            {
                failures++;
                Debug.LogError("[Pause setup] FAIL: " + message);
            }
        }
    }
}
