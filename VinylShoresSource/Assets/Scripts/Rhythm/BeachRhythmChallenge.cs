using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using VinylShores.Dialogue;
using VinylShores.Progress;

namespace VinylShores.Rhythm
{
    public enum BeachChallengeState { Pending, Running, Failed, Completed }

    /// <summary>
    /// Bridge between a guardian's dialogue and the rhythm challenge.
    /// Dialogue closes -> challenge opens -> player returns -> result goes to RecordProgress.
    /// </summary>
    [RequireComponent(typeof(DialogueInteractable))]
    public sealed class BeachRhythmChallenge : MonoBehaviour
    {
        [Tooltip("Record awarded by this guardian (e.g. beach_1). Leave empty for a practice challenge that grants nothing.")]
        [SerializeField] private string recordId = "";

        [Header("Result hooks (for guardian dialogue variants)")]
        [Tooltip("Returned from a challenge whose best attempt was below 50%.")]
        public UnityEvent onChallengeFailed = new UnityEvent();
        [Tooltip("First passing result: the record has just been granted.")]
        public UnityEvent onRecordGranted = new UnityEvent();
        [Tooltip("Passing result after the record was already granted (replay).")]
        public UnityEvent onReplayPassed = new UnityEvent();

        private DialogueInteractable dialogue;

        public string RecordId => recordId;
        public BeachChallengeState State { get; private set; } = BeachChallengeState.Pending;

        private void Awake() => dialogue = GetComponent<DialogueInteractable>();
        private void OnEnable() => dialogue.onDialogueFinished.AddListener(StartChallenge);
        private void OnDisable() => dialogue.onDialogueFinished.RemoveListener(StartChallenge);

        private void StartChallenge()
        {
            if (State == BeachChallengeState.Running || BeachRhythmSession.IsActive) return;
            SpriteRenderer guardian = GetComponentInChildren<SpriteRenderer>();
            if (guardian == null || guardian.sprite == null)
            {
                Debug.LogError("Guardian has no visible sprite for the rhythm challenge.", this);
                return;
            }
            if (BeachRhythmSession.StartChallenge(gameObject.scene, guardian.sprite, recordId, OnChallengeReturned))
                State = BeachChallengeState.Running;
        }

        private void OnChallengeReturned(BeachRhythmOutcome outcome)
        {
            bool hadRecord = RecordProgress.HasRecord(recordId);
            if (!outcome.Attempted)
            {
                // The song never finished (e.g. audio failed to load): nothing changes.
                State = hadRecord ? BeachChallengeState.Completed : BeachChallengeState.Pending;
                Log($"returned without a finished attempt. State: {State}.");
                return;
            }

            if (!outcome.Passed)
            {
                State = hadRecord ? BeachChallengeState.Completed : BeachChallengeState.Failed;
                Log($"failed with best {outcome.BestPercentage:0.#}% (below 50%). Nothing granted. State: {State}.");
                onChallengeFailed.Invoke();
                return;
            }

            if (string.IsNullOrEmpty(recordId))
            {
                State = BeachChallengeState.Completed;
                Log($"passed with {outcome.BestPercentage:0.#}% ({outcome.Stars} stars). Practice only: no record id.");
                return;
            }

            RecordSubmission result = RecordProgress.ReportChallengeResult(recordId, outcome.Stars);
            if (!result.Accepted)
            {
                // Unknown id (typo in the Inspector): RecordProgress already logged an error.
                State = BeachChallengeState.Pending;
                return;
            }
            State = BeachChallengeState.Completed;
            Log($"passed with {outcome.BestPercentage:0.#}% ({result.Stars} stars). " +
                (result.GrantedNow ? $"Record '{recordId}' GRANTED. " : $"Record '{recordId}' already owned, not granted again. ") +
                $"Best stars: {result.BestStars}. Beach: {RecordProgress.GetCollectedCount(IslandArea.Beach)}/{RecordCatalog.RecordsPerArea}. " +
                $"Records: {RecordProgress.CollectedCount}/{RecordCatalog.Count}, total best stars: {RecordProgress.TotalBestStars}/{RecordProgress.MaxTotalStars}.");
            if (result.GrantedNow) onRecordGranted.Invoke();
            else onReplayPassed.Invoke();
        }

        private void Log(string message) => Debug.Log($"[Beach record loop] {name}: {message}", this);
    }

    /// <summary>Best result of one visit to the rhythm scene (restarts included).</summary>
    public readonly struct BeachRhythmOutcome
    {
        public string ChallengeId { get; }
        public int Attempts { get; }
        public double BestPercentage { get; }
        public bool Attempted => Attempts > 0;
        public int Stars => Attempted ? RhythmResultView.StarsForPercentage(BestPercentage) : 0;
        public bool Passed => Stars > 0;

        public BeachRhythmOutcome(string challengeId, int attempts, double bestPercentage)
        {
            ChallengeId = challengeId;
            Attempts = attempts;
            BestPercentage = bestPercentage;
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
        private string challengeId;
        private Action<BeachRhythmOutcome> onReturned;
        private int attempts;
        private double bestPercentage;
        public static Sprite OpponentSprite => current != null ? current.opponentSprite : null;
        public static bool IsActive => current != null;

        /// <summary>Opens the rhythm scene. Returns false if a challenge is already open or the scene is missing.</summary>
        public static bool StartChallenge(Scene beachScene, Sprite sprite, string challengeId = "",
            Action<BeachRhythmOutcome> onReturned = null)
        {
            if (current != null) return false;
            if (!Application.CanStreamedLevelBeLoaded(RhythmScenePath))
            {
                Debug.LogError("RhythmPrototype is missing from the build scene list.");
                return false;
            }
            var sessionObject = new GameObject("BeachRhythmSession");
            DontDestroyOnLoad(sessionObject);
            current = sessionObject.AddComponent<BeachRhythmSession>();
            current.beach = beachScene;
            current.opponentSprite = sprite;
            current.challengeId = challengeId;
            current.onReturned = onReturned;
            current.StartCoroutine(current.OpenChallenge());
            return true;
        }

        /// <summary>Called by the rhythm controller each time a song finishes. Keeps the best attempt.</summary>
        public static void ReportResult(double percentage)
        {
            if (current == null || current.returning) return;
            if (current.attempts == 0 || percentage > current.bestPercentage) current.bestPercentage = percentage;
            current.attempts++;
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
            // Report after the Beach is active again so listeners can show dialogue or prompts.
            var callback = onReturned;
            onReturned = null;
            var outcome = new BeachRhythmOutcome(challengeId, attempts, bestPercentage);
            Destroy(gameObject);
            if (callback == null) return;
            try { callback(outcome); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private void OnDestroy()
        {
            if (current == this) current = null;
        }
    }
}
