using System;
using Echoryx.Interactions;
using Echoryx.Player;
using Echoryx.Rewind.Platforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoryx.Game.Editor
{
    /// <summary>Adds an enclosed two-stage room using existing gameplay and reset components.</summary>
    public static class S04CombinedPuzzleInstaller
    {
        [MenuItem("Echoryx/S04-C/Install Combined Puzzle")]
        public static void Install()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/_Echoryx/Scenes/Prototype.unity", OpenSceneMode.Single);
            if (GameObject.Find("S04-C Combined Puzzle") != null)
            {
                Debug.Log("S04-C already exists; no content replaced or duplicated.");
                return;
            }
            var checkpoints = GameObject.Find("S04-A Checkpoints");
            var player = GameObject.Find("Player");
            if (checkpoints == null || checkpoints.GetComponent<EchoAttemptReset>() == null
                || player == null || player.GetComponent<PlayerPlatformCarrier>() == null
                || GameObject.Find("S03-C Rewind Crossing") == null)
                throw new InvalidOperationException("Install S01-S04-B before the combined room.");
            var manager = checkpoints.GetComponent<CheckpointManager>();
            var plateMaterial = MaterialAt("PressurePlate");
            var doorMaterial = MaterialAt("EchoDoor");
            var platformMaterial = MaterialAt("RewindPlatform");
            var checkpointMaterial = MaterialAt("Checkpoint");
            var exitMaterial = EnsureExitMaterial();
            var root = NewChild(checkpoints.transform, "S04-C Combined Puzzle");
            root.transform.position = new Vector3(22f, 0f, 0f);
            var scope = root.AddComponent<RoomResetScope>();
            var parent = root.transform;

            // Only the entrance has an opening; high solid walls/roof prevent jumps around either phase.
            Block(parent, "Entrance Bridge", new Vector3(-9.5f, -0.5f, -1f), new Vector3(16f, 1f, 3f));
            Block(parent, "Entry Floor", new Vector3(0f, -0.5f, 5.5f), new Vector3(5f, 1f, 16f));
            Block(parent, "Safe Departure Floor", new Vector3(0f, -0.5f, 15.75f), new Vector3(5f, 1f, 4.5f));
            Block(parent, "Arrival Floor", new Vector3(0f, -0.5f, 29.5f), new Vector3(5f, 1f, 6f));
            Block(parent, "Left Wall", new Vector3(-2.7f, 3.75f, 16.5f), new Vector3(0.4f, 7.5f, 32f));
            Block(parent, "Right Wall", new Vector3(2.7f, 3.75f, 15f), new Vector3(0.4f, 7.5f, 35f));
            Block(parent, "Front Wall", new Vector3(0f, 3.75f, -2.7f), new Vector3(5.8f, 7.5f, 0.4f));
            Block(parent, "Back Wall", new Vector3(0f, 3.75f, 32.7f), new Vector3(5.8f, 7.5f, 0.4f));
            Block(parent, "Roof", new Vector3(0f, 7.7f, 15f), new Vector3(5.8f, 0.4f, 35f));

            var spawn = NewChild(parent, "Respawn Point").transform;
            spawn.localPosition = new Vector3(0f, 0.05f, -1f);
            var checkpoint = NewChild(parent, "Checkpoint Combined Room");
            checkpoint.transform.localPosition = new Vector3(0f, 0f, -1f);
            var checkpointVolume = checkpoint.AddComponent<BoxCollider>();
            checkpointVolume.isTrigger = true;
            checkpointVolume.center = Vector3.up * 3f;
            checkpointVolume.size = new Vector3(6f, 6f, 3f);
            var checkpointTrigger = checkpoint.AddComponent<CheckpointTrigger>();
            Reference(checkpointTrigger, "manager", manager);
            Reference(checkpointTrigger, "room", scope);
            Reference(checkpointTrigger, "spawnPoint", spawn);
            var checkpointMarker = Block(checkpoint.transform, "Checkpoint Marker", new Vector3(0f, 0.025f, 0f),
                new Vector3(4.5f, 0.05f, 1.5f), checkpointMaterial);
            UnityEngine.Object.DestroyImmediate(checkpointMarker.GetComponent<Collider>());
            Reference(checkpointTrigger, "indicator", checkpointMarker.GetComponent<Renderer>());

            var plateObject = NewChild(parent, "Combined Echo Plate");
            plateObject.transform.localPosition = new Vector3(0f, 0f, 2f);
            var plateVisual = Block(plateObject.transform, "Plate Surface", new Vector3(0f, 0.075f, 0f),
                new Vector3(2f, 0.15f, 2f), plateMaterial);
            var plate = plateObject.AddComponent<PressurePlate>();
            Reference(plate, "indicator", plateVisual.GetComponent<Renderer>());
            var plateReset = plateObject.AddComponent<PressurePlateAttemptReset>();

            var doorObject = NewChild(parent, "Combined Echo Door");
            doorObject.transform.localPosition = Vector3.forward * 11f;
            var panel = Block(doorObject.transform, "Door Panel", Vector3.up * 3.75f,
                new Vector3(5f, 7.5f, 0.6f), doorMaterial);
            var threshold = NewChild(doorObject.transform, "Threshold").transform;
            threshold.localPosition = Vector3.up;
            var door = doorObject.AddComponent<DoorController>();
            Reference(door, "plate", plate);
            Reference(door, "doorPanel", panel.transform);
            Reference(door, "threshold", threshold);
            var doorData = new SerializedObject(door);
            doorData.FindProperty("thresholdSize").vector3Value = new Vector3(5f, 4f, 1.8f);
            doorData.FindProperty("openOffset").vector3Value = Vector3.up * 8f;
            doorData.FindProperty("moveSpeed").floatValue = 24f;
            doorData.ApplyModifiedPropertiesWithoutUndo();
            var doorReset = doorObject.AddComponent<DoorAttemptReset>();

            var start = NewChild(parent, "Platform Start").transform;
            start.localPosition = new Vector3(0f, 0.05f, 18.5f);
            var end = NewChild(parent, "Platform End").transform;
            end.localPosition = new Vector3(0f, 0.05f, 25.5f);
            var platformObject = NewChild(parent, "Combined Rewind Platform");
            platformObject.transform.position = start.position;
            var surface = Block(platformObject.transform, "Platform Surface and Collider", Vector3.zero,
                new Vector3(3.5f, 0.4f, 2.5f), platformMaterial);
            var platform = platformObject.AddComponent<RewindMovingPlatform>();
            Reference(platform, "startPoint", start);
            Reference(platform, "endPoint", end);
            Reference(platform, "surface", surface.GetComponent<BoxCollider>());
            Reference(platform, "passenger", player.GetComponent<PlayerPlatformCarrier>());
            var platformData = new SerializedObject(platform);
            platformData.FindProperty("speed").floatValue = 3.5f;
            platformData.FindProperty("rewindSpeed").floatValue = 1.5f;
            platformData.ApplyModifiedPropertiesWithoutUndo();
            var platformReset = platformObject.AddComponent<RewindMechanismAttemptReset>();

            // Non-solid signs: colored lanes and a prominent exit; no extra jumping footholds.
            Sign(parent, "Echo Lane", new Vector3(0f, 0.012f, 7f), new Vector3(0.15f, 0.02f, 6f), doorMaterial);
            Sign(parent, "Rewind Aim Mark", new Vector3(0f, 0.012f, 16.5f), new Vector3(1.5f, 0.02f, 0.5f), platformMaterial);
            Sign(parent, "Exit Lane", new Vector3(0f, 0.012f, 29.5f), new Vector3(0.15f, 0.02f, 4f), exitMaterial);
            Sign(parent, "Combined Exit Marker", new Vector3(0f, 1f, 31f), new Vector3(1.2f, 2f, 0.25f), exitMaterial);

            var danger = NewChild(parent, "Combined Fall Zone");
            danger.transform.localPosition = new Vector3(0f, -8f, 15f);
            var dangerVolume = danger.AddComponent<BoxCollider>();
            dangerVolume.isTrigger = true;
            dangerVolume.size = new Vector3(12f, 6f, 40f);
            Reference(danger.AddComponent<RespawnZone>(), "manager", manager);
            var scopeData = new SerializedObject(scope);
            var participants = scopeData.FindProperty("participants");
            participants.arraySize = 3;
            participants.GetArrayElementAtIndex(0).objectReferenceValue = plateReset;
            participants.GetArrayElementAtIndex(1).objectReferenceValue = doorReset;
            participants.GetArrayElementAtIndex(2).objectReferenceValue = platformReset;
            scopeData.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Prototype.");
            AssetDatabase.SaveAssets();
            Debug.Log("S04-C installed: combined Echo/Rewind room at x=22, entrance checkpoint and scoped reset.");
        }

        private static Material MaterialAt(string name) => AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/_Echoryx/Materials/" + name + ".mat")
            ?? throw new InvalidOperationException("Missing material: " + name);

        private static Material EnsureExitMaterial()
        {
            const string path = "Assets/_Echoryx/Materials/CombinedExit.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("URP Unlit unavailable.");
            material = new Material(shader) { name = "CombinedExit" };
            material.SetColor("_BaseColor", new Color(0.4f, 1f, 0.1f));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject NewChild(Transform parent, string name)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            return item;
        }
        private static GameObject Block(Transform parent, string name, Vector3 position, Vector3 size, Material material = null)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = size;
            if (material != null) item.GetComponent<Renderer>().sharedMaterial = material;
            return item;
        }
        private static void Sign(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var sign = Block(parent, name, position, size, material);
            UnityEngine.Object.DestroyImmediate(sign.GetComponent<Collider>());
        }
        private static void Reference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
