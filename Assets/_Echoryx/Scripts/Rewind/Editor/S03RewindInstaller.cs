using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Echoryx.Rewind.Editor
{
    /// <summary>Uses Unity APIs for the RMB action, Player wiring and test rotor.</summary>
    public static class S03RewindInstaller
    {
        private const string ActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string PlayerPath = "Assets/_Echoryx/Prefabs/Player.prefab";
        private const string ScenePath = "Assets/_Echoryx/Scenes/Prototype.unity";
        private const string MaterialPath = "Assets/_Echoryx/Materials/RewindTestRotor.mat";

        [MenuItem("Echoryx/S03-B/Install Rewind Interaction")]
        public static void Install()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureAction();
            EnsurePlayer();
            EnsureTestRotor();
            AssetDatabase.SaveAssets();
            Debug.Log("S03-B RMB Rewind interaction and test rotor installed.");
        }

        private static void EnsureAction()
        {
            if (!File.Exists(ActionsPath))
                throw new FileNotFoundException("Existing Input Actions asset is missing.", ActionsPath);
            var editable = InputActionAsset.FromJson(File.ReadAllText(ActionsPath));
            try
            {
                var map = editable.FindActionMap("Player", true);
                var action = map.FindAction("Rewind");
                if (action == null)
                {
                    map.AddAction("Rewind", InputActionType.Button,
                        binding: "<Mouse>/rightButton", groups: "Keyboard&Mouse",
                        expectedControlLayout: "Button");
                    File.WriteAllText(ActionsPath, editable.ToJson());
                    AssetDatabase.ImportAsset(ActionsPath, ImportAssetOptions.ForceUpdate);
                }
                else
                {
                    var hasRmb = false;
                    foreach (var binding in action.bindings)
                        hasRmb |= binding.path == "<Mouse>/rightButton";
                    if (!hasRmb)
                        throw new InvalidOperationException(
                            "Player/Rewind exists without RMB. Review Input Actions.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editable);
            }
        }

        private static void EnsurePlayer()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            var prefab = PrefabUtility.LoadPrefabContents(PlayerPath);
            if (prefab == null) throw new InvalidOperationException("Player prefab is missing.");
            try
            {
                var interactor = prefab.GetComponent<RewindInteractor>();
                if (interactor == null) interactor = prefab.AddComponent<RewindInteractor>();
                var data = new SerializedObject(interactor);
                data.FindProperty("actions").objectReferenceValue = actions;
                data.ApplyModifiedPropertiesWithoutUndo();
                if (PrefabUtility.SaveAsPrefabAsset(prefab, PlayerPath) == null)
                    throw new InvalidOperationException("Could not save Player prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static void EnsureTestRotor()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (GameObject.Find("S01 Test Circuit") == null || GameObject.Find("Player") == null)
                throw new InvalidOperationException("Prototype lacks S01 content.");
            if (GameObject.Find("S03-B Rewind Test Rotor") != null) return;

            var rotor = new GameObject("S03-B Rewind Test Rotor");
            SceneManager.MoveGameObjectToScene(rotor, scene);
            rotor.transform.position = new Vector3(3f, 1.8f, -2f);
            rotor.AddComponent<RewindTestRotor>();
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Rotor Visual and Collider";
            visual.transform.SetParent(rotor.transform, false);
            visual.transform.localScale = new Vector3(1.8f, 0.3f, 0.5f);
            visual.GetComponent<Renderer>().sharedMaterial = EnsureMaterial();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save Prototype.");
        }

        private static Material EnsureMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("URP Unlit shader is unavailable.");
            var material = new Material(shader) { name = "RewindTestRotor" };
            material.SetColor("_BaseColor", new Color(0.55f, 0.28f, 0.9f));
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }
    }
}
