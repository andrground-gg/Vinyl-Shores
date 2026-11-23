using UnityEngine;

namespace VinylShores.Audio
{
    public sealed class BeachAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip music;
        [SerializeField] private AudioClip waves;
        private AudioSource musicSource;
        private AudioSource wavesSource;
        private bool started;

        private void Awake()
        {
            musicSource = CreateSource(music);
            wavesSource = CreateSource(waves);
        }

        private void OnEnable()
        {
            if (!started)
            {
                if (musicSource.clip != null) musicSource.Play();
                if (wavesSource.clip != null) wavesSource.Play();
                started = true;
            }
            else
            {
                musicSource.UnPause();
                wavesSource.UnPause();
            }
        }

        private void OnDisable()
        {
            if (musicSource != null) musicSource.Pause();
            if (wavesSource != null) wavesSource.Pause();
        }

        private AudioSource CreateSource(AudioClip clip)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = true;
            source.clip = clip;
            return source;
        }
    }
}
