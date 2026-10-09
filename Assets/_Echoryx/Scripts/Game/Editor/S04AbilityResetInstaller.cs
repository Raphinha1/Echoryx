using System;
using Echoryx.Echo;
using Echoryx.Interactions;
using Echoryx.Rewind;
using Echoryx.Rewind.Platforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoryx.Game.Editor
{
    /// <summary>Registers existing Prototype mechanisms without recreating gameplay resources.</summary>
    public static class S04AbilityResetInstaller
    {
        [MenuItem("Echoryx/S04-B/Install Coordinated Reset")]
        public static void Install()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/_Echoryx/Scenes/Prototype.unity", OpenSceneMode.Single);
            var root = Required("S04-A Checkpoints");
            var manager = root.GetComponent<CheckpointManager>();
            var roomA = root.transform.Find("Room A").GetComponent<RoomResetScope>();
            var roomB = root.transform.Find("Room B").GetComponent<RoomResetScope>();
            var echoPuzzle = Required("S02-C Echo Puzzle");
            var rotor = Required("S03-B Rewind Test Rotor");
            var crossing = Required("S03-C Rewind Crossing");
            var playback = Required("Player").GetComponent<EchoPlayback>();
            var plate = echoPuzzle.GetComponentInChildren<PressurePlate>();
            var door = echoPuzzle.GetComponentInChildren<DoorController>();
            var platform = crossing.GetComponentInChildren<RewindMovingPlatform>();
            if (manager == null || playback == null || plate == null || door == null
                || platform == null || rotor.GetComponent<RewindableMechanism>() == null)
                throw new InvalidOperationException("Existing Prototype gameplay components are missing.");

            var data = new SerializedObject(manager);
            var previousRoom = data.FindProperty("initialRoom").objectReferenceValue;
            var initial = root.transform.Find("Initial Room").GetComponent<RoomResetScope>();
            if (previousRoom != initial && previousRoom != roomA)
                throw new InvalidOperationException("Custom initial room assignment: configure reset membership manually.");
            MoveIntoRoom(echoPuzzle, roomA);
            MoveIntoRoom(rotor, roomA);
            MoveIntoRoom(crossing, roomB);

            var echoReset = Ensure<EchoAttemptReset>(root);
            SetReference(echoReset, "playback", playback);
            SetReference(manager, "echoReset", echoReset);
            // The initial spawn and checkpoint A share the central room; the spawn pose stays unchanged.
            SetReference(manager, "initialRoom", roomA);
            Append(roomA, Ensure<PressurePlateAttemptReset>(plate.gameObject));
            Append(roomA, Ensure<DoorAttemptReset>(door.gameObject));
            Append(roomA, Ensure<RewindMechanismAttemptReset>(rotor));
            Append(roomB, Ensure<RewindMechanismAttemptReset>(platform.gameObject));

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Prototype.");
            AssetDatabase.SaveAssets();
            Debug.Log("S04-B installed: global Echo cleanup; Room A plate/door/rotor; Room B platform. No duplicates.");
        }

        private static GameObject Required(string name) => GameObject.Find(name)
            ?? throw new InvalidOperationException("Missing scene object: " + name);

        private static T Ensure<T>(GameObject owner) where T : Component =>
            owner.GetComponent<T>() ?? owner.AddComponent<T>();

        private static void MoveIntoRoom(GameObject owner, RoomResetScope room)
        {
            var previous = owner.GetComponentInParent<RoomResetScope>(true);
            if (previous != null && previous != room)
                throw new InvalidOperationException(owner.name + " already belongs to another reset scope.");
            if (previous == null) owner.transform.SetParent(room.transform, true);
        }

        private static void Append(RoomResetScope room, MonoBehaviour participant)
        {
            var data = new SerializedObject(room);
            var list = data.FindProperty("participants");
            for (var i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == participant) return;
            var index = list.arraySize;
            list.arraySize++;
            list.GetArrayElementAtIndex(index).objectReferenceValue = participant;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
