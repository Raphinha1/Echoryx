using System;
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
using B = Echoryx.Level.Editor.Level01TutorialBuilder;
using Art = Echoryx.Level.Editor.Level01ArtPass;
using L2 = Echoryx.Level.Editor.Level02SincroniaBuilder;

namespace Echoryx.Level.Editor
{
    /// <summary>
    /// Builds Level_03_Paradojas ("Laboratorio de Paradojas": unlock the access to the reactor) from the level
    /// prefabs and the Level_02 helpers. Five sections, the last one in two stages; every measure is explained in
    /// docs/level-03.md and played by Level03ParadojasPlayChecks. Only existing gameplay components are used: the
    /// bridge is a DoorController whose panel is a deck, the elevators are one-way RewindMovingPlatforms going down
    /// with a pressure plate on top.
    /// </summary>
    public static class Level03ParadojasBuilder
    {
        public const string ScenePath = "Assets/_Echoryx/Scenes/Level_03_Paradojas.unity";
        public const string LevelId = "level-03-paradojas";
        private const string Prefabs = "Assets/_Echoryx/Prefabs/Level/";

        // ---- Layout (meters; +Z forward; floor top y = 0; corridor x in [-3, 3], walls at +-3.15).
        public static readonly Vector3 Spawn = new Vector3(0f, 0.05f, 0f);
        public const float StartZ = -2f, EndZ = 124f;

        // 1. Reminder: a platform that has left over a pit too wide to jump, then a plate and its door.
        public const float PitOneStart = 6f, PitOneEnd = 16f;
        public static readonly Vector3 PlatformOneStart = new Vector3(0f, 0.05f, 7.6f), PlatformOneEnd = new Vector3(0f, 0.05f, 14.4f);
        public const float PlatformOneSpeed = 1.75f, PlatformOneRewind = 2f;
        public static readonly Vector3 PlateReminder = new Vector3(-1.6f, 0f, 18.5f);
        public const float DoorReminderZ = 25.5f, CheckpointTwoZ = 28f;

        // 2. A bridge that is only up while its plate is pressed.
        public static readonly Vector3 PlateBridge = new Vector3(1.6f, 0f, 31.5f);
        public const float PitTwoStart = 34f, PitTwoEnd = 48f, BridgeDrop = 6.3f, BridgeSpeed = 5f;
        public const float CheckpointThreeZ = 50.5f;

        // 3. A platform parked next to the player; only its rewind goes to the far side.
        public const float PitThreeStart = 53f, PitThreeEnd = 61.5f;
        public static readonly Vector3 PlatformThreeStart = new Vector3(0f, 0.05f, 59.9f), PlatformThreeEnd = new Vector3(0f, 0.05f, 54.6f);
        public const float PlatformThreeSpeed = 1.4f, PlatformThreeRewind = 1f;
        public const float CheckpointFourZ = 63.5f;

        // 4 and 5a. Elevators: a plate on a platform that sinks into a lethal shaft beside the walkway.
        public const float ShaftX0 = -3.3f, ShaftX1 = -0.2f, ElevatorX = -1.6f, ElevatorSize = 2.6f;
        public const float ShaftFourStart = 68f, ShaftFourEnd = 71f, ShaftFiveStart = 89f, ShaftFiveEnd = 92f;
        public const float ElevatorTop = 0.05f, ElevatorBottom = -2.95f, ElevatorSpeed = 0.8f, ElevatorRewind = 1.5f;
        public const float GratePhaseZ = 80f, CheckpointFiveZ = 84f;
        public static readonly Vector3 PlateDoorFive = new Vector3(1.6f, 0f, 97.6f);
        public const float DoorFiveZ = 99f, CheckpointFinalZ = 101.4f;

        // 5b. A hanging gate over the last pit; the platform's own trip carries the rider under it. Its plate is
        // far enough that, standing on it, the parked platform is out of Rewind range (12 m from the camera).
        public static readonly Vector3 PlateGate = new Vector3(-1.6f, 0f, 103.4f);
        public const float PitSixStart = 107f, PitSixEnd = 116f, GateZ = 112.8f;
        public static readonly Vector3 PlatformSixStart = new Vector3(0f, 0.05f, 108.6f), PlatformSixEnd = new Vector3(0f, 0.05f, 114.4f);
        public const float PlatformSixSpeed = 1.3f, PlatformSixRewind = 1.6f;
        public const float GoalZ = 120f;

        // Hints explain rules only, never the solution.
        public const string HintIntro = "Laboratorio de Paradojas. Objetivo: desbloquear el acceso al reactor.";
        public const string HintRewind = "Rewind: mantén clic derecho apuntando a un mecanismo y retrocede por el camino que ya recorrió (5 segundos como máximo). Al soltar, sigue su recorrido.";
        public const string HintEcho = "Q graba hasta 5 segundos. E hace que tu Echo lo repita desde el punto donde empezaste a grabar.";
        public const string HintBridge = "El puente solo está arriba mientras alguien pisa su placa.";
        public const string HintRide = "Rewind lleva consigo a quien esté encima del mecanismo.";
        public const string HintPhase = "Una placa sobre una plataforma viaja con ella. Tu Echo repite tu recorrido en el mismo sitio y al mismo ritmo, pero el mundo no lo espera.";
        public const string HintHold = "Mientras mantienes Rewind, el mecanismo no avanza. R te devuelve al último checkpoint y conserva tu última grabación terminada.";
        public const string HintTwoPlates = "Esta puerta solo se abre si sus dos placas están pulsadas a la vez.";
        public const string HintGate = "La compuerta se abre con su placa. Cerrada, no deja apuntar a través de ella y, si te alcanza, te empuja fuera. Tu Echo puede repetir su grabación otra vez cuando termina.";

        [MenuItem("Echoryx/Level 03/Build Paradojas")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
                throw new InvalidOperationException("TMP Essentials missing. Run Echoryx/Level 01/Import TMP Essentials first.");
            var actions = L2.Load<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            var playerPrefab = L2.Load<GameObject>("Assets/_Echoryx/Prefabs/Player.prefab");
            var plate = L2.Load<GameObject>(Prefabs + "PressurePlate.prefab");
            var door = L2.Load<GameObject>(Prefabs + "Door.prefab");
            var hazard = L2.Load<GameObject>(Prefabs + "Hazard.prefab");
            var goal = L2.Load<GameObject>(Prefabs + "Goal.prefab");
            var hint = L2.Load<GameObject>(Prefabs + "TutorialHint.prefab");
            var grate = L2.Load<GameObject>(L2.GratePath);
            var bar = L2.Load<Material>("Assets/_Echoryx/Materials/Level02/ArtGrateBar.mat");
            var m = L2.LevelMaterials();
            var a = L2.ArtMaterialsSet();
            var dark = LevelEnclosure.DarkMaterial();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            B.ConfigureLighting();
            var cm = B.CreatePlayerRig(scene, playerPrefab, Spawn, out var player, out var input, out var orbit);
            var carrier = player.GetComponent<PlayerPlatformCarrier>();

            var geometry = B.NewChild(null, "Level Geometry").transform;
            var art = B.NewChild(null, "Level Art").transform;
            BuildGeometry(geometry, art, a, dark);

            B.BuildHud(out var hintDisplay, out var hud);
            var systems = B.NewChild(null, "Level Systems");
            var stats = systems.AddComponent<LevelStats>();
            var checkpoints = B.NewChild(null, "Checkpoints");
            var manager = checkpoints.AddComponent<CheckpointManager>();
            var room1 = B.NewRoom(checkpoints.transform, "Room 1 Reminder", stats);
            var room2 = B.NewRoom(checkpoints.transform, "Room 2 Bridge", stats);
            var room3 = B.NewRoom(checkpoints.transform, "Room 3 Rewind Ride", stats);
            var room4 = B.NewRoom(checkpoints.transform, "Room 4 Phase", stats);
            var room5 = B.NewRoom(checkpoints.transform, "Room 5 Two Plates", stats);
            var room6 = B.NewRoom(checkpoints.transform, "Room 6 Final Gate", stats);
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
            B.NewCheckpoint(room4.transform, "Checkpoint End Room 3", new Vector3(0f, 0f, CheckpointFourZ), manager, room4, m);
            B.NewCheckpoint(room5.transform, "Checkpoint End Room 4", new Vector3(0f, 0f, CheckpointFiveZ), manager, room5, m);
            B.NewCheckpoint(room6.transform, "Checkpoint Final Stage", new Vector3(0f, 0f, CheckpointFinalZ), manager, room6, m);

            // 1. Reminder: Rewind the platform back and ride it, then the Echo holds a door.
            var platformOne = L2.BuildPlatform(room1.transform, "Room 1", PlatformOneStart, PlatformOneEnd, PlatformOneSpeed, PlatformOneRewind, carrier, m);
            B.Append(room1, platformOne.GetComponent<ParkedPlatformAttemptReset>());
            var plateReminder = B.Instantiate(plate, room1.transform, "Plate Reminder", PlateReminder);
            var doorReminder = B.Instantiate(door, room1.transform, "Door Reminder", new Vector3(0f, 0f, DoorReminderZ));
            B.Set(doorReminder.GetComponent<DoorController>(), "plate", plateReminder.GetComponent<PressurePlate>());
            B.Append(room1, plateReminder.GetComponent<PressurePlateAttemptReset>());
            B.Append(room1, doorReminder.GetComponent<DoorAttemptReset>());

            // 2. The bridge deck rests at the bottom of the pit and rises while its plate is pressed.
            var plateBridge = B.Instantiate(plate, room2.transform, "Plate Bridge", PlateBridge);
            var bridge = BuildBridge(room2.transform, a, bar);
            B.Set(bridge, "plate", plateBridge.GetComponent<PressurePlate>());
            B.Append(room2, plateBridge.GetComponent<PressurePlateAttemptReset>());
            B.Append(room2, bridge.GetComponent<DoorAttemptReset>());

            // 3. Parked next to the player: only its rewind crosses the pit.
            var platformThree = L2.BuildPlatform(room3.transform, "Room 3", PlatformThreeStart, PlatformThreeEnd, PlatformThreeSpeed, PlatformThreeRewind, carrier, m);
            B.Append(room3, platformThree.GetComponent<ParkedPlatformAttemptReset>());

            // 4. Phase: the grate's plate rides an elevator that sinks into the shaft.
            var elevatorFour = BuildElevator(room4, "Room 4", (ShaftFourStart + ShaftFourEnd) * 0.5f, plate, carrier, m, out var plateFour);
            var grateFour = B.Instantiate(grate, room4.transform, "Grate Phase", new Vector3(0f, 0f, GratePhaseZ));
            B.Set(grateFour.GetComponent<DoorController>(), "plate", plateFour);
            L2.Lintel(room4.transform, dark, GratePhaseZ);
            B.Append(room4, grateFour.GetComponent<DoorAttemptReset>());

            // 5a. A door with two plates: one in front of it, the other on a second elevator.
            var elevatorFive = BuildElevator(room5, "Room 5", (ShaftFiveStart + ShaftFiveEnd) * 0.5f, plate, carrier, m, out var plateFive);
            var plateDoor = B.Instantiate(plate, room5.transform, "Plate Door Room 5", PlateDoorFive);
            var doorFive = B.Instantiate(door, room5.transform, "Door Two Plates", new Vector3(0f, 0f, DoorFiveZ));
            B.Set(doorFive.GetComponent<DoorController>(), "plate", plateDoor.GetComponent<PressurePlate>());
            L2.SetArray(doorFive.GetComponent<DoorController>(), "extraPlates", plateFive);
            B.Append(room5, plateDoor.GetComponent<PressurePlateAttemptReset>());
            B.Append(room5, doorFive.GetComponent<DoorAttemptReset>());

            // 5b. Hanging gate (bottom 0.4 m: above the platform, below a rider); the platform parks behind it.
            var plateGate = B.Instantiate(plate, room6.transform, "Plate Gate", PlateGate);
            var gateSix = B.Instantiate(grate, room6.transform, "Hanging Gate", new Vector3(0f, 0f, GateZ));
            var gatePanel = gateSix.transform.Find("Grate Panel");
            gatePanel.localPosition = new Vector3(0f, 3.2f, 0f);
            gatePanel.localScale = new Vector3(1f, 5.6f / 6f, 1f);
            B.Set(gateSix.GetComponent<DoorController>(), "plate", plateGate.GetComponent<PressurePlate>());
            L2.SetVector(gateSix.GetComponent<DoorController>(), "thresholdSize", new Vector3(6f, 4f, 1.2f));
            L2.Lintel(room6.transform, dark, GateZ);
            var platformSix = L2.BuildPlatform(room6.transform, "Room 6", PlatformSixStart, PlatformSixEnd, PlatformSixSpeed, PlatformSixRewind, carrier, m);
            B.Append(room6, plateGate.GetComponent<PressurePlateAttemptReset>());
            B.Append(room6, gateSix.GetComponent<DoorAttemptReset>());
            B.Append(room6, platformSix.GetComponent<ParkedPlatformAttemptReset>());

            L2.SetArray(stats, "watchedMechanisms",
                platformOne.GetComponent<RewindableMechanism>(), platformThree.GetComponent<RewindableMechanism>(),
                elevatorFour.GetComponent<RewindableMechanism>(), elevatorFive.GetComponent<RewindableMechanism>(),
                platformSix.GetComponent<RewindableMechanism>());

            foreach (var (name, from, to) in new[] { ("1", PitOneStart, PitOneEnd), ("2", PitTwoStart, PitTwoEnd), ("3", PitThreeStart, PitThreeEnd), ("6", PitSixStart, PitSixEnd) })
                B.NewHazard(hazard, geometry, "Hazard Pit " + name, manager, new Vector3(0f, -5f, (from + to) * 0.5f), new Vector3(6.4f, 4f, to - from));
            // Shafts: lethal from 1.5 m down, so the elevator parks where nobody can stand on it.
            foreach (var (name, from, to) in new[] { ("4", ShaftFourStart, ShaftFourEnd), ("5", ShaftFiveStart, ShaftFiveEnd) })
                B.NewHazard(hazard, geometry, "Hazard Shaft " + name, manager, new Vector3(0f, -3.75f, (from + to) * 0.5f), new Vector3(6.4f, 4.5f, to - from));
            var goalObject = B.Instantiate(goal, geometry, "Goal", new Vector3(0f, 0f, GoalZ));
            B.Set(goalObject.GetComponent<GoalZone>(), "stats", stats);
            B.Set(hud.GetComponent<LevelCompletePanel>(), "stats", stats);

            var hints = B.NewChild(null, "Tutorial Hints").transform;
            B.NewHint(hint, hints, hintDisplay, "Hint Intro", new Vector3(0f, 2f, -0.6f), new Vector3(6f, 4f, 2.6f), HintIntro);
            B.NewHint(hint, hints, hintDisplay, "Hint Rewind", new Vector3(0f, 2f, 3.4f), new Vector3(6f, 4f, 4.4f), HintRewind);
            B.NewHint(hint, hints, hintDisplay, "Hint Echo", new Vector3(0f, 2f, 18.2f), new Vector3(6f, 4f, 3.6f), HintEcho);
            B.NewHint(hint, hints, hintDisplay, "Hint Bridge", new Vector3(0f, 2f, 30f), new Vector3(6f, 4f, 2.4f), HintBridge);
            B.NewHint(hint, hints, hintDisplay, "Hint Rewind Ride", new Vector3(0f, 2f, 52.2f), new Vector3(6f, 4f, 1.6f), HintRide);
            B.NewHint(hint, hints, hintDisplay, "Hint Phase", new Vector3(0f, 2f, 66f), new Vector3(6f, 4f, 3.2f), HintPhase);
            B.NewHint(hint, hints, hintDisplay, "Hint Hold", new Vector3(1.4f, 2f, 74f), new Vector3(3.2f, 4f, 3f), HintHold);
            B.NewHint(hint, hints, hintDisplay, "Hint Two Plates", new Vector3(0f, 2f, 86.6f), new Vector3(6f, 4f, 2.4f), HintTwoPlates);
            B.NewHint(hint, hints, hintDisplay, "Hint Gate", new Vector3(0f, 2f, 106f), new Vector3(6f, 4f, 1.8f), HintGate);

            L2.CheckpointMarkers(a);
            Art.MarkScenery(art);
            Art.ConfigureCamera();
            L2.Atmosphere(a);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save " + ScenePath);
            AddToCatalog();
            GameFlowBuilder.SyncBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("Level_03_Paradojas built.");
        }

        // ---------------------------------------------------------------- geometry and art

        private static void BuildGeometry(Transform logic, Transform art, Art.Mats a, Material dark)
        {
            // Floor blocks; the two shafts are holes between walkway blocks.
            L2.Block(logic, "Floor 1", -3.3f, 3.3f, -1f, 0f, StartZ, PitOneStart);
            L2.Block(logic, "Floor 2", -3.3f, 3.3f, -1f, 0f, PitOneEnd, PitTwoStart);
            L2.Block(logic, "Floor 3", -3.3f, 3.3f, -1f, 0f, PitTwoEnd, PitThreeStart);
            L2.Block(logic, "Floor 4", -3.3f, 3.3f, -1f, 0f, PitThreeEnd, ShaftFourStart);
            L2.Block(logic, "Floor 4 Walkway", ShaftX1, 3.3f, -1f, 0f, ShaftFourStart, ShaftFourEnd);
            L2.Block(logic, "Floor 5", -3.3f, 3.3f, -1f, 0f, ShaftFourEnd, ShaftFiveStart);
            L2.Block(logic, "Floor 5 Walkway", ShaftX1, 3.3f, -1f, 0f, ShaftFiveStart, ShaftFiveEnd);
            L2.Block(logic, "Floor 6", -3.3f, 3.3f, -1f, 0f, ShaftFiveEnd, PitSixStart);
            L2.Block(logic, "Floor 7 Goal", -3.3f, 3.3f, -1f, 0f, PitSixEnd, EndZ);
            var length = EndZ - StartZ;
            var middle = (StartZ + EndZ) * 0.5f;
            B.Cube(logic, "Wall Left", new Vector3(-3.15f, 1f, middle), new Vector3(0.3f, 10f, length), null).isStatic = true;
            B.Cube(logic, "Wall Right", new Vector3(3.15f, 1f, middle), new Vector3(0.3f, 10f, length), null).isStatic = true;
            B.Cube(logic, "Wall Start", new Vector3(0f, 1f, StartZ - 0.15f), new Vector3(6.6f, 10f, 0.3f), null).isStatic = true;
            B.Cube(logic, "Wall End", new Vector3(0f, 1f, EndZ + 0.15f), new Vector3(6.6f, 10f, 0.3f), null).isStatic = true;
            foreach (var renderer in logic.GetComponentsInChildren<MeshRenderer>()) renderer.enabled = false;

            // Visuals: Kenney tiles and hull (none under the shafts), walls, closed pits and lined shafts.
            Art.Floor(art, "Floor 1", StartZ, PitOneStart, a);
            Art.Floor(art, "Floor 2", PitOneEnd, PitTwoStart, a);
            Art.Floor(art, "Floor 3", PitTwoEnd, PitThreeStart, a);
            Art.Floor(art, "Floor 4", PitThreeEnd, ShaftFourStart, a);
            L2.FloorPiece(art, "Floor 4 Walkway", ShaftX1, 3.3f, ShaftFourStart, ShaftFourEnd, 0f, a);
            Art.Floor(art, "Floor 5", ShaftFourEnd, ShaftFiveStart, a);
            L2.FloorPiece(art, "Floor 5 Walkway", ShaftX1, 3.3f, ShaftFiveStart, ShaftFiveEnd, 0f, a);
            Art.Floor(art, "Floor 6", ShaftFiveEnd, PitSixStart, a);
            Art.Floor(art, "Floor 7", PitSixEnd, EndZ, a);

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
            LevelEnclosure.Backing(art, art, dark);
            Art.Display(art, "Display 1", new Vector3(2.92f, 3.7f, 20f), -90f, a);
            Art.Display(art, "Display 2", new Vector3(-2.92f, 3.7f, 41f), 90f, a);
            Art.Display(art, "Display 3", new Vector3(2.92f, 3.7f, 69.5f), -90f, a);
            Art.Display(art, "Display 4", new Vector3(2.92f, 3.7f, 90.5f), -90f, a);
            Art.Display(art, "Display 5", new Vector3(-2.92f, 3.7f, 120f), 90f, a);
            LevelEnclosure.EnclosePits(art, dark, ("Pit 1", PitOneStart, PitOneEnd), ("Pit 2", PitTwoStart, PitTwoEnd),
                ("Pit 3", PitThreeStart, PitThreeEnd), ("Pit 6", PitSixStart, PitSixEnd));
            foreach (var (name, from, to) in new[] { ("Shaft 4", ShaftFourStart, ShaftFourEnd), ("Shaft 5", ShaftFiveStart, ShaftFiveEnd) })
            {
                var z = (from + to) * 0.5f;
                const float depth = 6.2f;
                LevelEnclosure.Lining(art, name + " Inner", new Vector3(ShaftX1 - 0.02f, -depth * 0.5f, z), new Vector3(0.04f, depth, to - from), dark);
                LevelEnclosure.Lining(art, name + " Outer", new Vector3(-2.98f, -depth * 0.5f, z), new Vector3(0.04f, depth, to - from), dark);
                LevelEnclosure.Lining(art, name + " Front", new Vector3((-3f + ShaftX1) * 0.5f, -depth * 0.5f, from + 0.02f), new Vector3(ShaftX1 + 3f, depth, 0.04f), dark);
                LevelEnclosure.Lining(art, name + " Back", new Vector3((-3f + ShaftX1) * 0.5f, -depth * 0.5f, to - 0.02f), new Vector3(ShaftX1 + 3f, depth, 0.04f), dark);
                LevelEnclosure.Lining(art, name + " Bottom", new Vector3((-3f + ShaftX1) * 0.5f, -depth, z), new Vector3(ShaftX1 + 3f, 0.04f, to - from), dark);
            }
        }

        /// <summary>
        /// A DoorController whose panel is a 14 m deck resting 6.3 m down the pit: "open" means raised to the floor.
        /// Its threshold sits far below, so the deck never waits for a player on it: it sinks as soon as the plate
        /// is free and whoever stands on it falls into the pit.
        /// </summary>
        private static DoorController BuildBridge(Transform parent, Art.Mats a, Material bar)
        {
            var root = B.NewChild(parent, "Bridge");
            root.transform.position = new Vector3(0f, 0f, (PitTwoStart + PitTwoEnd) * 0.5f);
            var deck = B.NewChild(root.transform, "Bridge Deck");
            deck.transform.localPosition = new Vector3(0f, -0.2f - BridgeDrop, 0f);
            var length = PitTwoEnd - PitTwoStart - 0.1f;
            var collider = deck.AddComponent<BoxCollider>();
            collider.size = new Vector3(5.9f, 0.4f, length);
            const int tiles = 4;
            for (var i = 0; i < tiles; i++)
                Art.Spawn(Art.Modular, "template-floor-layer", deck.transform, "Deck Tile " + i,
                    new Vector3(0f, -0.2f, -length * 0.5f + (i + 0.5f) * length / tiles), Vector3.zero,
                    new Vector3(5.9f / 4.2f, 1f, length / tiles / 4.2f), a.Floor);
            foreach (var x in new[] { -2.9f, 2.9f })
            {
                var edge = B.Cube(deck.transform, "Deck Edge", new Vector3(x, 0.22f, 0f), new Vector3(0.1f, 0.06f, length), bar);
                UnityEngine.Object.DestroyImmediate(edge.GetComponent<Collider>());
            }
            var threshold = B.NewChild(root.transform, "Threshold").transform;
            threshold.localPosition = new Vector3(0f, -40f, 0f);
            var door = root.AddComponent<DoorController>();
            B.Set(door, "doorPanel", deck.transform);
            B.Set(door, "threshold", threshold);
            var data = new SerializedObject(door);
            data.FindProperty("thresholdSize").vector3Value = new Vector3(0.1f, 0.1f, 0.1f);
            data.FindProperty("openOffset").vector3Value = Vector3.up * BridgeDrop;
            data.FindProperty("moveSpeed").floatValue = BridgeSpeed;
            data.ApplyModifiedPropertiesWithoutUndo();
            root.AddComponent<DoorAttemptReset>();
            return door;
        }

        /// <summary>
        /// A one-way platform that starts flush with the floor and sinks into its shaft, carrying a pressure plate.
        /// Its parked position is inside the shaft hazard: the plate can only be stood on while Rewind holds the
        /// elevator up. The plate's own collider is removed so a rider stands on the platform surface.
        /// </summary>
        private static GameObject BuildElevator(RoomResetScope room, string label, float z, GameObject platePrefab,
            PlayerPlatformCarrier carrier, B.Materials m, out PressurePlate plate)
        {
            var parent = room.transform;
            var top = new Vector3(ElevatorX, ElevatorTop, z);
            var bottom = new Vector3(ElevatorX, ElevatorBottom, z);
            var root = L2.BuildPlatform(parent, label, top, bottom, ElevatorSpeed, ElevatorRewind, carrier, m);
            var surface = root.transform.Find("Platform Surface and Collider");
            surface.localScale = new Vector3(ElevatorSize, 0.4f, ElevatorSize);
            var visual = root.transform.Find("Platform Art");
            visual.localScale = new Vector3(ElevatorSize / 5f, 1f, ElevatorSize / 3f);
            var plateObject = B.Instantiate(platePrefab, root.transform, "Plate Elevator " + label, top);
            PrefabUtility.UnpackPrefabInstance(plateObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            plateObject.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            foreach (var plateCollider in plateObject.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(plateCollider);
            L2.EmphasizeBeacon(plateObject);
            plate = plateObject.GetComponent<PressurePlate>();
            B.Append(room, root.GetComponent<ParkedPlatformAttemptReset>());
            B.Append(room, plateObject.GetComponent<PressurePlateAttemptReset>());
            return root;
        }

        // ---------------------------------------------------------------- catalog

        private static void AddToCatalog()
        {
            var catalog = L2.Load<LevelCatalog>(GameFlowBuilder.CatalogPath);
            if (catalog.IndexOfScene(ScenePath) >= 0) return;
            var data = new SerializedObject(catalog);
            var levels = data.FindProperty("levels");
            var previous = -1;
            for (var i = 0; i < levels.arraySize; i++)
                if (levels.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == L2.LevelId) previous = i;
            var index = previous >= 0 ? previous + 1 : levels.arraySize; // right after Level 02 (at the end if it is missing)
            levels.InsertArrayElementAtIndex(index);
            var entry = levels.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").stringValue = LevelId;
            entry.FindPropertyRelative("displayName").stringValue = "Paradojas";
            entry.FindPropertyRelative("scenePath").stringValue = ScenePath;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }
    }
}
