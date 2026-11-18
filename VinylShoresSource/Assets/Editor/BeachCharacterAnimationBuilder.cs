using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace VinylShores.EditorTools
{
    /*
    Generates the Beach character animation assets from the sliced Ninja Adventure sheets:
        Walk.png (64x64, 4x4 grid): columns = Down/Up/Left/Right, rows = walk frames.
        Idle.png (64x16, 1x4 grid): one standing pose per direction, same column order.
    Output (all inside Assets/Art/Animations/Beach):
        Clips/&lt;Character&gt;/&lt;Character&gt;_{Walk|Idle}_{Direction}.anim
        BeachCharacter.controller            (base controller, uses the Boy clips)
        Overrides/&lt;Character&gt;.overrideController (NPCs, swap in their own clips)

    Safe to re-run: existing clips are updated in place (GUIDs are kept) and the
    base controller is only created once. Shared sprite importers are never touched. */
    
    
    public static class BeachCharacterAnimationBuilder
    {
        private const string Root = "Assets/Art/Animations/Beach";
        private const string CharactersFolder = Root + "/Characters";
        private const string ControllerPath = Root + "/BeachCharacter.controller";

        private const string BaseCharacter = "Boy";
        private static readonly string[] OverrideCharacters = { "Knight", "OldMan2", "Village6", "Villager3" };

        private const int SheetColumns = 4;
        private const int FramesPerDirection = 4;
        private const float FrameRate = 8f;

        // Must match the optional parameters read by PlayerMovement.
        private const string MoveX = "MoveX";
        private const string MoveY = "MoveY";
        private const string IsMoving = "IsMoving";

        // Value = column index in Walk.png and Idle.png
        private enum Direction { Down = 0, Up = 1, Left = 2, Right = 3 }

        private static readonly Dictionary<Direction, Vector2> BlendPositions = new()
        {
            { Direction.Down, Vector2.down },
            { Direction.Up, Vector2.up },
            { Direction.Left, Vector2.left },
            { Direction.Right, Vector2.right },
        };

        // Animates only the SpriteRenderer on the same GameObject as the Animator (the "Visual" child)
        private static readonly EditorCurveBinding SpriteBinding =
            EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        [MenuItem("Tools/Vinyl Shores/Build Beach Character Animations")]
        public static void Build()
        {
            try
            {
                string clipsFolder = EnsureFolder(Root, "Clips");
                string overridesFolder = EnsureFolder(Root, "Overrides");

                Dictionary<string, AnimationClip> baseClips = BuildClips(BaseCharacter, clipsFolder);
                AnimatorController controller = LoadOrCreateController(baseClips);

                foreach (string character in OverrideCharacters)
                {
                    BuildOverride(character, controller, baseClips, clipsFolder, overridesFolder);
                }

                AssetDatabase.SaveAssets();
                Debug.Log($"Beach character animations built in {Root}.");
            }
            catch (System.InvalidOperationException e)
            {
                Debug.LogError(e.Message);
            }
        }

        private static Dictionary<string, AnimationClip> BuildClips(string character, string clipsFolder)
        {
            string folder = EnsureFolder(clipsFolder, character);
            Sprite[] walk = LoadFrames(character, "Walk", SheetColumns * FramesPerDirection);
            Sprite[] idle = LoadFrames(character, "Idle", SheetColumns);

            var clips = new Dictionary<string, AnimationClip>();
            foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
            {
                int column = (int)direction;
                Sprite[] walkFrames = Enumerable.Range(0, FramesPerDirection)
                    .Select(row => walk[row * SheetColumns + column])
                    .ToArray();

                clips[$"Walk_{direction}"] = SaveClip(folder, $"{character}_Walk_{direction}", walkFrames);
                clips[$"Idle_{direction}"] = SaveClip(folder, $"{character}_Idle_{direction}", new[] { idle[column] });
            }

            return clips;
        }

        private static Sprite[] LoadFrames(string character, string sheet, int expectedCount)
        {
            string path = $"{CharactersFolder}/{character}/{sheet}.png";
            Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .ToDictionary(sprite => sprite.name);

            var frames = new Sprite[expectedCount];
            for (int i = 0; i < expectedCount; i++)
            {
                if (!sprites.TryGetValue($"{sheet}_{i}", out frames[i]))
                {
                    throw new System.InvalidOperationException(
                        $"Missing sprite '{sheet}_{i}' in {path}. Slice the sheet as a 16x16 grid first.");
                }
            }

            return frames;
        }

        private static AnimationClip SaveClip(string folder, string clipName, Sprite[] frames)
        {
            var clip = new AnimationClip { name = clipName, frameRate = FrameRate };
            float frameDuration = 1f / FrameRate;

            // The last sprite is keyed once more one frame later, so it stays visible as long as the others
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = i * frameDuration, value = frames[i] };
            }
            keys[^1] = new ObjectReferenceKeyframe { time = frames.Length * frameDuration, value = frames[^1] };

            AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding, keys);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            string path = $"{folder}/{clipName}.anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(clip, path);
                return clip;
            }

            EditorUtility.CopySerialized(clip, existing);
            Object.DestroyImmediate(clip);
            return existing;
        }

        private static AnimatorController LoadOrCreateController(Dictionary<string, AnimationClip> clips)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null)
            {
                // Clips are updated in place, so the existing graph still points at the right assets
                return existing;
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(MoveX, AnimatorControllerParameterType.Float);
            controller.AddParameter(MoveY, AnimatorControllerParameterType.Float);
            controller.AddParameter(IsMoving, AnimatorControllerParameterType.Bool);

            // Characters without a movement script (NPCs) and the player before the first input face down
            AnimatorControllerParameter[] parameters = controller.parameters;
            parameters.First(p => p.name == MoveY).defaultFloat = -1f;
            controller.parameters = parameters;

            AnimatorState idle = CreateBlendState(controller, "Idle", clips);
            AnimatorState walk = CreateBlendState(controller, "Walk", clips);
            controller.layers[0].stateMachine.defaultState = idle;

            AddTransition(idle, walk, AnimatorConditionMode.If);
            AddTransition(walk, idle, AnimatorConditionMode.IfNot);

            return controller;
        }

        private static AnimatorState CreateBlendState(
            AnimatorController controller, string stateName, Dictionary<string, AnimationClip> clips)
        {
            AnimatorState state = controller.CreateBlendTreeInController(stateName, out BlendTree tree, 0);
            tree.name = stateName;
            tree.blendType = BlendTreeType.SimpleDirectional2D;
            tree.blendParameter = MoveX;
            tree.blendParameterY = MoveY;

            foreach (KeyValuePair<Direction, Vector2> entry in BlendPositions)
            {
                tree.AddChild(clips[$"{stateName}_{entry.Key}"], entry.Value);
            }

            return state;
        }

        private static void AddTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(mode, 0f, IsMoving);
        }

        private static void BuildOverride(
            string character,
            AnimatorController baseController,
            Dictionary<string, AnimationClip> baseClips,
            string clipsFolder,
            string overridesFolder)
        {
            Dictionary<string, AnimationClip> clips = BuildClips(character, clipsFolder);

            string path = $"{overridesFolder}/{character}.overrideController";
            var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (overrideController == null)
            {
                overrideController = new AnimatorOverrideController(baseController) { name = character };
                AssetDatabase.CreateAsset(overrideController, path);
            }

            overrideController.runtimeAnimatorController = baseController;
            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = baseClips
                .Select(entry => new KeyValuePair<AnimationClip, AnimationClip>(entry.Value, clips[entry.Key]))
                .ToList();
            overrideController.ApplyOverrides(overrides);
            EditorUtility.SetDirty(overrideController);
        }

        private static string EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }

            return path;
        }
    }
}