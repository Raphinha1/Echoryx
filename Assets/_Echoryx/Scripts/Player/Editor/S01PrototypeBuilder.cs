using System;
using Echoryx.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Echoryx.Player.Editor
{
    /// <summary>Creates Unity-owned prefab and scene assets; never hand-writes their YAML.</summary>
    public static class S01PrototypeBuilder
    {
        private const string PrefabPath = "Assets/_Echoryx/Prefabs/Player.prefab";
        private const string ScenePath = "Assets/_Echoryx/Scenes/Prototype.unity";
        private const string InputPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Echoryx/S01/Build Player and Prototype")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (actions == null)
                throw new InvalidOperationException("Existing InputSystem_Actions asset was not found.");

            var prefab = EnsurePlayerPrefab(actions);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (GameObject.Find("S01 Test Circuit") != null || GameObject.Find("Player") != null)
                throw new InvalidOperationException("Prototype already has S01 content. Review it before rebuilding.");

            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            var anchor = player.transform.Find("CameraAnchor");
            var input = player.GetComponent<PlayerInputReader>();

            var rig = new GameObject("Camera Orbit Target");
            SceneManager.MoveGameObjectToScene(rig, scene);
            var orbit = rig.AddComponent<PlayerCameraOrbit>();
            orbit.SetTargets(anchor, input);
            rig.transform.SetPositionAndRotation(anchor.position, Quaternion.Euler(12f, 0f, 0f));
            player.GetComponent<PlayerMotor>().SetCameraReference(rig.transform);

            var mainCamera = Camera.main;
            if (mainCamera == null)
                throw new InvalidOperationException("Prototype needs its existing Main Camera.");
            if (mainCamera.GetComponent<CinemachineBrain>() == null)
                mainCamera.gameObject.AddComponent<CinemachineBrain>();

            var virtualCamera = new GameObject("Player Cinemachine Camera");
            SceneManager.MoveGameObjectToScene(virtualCamera, scene);
            var cmCamera = virtualCamera.AddComponent<CinemachineCamera>();
            cmCamera.Follow = rig.transform;
            var follow = virtualCamera.AddComponent<CinemachineThirdPersonFollow>();
            follow.Damping = new Vector3(0.1f, 0.15f, 0.1f);
            follow.ShoulderOffset = new Vector3(0.35f, -0.25f, 0f);
            follow.VerticalArmLength = 0.25f;
            follow.CameraSide = 0.5f;
            follow.CameraDistance = 4f;
            var obstacles = follow.AvoidObstacles;
            obstacles.Enabled = true;
            obstacles.CollisionFilter = LayerMask.GetMask("Default");
            obstacles.IgnoreTag = "Player";
            obstacles.CameraRadius = 0.2f;
            obstacles.DampingIntoCollision = 0f;
            obstacles.DampingFromCollision = 0.25f;
            follow.AvoidObstacles = obstacles;

            var circuit = new GameObject("S01 Test Circuit");
            SceneManager.MoveGameObjectToScene(circuit, scene);
            AddBlock(circuit.transform, "Start Floor", new Vector3(0, -0.5f, 0), new Vector3(10, 1, 10));
            AddBlock(circuit.transform, "Low Obstacle", new Vector3(-1.5f, 0.65f, 2), new Vector3(1.3f, 1.3f, 1));
            AddBlock(circuit.transform, "Side Obstacle", new Vector3(2.5f, 1f, 3), new Vector3(1, 2, 1));
            AddBlock(circuit.transform, "Platform 1", new Vector3(0, 0.55f, 7), new Vector3(4, 1.1f, 3));
            AddBlock(circuit.transform, "Platform 2", new Vector3(1, 1.1f, 11), new Vector3(4, 1, 3));
            AddBlock(circuit.transform, "Platform 3", new Vector3(-0.5f, 1.7f, 15), new Vector3(4, 1, 3));
            AddBlock(circuit.transform, "Finish Floor", new Vector3(0, 1.7f, 19), new Vector3(7, 1, 5));
            AddBlock(circuit.transform, "Finish Marker", new Vector3(0, 3.2f, 20), new Vector3(0.4f, 2, 0.4f));

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save Prototype.");
            AssetDatabase.SaveAssets();
            Debug.Log("S01 Player prefab and Prototype circuit created.");
        }

        private static GameObject EnsurePlayerPrefab(InputActionAsset actions)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
                return existing;

            var player = new GameObject("Player");
            player.tag = "Player";
            var controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0f, 1f, 0f);
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45f;

            var input = player.AddComponent<PlayerInputReader>();
            var serializedInput = new SerializedObject(input);
            serializedInput.FindProperty("actions").objectReferenceValue = actions;
            serializedInput.ApplyModifiedPropertiesWithoutUndo();
            player.AddComponent<PlayerMotor>();

            var visuals = new GameObject("VisualRoot");
            visuals.transform.SetParent(player.transform, false);
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Capsule Placeholder";
            capsule.transform.SetParent(visuals.transform, false);
            capsule.transform.localPosition = Vector3.up;
            UnityEngine.Object.DestroyImmediate(capsule.GetComponent<Collider>());

            var anchor = new GameObject("CameraAnchor");
            anchor.transform.SetParent(player.transform, false);
            anchor.transform.localPosition = new Vector3(0f, 1.5f, 0f);

            var saved = PrefabUtility.SaveAsPrefabAsset(player, PrefabPath);
            UnityEngine.Object.DestroyImmediate(player);
            if (saved == null)
                throw new InvalidOperationException("Could not save Player prefab.");
            return saved;
        }

        private static void AddBlock(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            block.isStatic = true;
        }
    }
}
