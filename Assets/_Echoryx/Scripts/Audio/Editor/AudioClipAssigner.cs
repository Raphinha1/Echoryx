using System;
using UnityEditor;
using UnityEngine;

namespace Echoryx.Audio.Editor
{
    /// <summary>
    /// Assigns the clips in Assets/_Echoryx/Audio to the AudioLibrary with their volumes. Overwrites those
    /// entries; music entries are left as they are.
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
            Set(library.rewind, "rewind", 0.55f);
            Set(library.checkpoint, "checkpoint", 0.75f);
            Set(library.goal, "meta", 0.85f);
            Set(library.uiClick, "Clic", 0.4f);
            Set(library.uiHover, "Clic", 0.15f);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();

            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
                if (library.Get(id).Pick() == null) Debug.Log("AUDIO sin clip: " + id);
            Debug.Log("AUDIO clips asignados.");
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
