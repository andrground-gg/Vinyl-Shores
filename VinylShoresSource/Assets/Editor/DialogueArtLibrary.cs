using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace VinylShores.EditorTools
{
    /// <summary>
    /// Shared editor helpers for the dialogue prototype: pixel-art sprite
    /// loading, placeholder generation, visual hierarchy creation and
    /// serialized dialogue line setup.
    /// </summary>
    public static class DialogueArtLibrary
    {
        public const string PlaceholderFolder = "Assets/Art/Placeholders";
        public const string PlayerPlaceholderPath = PlaceholderFolder + "/PlayerPlaceholder.png";
        public const string NpcPlaceholderPath = PlaceholderFolder + "/NpcPlaceholder.png";

        public const string PlayerSpritePath =
            "Assets/Sprites/NinjaAdventure/Actor/Character/Boy/SpriteSheet.png";
        public const string NpcSpritePath =
            "Assets/Sprites/NinjaAdventure/Actor/Character/OldMan/SpriteSheet.png";
        public const string NpcPortraitPath =
            "Assets/Sprites/NinjaAdventure/Actor/Character/OldMan/Faceset.png";

        public const string PlayerSpriteFrame = "SpriteSheet_0";
        public const string NpcSpriteFrame = "SpriteSheet_0";
        public const string NpcPortraitFrame = "Faceset_0";

        /// <summary>
        /// Loads a named sub-sprite from a texture and configures the texture
        /// for crisp pixel art. Returns null when the texture or frame is
        /// missing.
        /// </summary>
        public static Sprite LoadSpriteFrame(string path, string frameName)
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

        public static void ConfigurePixelArtTexture(string path)
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

        public static Sprite GetOrCreatePlaceholderSprite(string path, Color fill, Color border)
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

        /// <summary>
        /// Adds a centered "Visual" child that renders the supplied sprite at
        /// roughly one world unit tall.
        /// </summary>
        public static GameObject CreateVisualChild(GameObject parent, Sprite sprite, int sortingOrder)
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

        public static void SetLine(SerializedProperty line, string speaker, string text, Sprite portrait)
        {
            line.FindPropertyRelative("speaker").stringValue = speaker;
            line.FindPropertyRelative("portrait").objectReferenceValue = portrait;
            line.FindPropertyRelative("text").stringValue = text;
        }

        public static Text FindText(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        public static Image FindImage(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        public static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning("Serialized property '" + propertyName + "' was not found.");
                return;
            }

            property.objectReferenceValue = value;
        }

        public static void EnsureFolder(string folderPath)
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

        public static int UiLayer
        {
            get
            {
                int layer = LayerMask.NameToLayer("UI");
                return layer >= 0 ? layer : 5;
            }
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
            Object.DestroyImmediate(texture);

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
    }
}
