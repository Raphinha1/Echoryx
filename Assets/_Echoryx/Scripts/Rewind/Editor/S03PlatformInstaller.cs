using System;
using Echoryx.Player;
using Echoryx.Rewind.Platforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoryx.Rewind.Editor
{
    /// <summary>Builds the S03-C crossing with Unity scene and prefab APIs.</summary>
    public static class S03PlatformInstaller
    {
        private const string PlayerPath = "Assets/_Echoryx/Prefabs/Player.prefab";
        private const string ScenePath = "Assets/_Echoryx/Scenes/Prototype.unity";
        private const string MaterialPath = "Assets/_Echoryx/Materials/RewindPlatform.mat";

        [MenuItem("Echoryx/S03-C/Install Rewind Crossing")]
        public static void Install()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EnsurePlayerCarrier();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var player = GameObject.Find("Player");
            if (player == null || GameObject.Find("S01 Test Circuit") == null
                || GameObject.Find("S02-C Echo Puzzle") == null)
                throw new InvalidOperationException("Prototype is missing its earlier content.");
            var existing = GameObject.Find("S03-C Rewind Crossing");
            if (existing != null)
            {
                ConfigureFinalCrossing(existing.transform);
                SaveCrossing(scene);
                Debug.Log("S03-C crossing updated to the one-way Rewind layout.");
                return;
            }

            var carrier = player.GetComponent<PlayerPlatformCarrier>();
            if (carrier == null)
                throw new InvalidOperationException("Player prefab instance lacks PlayerPlatformCarrier.");
            var root = new GameObject("S03-C Rewind Crossing");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.position = new Vector3(0f, 0f, -20f);
            var parent = root.transform;
            AddBlock(parent, "Approach Floor", new Vector3(5.5f, -0.5f, 8.5f),
                new Vector3(3f, 1f, 17f));
            AddBlock(parent, "Departure Dock", new Vector3(6.5f, -0.5f, 0f),
                new Vector3(3f, 1f, 4f));
            AddBlock(parent, "Arrival Dock", new Vector3(18f, -0.5f, 0f),
                new Vector3(3f, 1f, 4f));
            AddBlock(parent, "Arrival Marker", new Vector3(18.75f, 0.75f, 0f),
                new Vector3(0.35f, 1.5f, 0.35f));

            var start = new GameObject("Platform Start");
            start.transform.SetParent(parent, false);
            start.transform.localPosition = new Vector3(8.5f, 0.05f, 0f);
            var end = new GameObject("Platform End");
            end.transform.SetParent(parent, false);
            end.transform.localPosition = new Vector3(15.5f, 0.05f, 0f);

            var platform = new GameObject("Rewind Moving Platform");
            platform.transform.SetParent(parent, false);
            platform.transform.localPosition = start.transform.localPosition;
            var visual = AddBlock(platform.transform, "Platform Surface and Collider",
                Vector3.zero, new Vector3(2.4f, 0.4f, 2.5f));
            visual.GetComponent<Renderer>().sharedMaterial = EnsureMaterial();
            var mechanism = platform.AddComponent<RewindMovingPlatform>();
            var data = new SerializedObject(mechanism);
            data.FindProperty("startPoint").objectReferenceValue = start.transform;
            data.FindProperty("endPoint").objectReferenceValue = end.transform;
            data.FindProperty("surface").objectReferenceValue = visual.GetComponent<BoxCollider>();
            data.FindProperty("passenger").objectReferenceValue = carrier;
            data.FindProperty("speed").floatValue = 3.5f;
            data.FindProperty("rewindSpeed").floatValue = 1.5f;
            data.ApplyModifiedPropertiesWithoutUndo();

            SaveCrossing(scene);
            Debug.Log("S03-C moving platform and crossing installed in Prototype.");
        }

        private static void ConfigureFinalCrossing(Transform root)
        {
            root.position = new Vector3(0f, 0f, -20f);
            if (root.Find("Approach Floor") == null)
                AddBlock(root, "Approach Floor", new Vector3(5.5f, -0.5f, 8.5f),
                    new Vector3(3f, 1f, 17f));
            var end = root.Find("Platform End");
            var dock = root.Find("Arrival Dock");
            var marker = root.Find("Arrival Marker");
            var mechanism = root.GetComponentInChildren<RewindMovingPlatform>();
            if (end == null || dock == null || marker == null || mechanism == null)
                throw new InvalidOperationException("S03-C crossing is incomplete; cannot update it safely.");

            end.localPosition = new Vector3(15.5f, 0.05f, 0f);
            dock.localPosition = new Vector3(18f, -0.5f, 0f);
            marker.localPosition = new Vector3(18.75f, 0.75f, 0f);
            var data = new SerializedObject(mechanism);
            data.FindProperty("speed").floatValue = 3.5f;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SaveCrossing(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save Prototype.");
            AssetDatabase.SaveAssets();
        }

        private static void EnsurePlayerCarrier()
        {
            var prefab = PrefabUtility.LoadPrefabContents(PlayerPath);
            if (prefab == null) throw new InvalidOperationException("Player prefab is missing.");
            try
            {
                if (prefab.GetComponent<PlayerPlatformCarrier>() == null)
                    prefab.AddComponent<PlayerPlatformCarrier>();
                if (PrefabUtility.SaveAsPrefabAsset(prefab, PlayerPath) == null)
                    throw new InvalidOperationException("Could not save Player prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static GameObject AddBlock(Transform parent, string name,
            Vector3 localPosition, Vector3 localScale)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = localScale;
            return block;
        }

        private static Material EnsureMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("URP Unlit shader is unavailable.");
            var material = new Material(shader) { name = "RewindPlatform" };
            material.SetColor("_BaseColor", new Color(0.08f, 0.75f, 0.63f));
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }
    }
}
