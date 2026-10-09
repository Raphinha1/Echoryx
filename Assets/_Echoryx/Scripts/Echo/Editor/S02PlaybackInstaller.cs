using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Echoryx.Echo.Editor
{
    /// <summary>Creates the S02-B assets through Unity importers and PrefabUtility.</summary>
    public static class S02PlaybackInstaller
    {
        private const string ActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string PlayerPath = "Assets/_Echoryx/Prefabs/Player.prefab";
        private const string ActorPath = "Assets/_Echoryx/Prefabs/EchoActor.prefab";
        private const string MaterialPath = "Assets/_Echoryx/Materials/EchoHologram.mat";

        [MenuItem("Echoryx/S02-B/Install Echo Playback")]
        public static void Install()
        {
            EnsureAction();
            var material = EnsureMaterial();
            var actor = EnsureActorPrefab(material);
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);

            var player = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                if (player.GetComponent<EchoRecorder>() == null)
                    throw new InvalidOperationException("S02-A EchoRecorder is missing from Player.");

                var playback = player.GetComponent<EchoPlayback>();
                if (playback == null)
                    playback = player.AddComponent<EchoPlayback>();
                var playbackData = new SerializedObject(playback);
                playbackData.FindProperty("actorPrefab").objectReferenceValue = actor;
                playbackData.ApplyModifiedPropertiesWithoutUndo();

                var input = player.GetComponent<EchoPlaybackInput>();
                if (input == null)
                    input = player.AddComponent<EchoPlaybackInput>();
                var inputData = new SerializedObject(input);
                inputData.FindProperty("actions").objectReferenceValue = actions;
                inputData.ApplyModifiedPropertiesWithoutUndo();

                if (PrefabUtility.SaveAsPrefabAsset(player, PlayerPath) == null)
                    throw new InvalidOperationException("Could not save Player prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("S02-B EchoPlay E action, hologram prefab and Player playback installed.");
        }

        private static void EnsureAction()
        {
            if (!File.Exists(ActionsPath))
                throw new FileNotFoundException("The existing Input Actions asset is missing.", ActionsPath);

            var editable = InputActionAsset.FromJson(File.ReadAllText(ActionsPath));
            try
            {
                var map = editable.FindActionMap("Player", true);
                var action = map.FindAction("EchoPlay");
                if (action == null)
                {
                    map.AddAction("EchoPlay", InputActionType.Button,
                        binding: "<Keyboard>/e",
                        groups: "Keyboard&Mouse",
                        expectedControlLayout: "Button");
                    File.WriteAllText(ActionsPath, editable.ToJson());
                    AssetDatabase.ImportAsset(ActionsPath, ImportAssetOptions.ForceUpdate);
                }
                else
                {
                    var hasE = false;
                    foreach (var binding in action.bindings)
                        hasE |= binding.path == "<Keyboard>/e";
                    if (!hasE)
                        throw new InvalidOperationException(
                            "Player/EchoPlay exists without an E binding. Review Input Actions.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editable);
            }
        }

        private static Material EnsureMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("URP Unlit shader is unavailable.");

            var material = new Material(shader) { name = "EchoHologram" };
            material.SetColor("_BaseColor", new Color(0.05f, 0.9f, 1f, 0.42f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static EchoActor EnsureActorPrefab(Material material)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ActorPath);
            if (existing != null)
            {
                var actor = existing.GetComponent<EchoActor>();
                if (actor == null)
                    throw new InvalidOperationException("EchoActor prefab lacks its identity component.");
                return actor;
            }

            var root = new GameObject("EchoActor");
            var identity = root.AddComponent<EchoActor>();
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Hologram Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.up;
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            var renderer = visual.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var saved = PrefabUtility.SaveAsPrefabAsset(root, ActorPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (saved == null)
                throw new InvalidOperationException("Could not save EchoActor prefab.");
            return saved.GetComponent<EchoActor>();
        }
    }
}
