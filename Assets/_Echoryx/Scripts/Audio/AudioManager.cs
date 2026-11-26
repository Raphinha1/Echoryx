using System;
using UnityEngine;

namespace Echoryx.Audio
{
    /// <summary>
    /// Persistent player of one-shot effects, the rewind loop and crossfaded music. Fades use unscaled
    /// time so they work while paused. A missing library or clip is silently ignored.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public const float CrossfadeSeconds = 1.2f;
        /// <summary>Music level while paused. Applied to the sources, leaving mixer volumes untouched.</summary>
        public const float PausedMusicLevel = 0.35f;
        private const float DuckSeconds = 0.25f;

        private readonly AudioSource[] music = new AudioSource[2];
        private readonly float[] weight = new float[2];
        private readonly float[] baseVolume = new float[2];
        private AudioSource sfx;
        private AudioSource loop;
        private int current;
        private float duck = 1f;

        public static AudioManager Instance { get; private set; }
        public AudioLibrary Library { get; set; }
        public bool Paused { get; set; }
        public SoundId? CurrentMusic { get; private set; }
        public AudioSource ActiveMusicSource => music[current];
        public bool IsRewindLoopRequested { get; private set; }
        /// <summary>Raised for every requested effect, even when no clip is assigned.</summary>
        public event Action<SoundId> Requested;

        public static AudioManager Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("Audio Manager");
            DontDestroyOnLoad(go);
            return go.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Library = Resources.Load<AudioLibrary>(AudioLibrary.ResourceName);
            sfx = NewSource(false);
            loop = NewSource(true);
            music[0] = NewSource(true);
            music[1] = NewSource(true);
        }

        private AudioSource NewSource(bool looping)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = looping;
            source.spatialBlend = 0f;
            return source;
        }

        public void Play(SoundId id)
        {
            Requested?.Invoke(id);
            var sound = Library != null ? Library.Get(id) : null;
            var clip = sound?.Pick();
            if (clip == null) return;
            sfx.outputAudioMixerGroup = Library.sfxGroup;
            sfx.PlayOneShot(clip, sound.volume);
        }

        public void SetRewindLoop(bool active)
        {
            if (active == IsRewindLoopRequested) return;
            IsRewindLoopRequested = active;
            if (!active)
            {
                loop.Stop();
                return;
            }
            Requested?.Invoke(SoundId.Rewind);
            var sound = Library != null ? Library.rewind : null;
            var clip = sound?.Pick();
            if (clip == null) return;
            loop.outputAudioMixerGroup = Library.sfxGroup;
            loop.clip = clip;
            loop.volume = sound.volume;
            loop.Play();
        }

        /// <summary>Crossfades to a music entry. Asking for the one already playing keeps it running.</summary>
        public void PlayMusic(SoundId id)
        {
            if (CurrentMusic == id) return;
            CurrentMusic = id;
            current = 1 - current;
            var source = music[current];
            var sound = Library != null ? Library.Get(id) : null;
            source.Stop();
            source.clip = sound?.Pick();
            weight[current] = 0f;
            baseVolume[current] = sound != null ? sound.volume : 0f;
            source.volume = 0f;
            if (source.clip == null) return; // the previous track still fades out
            source.outputAudioMixerGroup = Library.musicGroup;
            source.Play();
        }

        private void Update()
        {
            var delta = Time.unscaledDeltaTime;
            duck = Mathf.MoveTowards(duck, Paused ? PausedMusicLevel : 1f, delta / DuckSeconds);
            for (var i = 0; i < music.Length; i++)
            {
                weight[i] = Mathf.MoveTowards(weight[i], i == current ? 1f : 0f, delta / CrossfadeSeconds);
                music[i].volume = weight[i] * baseVolume[i] * duck;
                if (i != current && weight[i] <= 0f && music[i].isPlaying) music[i].Stop();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
