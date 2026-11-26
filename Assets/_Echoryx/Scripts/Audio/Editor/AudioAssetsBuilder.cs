using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Echoryx.Audio.Editor
{
    /// <summary>
    /// Creates the mixer (Master, Music, SFX with exposed volumes) and the empty AudioLibrary once.
    /// Unity has no public API to author mixers, so this uses the Editor's internal AudioMixerController.
    /// Existing assets are never overwritten.
    /// </summary>
    public static class AudioAssetsBuilder
    {
        public const string MixerPath = "Assets/_Echoryx/Audio/EchoryxMixer.mixer";
        public const string LibraryPath = "Assets/_Echoryx/Resources/AudioLibrary.asset";
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem("Echoryx/Audio/Create Mixer and Library")]
        public static void Build()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath) ?? CreateMixer();
            if (AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath) == null)
            {
                var library = ScriptableObject.CreateInstance<AudioLibrary>();
                library.musicGroup = mixer.FindMatchingGroups("Master/Music").Single();
                library.sfxGroup = mixer.FindMatchingGroups("Master/SFX").Single();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Audio assets ready.");
        }

        private static AudioMixer CreateMixer()
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var controllerType = assembly.GetType("UnityEditor.Audio.AudioMixerController", true);
            var groupType = assembly.GetType("UnityEditor.Audio.AudioMixerGroupController", true);
            var parameterType = assembly.GetType("UnityEditor.Audio.ExposedAudioParameter", true);
            var controller = controllerType.GetMethod("CreateMixerControllerAtPath", Any).Invoke(null, new object[] { MixerPath });
            var master = controllerType.GetProperty("masterGroup", Any).GetValue(controller);

            object NewGroup(string name)
            {
                var group = controllerType.GetMethod("CreateNewGroup", Any).Invoke(controller, new object[] { name, false });
                controllerType.GetMethod("AddChildToParent", Any).Invoke(controller, new[] { group, master });
                return group;
            }

            var groups = new[] { master, NewGroup("Music"), NewGroup("SFX") };
            var names = new[] { "MasterVolume", "MusicVolume", "SFXVolume" };
            var exposed = Array.CreateInstance(parameterType, groups.Length);
            for (var i = 0; i < groups.Length; i++)
            {
                var parameter = Activator.CreateInstance(parameterType);
                parameterType.GetField("guid", Any).SetValue(parameter, groupType.GetMethod("GetGUIDForVolume", Any).Invoke(groups[i], null));
                parameterType.GetField("name", Any).SetValue(parameter, names[i]);
                exposed.SetValue(parameter, i);
            }
            controllerType.GetProperty("exposedParameters", Any).SetValue(controller, exposed);
            // Creates the default mixer-window view, listing every group, if there is none.
            controllerType.GetMethod("SanitizeGroupViews", Any)?.Invoke(controller, null);
            EditorUtility.SetDirty((UnityEngine.Object)controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(MixerPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        }
    }
}
