using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VinylShores.Dialogue;

namespace VinylShores.Rhythm
{
    [RequireComponent(typeof(DialogueInteractable))]
    public sealed class BeachRhythmChallenge : MonoBehaviour
    {
        private DialogueInteractable dialogue;

        private void Awake() => dialogue = GetComponent<DialogueInteractable>();
        private void OnEnable() => dialogue.onDialogueFinished.AddListener(StartChallenge);
        private void OnDisable() => dialogue.onDialogueFinished.RemoveListener(StartChallenge);

        private void StartChallenge()
        {
            SpriteRenderer guardian = GetComponentInChildren<SpriteRenderer>();
            if (guardian == null || guardian.sprite == null)
            {
                Debug.LogError("Guardian has no visible sprite for the rhythm challenge.", this);
                return;
            }
            BeachRhythmSession.StartChallenge(gameObject.scene, guardian.sprite);
        }
    }

    // Keeps the map and its runtime state intact while the challenge is open.
    public sealed class BeachRhythmSession : MonoBehaviour
    {
        private const string RhythmScenePath = "Assets/Scenes/Prototypes/RhythmPrototype.unity";
        private static BeachRhythmSession current;
        private Scene beach;
        private GameObject[] roots;
        private bool[] wasActive;
        private bool returning;
        private Sprite opponentSprite;
        public static Sprite OpponentSprite => current != null ? current.opponentSprite : null;
        public static bool IsActive => current != null;

        public static void StartChallenge(Scene beachScene, Sprite sprite)
        {
            if (current != null) return;
            if (!Application.CanStreamedLevelBeLoaded(RhythmScenePath))
            {
                Debug.LogError("RhythmPrototype is missing from the build scene list.");
                return;
            }
            var sessionObject = new GameObject("BeachRhythmSession");
            DontDestroyOnLoad(sessionObject);
            current = sessionObject.AddComponent<BeachRhythmSession>();
            current.beach = beachScene;
            current.opponentSprite = sprite;
            current.StartCoroutine(current.OpenChallenge());
        }

        private IEnumerator OpenChallenge()
        {
            // Let the dialogue close event finish before disabling its UI.
            yield return null;
            roots = beach.GetRootGameObjects();
            wasActive = new bool[roots.Length];
            for (int i = 0; i < roots.Length; i++)
            {
                wasActive[i] = roots[i].activeSelf;
                if (wasActive[i]) roots[i].SetActive(false);
            }
            AsyncOperation load = SceneManager.LoadSceneAsync(RhythmScenePath, LoadSceneMode.Additive);
            if (load == null)
            {
                RestoreBeach();
                yield break;
            }
            yield return load;
            Scene rhythm = SceneManager.GetSceneByPath(RhythmScenePath);
            if (rhythm.IsValid() && rhythm.isLoaded) SceneManager.SetActiveScene(rhythm);
            else RestoreBeach();
        }

        public static void ReturnToBeach()
        {
            if (current != null && !current.returning) current.StartCoroutine(current.CloseChallenge());
        }

        private IEnumerator CloseChallenge()
        {
            returning = true;
            Scene rhythm = SceneManager.GetSceneByPath(RhythmScenePath);
            if (beach.IsValid() && beach.isLoaded) SceneManager.SetActiveScene(beach);
            if (rhythm.IsValid() && rhythm.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(rhythm);
                if (unload != null) yield return unload;
            }
            RestoreBeach();
        }

        private void RestoreBeach()
        {
            if (roots != null)
            {
                for (int i = 0; i < roots.Length; i++)
                    if (roots[i] != null && wasActive[i]) roots[i].SetActive(true);
            }
            current = null;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (current == this) current = null;
        }
    }
}
