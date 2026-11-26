using System;
using System.Collections.Generic;
using Echoryx.Flow;
using UnityEngine;

namespace Echoryx.Audio
{
    /// <summary>
    /// Persistent player of one-shot effects, faded loops and music with fades. Fades use unscaled
    /// time so they work while paused. A missing library or clip is silently ignored.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public const float MusicFadeOutSeconds = 1.5f;
        public const float MusicFadeInSeconds = 2f;
        /// <summary>Music level while paused. Applied to the sources, leaving mixer volumes untouched.</summary>
        public const float PausedMusicLevel = 0.35f;
        private const float DuckSeconds = 0.25f;
        public const float LoopFadeSeconds = 0.1f;

        private sealed class Loop
        {
            public AudioSource Source;
            public bool Active;
            public bool Held;
            public float Gain;
            public float BaseVolume;
        }

        private readonly Dictionary<SoundId, Loop> loops = new Dictionary<SoundId, Loop>();

        private readonly AudioSource[] music = new AudioSource[2];
        private readonly float[] weight = new float[2];
        private readonly float[] baseVolume = new float[2];
        private AudioSource sfx;
        private int current;
        private bool silencing;
        private float duck = 1f;

        public static AudioManager Instance { get; private set; }
        public AudioLibrary Library { get; set; }
        public bool Paused { get; set; }
        public SoundId? CurrentMusic { get; private set; }
        public AudioSource ActiveMusicSource => music[current];
        /// <summary>Raised for every requested effect, even when no clip is assigned.</summary>
        public event Action<SoundId> Requested;

        // The mixer ignores SetFloat during Awake, so the saved volumes are applied from Start.
        private void Start()
        {
            GameSettings.VolumesChanged += ApplyVolumes;
            ApplyVolumes();
        }

        /// <summary>Writes the saved Music and SFX volumes to the mixer's exposed parameters.</summary>
        public void ApplyVolumes()
        {
            var group = Library != null ? Library.musicGroup : null;
            if (group == null) return;
            group.audioMixer.SetFloat("MusicVolume", GameSettings.ToDecibels(GameSettings.MusicVolume));
            group.audioMixer.SetFloat("SFXVolume", GameSettings.ToDecibels(GameSettings.SfxVolume));
        }

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

        /// <summary>
        /// Starts or stops a looping effect (Rewind, EchoRecord, EchoPlay) with a short fade. Safe to call every
        /// frame; Requested is raised only when a loop starts. While Paused, loops fade out and hold their position.
        /// </summary>
        public void SetLoop(SoundId id, bool active)
        {
            if (!loops.TryGetValue(id, out var loop))
            {
                if (!active) return;
                loops[id] = loop = new Loop { Source = NewSource(true) };
                loop.Source.volume = 0f;
            }
            if (loop.Active == active) return;
            loop.Active = active;
            if (!active) return; // Update fades it out and then stops it
            Requested?.Invoke(id);
            if (loop.Gain > 0f && loop.Source.clip != null) return; // still fading out: just bring it back
            var sound = Library != null ? Library.Get(id) : null;
            loop.Source.Stop();
            loop.Held = false;
            loop.Gain = 0f;
            loop.Source.volume = 0f;
            loop.Source.clip = sound?.Pick();
            if (loop.Source.clip == null) return;
            loop.BaseVolume = sound.volume;
            loop.Source.outputAudioMixerGroup = Library.sfxGroup;
            loop.Source.Play();
        }

        /// <summary>Fades out every loop (scene changes).</summary>
        public void StopLoops()
        {
            foreach (var loop in loops.Values) loop.Active = false;
        }

        public bool IsLoopActive(SoundId id) => loops.TryGetValue(id, out var loop) && loop.Active;
        /// <summary>Current fade position of a loop, from 0 (silent) to 1.</summary>
        public float LoopGain(SoundId id) => loops.TryGetValue(id, out var loop) ? loop.Gain : 0f;
        /// <summary>True once a paused game has faded the loop out and paused its source.</summary>
        public bool IsLoopHeld(SoundId id) => loops.TryGetValue(id, out var loop) && loop.Held;

        private void UpdateLoops(float delta)
        {
            foreach (var loop in loops.Values)
            {
                var audible = loop.Active && !Paused;
                if (audible && loop.Held)
                {
                    loop.Source.UnPause();
                    loop.Held = false;
                }
                loop.Gain = Mathf.MoveTowards(loop.Gain, audible ? 1f : 0f, delta / LoopFadeSeconds);
                loop.Source.volume = loop.Gain * loop.BaseVolume;
                if (loop.Gain > 0f) continue;
                if (!loop.Active)
                {
                    loop.Source.Stop();
                    loop.Held = false;
                }
                else if (Paused && !loop.Held)
                {
                    loop.Source.Pause();
                    loop.Held = true;
                }
            }
        }

        /// <summary>
        /// Fades the previous track out and the new one in. Asking for the one already playing keeps it running.
        /// </summary>
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
            silencing = false;
            if (source.clip == null) return; // the previous track still fades out
            source.outputAudioMixerGroup = Library.musicGroup;
            source.Play();
        }

        /// <summary>Starts fading the music out ahead of a scene change.</summary>
        public void FadeOutMusic()
        {
            CurrentMusic = null;
            silencing = true;
        }

        private void Update()
        {
            var delta = Time.unscaledDeltaTime;
            UpdateLoops(delta);
            duck = Mathf.MoveTowards(duck, Paused ? PausedMusicLevel : 1f, delta / DuckSeconds);
            // A loading hitch must not turn a fade into a jump.
            var fade = Mathf.Min(delta, 1f / 30f);
            for (var i = 0; i < music.Length; i++)
            {
                var rising = i == current && !silencing;
                weight[i] = Mathf.MoveTowards(weight[i], rising ? 1f : 0f,
                    fade / (rising ? MusicFadeInSeconds : MusicFadeOutSeconds));
                music[i].volume = weight[i] * baseVolume[i] * duck;
                if (!rising && weight[i] <= 0f && music[i].isPlaying) music[i].Stop();
            }
        }

        private void OnDestroy()
        {
            GameSettings.VolumesChanged -= ApplyVolumes;
            if (Instance == this) Instance = null;
        }
    }
}
