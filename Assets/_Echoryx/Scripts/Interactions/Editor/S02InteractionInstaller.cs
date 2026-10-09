using System;
using Echoryx.Echo;
using Echoryx.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoryx.Interactions.Editor
{
    /// <summary>Adds S02-C prefab identities and the puzzle with Unity's asset APIs.</summary>
    public static class S02InteractionInstaller
    {
        private const string PlayerPath = "Assets/_Echoryx/Prefabs/Player.prefab";
        private const string EchoPath = "Assets/_Echoryx/Prefabs/EchoActor.prefab";
        private const string ScenePath = "Assets/_Echoryx/Scenes/Prototype.unity";
        private const string PlateMaterialPath = "Assets/_Echoryx/Materials/PressurePlate.mat";
        private const string DoorMaterialPath = "Assets/_Echoryx/Materials/EchoDoor.mat";

        [MenuItem("Echoryx/S02-C/Install Echo Puzzle")]
        public static void Install()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureActor(PlayerPath, MechanismActorKind.Player, typeof(PlayerMotor));
            EnsureActor(EchoPath, MechanismActorKind.Echo, typeof(EchoActor));

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (GameObject.Find("S01 Test Circuit") == null || GameObject.Find("Player") == null)
                throw new InvalidOperationException("Prototype lacks the S01 circuit or Player.");
            if (GameObject.Find("S02-C Echo Puzzle") != null)
            {
                Debug.Log("S02-C puzzle already exists; no scene objects were duplicated.");
                return;
            }

            var plateMaterial = EnsureMaterial(PlateMaterialPath, new Color(0.85f, 0.28f, 0.08f));
            var doorMaterial = EnsureMaterial(DoorMaterialPath, new Color(0.15f, 0.45f, 0.85f));
            var root = new GameObject("S02-C Echo Puzzle");
            SceneManager.MoveGameObjectToScene(root, scene);
            var parent = root.transform;

            AddBlock(parent, "Connector Floor", new Vector3(-5.75f, -0.5f, 0f),
                new Vector3(2f, 1f, 4f));
            AddBlock(parent, "Puzzle Floor", new Vector3(-10f, -0.5f, 4f),
                new Vector3(8f, 1f, 14f));
            AddBlock(parent, "Left Door Wall", new Vector3(-12.625f, 1.75f, 5f),
                new Vector3(2.75f, 3.5f, 0.8f));
            AddBlock(parent, "Right Door Wall", new Vector3(-7.375f, 1.75f, 5f),
                new Vector3(2.75f, 3.5f, 0.8f));
            AddBlock(parent, "Left Corridor Wall", new Vector3(-14.15f, 1.75f, 4.5f),
                new Vector3(0.3f, 3.5f, 13f));
            AddBlock(parent, "Right Corridor Wall", new Vector3(-5.85f, 1.75f, 6.5f),
                new Vector3(0.3f, 3.5f, 9f));
            AddBlock(parent, "Exit Marker", new Vector3(-10f, 0.75f, 9f),
                new Vector3(0.35f, 1.5f, 0.35f));

            var plate = new GameObject("Echo Pressure Plate");
            plate.transform.SetParent(parent, false);
            plate.transform.localPosition = new Vector3(-10f, 0f, 0f);
            var plateSurface = AddBlock(plate.transform, "Plate Surface",
                new Vector3(0f, 0.075f, 0f), new Vector3(2f, 0.15f, 2f));
            plateSurface.GetComponent<Renderer>().sharedMaterial = plateMaterial;
            var pressurePlate = plate.AddComponent<PressurePlate>();
            var plateData = new SerializedObject(pressurePlate);
            plateData.FindProperty("indicator").objectReferenceValue = plateSurface.GetComponent<Renderer>();
            plateData.ApplyModifiedPropertiesWithoutUndo();

            var doorRoot = new GameObject("Echo Door");
            doorRoot.transform.SetParent(parent, false);
            doorRoot.transform.localPosition = new Vector3(-10f, 0f, 5f);
            var panel = AddBlock(doorRoot.transform, "Door Panel", new Vector3(0f, 1.75f, 0f),
                new Vector3(2.5f, 3.5f, 0.8f));
            panel.GetComponent<Renderer>().sharedMaterial = doorMaterial;
            var threshold = new GameObject("Door Threshold");
            threshold.transform.SetParent(doorRoot.transform, false);
            threshold.transform.localPosition = new Vector3(0f, 1f, 0f);
            var door = doorRoot.AddComponent<DoorController>();
            var doorData = new SerializedObject(door);
            doorData.FindProperty("plate").objectReferenceValue = pressurePlate;
            doorData.FindProperty("doorPanel").objectReferenceValue = panel.transform;
            doorData.FindProperty("threshold").objectReferenceValue = threshold.transform;
            doorData.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save Prototype.");
            AssetDatabase.SaveAssets();
            Debug.Log("S02-C Echo puzzle installed in Prototype.");
        }

        private static void EnsureActor(string path, MechanismActorKind kind, Type expected)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing prefab: " + path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent(expected) == null)
                    throw new InvalidOperationException(path + " is missing " + expected.Name);
                var actor = root.GetComponent<MechanismActor>();
                if (actor == null) actor = root.AddComponent<MechanismActor>();
                var data = new SerializedObject(actor);
                data.FindProperty("kind").enumValueIndex = (int)kind;
                data.ApplyModifiedPropertiesWithoutUndo();
                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                    throw new InvalidOperationException("Could not save " + path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Material EnsureMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("URP Unlit shader is unavailable.");
            var material = new Material(shader);
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject AddBlock(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            return block;
        }
    }
}
