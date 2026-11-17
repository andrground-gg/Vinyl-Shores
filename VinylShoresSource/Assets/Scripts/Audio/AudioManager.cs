using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace VinylShores.Audio
{
    /// <summary>The four volume channels exposed in MainMixer.</summary>
    public enum AudioChannel { Master, Music, Sfx, Ambience }

    /// <summary>
    /// Single entry point for music, ambience and sound effects.
    /// Singleton that survives scene loads, so music keeps playing between scenes.
    /// Every sound is routed through MainMixer (Music / SFX / Ambience groups).
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        // Names of the exposed parameters in MainMixer (must match exactly).
        private const string MasterVolumeParam = "MasterVolume";
        private const string MusicVolumeParam = "MusicVolume";
        private const string SfxVolumeParam = "SFXVolume";
        private const string AmbienceVolumeParam = "AmbienceVolume";

        // Lowest slider value; converts to -80 dB (silence) in the mixer.
        private const float MinLinearVolume = 0.0001f;

        // Small delay so scheduled audio is ready before it has to start.
        private const double ScheduleLeadSeconds = 0.1;

        public static AudioManager Instance { get; private set; }

        [Header("Mixer")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup ambienceGroup;

        [Header("Defaults")]
        [Tooltip("Crossfade time in seconds when changing music.")]
        [SerializeField, Min(0f)] private float defaultMusicFade = 1f;

        // Two music sources so one can fade out while the other fades in.
        private AudioSource musicA;
        private AudioSource musicB;
        private AudioSource activeMusic;
        private AudioSource ambienceSource;
        private AudioSource sfxSource;

        private Coroutine musicFade;
        private Coroutine ambienceFade;

        private void Awake()
        {
            // Keep only one AudioManager, even if a scene contains another copy.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            musicA = CreateSource("Music A", musicGroup, true);
            musicB = CreateSource("Music B", musicGroup, true);
            ambienceSource = CreateSource("Ambience", ambienceGroup, true);
            sfxSource = CreateSource("SFX", sfxGroup, false);
            activeMusic = musicA;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ---------- Music ----------

        /// <summary>
        /// Crossfades to a new music clip. Does nothing if that clip is already playing.
        /// Pass a negative fade to use the default fade time.
        /// </summary>
        public void PlayMusic(AudioClip clip, float fadeSeconds = -1f, bool loop = true)
        {
            if (clip == null || (activeMusic.clip == clip && activeMusic.isPlaying))
            {
                return;
            }

            float fade = fadeSeconds < 0f ? defaultMusicFade : fadeSeconds;
            AudioSource next = activeMusic == musicA ? musicB : musicA;

            StopRunning(ref musicFade);
            next.clip = clip;
            next.loop = loop;
            next.volume = 0f;
            next.Play();

            musicFade = StartCoroutine(Crossfade(activeMusic, next, fade));
            activeMusic = next;
        }

        /// <summary>
        /// Plays an intro once, then starts the looping music exactly
        /// loopStartSeconds after the intro starts (sample-accurate, no gap).
        /// The intro's tail can keep ringing over the start of the loop.
        /// Optionally starts an ambience track at the same moment as the intro.
        /// </summary>
        public void PlayMusicWithIntro(AudioClip intro, AudioClip loopClip, double loopStartSeconds,
            AudioClip ambience = null)
        {
            if (intro == null || loopClip == null)
            {
                return;
            }

            StopRunning(ref musicFade);
            musicA.Stop();
            musicB.Stop();

            double startDsp = AudioSettings.dspTime + ScheduleLeadSeconds;

            musicA.clip = intro;
            musicA.loop = false;
            musicA.volume = 1f;
            musicA.PlayScheduled(startDsp);

            musicB.clip = loopClip;
            musicB.loop = true;
            musicB.volume = 1f;
            musicB.PlayScheduled(startDsp + loopStartSeconds);

            // The loop is the music that stays, so later crossfades start from it.
            activeMusic = musicB;

            if (ambience != null)
            {
                StopRunning(ref ambienceFade);
                ambienceSource.clip = ambience;
                ambienceSource.volume = 1f;
                ambienceSource.PlayScheduled(startDsp);
            }
        }

        /// <summary>Fades out all music. Pass a negative fade to use the default.</summary>
        public void StopMusic(float fadeSeconds = -1f)
        {
            float fade = fadeSeconds < 0f ? defaultMusicFade : fadeSeconds;
            StopRunning(ref musicFade);
            musicFade = StartCoroutine(FadeOutAndStop(fade, musicA, musicB));
        }

        // ---------- Ambience ----------

        /// <summary>Fades in a looping ambience track (waves, birds...).</summary>
        public void PlayAmbience(AudioClip clip, float fadeSeconds = 1f)
        {
            if (clip == null || (ambienceSource.clip == clip && ambienceSource.isPlaying))
            {
                return;
            }

            StopRunning(ref ambienceFade);
            ambienceSource.clip = clip;
            ambienceSource.volume = 0f;
            ambienceSource.Play();
            ambienceFade = StartCoroutine(FadeTo(ambienceSource, 1f, fadeSeconds));
        }

        /// <summary>Fades out the ambience track.</summary>
        public void StopAmbience(float fadeSeconds = 1f)
        {
            StopRunning(ref ambienceFade);
            ambienceFade = StartCoroutine(FadeOutAndStop(fadeSeconds, ambienceSource));
        }

        // ---------- Sound effects ----------

        /// <summary>Plays a one-shot sound effect through the SFX group.</summary>
        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        // ---------- Volume (for the pause menu sliders) ----------

        /// <summary>Sets a channel's volume from a 0-1 slider value.</summary>
        public void SetVolume(AudioChannel channel, float linearVolume)
        {
            float clamped = Mathf.Clamp(linearVolume, MinLinearVolume, 1f);
            // Sliders are linear, the mixer works in decibels: dB = 20 * log10(volume).
            mixer.SetFloat(GetParameterName(channel), Mathf.Log10(clamped) * 20f);
        }

        /// <summary>Returns a channel's current volume as a 0-1 slider value.</summary>
        public float GetVolume(AudioChannel channel)
        {
            if (!mixer.GetFloat(GetParameterName(channel), out float decibels))
            {
                return 1f;
            }

            return Mathf.Pow(10f, decibels / 20f);
        }

        // ---------- Helpers ----------

        private AudioSource CreateSource(string sourceName, AudioMixerGroup group, bool loop)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform);

            var source = child.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = group;
            source.loop = loop;
            source.playOnAwake = false;
            return source;
        }

        private static string GetParameterName(AudioChannel channel)
        {
            switch (channel)
            {
                case AudioChannel.Music: return MusicVolumeParam;
                case AudioChannel.Sfx: return SfxVolumeParam;
                case AudioChannel.Ambience: return AmbienceVolumeParam;
                default: return MasterVolumeParam;
            }
        }

        private void StopRunning(ref Coroutine routine)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        // Unscaled time, so fades still work while the game is paused.
        private static IEnumerator Crossfade(AudioSource from, AudioSource to, float seconds)
        {
            float fromStart = from.volume;
            float toStart = to.volume;
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                from.volume = Mathf.Lerp(fromStart, 0f, t);
                to.volume = Mathf.Lerp(toStart, 1f, t);
                yield return null;
            }

            from.Stop();
            from.volume = 0f;
            to.volume = 1f;
        }

        private static IEnumerator FadeTo(AudioSource source, float target, float seconds)
        {
            float start = source.volume;
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / seconds));
                yield return null;
            }

            source.volume = target;
        }

        private static IEnumerator FadeOutAndStop(float seconds, params AudioSource[] sources)
        {
            var startVolumes = new float[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                startVolumes[i] = sources[i].volume;
            }

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                for (int i = 0; i < sources.Length; i++)
                {
                    sources[i].volume = Mathf.Lerp(startVolumes[i], 0f, t);
                }
                yield return null;
            }

            foreach (AudioSource source in sources)
            {
                source.Stop();
                source.volume = 0f;
            }
        }
    }
}
