using UnityEngine;
using UnityEngine.Audio;

namespace VinylShores.Audio
{
    public sealed class BeachAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip music;
        [SerializeField] private AudioClip waves;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup ambienceGroup;
        private AudioSource musicSource;
        private AudioSource wavesSource;
        private int musicPosition;
        private int wavesPosition;
        private bool suspended;

        private void Awake()
        {
            musicSource = CreateSource(music, musicGroup);
            wavesSource = CreateSource(waves, ambienceGroup);
        }

        private void OnEnable()
        {
            PlayAt(musicSource, musicPosition);
            PlayAt(wavesSource, wavesPosition);
            suspended = false;
        }

        private void OnDisable() => Suspend();

        public void Suspend()
        {
            if (suspended) return;
            musicPosition = StopAtCurrentPosition(musicSource);
            wavesPosition = StopAtCurrentPosition(wavesSource);
            suspended = true;
        }

        private static int StopAtCurrentPosition(AudioSource source)
        {
            if (source == null || source.clip == null) return 0;
            int position = source.timeSamples;
            source.Stop();
            return position;
        }

        private static void PlayAt(AudioSource source, int position)
        {
            if (source.clip == null) return;
            source.timeSamples = Mathf.Clamp(position, 0, source.clip.samples - 1);
            source.Play();
        }

        private AudioSource CreateSource(AudioClip clip, AudioMixerGroup group)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = group;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = true;
            source.clip = clip;
            return source;
        }
    }
}
