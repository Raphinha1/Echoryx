using System;
using System.Collections.Generic;
using Echoryx.Echo;
using Echoryx.Flow;
using Echoryx.Flow.Editor;
using Echoryx.Game;
using Echoryx.Interactions;
using Echoryx.Player;
using Echoryx.Rewind;
using Echoryx.Rewind.Platforms;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using B = Echoryx.Level.Editor.Level01TutorialBuilder;
using Art = Echoryx.Level.Editor.Level01ArtPass;

namespace Echoryx.Level.Editor
{
    /// <summary>
    /// Builds Level_02_Sincronia (three rooms that combine Echo and Rewind) from the level prefabs, with the same
    /// art kit as Level_01. Every measure below is explained in docs/level-02.md. Only reuses existing gameplay
    /// components; the grate/gate is a DoorController with a see-through panel.
    /// </summary>
    public static class Level02SincroniaBuilder
    {
        public const string ScenePath = "Assets/_Echoryx/Scenes/Level_02_Sincronia.unity";
        public const string LevelId = "level-02-sincronia";
        private const string Prefabs = "Assets/_Echoryx/Prefabs/Level/";
        private const string GratePath = Prefabs + "Grate.prefab";
        private const string ArtMaterials = "Assets/_Echoryx/Materials/Art/";
        private const string OwnMaterials = "Assets/_Echoryx/Materials/Level02/"; // grate and well (not the Lit-only Art set)
        private const string LevelMaterialFolder = "Assets/_Echoryx/Materials/Level/";

        // ---- Layout (meters; +Z forward; floor top y = 0; corridor x in [-3, 3], walls at +-3.15).
        public static readonly Vector3 Spawn = new Vector3(-1.5f, 0.05f, 0f);
        public const float StartZ = -2f, EndZ = 64f;

        // Room 1: a well the player cannot climb out of (2.6 m deep, jump reaches 1.65 m).
        public const float WellX0 = 0.6f, WellX1 = 3.3f, WellZ0 = 2.5f, WellZ1 = 9.5f, WellDepth = 2.6f;
        public static readonly Vector3 PlateWell = new Vector3(1.95f, -WellDepth, 7.6f); // far end: room for the camera behind
        public const float DoorOneZ = 14f;
        public static readonly Vector3 PlateDoor = new Vector3(0f, 0f, 12.6f); // just before the door's threshold
        public const float CheckpointTwoZ = 17f;

        // Room 2: grate at the edge of the pit; the platform parks far beyond it.
        public static readonly Vector3 PlateGrate = new Vector3(-1.6f, 0f, 21f);
        public const float GrateZ = 30.15f, PitTwoStart = 30f, PitTwoEnd = 38.9f;
        public static readonly Vector3 PlatformTwoStart = new Vector3(0f, 0.05f, 31.9f);
        public static readonly Vector3 PlatformTwoEnd = new Vector3(0f, 0.05f, 37.3f);
        public const float PlatformTwoSpeed = 2.4f, PlatformTwoRewind = 1.2f; // 1.6x the first version
        public const float CheckpointThreeZ = 40.2f;

        // Room 3: the platform's one-way trip runs from the far side to the near side and parks next to the
        // player. A gate hangs over the pit; it lets the platform pass under it but not a rider.
        public static readonly Vector3 PlateGate = new Vector3(1.6f, 0f, 41.6f);
        public const float PitThreeStart = 46f, PitThreeEnd = 55.7f, GateZ = 52.4f;
        public static readonly Vector3 PlatformThreeStart = new Vector3(0f, 0.05f, 54.1f); // far
        public static readonly Vector3 PlatformThreeEnd = new Vector3(0f, 0.05f, 47.6f);   // near
        public const float PlatformThreeSpeed = 3.2f, PlatformThreeRewind = 0.7f; // ride (rewind) at 2.24 m/s
        public const float GoalZ = 60.5f;

        // Hints explain rules only, never the solution.
        public const string HintDoor = "Esta puerta solo se abre si sus dos placas están pulsadas a la vez. Una placa cuenta mientras algo la pisa.";
        public const string HintWell = "Del pozo no se sale saltando. R te devuelve al último checkpoint y conserva tu última grabación terminada.";
        public const string HintGrate = "La reja solo se abre mientras alguien pisa su placa. Cerrada, no deja pasar ni apuntar a través de ella.";
        public const string HintEcho = "Tu Echo repite lo grabado desde el punto donde empezaste a grabar, durante 5 segundos como máximo.";
        public const string HintRewindRide = "Rewind devuelve un mecanismo por el camino que ya recorrió, y lleva consigo a quien esté encima.";
        public const string HintGate = "La compuerta sobre el foso se abre con su placa. Si te alcanza cerrada, te empuja fuera.";

        [MenuItem("Echoryx/Level 02/Build Sincronía")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
                throw new InvalidOperationException("TMP Essentials missing. Run Echoryx/Level 01/Import TMP Essentials first.");
            var actions = Load<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            var playerPrefab = Load<GameObject>("Assets/_Echoryx/Prefabs/Player.prefab");
            var plate = Load<GameObject>(Prefabs + "PressurePlate.prefab");
            var door = Load<GameObject>(Prefabs + "Door.prefab");
            var hazard = Load<GameObject>(Prefabs + "Hazard.prefab");
            var goal = Load<GameObject>(Prefabs + "Goal.prefab");
            var hint = Load<GameObject>(Prefabs + "TutorialHint.prefab");
            var m = LevelMaterials();
            var a = ArtMaterialsSet();
            if (!AssetDatabase.IsValidFolder("Assets/_Echoryx/Materials/Level02")) AssetDatabase.CreateFolder("Assets/_Echoryx/Materials", "Level02");
            var dark = FlatMaterial("ArtWellWall", new Color(0.28f, 0.24f, 0.42f), false);
            var grate = BuildGratePrefab(a);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            B.ConfigureLighting();
            var cm = B.CreatePlayerRig(scene, playerPrefab, Spawn, out var player, out var input, out var orbit);

            var geometry = B.NewChild(null, "Level Geometry").transform;
            var art = B.NewChild(null, "Level Art").transform;
            BuildGeometry(geometry, art, a, dark);

            B.BuildHud(out var hintDisplay, out var hud);
            var systems = B.NewChild(null, "Level Systems");
            var stats = systems.AddComponent<LevelStats>();
            var checkpoints = B.NewChild(null, "Checkpoints");
            var manager = checkpoints.AddComponent<CheckpointManager>();
            var room1 = B.NewRoom(checkpoints.transform, "Room 1 Two Plates", stats);
            var room2 = B.NewRoom(checkpoints.transform, "Room 2 Grate", stats);
            var room3 = B.NewRoom(checkpoints.transform, "Room 3 Gate", stats);
            var initialSpawn = B.NewChild(room1.transform, "Initial Spawn").transform;
            initialSpawn.SetPositionAndRotation(Spawn, Quaternion.identity);
            var echoReset = checkpoints.AddComponent<EchoAttemptReset>();
            B.Set(echoReset, "playback", player.GetComponent<EchoPlayback>());
            var rewindRelease = checkpoints.AddComponent<RewindAttemptRelease>();
            B.Set(rewindRelease, "interactor", player.GetComponent<RewindInteractor>());
            B.Set(manager, "player", player.GetComponent<PlayerMotor>());
            B.Set(manager, "initialSpawn", initialSpawn);
            B.Set(manager, "initialRoom", room1);
            B.Set(manager, "rewindRelease", rewindRelease);
            B.Set(manager, "echoReset", echoReset);
            B.Set(manager, "cameraOrbit", orbit);
            B.Set(manager, "gameplayCamera", cm);
            var restart = checkpoints.AddComponent<RestartInput>();
            B.Set(restart, "actions", actions);
            B.Set(restart, "manager", manager);
            B.Set(restart, "input", input);
            B.NewCheckpoint(room2.transform, "Checkpoint End Room 1", new Vector3(0f, 0f, CheckpointTwoZ), manager, room2, m);
            B.NewCheckpoint(room3.transform, "Checkpoint End Room 2", new Vector3(0f, 0f, CheckpointThreeZ), manager, room3, m);

            // Room 1: one plate at the bottom of the well, one in front of the door; the door needs both.
            var plateWell = B.Instantiate(plate, room1.transform, "Plate Well", PlateWell);
            var plateDoor = B.Instantiate(plate, room1.transform, "Plate Door", PlateDoor);
            EmphasizeBeacon(plateWell);
            var doorOne = B.Instantiate(door, room1.transform, "Door Two Plates", new Vector3(0f, 0f, DoorOneZ));
            var doorController = doorOne.GetComponent<DoorController>();
            B.Set(doorController, "plate", plateDoor.GetComponent<PressurePlate>());
            SetArray(doorController, "extraPlates", plateWell.GetComponent<PressurePlate>());
            B.Append(room1, plateWell.GetComponent<PressurePlateAttemptReset>());
            B.Append(room1, plateDoor.GetComponent<PressurePlateAttemptReset>());
            B.Append(room1, doorOne.GetComponent<DoorAttemptReset>());

            // Room 2: plate far from the grate; grate at the pit edge; platform parked out of rewind reach
            // while the grate is closed.
            var plateGrate = B.Instantiate(plate, room2.transform, "Plate Grate", PlateGrate);
            var grateTwo = B.Instantiate(grate, room2.transform, "Grate", new Vector3(0f, 0f, GrateZ));
            B.Set(grateTwo.GetComponent<DoorController>(), "plate", plateGrate.GetComponent<PressurePlate>());
            // Anti-crush threshold only over the pit (a rider on the platform), never in front of the grate:
            // otherwise standing under the open grate would keep it open forever.
            var grateThreshold = grateTwo.transform.Find("Threshold");
            grateThreshold.localPosition = new Vector3(0f, 1f, 0.75f);
            SetVector(grateTwo.GetComponent<DoorController>(), "thresholdSize", new Vector3(6f, 4f, 0.9f));
            Lintel(room2.transform, dark, GrateZ);
            var platformTwo = BuildPlatform(room2.transform, "Room 2", PlatformTwoStart, PlatformTwoEnd,
                PlatformTwoSpeed, PlatformTwoRewind, player.GetComponent<PlayerPlatformCarrier>(), m);
            B.Append(room2, plateGrate.GetComponent<PressurePlateAttemptReset>());
            B.Append(room2, grateTwo.GetComponent<DoorAttemptReset>());
            B.Append(room2, platformTwo.GetComponent<ParkedPlatformAttemptReset>());

            // Room 3: hanging gate (bottom 0.4 m, above the platform, below a rider) opened by its plate.
            var plateGate = B.Instantiate(plate, room3.transform, "Plate Gate", PlateGate);
            var gateThree = B.Instantiate(grate, room3.transform, "Hanging Gate", new Vector3(0f, 0f, GateZ));
            var gatePanel = gateThree.transform.Find("Grate Panel");
            gatePanel.localPosition = new Vector3(0f, 3.2f, 0f);
            gatePanel.localScale = new Vector3(1f, 5.6f / 6f, 1f);
            B.Set(gateThree.GetComponent<DoorController>(), "plate", plateGate.GetComponent<PressurePlate>());
            SetVector(gateThree.GetComponent<DoorController>(), "thresholdSize", new Vector3(6f, 4f, 1.2f));
            Lintel(room3.transform, dark, GateZ);
            var platformThree = BuildPlatform(room3.transform, "Room 3", PlatformThreeStart, PlatformThreeEnd,
                PlatformThreeSpeed, PlatformThreeRewind, player.GetComponent<PlayerPlatformCarrier>(), m);
            B.Append(room3, plateGate.GetComponent<PressurePlateAttemptReset>());
            B.Append(room3, gateThree.GetComponent<DoorAttemptReset>());
            B.Append(room3, platformThree.GetComponent<ParkedPlatformAttemptReset>());

            var statsData = new SerializedObject(stats);
            var watched = statsData.FindProperty("watchedMechanisms");
            watched.arraySize = 2;
            watched.GetArrayElementAtIndex(0).objectReferenceValue = platformTwo.GetComponent<RewindableMechanism>();
            watched.GetArrayElementAtIndex(1).objectReferenceValue = platformThree.GetComponent<RewindableMechanism>();
            statsData.ApplyModifiedPropertiesWithoutUndo();

            B.NewHazard(hazard, geometry, "Hazard Pit 2", manager, new Vector3(0f, -5f, (PitTwoStart + PitTwoEnd) * 0.5f),
                new Vector3(6.4f, 4f, PitTwoEnd - PitTwoStart));
            B.NewHazard(hazard, geometry, "Hazard Pit 3", manager, new Vector3(0f, -5f, (PitThreeStart + PitThreeEnd) * 0.5f),
                new Vector3(6.4f, 4f, PitThreeEnd - PitThreeStart));
            var goalObject = B.Instantiate(goal, geometry, "Goal", new Vector3(0f, 0f, GoalZ));
            B.Set(goalObject.GetComponent<GoalZone>(), "stats", stats);
            B.Set(hud.GetComponent<LevelCompletePanel>(), "stats", stats);

            var hints = B.NewChild(null, "Tutorial Hints").transform;
            B.NewHint(hint, hints, hintDisplay, "Hint Door", new Vector3(-1.5f, 2f, 1f), new Vector3(3.6f, 4f, 4f), HintDoor);
            B.NewHint(hint, hints, hintDisplay, "Hint Well", new Vector3(1.95f, 0f, (WellZ0 + WellZ1) * 0.5f), new Vector3(2.8f, 6f, WellZ1 - WellZ0), HintWell);
            B.NewHint(hint, hints, hintDisplay, "Hint Grate", new Vector3(0f, 2f, 18.6f), new Vector3(6f, 4f, 2.8f), HintGrate);
            B.NewHint(hint, hints, hintDisplay, "Hint Echo", new Vector3(0f, 2f, 27.5f), new Vector3(6f, 4f, 4.6f), HintEcho);
            B.NewHint(hint, hints, hintDisplay, "Hint Rewind Ride", new Vector3(0f, 2f, 39.7f), new Vector3(6f, 4f, 1.6f), HintRewindRide);
            B.NewHint(hint, hints, hintDisplay, "Hint Gate", new Vector3(0f, 2f, 44.6f), new Vector3(6f, 4f, 2.6f), HintGate);

            CheckpointMarkers(a);
            Art.MarkScenery(art);
            Art.ConfigureCamera();
            Atmosphere(a);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save " + ScenePath);
            AddToCatalog();
            GameFlowBuilder.SyncBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("Level_02_Sincronia built.");
        }

        // ---------------------------------------------------------------- geometry and art

        private static void BuildGeometry(Transform logic, Transform art, Art.Mats a, Material dark)
        {
            // Room 1: thick floor blocks (their sides are the walls of the well).
            Block(logic, "Floor 1 Lane", -3.3f, WellX0, -WellDepth - 1f, 0f, StartZ, DoorOneZ + 0.3f);
            Block(logic, "Floor 1 Right Front", WellX0, 3.3f, -WellDepth - 1f, 0f, StartZ, WellZ0);
            Block(logic, "Floor 1 Right Back", WellX0, 3.3f, -WellDepth - 1f, 0f, WellZ1, DoorOneZ + 0.3f);
            Block(logic, "Well Bottom", WellX0, 3.3f, -WellDepth - 1f, -WellDepth, WellZ0, WellZ1);
            Block(logic, "Floor 2", -3.3f, 3.3f, -1f, 0f, DoorOneZ + 0.3f, PitTwoStart);
            Block(logic, "Floor 3", -3.3f, 3.3f, -1f, 0f, PitTwoEnd, PitThreeStart);
            Block(logic, "Floor 4 Goal", -3.3f, 3.3f, -1f, 0f, PitThreeEnd, EndZ);
            var length = EndZ - StartZ;
            var middle = (StartZ + EndZ) * 0.5f;
            B.Cube(logic, "Wall Left", new Vector3(-3.15f, 1f, middle), new Vector3(0.3f, 10f, length), null).isStatic = true;
            B.Cube(logic, "Wall Right", new Vector3(3.15f, 1f, middle), new Vector3(0.3f, 10f, length), null).isStatic = true;
            B.Cube(logic, "Wall Start", new Vector3(0f, 1f, StartZ - 0.15f), new Vector3(6.6f, 10f, 0.3f), null).isStatic = true;
            B.Cube(logic, "Wall End", new Vector3(0f, 1f, EndZ + 0.15f), new Vector3(6.6f, 10f, 0.3f), null).isStatic = true;
            foreach (var renderer in logic.GetComponentsInChildren<MeshRenderer>()) renderer.enabled = false;

            // Visuals: Kenney floor tiles, the well lined with a dark flat material, walls and background.
            FloorPiece(art, "Floor 1 Lane", -3.3f, WellX0, StartZ, DoorOneZ + 0.3f, 0f, a);
            FloorPiece(art, "Floor 1 Right Front", WellX0, 3.3f, StartZ, WellZ0, 0f, a);
            FloorPiece(art, "Floor 1 Right Back", WellX0, 3.3f, WellZ1, DoorOneZ + 0.3f, 0f, a);
            FloorPiece(art, "Well Bottom", WellX0, 3.3f, WellZ0, WellZ1, -WellDepth, a);
            Lining(art, "Well Side", new Vector3(WellX0 + 0.02f, -WellDepth * 0.5f, (WellZ0 + WellZ1) * 0.5f), new Vector3(0.04f, WellDepth, WellZ1 - WellZ0), dark);
            Lining(art, "Well Front", new Vector3((WellX0 + 3.3f) * 0.5f, -WellDepth * 0.5f, WellZ0 + 0.02f), new Vector3(3.3f - WellX0, WellDepth, 0.04f), dark);
            Lining(art, "Well Back", new Vector3((WellX0 + 3.3f) * 0.5f, -WellDepth * 0.5f, WellZ1 - 0.02f), new Vector3(3.3f - WellX0, WellDepth, 0.04f), dark);
            Lining(art, "Well Outer", new Vector3(2.98f, -WellDepth * 0.5f, (WellZ0 + WellZ1) * 0.5f), new Vector3(0.04f, WellDepth, WellZ1 - WellZ0), dark);
            WellLight(art);
            // Hull pivot is at its bottom; keep its top under the well floor.
            Art.Spawn(Art.Station, "floor", art, "Floor 1 Hull", new Vector3(0f, -WellDepth - 0.45f - 2.8f, (StartZ + DoorOneZ) * 0.5f), Vector3.zero,
                new Vector3(5.6f, 2.8f / 0.3f, DoorOneZ - StartZ), a.Hull);
            Art.Floor(art, "Floor 2", DoorOneZ + 0.3f, PitTwoStart, a);
            Art.Floor(art, "Floor 3", PitTwoEnd, PitThreeStart, a);
            Art.Floor(art, "Floor 4", PitThreeEnd, EndZ, a);

            var count = Mathf.CeilToInt(length / 4f);
            var scaleLength = length / (count * 4f);
            for (var i = 0; i < count; i++)
            {
                var z = StartZ + (i + 0.5f) * (length / count);
                Art.Spawn(Art.Modular, "template-wall", art, "Wall L " + i, new Vector3(-4f, 0f, z), new Vector3(0f, -90f, 0f), new Vector3(scaleLength, 1.4f, 1f), a.Wall);
                Art.Spawn(Art.Modular, "template-wall", art, "Wall R " + i, new Vector3(4f, 0f, z), new Vector3(0f, 90f, 0f), new Vector3(scaleLength, 1.4f, 1f), a.Wall);
            }
            for (var i = 0; i < 2; i++)
            {
                var x = -1.65f + i * 3.3f;
                Art.Spawn(Art.Modular, "template-wall", art, "Wall Start " + i, new Vector3(x, 0f, StartZ - 1f), new Vector3(0f, 180f, 0f), new Vector3(0.825f, 1.4f, 1f), a.Wall);
                Art.Spawn(Art.Modular, "template-wall", art, "Wall End " + i, new Vector3(x, 0f, EndZ + 1f), Vector3.zero, new Vector3(0.825f, 1.4f, 1f), a.Wall);
            }
            Backing(art, dark);
            Art.Display(art, "Display 1", new Vector3(-2.92f, 3.7f, 8f), 90f, a);
            Art.Display(art, "Display 2", new Vector3(2.92f, 3.7f, 24f), -90f, a);
            Art.Display(art, "Display 3", new Vector3(-2.92f, 3.7f, 43f), 90f, a);
            Art.Display(art, "Display 4", new Vector3(2.92f, 3.7f, 60f), -90f, a);
            // Level 02 is a closed station section: no floating scenery outside the walls.
            Enclose(art, dark);
        }

        /// <summary>
        /// Opaque backing behind every perimeter wall (measured from the spawned wall art, floor of the pits to the
        /// top of the walls, a little longer at each end) plus corner pillars that overlap both panels. Whatever
        /// slit the modular wall pieces leave, only these dark panels show through. No colliders: neither the
        /// player nor the camera deoccluder can touch them.
        /// </summary>
        private static void Backing(Transform art, Material dark)
        {
            Bounds Of(string prefix)
            {
                var bounds = new Bounds();
                var first = true;
                foreach (var renderer in art.GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.name.StartsWith(prefix) && !renderer.transform.parent.name.StartsWith(prefix)
                        && !HasAncestor(renderer.transform, prefix)) continue;
                    if (first) { bounds = renderer.bounds; first = false; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                if (first) throw new InvalidOperationException("No wall art named " + prefix);
                return bounds;
            }
            var left = Of("Wall L ");
            var right = Of("Wall R ");
            var start = Of("Wall Start ");
            var end = Of("Wall End ");
            const float bottom = -7.6f, thickness = 0.1f, over = 0.3f;
            var top = Mathf.Max(Mathf.Max(left.max.y, right.max.y), Mathf.Max(start.max.y, end.max.y));
            var height = top - bottom;
            var middleY = (top + bottom) * 0.5f;
            var outerLeft = left.min.x - thickness;
            var outerRight = right.max.x + thickness;
            var outerStart = Mathf.Min(start.min.z, left.min.z, right.min.z) - thickness;
            var outerEnd = Mathf.Max(end.max.z, left.max.z, right.max.z) + thickness;
            Panel(art, "Backing Left", new Vector3(outerLeft + thickness * 0.5f, middleY, (outerStart + outerEnd) * 0.5f),
                new Vector3(thickness, height, outerEnd - outerStart + 2f * over), dark);
            Panel(art, "Backing Right", new Vector3(outerRight - thickness * 0.5f, middleY, (outerStart + outerEnd) * 0.5f),
                new Vector3(thickness, height, outerEnd - outerStart + 2f * over), dark);
            Panel(art, "Backing Start", new Vector3((outerLeft + outerRight) * 0.5f, middleY, outerStart + thickness * 0.5f),
                new Vector3(outerRight - outerLeft + 2f * over, height, thickness), dark);
            Panel(art, "Backing End", new Vector3((outerLeft + outerRight) * 0.5f, middleY, outerEnd - thickness * 0.5f),
                new Vector3(outerRight - outerLeft + 2f * over, height, thickness), dark);
            // Corner pillars: from inside the end wall's run to the outer backing, overlapping both.
            foreach (var (x0, x1) in new[] { (outerLeft, Mathf.Min(start.min.x, end.min.x) + 0.1f), (Mathf.Max(start.max.x, end.max.x) - 0.1f, outerRight) })
                foreach (var (z0, z1) in new[] { (outerStart, Mathf.Max(left.min.z, right.min.z) + 0.3f), (Mathf.Min(left.max.z, right.max.z) - 0.3f, outerEnd) })
                    Panel(art, "Corner Pillar", new Vector3((x0 + x1) * 0.5f, middleY, (z0 + z1) * 0.5f), new Vector3(x1 - x0, height, z1 - z0), dark);
        }

        private static bool HasAncestor(Transform child, string prefix)
        {
            for (var t = child; t != null; t = t.parent)
                if (t.name.StartsWith(prefix)) return true;
            return false;
        }

        private static void Panel(Transform art, string name, Vector3 position, Vector3 size, Material material)
        {
            var cube = B.Cube(art, name, position, size, material);
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Each pit is a closed box (side, end and bottom panels), so no gap shows the void below.</summary>
        private static void Enclose(Transform art, Material dark)
        {
            foreach (var (name, from, to) in new[] { ("Pit 2", PitTwoStart, PitTwoEnd), ("Pit 3", PitThreeStart, PitThreeEnd) })
            {
                var middle = (from + to) * 0.5f;
                var length = to - from;
                Lining(art, name + " Side L", new Vector3(-2.98f, -3.6f, middle), new Vector3(0.04f, 7.2f, length), dark);
                Lining(art, name + " Side R", new Vector3(2.98f, -3.6f, middle), new Vector3(0.04f, 7.2f, length), dark);
                Lining(art, name + " End Near", new Vector3(0f, -3.8f, from + 0.02f), new Vector3(6f, 6.8f, 0.04f), dark);
                Lining(art, name + " End Far", new Vector3(0f, -3.8f, to - 0.02f), new Vector3(6f, 6.8f, 0.04f), dark);
                Lining(art, name + " Bottom", new Vector3(0f, -7.2f, middle), new Vector3(6f, 0.04f, length), dark);
            }
        }

        /// <summary>
        /// The well plate sits 2.6 m down in shadow: its beacon (same colours as every plate) is brighter, reacts
        /// from farther away and emits more particles, so it reads as interactive from the lane and the well camera.
        /// </summary>
        private static void EmphasizeBeacon(GameObject plateObject)
        {
            var driver = new SerializedObject(plateObject.GetComponent<PlateBeaconDriver>());
            driver.FindProperty("nearRange").floatValue = 10f;
            driver.ApplyModifiedPropertiesWithoutUndo();
            var beacon = new SerializedObject(plateObject.GetComponentInChildren<InteractionBeacon>(true));
            beacon.FindProperty("idleHaloAlpha").floatValue = 0.3f;
            beacon.FindProperty("activeHaloAlpha").floatValue = 0.85f;
            beacon.FindProperty("idleRate").floatValue = 6f;
            beacon.FindProperty("activeRate").floatValue = 22f;
            beacon.ApplyModifiedPropertiesWithoutUndo();
        }

        // A warm fill light at the bottom of the well, so its plate and beacon read from the well camera.
        private static void WellLight(Transform art)
        {
            var light = new GameObject("Well Light").AddComponent<Light>();
            light.transform.SetParent(art, false);
            light.transform.position = PlateWell + new Vector3(0f, 1.8f, -0.6f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.7f, 0.42f);
            light.intensity = 4f;
            light.range = 7f;
            light.shadows = LightShadows.None;
        }

        private static void Block(Transform parent, string name, float x0, float x1, float y0, float y1, float z0, float z1) =>
            B.Cube(parent, name, new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, y1 - y0, z1 - z0), null).isStatic = true;

        private static void FloorPiece(Transform art, string name, float x0, float x1, float z0, float z1, float top, Art.Mats a)
        {
            var count = Mathf.CeilToInt((z1 - z0) / 4.2f);
            var tile = (z1 - z0) / count;
            for (var i = 0; i < count; i++)
                Art.Spawn(Art.Modular, "template-floor-layer", art, name + " Tile " + i, new Vector3((x0 + x1) * 0.5f, top - 0.4f, z0 + (i + 0.5f) * tile),
                    Vector3.zero, new Vector3((x1 - x0) / 4.2f, 1f, tile / 4.2f), a.Floor);
        }

        // Linings overlap their neighbours by 6 cm at each end (the thin axis is kept), so corners have no slit.
        private static void Lining(Transform art, string name, Vector3 position, Vector3 size, Material material)
        {
            const float overlap = 0.12f;
            size = new Vector3(size.x > 0.2f ? size.x + overlap : size.x, size.y > 0.2f ? size.y + overlap : size.y,
                size.z > 0.2f ? size.z + overlap : size.z);
            var cube = B.Cube(art, name, position, size, material);
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
        }

        // A static beam above a grate up to the top of the walls, so nothing can be aimed or thrown over it.
        private static void Lintel(Transform parent, Material material, float z)
        {
            var beam = B.Cube(parent, "Lintel", new Vector3(0f, 8f, z), new Vector3(6.6f, 4f, 0.6f), material);
            beam.isStatic = true;
        }

        private static GameObject BuildPlatform(Transform parent, string label, Vector3 from, Vector3 to, float speed, float rewindSpeed,
            PlayerPlatformCarrier passenger, B.Materials m)
        {
            var start = B.NewChild(parent, "Platform Start").transform;
            start.position = from;
            var end = B.NewChild(parent, "Platform End").transform;
            end.position = to;
            var root = B.NewChild(parent, "Rewind Platform " + label);
            root.transform.position = from;
            var surface = B.Cube(root.transform, "Platform Surface and Collider", Vector3.zero, new Vector3(5f, 0.4f, 3f), m.Platform);
            surface.GetComponent<MeshRenderer>().enabled = false;
            var platform = root.AddComponent<RewindMovingPlatform>();
            B.Set(platform, "startPoint", start);
            B.Set(platform, "endPoint", end);
            B.Set(platform, "surface", surface.GetComponent<BoxCollider>());
            B.Set(platform, "passenger", passenger);
            var data = new SerializedObject(platform);
            data.FindProperty("speed").floatValue = speed;
            data.FindProperty("rewindSpeed").floatValue = rewindSpeed;
            data.ApplyModifiedPropertiesWithoutUndo();
            root.AddComponent<ParkedPlatformAttemptReset>();
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(Prefabs + "RewindPlatformVisual.prefab"), surface.transform.parent);
            visual.name = "Platform Art";
            visual.transform.localPosition = Vector3.zero;
            return root;
        }

        /// <summary>See-through door: a collider panel carrying lilac field and orange bars. Blocks walking and aiming.</summary>
        private static GameObject BuildGratePrefab(Art.Mats a)
        {
            var bar = FlatMaterial("ArtGrateBar", new Color(1f, 0.62f, 0.25f), true);
            var field = FieldMaterial();
            var root = new GameObject("Grate");
            var panel = new GameObject("Grate Panel");
            panel.transform.SetParent(root.transform, false);
            panel.transform.localPosition = new Vector3(0f, 3f, 0f);
            var collider = panel.AddComponent<BoxCollider>();
            collider.size = new Vector3(6f, 6f, 0.3f);
            for (var i = 0; i < 9; i++)
            {
                var x = -2.8f + i * 0.7f;
                var post = B.Cube(panel.transform, "Bar " + i, new Vector3(x, 0f, 0f), new Vector3(0.1f, 6f, 0.12f), bar);
                UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());
            }
            foreach (var y in new[] { -2.9f, 0f, 2.9f })
            {
                var rail = B.Cube(panel.transform, "Rail", new Vector3(0f, y, 0f), new Vector3(6f, 0.12f, 0.14f), bar);
                UnityEngine.Object.DestroyImmediate(rail.GetComponent<Collider>());
            }
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Field";
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(panel.transform, false);
            quad.transform.localScale = new Vector3(6f, 6f, 1f);
            quad.GetComponent<Renderer>().sharedMaterial = field;
            quad.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var threshold = new GameObject("Threshold").transform;
            threshold.SetParent(root.transform, false);
            threshold.localPosition = Vector3.up;
            var door = root.AddComponent<DoorController>();
            B.Set(door, "doorPanel", panel.transform);
            B.Set(door, "threshold", threshold);
            var data = new SerializedObject(door);
            data.FindProperty("thresholdSize").vector3Value = new Vector3(6f, 4f, 1.2f);
            data.FindProperty("openOffset").vector3Value = Vector3.up * 6.3f;
            data.FindProperty("moveSpeed").floatValue = 12f;
            data.ApplyModifiedPropertiesWithoutUndo();
            root.AddComponent<DoorAttemptReset>();
            var saved = PrefabUtility.SaveAsPrefabAsset(root, GratePath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved ?? throw new InvalidOperationException("Could not save the grate prefab.");
        }

        private static void CheckpointMarkers(Art.Mats a)
        {
            foreach (var marker in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (marker.name != "Checkpoint Marker") continue;
                marker.GetComponent<MeshFilter>().sharedMesh = Art.MeshOf(Art.Station, "floor-panel");
                marker.GetComponent<Renderer>().sharedMaterial = a.Flat;
                marker.localPosition = new Vector3(0f, 0.015f, 0f);
                marker.localScale = new Vector3(5f, 0.2f, 1.5f);
            }
        }

        // Same sky, fog, light and post-processing as Level_01 (its volume profile is reused, not modified).
        private static void Atmosphere(Art.Mats a)
        {
            RenderSettings.skybox = a.Sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.55f, 0.88f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.42f, 0.62f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.20f, 0.34f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.007f;
            RenderSettings.fogColor = new Color(0.30f, 0.20f, 0.46f);
            var light = GameObject.Find("Directional Light").GetComponent<Light>();
            light.color = new Color(1f, 0.82f, 0.64f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.8f;
            light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            RenderSettings.sun = light;
            Camera.main.clearFlags = CameraClearFlags.Skybox;
            Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = Load<VolumeProfile>("Assets/_Echoryx/Settings/Level01VolumeProfile.asset");
        }

        // ---------------------------------------------------------------- catalog and assets

        private static void AddToCatalog()
        {
            var catalog = Load<LevelCatalog>(GameFlowBuilder.CatalogPath);
            if (catalog.IndexOfScene(ScenePath) >= 0) return;
            var data = new SerializedObject(catalog);
            var levels = data.FindProperty("levels");
            var first = -1;
            for (var i = 0; i < levels.arraySize; i++)
                if (levels.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == "level-01-tutorial") first = i;
            var index = first + 1; // right after Level 01 (at the start if it is missing)
            levels.InsertArrayElementAtIndex(index);
            var entry = levels.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").stringValue = LevelId;
            entry.FindPropertyRelative("displayName").stringValue = "Sincronía";
            entry.FindPropertyRelative("scenePath").stringValue = ScenePath;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static B.Materials LevelMaterials() => new B.Materials
        {
            Floor = Load<Material>(LevelMaterialFolder + "LevelFloor.mat"),
            Wall = Load<Material>(LevelMaterialFolder + "LevelWall.mat"),
            Platform = Load<Material>(LevelMaterialFolder + "LevelPlatform.mat"),
            Checkpoint = Load<Material>(LevelMaterialFolder + "LevelCheckpoint.mat"),
        };

        private static Art.Mats ArtMaterialsSet() => new Art.Mats
        {
            Wall = Load<Material>(ArtMaterials + "ArtWall.mat"),
            Floor = Load<Material>(ArtMaterials + "ArtFloor.mat"),
            Hull = Load<Material>(ArtMaterials + "ArtHull.mat"),
            Accent = Load<Material>(ArtMaterials + "ArtAccent.mat"),
            Flat = Load<Material>(ArtMaterials + "ArtFlat.mat"),
            Sky = Load<Material>(ArtMaterials + "ArtSky.mat"),
        };

        private static Material FlatMaterial(string name, Color color, bool glow)
        {
            var path = OwnMaterials + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.1f);
            material.SetFloat("_Metallic", 0f);
            if (glow)
            {
                material.SetColor("_EmissionColor", color * 0.6f);
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        // A copy of the goal field (transparent, double sided) in lilac.
        private static Material FieldMaterial()
        {
            const string path = OwnMaterials + "ArtGrateField.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Load<Material>(ArtMaterials + "ArtGoalField.mat")) { name = "ArtGrateField" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", new Color(0.72f, 0.55f, 1f, 0.22f));
            material.SetColor("_EmissionColor", new Color(0.6f, 0.35f, 1.1f));
            material.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing asset: " + path);

        private static void SetArray(UnityEngine.Object target, string field, params UnityEngine.Object[] values)
        {
            var data = new SerializedObject(target);
            var list = data.FindProperty(field) ?? throw new InvalidOperationException("Missing field " + field);
            list.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector(UnityEngine.Object target, string field, Vector3 value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).vector3Value = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
