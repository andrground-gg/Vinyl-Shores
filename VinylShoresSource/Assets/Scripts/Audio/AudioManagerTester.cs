using UnityEngine;

namespace VinylShores.Audio
{
    /// <summary>
    /// Test helper for the AudioManager, used only in the AudioTest scene.
    /// In Play mode, open this component's ⋮ menu (or right-click its title)
    /// to trigger sounds. Volume sliders apply live while playing.
    /// </summary>
    public sealed class AudioManagerTester : MonoBehaviour
    {
        [Header("Clips to test with")]
        [SerializeField] private AudioClip musicA;
        [SerializeField] private AudioClip musicB;
        [SerializeField] private AudioClip intro;
        [SerializeField] private AudioClip ambience;
        [SerializeField] private AudioClip sfx;

        [Tooltip("Seconds after the intro starts when the loop music begins.")]
        [SerializeField, Min(0f)] private float introLoopStartSeconds = 4f;

        [Header("Volumes (live in Play mode)")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 1f;

        [ContextMenu("Play Music A")]
        private void PlayMusicA()
        {
            if (HasManager()) AudioManager.Instance.PlayMusic(musicA);
        }

        [ContextMenu("Play Music B (crossfade)")]
        private void PlayMusicB()
        {
            if (HasManager()) AudioManager.Instance.PlayMusic(musicB);
        }

        [ContextMenu("Play Intro, then Music A + Ambience")]
        private void PlayIntroSequence()
        {
            if (HasManager())
            {
                AudioManager.Instance.PlayMusicWithIntro(intro, musicA, introLoopStartSeconds, ambience);
            }
        }

        [ContextMenu("Stop Music")]
        private void StopMusic()
        {
            if (HasManager()) AudioManager.Instance.StopMusic();
        }

        [ContextMenu("Play Ambience")]
        private void PlayAmbience()
        {
            if (HasManager()) AudioManager.Instance.PlayAmbience(ambience);
        }

        [ContextMenu("Stop Ambience")]
        private void StopAmbience()
        {
            if (HasManager()) AudioManager.Instance.StopAmbience();
        }

        [ContextMenu("Play SFX")]
        private void PlaySfx()
        {
            if (HasManager()) AudioManager.Instance.PlaySfx(sfx);
        }

        // Called by Unity when a value changes in the Inspector.
        private void OnValidate()
        {
            if (!Application.isPlaying || AudioManager.Instance == null)
            {
                return;
            }

            AudioManager.Instance.SetVolume(AudioChannel.Master, masterVolume);
            AudioManager.Instance.SetVolume(AudioChannel.Music, musicVolume);
            AudioManager.Instance.SetVolume(AudioChannel.Sfx, sfxVolume);
            AudioManager.Instance.SetVolume(AudioChannel.Ambience, ambienceVolume);
        }

        private static bool HasManager()
        {
            if (AudioManager.Instance != null)
            {
                return true;
            }

            Debug.LogWarning("No AudioManager in the scene (or the game is not in Play mode).");
            return false;
        }
    }
}
