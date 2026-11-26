using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Echoryx.Audio
{
    public enum SoundId
    {
        Footstep, Jump, Land, PressurePlate, Door, EchoRecord, EchoPlay, EchoEnd,
        Rewind, Checkpoint, Goal, UiClick, UiHover, MenuMusic, LevelMusic
    }

    /// <summary>One clip, or several to pick from at random. May be left empty.</summary>
    [Serializable]
    public sealed class Sound
    {
        public AudioClip[] clips = Array.Empty<AudioClip>();
        [Range(0f, 1f)] public float volume = 1f;

        /// <summary>A random assigned clip, or null when none is assigned.</summary>
        public AudioClip Pick()
        {
            if (clips == null) return null;
            var count = 0;
            foreach (var clip in clips) if (clip != null) count++;
            if (count == 0) return null;
            var chosen = UnityEngine.Random.Range(0, count);
            foreach (var clip in clips)
                if (clip != null && chosen-- == 0) return clip;
            return null;
        }
    }

    /// <summary>Every sound of the game. Assign clips in Resources/AudioLibrary.asset.</summary>
    [CreateAssetMenu(fileName = ResourceName, menuName = "Echoryx/Audio Library")]
    public sealed class AudioLibrary : ScriptableObject
    {
        public const string ResourceName = "AudioLibrary";

        [Header("Mixer")]
        public AudioMixerGroup musicGroup;
        public AudioMixerGroup sfxGroup;

        [Header("Player")]
        public Sound footsteps = new Sound();
        public Sound jump = new Sound();
        public Sound land = new Sound();

        [Header("World")]
        public Sound pressurePlate = new Sound();
        public Sound door = new Sound();
        public Sound checkpoint = new Sound();
        public Sound goal = new Sound();

        [Header("Echo and Rewind")]
        public Sound echoRecord = new Sound();
        public Sound echoPlay = new Sound();
        public Sound echoEnd = new Sound();
        [Tooltip("Loops while a mechanism is being rewound.")]
        public Sound rewind = new Sound();

        [Header("UI")]
        public Sound uiClick = new Sound();
        public Sound uiHover = new Sound();

        [Header("Music (loops)")]
        public Sound menuMusic = new Sound();
        public Sound levelMusic = new Sound();

        public Sound Get(SoundId id)
        {
            switch (id)
            {
                case SoundId.Footstep: return footsteps;
                case SoundId.Jump: return jump;
                case SoundId.Land: return land;
                case SoundId.PressurePlate: return pressurePlate;
                case SoundId.Door: return door;
                case SoundId.EchoRecord: return echoRecord;
                case SoundId.EchoPlay: return echoPlay;
                case SoundId.EchoEnd: return echoEnd;
                case SoundId.Rewind: return rewind;
                case SoundId.Checkpoint: return checkpoint;
                case SoundId.Goal: return goal;
                case SoundId.UiClick: return uiClick;
                case SoundId.UiHover: return uiHover;
                case SoundId.MenuMusic: return menuMusic;
                case SoundId.LevelMusic: return levelMusic;
                default: return null;
            }
        }
    }
}
