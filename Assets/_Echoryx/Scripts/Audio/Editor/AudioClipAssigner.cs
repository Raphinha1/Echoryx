using System;
using UnityEditor;
using UnityEngine;

namespace Echoryx.Audio.Editor
{
    /// <summary>
    /// Assigns the clips in Assets/_Echoryx/Audio to the AudioLibrary with their volumes. Overwrites those
    /// entries. Music is imported as streaming; it loops because the AudioManager music sources loop.
    /// </summary>
    public static class AudioClipAssigner
    {
        private const string Folder = "Assets/_Echoryx/Audio/";

        [MenuItem("Echoryx/Audio/Assign Clips")]
        public static void Assign()
        {
            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(AudioAssetsBuilder.LibraryPath)
                ?? throw new InvalidOperationException("Missing " + AudioAssetsBuilder.LibraryPath);
            Set(library.footsteps, "pasos", 0.35f);
            Set(library.jump, "salto", 0.55f);
            Set(library.land, "aterrizaje", 0.55f);
            Set(library.pressurePlate, "activar-placa", 0.7f);
            Set(library.doorOpen, "abrir-puerta", 0.7f);
            Set(library.doorClose, "cerrar-puerta", 0.7f);
            Set(library.echoRecord, "echo-grabando", 0.45f);
            Set(library.echoPlay, "echo-reproduciendo", 0.45f);
            Set(library.echoEnd, "echo-terminar", 0.65f);
            // rewind.ogg is almost all low end, inaudible on small speakers; this derived copy is high-passed at 500 Hz.
            Set(library.rewind, "rewind-audible", 0.9f);
            Set(library.checkpoint, "checkpoint", 0.75f);
            Set(library.goal, "meta", 0.85f);
            Set(library.uiClick, "Clic", 0.4f);
            Set(library.uiHover, "Clic", 0.15f);
            SetMusic(library.menuMusic, "sci-fi_puzzle_title_bpm120", 0.5f);
            SetMusic(library.levelMusic, "sci-fi_puzzle_in-game_2_bpm95", 0.5f);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();

            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
                if (library.Get(id).Pick() == null) Debug.Log("AUDIO sin clip: " + id);
            Debug.Log("AUDIO clips asignados.");
        }

        private static void SetMusic(Sound sound, string name, float volume)
        {
            var importer = AssetImporter.GetAtPath(Folder + name + ".ogg") as AudioImporter
                ?? throw new InvalidOperationException("Missing clip: " + Folder + name + ".ogg");
            var settings = importer.defaultSampleSettings;
            if (settings.loadType != AudioClipLoadType.Streaming)
            {
                settings.loadType = AudioClipLoadType.Streaming;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            Set(sound, name, volume);
        }

        // Missing files stop the run before anything is saved.
        private static void Set(Sound sound, string name, float volume)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + name + ".ogg")
                ?? throw new InvalidOperationException("Missing clip: " + Folder + name + ".ogg");
            sound.clips = new[] { clip };
            sound.volume = volume;
        }
    }
}
