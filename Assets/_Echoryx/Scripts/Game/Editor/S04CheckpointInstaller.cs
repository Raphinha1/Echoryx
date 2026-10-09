using System;
using System.IO;
using Echoryx.Player;
using Echoryx.Rewind;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace Echoryx.Game.Editor
{
    public static class S04CheckpointInstaller
    {
        private const string ActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string ScenePath = "Assets/_Echoryx/Scenes/Prototype.unity";
        // Keep the entire reset approach longer than the platform's two-second initial trip.
        private static readonly Vector3 RoomBEntrance = new Vector3(5.5f, 0f, -4f);

        [MenuItem("Echoryx/S04/Fix Room B Reset Access")]
        public static void FixRoomBResetAccess()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var room = GameObject.Find("S04-A Checkpoints")?.transform.Find("Room B");
            var checkpoint = room?.Find("Checkpoint Room B")?.GetComponent<CheckpointTrigger>();
            var approach = room?.Find("S03-C Rewind Crossing/Approach Floor")?.GetComponent<BoxCollider>();
            if (checkpoint == null || checkpoint.SpawnPoint == null || approach == null
                || checkpoint.Room != room.GetComponent<RoomResetScope>())
                throw new InvalidOperationException("Room B checkpoint, scope or approach is missing.");
            Physics.SyncTransforms();
            var footprint = RoomBEntrance;
            footprint.y = approach.bounds.center.y;
            if (!approach.bounds.Contains(footprint))
                throw new InvalidOperationException("Room B entrance must be on its existing approach floor.");
            checkpoint.transform.position = RoomBEntrance;
            checkpoint.SpawnPoint.position = RoomBEntrance + Vector3.up * 0.05f;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Prototype.");
            Debug.Log("Room B checkpoint and respawn moved to the approach entrance; mechanisms and other rooms preserved.");
        }

        [MenuItem("Echoryx/S04-A/Install Checkpoints")]
        public static void Install()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureRestartAction();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var existing = GameObject.Find("S04-A Checkpoints");
            if (existing != null)
            {
                SetCameraReferences(existing.GetComponent<CheckpointManager>());
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("S04-A already installed; no scene content duplicated.");
                return;
            }
            var playerObject = GameObject.Find("Player");
            if (playerObject == null || GameObject.Find("S01 Test Circuit") == null
                || GameObject.Find("S02-C Echo Puzzle") == null || GameObject.Find("S03-C Rewind Crossing") == null)
                throw new InvalidOperationException("Prototype is missing existing gameplay content.");

            var root = new GameObject("S04-A Checkpoints");
            var manager = root.AddComponent<CheckpointManager>();
            var initial = NewChild(root.transform, "Initial Room").AddComponent<RoomResetScope>();
            var initialSpawn = NewChild(initial.transform, "Initial Spawn").transform;
            initialSpawn.SetPositionAndRotation(playerObject.transform.position, playerObject.transform.rotation);
            var release = root.AddComponent<RewindAttemptRelease>();
            SetReference(release, "interactor", playerObject.GetComponent<RewindInteractor>());
            SetReference(manager, "player", playerObject.GetComponent<PlayerMotor>());
            SetReference(manager, "initialSpawn", initialSpawn);
            SetReference(manager, "initialRoom", initial);
            SetReference(manager, "rewindRelease", release);
            SetCameraReferences(manager);

            var material = EnsureMaterial();
            CreateCheckpointRoom(root.transform, manager, "Room A", new Vector3(0f, 0f, 4f),
                new Vector3(0f, 0.05f, 3.8f), new Vector3(4f, 0.4f, 4f), material);
            CreateCheckpointRoom(root.transform, manager, "Room B", RoomBEntrance,
                RoomBEntrance + Vector3.up * 0.05f, new Vector3(5.4f, 0.4f, -21.4f), material);

            var danger = NewChild(root.transform, "Fall Respawn Zone");
            danger.transform.position = new Vector3(0f, -8f, 0f);
            var dangerCollider = danger.AddComponent<BoxCollider>();
            dangerCollider.isTrigger = true;
            dangerCollider.size = new Vector3(100f, 6f, 100f);
            SetReference(danger.AddComponent<RespawnZone>(), "manager", manager);

            var restart = root.AddComponent<RestartInput>();
            SetReference(restart, "actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath));
            SetReference(restart, "manager", manager);
            SetReference(restart, "input", playerObject.GetComponent<PlayerInputReader>());
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Prototype.");
            AssetDatabase.SaveAssets();
            Debug.Log("S04-A checkpoints, scoped reset probes, Restart/R and fall zone installed.");
        }

        private static void CreateCheckpointRoom(Transform root, CheckpointManager manager, string name,
            Vector3 checkpointPosition, Vector3 spawnPosition, Vector3 probePosition, Material material)
        {
            var room = NewChild(root, name).AddComponent<RoomResetScope>();
            var spawn = NewChild(room.transform, "Respawn Point").transform;
            spawn.position = spawnPosition;
            var checkpoint = NewChild(room.transform, "Checkpoint " + name);
            checkpoint.transform.position = checkpointPosition;
            var volume = checkpoint.AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.center = Vector3.up * 0.75f;
            volume.size = new Vector3(2f, 2f, 1.5f);
            var trigger = checkpoint.AddComponent<CheckpointTrigger>();
            SetReference(trigger, "manager", manager);
            SetReference(trigger, "room", room);
            SetReference(trigger, "spawnPoint", spawn);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Checkpoint Marker";
            marker.transform.SetParent(checkpoint.transform, false);
            marker.transform.localPosition = Vector3.up * 0.025f;
            marker.transform.localScale = new Vector3(1.6f, 0.05f, 1.2f);
            UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.GetComponent<Renderer>().sharedMaterial = material;
            SetReference(trigger, "indicator", marker.GetComponent<Renderer>());
            trigger.SetActive(false);

            var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            probe.name = "Reset Probe " + name;
            probe.transform.SetParent(room.transform, false);
            probe.transform.position = probePosition;
            probe.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
            var participant = probe.AddComponent<AttemptResetTransform>();
            var data = new SerializedObject(room);
            var participants = data.FindProperty("participants");
            participants.arraySize = 1;
            participants.GetArrayElementAtIndex(0).objectReferenceValue = participant;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject NewChild(Transform parent, string name)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            return item;
        }

        private static void SetReference(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(property).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetCameraReferences(CheckpointManager manager)
        {
            SetReference(manager, "cameraOrbit", GameObject.Find("Camera Orbit Target").GetComponent<PlayerCameraOrbit>());
            SetReference(manager, "gameplayCamera", GameObject.Find("Player Cinemachine Camera").GetComponent<CinemachineCamera>());
        }

        private static Material EnsureMaterial()
        {
            const string path = "Assets/_Echoryx/Materials/Checkpoint.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("URP Unlit shader unavailable.");
            material = new Material(shader) { name = "Checkpoint" };
            material.SetColor("_BaseColor", new Color(1f, 0.65f, 0.15f));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureRestartAction()
        {
            var editable = InputActionAsset.FromJson(File.ReadAllText(ActionsPath));
            try
            {
                var map = editable.FindActionMap("Player", true);
                var restart = map.FindAction("Restart");
                if (restart == null)
                {
                    map.AddAction("Restart", InputActionType.Button, "<Keyboard>/r",
                        groups: "Keyboard&Mouse", expectedControlLayout: "Button");
                    File.WriteAllText(ActionsPath, editable.ToJson());
                    AssetDatabase.ImportAsset(ActionsPath, ImportAssetOptions.ForceUpdate);
                }
                else
                {
                    var bound = false;
                    foreach (var binding in restart.bindings) bound |= binding.path == "<Keyboard>/r";
                    if (!bound) throw new InvalidOperationException("Player/Restart exists without R binding.");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(editable); }
        }
    }
}
