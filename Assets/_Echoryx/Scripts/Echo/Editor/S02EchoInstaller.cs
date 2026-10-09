using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoryx.Echo.Editor
{
    /// <summary>Uses Unity APIs to add the action and update the existing prefab.</summary>
    public static class S02EchoInstaller
    {
        private const string ActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string PrefabPath = "Assets/_Echoryx/Prefabs/Player.prefab";

        [MenuItem("Echoryx/S02-A/Install Echo Recorder")]
        public static void Install()
        {
            if (!File.Exists(ActionsPath))
                throw new FileNotFoundException("The existing Input Actions asset is missing.", ActionsPath);

            var editable = InputActionAsset.FromJson(File.ReadAllText(ActionsPath));
            try
            {
                var playerMap = editable.FindActionMap("Player", true);
                var action = playerMap.FindAction("EchoRecord");
                if (action == null)
                {
                    playerMap.AddAction(
                        "EchoRecord",
                        InputActionType.Button,
                        binding: "<Keyboard>/q",
                        groups: "Keyboard&Mouse",
                        expectedControlLayout: "Button");
                    File.WriteAllText(ActionsPath, editable.ToJson());
                    AssetDatabase.ImportAsset(ActionsPath, ImportAssetOptions.ForceUpdate);
                }
                else if (!HasKeyboardQBinding(action))
                {
                    throw new InvalidOperationException(
                        "Player/EchoRecord exists without a Q binding. Review it in Input Actions.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editable);
            }

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Player prefab is missing.");
            try
            {
                if (prefab.GetComponent<EchoRecorder>() == null)
                    prefab.AddComponent<EchoRecorder>();

                var recordInput = prefab.GetComponent<EchoRecordInput>();
                if (recordInput == null)
                    recordInput = prefab.AddComponent<EchoRecordInput>();
                var serialized = new SerializedObject(recordInput);
                serialized.FindProperty("actions").objectReferenceValue = actions;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                if (PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath) == null)
                    throw new InvalidOperationException("Could not save Player prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("S02-A EchoRecord Q action and Player prefab recorder installed.");
        }

        private static bool HasKeyboardQBinding(InputAction action)
        {
            foreach (var binding in action.bindings)
                if (binding.path == "<Keyboard>/q")
                    return true;
            return false;
        }
    }
}
