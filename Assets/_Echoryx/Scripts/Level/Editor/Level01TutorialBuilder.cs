using System;
using System.Collections.Generic;
using Echoryx.Echo;
using Echoryx.Game;
using Echoryx.Interactions;
using Echoryx.Player;
using Echoryx.Rewind;
using Echoryx.Rewind.Platforms;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Echoryx.Level.Editor
{
    /// <summary>
    /// Builds the reusable level prefabs and Level_01_Tutorial with Unity's own APIs.
    /// Solution route: see docs/level-01-tutorial.md.
    /// </summary>
    public static class Level01TutorialBuilder
    {
        public const string ScenePath = "Assets/_Echoryx/Scenes/Level_01_Tutorial.unity";
        public const string PrefabFolder = "Assets/_Echoryx/Prefabs/Level";
        private const string MaterialFolder = "Assets/_Echoryx/Materials/Level";
        private const string PlayerPrefabPath = "Assets/_Echoryx/Prefabs/Player.prefab";
        private const string ActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string TmpEssentials =
            "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        // Layout (meters, +Z is forward, floor top is y = 0). Corridor width is 6 (x in [-3, 3]).
        // Rewind rewinds a mechanism, not the player: hints are guiding questions, not the solution.
        public const string RewindPlatformHint = "La plataforma se fue sin ti. ¿Y si pudieras rebobinar su movimiento? Mantén clic derecho apuntando a ella (Rewind).";
        public const string RewindRetryHint = "Si la plataforma se te escapa o caes, ¿puedes hacer que empiece su recorrido de nuevo y probar otra estrategia?";

        private const float WallX = 3.15f;
        private const float PitOneStart = 8f, PitOneEnd = 10.5f;
        private const float DoorZ = 30f, PlateZ = 21f;
        private const float PitTwoStart = 36f, PitTwoEnd = 45f;
        private const float LevelStartZ = -2f, LevelEndZ = 56f;

        private sealed class Materials
        {
            public Material Floor, Wall, Plate, Door, Hazard, Goal, Platform, Checkpoint, Step;
        }

        [MenuItem("Echoryx/Level 01/Import TMP Essentials")]
        public static void ImportTmpEssentials()
        {
            AssetDatabase.ImportPackage(TmpEssentials, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("TMP Essential Resources imported.");
        }

        /// <summary>
        /// Migrates an existing Level_01_Tutorial: the room 3 platform resets parked at its destination
        /// (out of reach) instead of at its start next to the checkpoint. Idempotent.
        /// </summary>
        [MenuItem("Echoryx/Level 01/Apply Parked Platform Reset")]
        public static void ApplyParkedPlatformReset()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var platform = UnityEngine.Object.FindFirstObjectByType<RewindMovingPlatform>()
                ?? throw new InvalidOperationException("Level has no RewindMovingPlatform.");
            var room = platform.GetComponentInParent<RoomResetScope>();
            var parked = platform.GetComponent<ParkedPlatformAttemptReset>();
            if (parked == null) parked = platform.gameObject.AddComponent<ParkedPlatformAttemptReset>();
            var old = platform.GetComponent<RewindMechanismAttemptReset>();
            var data = new SerializedObject(room);
            var list = data.FindProperty("participants");
            var replaced = false;
            for (var i = 0; i < list.arraySize; i++)
                if (old != null && list.GetArrayElementAtIndex(i).objectReferenceValue == old)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = parked;
                    replaced = true;
                }
            if (!replaced)
            {
                var present = false;
                for (var i = 0; i < list.arraySize; i++) present |= list.GetArrayElementAtIndex(i).objectReferenceValue == parked;
                if (!present) { list.arraySize++; list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = parked; }
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + ScenePath);
            Debug.Log("Room 3 platform now resets parked at its destination.");
        }

        [MenuItem("Echoryx/Level 01/Build Tutorial")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
                throw new InvalidOperationException("TMP Essentials missing. Run Echoryx/Level 01/Import TMP Essentials first.");
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath)
                ?? throw new InvalidOperationException("InputSystem_Actions asset not found.");
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath)
                ?? throw new InvalidOperationException("Player prefab not found.");

            EnsureFolder("Assets/_Echoryx/Prefabs", "Level");
            EnsureFolder("Assets/_Echoryx/Materials", "Level");
            var m = BuildMaterials();
            var platePrefab = BuildPlatePrefab(m);
            var doorPrefab = BuildDoorPrefab(m);
            var hazardPrefab = BuildHazardPrefab(m);
            var goalPrefab = BuildGoalPrefab(m);
            var hintPrefab = BuildHintPrefab();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            ConfigureLighting();

            // Player + camera rig (same wiring as the S01 prototype).
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            var anchor = player.transform.Find("CameraAnchor");
            var input = player.GetComponent<PlayerInputReader>();
            var rig = new GameObject("Camera Orbit Target");
            var orbit = rig.AddComponent<PlayerCameraOrbit>();
            orbit.SetTargets(anchor, input);
            rig.transform.SetPositionAndRotation(anchor.position, Quaternion.Euler(12f, 0f, 0f));
            player.GetComponent<PlayerMotor>().SetCameraReference(rig.transform);
            var mainCamera = Camera.main ?? throw new InvalidOperationException("Default scene lacks a Main Camera.");
            if (mainCamera.GetComponent<CinemachineBrain>() == null) mainCamera.gameObject.AddComponent<CinemachineBrain>();
            var virtualCamera = new GameObject("Player Cinemachine Camera");
            var cm = virtualCamera.AddComponent<CinemachineCamera>();
            cm.Follow = rig.transform;
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

            // Static geometry.
            var geometry = NewChild(null, "Level Geometry");
            BuildGeometry(geometry.transform, m);

            // HUD.
            BuildHud(out var hintDisplay, out var completePanelObject);

            // Reset scopes (one per room) and managers.
            var systems = NewChild(null, "Level Systems");
            var stats = systems.AddComponent<LevelStats>();
            var checkpoints = NewChild(null, "Checkpoints");
            var manager = checkpoints.AddComponent<CheckpointManager>();
            var room1 = NewRoom(checkpoints.transform, "Room 1 Movement", stats);
            var room2 = NewRoom(checkpoints.transform, "Room 2 Echo", stats);
            var room3 = NewRoom(checkpoints.transform, "Room 3 Rewind", stats);

            var initialSpawn = NewChild(room1.transform, "Initial Spawn").transform;
            initialSpawn.SetPositionAndRotation(player.transform.position, player.transform.rotation);

            var echoReset = checkpoints.AddComponent<EchoAttemptReset>();
            Set(echoReset, "playback", player.GetComponent<EchoPlayback>());
            var rewindRelease = checkpoints.AddComponent<RewindAttemptRelease>();
            Set(rewindRelease, "interactor", player.GetComponent<RewindInteractor>());
            Set(manager, "player", player.GetComponent<PlayerMotor>());
            Set(manager, "initialSpawn", initialSpawn);
            Set(manager, "initialRoom", room1);
            Set(manager, "rewindRelease", rewindRelease);
            Set(manager, "echoReset", echoReset);
            Set(manager, "cameraOrbit", orbit);
            Set(manager, "gameplayCamera", cm);
            var restart = checkpoints.AddComponent<RestartInput>();
            Set(restart, "actions", actions);
            Set(restart, "manager", manager);
            Set(restart, "input", input);

            // Checkpoints: end of room 1 resets room 2; end of room 2 resets room 3.
            NewCheckpoint(room2.transform, "Checkpoint End Room 1", new Vector3(0f, 0f, 15.5f), manager, room2, m);
            NewCheckpoint(room3.transform, "Checkpoint End Room 2", new Vector3(0f, 0f, 34.5f), manager, room3, m);

            // Hazards under both pits.
            NewHazard(hazardPrefab, geometry.transform, "Hazard Pit 1", manager,
                new Vector3(0f, -5f, (PitOneStart + PitOneEnd) * 0.5f), new Vector3(6.4f, 4f, PitOneEnd - PitOneStart));
            NewHazard(hazardPrefab, geometry.transform, "Hazard Pit 2", manager,
                new Vector3(0f, -5f, (PitTwoStart + PitTwoEnd) * 0.5f), new Vector3(6.4f, 4f, PitTwoEnd - PitTwoStart));

            // Room 2: plate and door.
            var plate = Instantiate(platePrefab, room2.transform, "Echo Plate", new Vector3(0f, 0f, PlateZ));
            var door = Instantiate(doorPrefab, room2.transform, "Echo Door", new Vector3(0f, 0f, DoorZ));
            Set(door.GetComponent<DoorController>(), "plate", plate.GetComponent<PressurePlate>());
            Append(room2, plate.GetComponent<PressurePlateAttemptReset>());
            Append(room2, door.GetComponent<DoorAttemptReset>());

            // Room 3: rewindable platform across the second pit.
            var platform = BuildPlatform(room3.transform, m, player.GetComponent<PlayerPlatformCarrier>());
            Append(room3, platform.GetComponent<ParkedPlatformAttemptReset>());
            var statsData = new SerializedObject(stats);
            var watched = statsData.FindProperty("watchedMechanisms");
            watched.arraySize = 1;
            watched.GetArrayElementAtIndex(0).objectReferenceValue = platform.GetComponent<RewindableMechanism>();
            statsData.ApplyModifiedPropertiesWithoutUndo();

            // Goal.
            var goal = Instantiate(goalPrefab, geometry.transform, "Goal", new Vector3(0f, 0f, 51f));
            Set(goal.GetComponent<GoalZone>(), "stats", stats);
            Set(completePanelObject.GetComponent<LevelCompletePanel>(), "stats", stats);

            // Hints (text is Spanish player-facing copy; room 2 hints are guiding questions).
            var hints = NewChild(null, "Tutorial Hints").transform;
            NewHint(hintPrefab, hints, hintDisplay, "Hint Controls", new Vector3(0f, 2f, 1.5f), new Vector3(6f, 4f, 5f),
                "WASD para moverte, ratón para mirar y Espacio para saltar. Llega al checkpoint del fondo.");
            NewHint(hintPrefab, hints, hintDisplay, "Hint Fall", new Vector3(0f, 2f, 6.75f), new Vector3(6f, 4f, 2.5f),
                "Si caes vuelves al último checkpoint. Con R también puedes reiniciar el intento.");
            NewHint(hintPrefab, hints, hintDisplay, "Hint Echo Door", new Vector3(0f, 2f, 18.5f), new Vector3(6f, 4f, 2f),
                "¿Cómo puedes mantener la placa presionada y cruzar al mismo tiempo?");
            NewHint(hintPrefab, hints, hintDisplay, "Hint Echo Plate", new Vector3(0f, 2f, 22.5f), new Vector3(6f, 4f, 4.5f),
                "¿Y si algo pudiera repetir tus pasos por ti? Q graba y detiene, E reproduce tu Echo.");
            NewHint(hintPrefab, hints, hintDisplay, "Hint Rewind Platform", new Vector3(0f, 2f, 32.25f), new Vector3(6f, 4f, 4.5f),
                RewindPlatformHint);
            NewHint(hintPrefab, hints, hintDisplay, "Hint Rewind Retry", new Vector3(0f, 2f, 35.25f), new Vector3(6f, 4f, 1.5f),
                RewindRetryHint);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save " + ScenePath);
            AddToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("Level_01_Tutorial built and added to Build Settings.");
        }

        // ---------------------------------------------------------------- materials

        private static Materials BuildMaterials() => new Materials
        {
            Floor = Lit("LevelFloor", new Color(0.55f, 0.58f, 0.62f)),
            Wall = Lit("LevelWall", new Color(0.30f, 0.34f, 0.42f)),
            Step = Lit("LevelStep", new Color(0.45f, 0.50f, 0.58f)),
            Plate = Lit("LevelPlate", new Color(0.85f, 0.28f, 0.08f)),
            Door = Lit("LevelDoor", new Color(0.15f, 0.45f, 0.85f)),
            Hazard = Lit("LevelHazard", new Color(0.80f, 0.08f, 0.10f)),
            Goal = Lit("LevelGoal", new Color(0.30f, 0.90f, 0.30f)),
            Platform = Lit("LevelPlatform", new Color(0.55f, 0.30f, 0.90f)),
            Checkpoint = Lit("LevelCheckpoint", new Color(1f, 0.65f, 0.15f)),
        };

        private static Material Lit(string name, Color color)
        {
            var path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? throw new InvalidOperationException("URP Lit shader unavailable.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.15f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------------------------------------------------------------- prefabs

        private static GameObject BuildPlatePrefab(Materials m)
        {
            var root = new GameObject("PressurePlate");
            var surface = Cube(root.transform, "Plate Surface", new Vector3(0f, 0.075f, 0f), new Vector3(2f, 0.15f, 2f), m.Plate);
            var plate = root.AddComponent<PressurePlate>();
            Set(plate, "indicator", surface.GetComponent<Renderer>());
            root.AddComponent<PressurePlateAttemptReset>();
            root.AddComponent<PressurePlateEvents>();
            return SavePrefab(root, "PressurePlate");
        }

        private static GameObject BuildDoorPrefab(Materials m)
        {
            var root = new GameObject("Door");
            var panel = Cube(root.transform, "Door Panel", new Vector3(0f, 3f, 0f), new Vector3(6f, 6f, 0.6f), m.Door);
            var threshold = NewChild(root.transform, "Threshold").transform;
            threshold.localPosition = Vector3.up;
            var door = root.AddComponent<DoorController>();
            Set(door, "doorPanel", panel.transform);
            Set(door, "threshold", threshold);
            var data = new SerializedObject(door);
            data.FindProperty("thresholdSize").vector3Value = new Vector3(6f, 4f, 1.8f);
            data.FindProperty("openOffset").vector3Value = Vector3.up * 6.3f;
            data.FindProperty("moveSpeed").floatValue = 12f;
            data.ApplyModifiedPropertiesWithoutUndo();
            root.AddComponent<DoorAttemptReset>();
            return SavePrefab(root, "Door");
        }

        private static GameObject BuildHazardPrefab(Materials m)
        {
            // Unit volume; instances are scaled to the pit. RespawnZone already restarts at the last checkpoint.
            var root = new GameObject("Hazard");
            var volume = root.AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.size = Vector3.one;
            root.AddComponent<RespawnZone>();
            var visual = Cube(root.transform, "Hazard Surface", Vector3.zero, Vector3.one, m.Hazard);
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            return SavePrefab(root, "Hazard");
        }

        private static GameObject BuildGoalPrefab(Materials m)
        {
            var root = new GameObject("Goal");
            var volume = root.AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.center = new Vector3(0f, 1.5f, 0f);
            volume.size = new Vector3(4f, 3f, 3f);
            root.AddComponent<GoalZone>();
            var pad = Cube(root.transform, "Goal Pad", new Vector3(0f, 0.05f, 0f), new Vector3(3.5f, 0.1f, 3f), m.Goal);
            UnityEngine.Object.DestroyImmediate(pad.GetComponent<Collider>());
            var pillar = Cube(root.transform, "Goal Pillar", new Vector3(0f, 1.5f, 1.2f), new Vector3(0.5f, 3f, 0.5f), m.Goal);
            UnityEngine.Object.DestroyImmediate(pillar.GetComponent<Collider>());
            return SavePrefab(root, "Goal");
        }

        private static GameObject BuildHintPrefab()
        {
            var root = new GameObject("TutorialHint");
            var volume = root.AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.size = new Vector3(4f, 4f, 4f);
            root.AddComponent<TutorialHint>();
            return SavePrefab(root, "TutorialHint");
        }

        private static GameObject SavePrefab(GameObject root, string name)
        {
            var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return saved ?? throw new InvalidOperationException("Could not save prefab " + name);
        }

        // ---------------------------------------------------------------- scene content

        private static void ConfigureLighting()
        {
            var light = UnityEngine.Object.FindFirstObjectByType<Light>();
            if (light != null)
            {
                light.name = "Directional Light";
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.96f, 0.88f);
                light.intensity = 1.1f;
                light.shadows = LightShadows.Soft;
                light.transform.rotation = Quaternion.Euler(50f, -25f, 0f);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.46f, 0.50f, 0.58f);
        }

        private static void BuildGeometry(Transform parent, Materials m)
        {
            // Floors (top at y = 0).
            Block(parent, "Floor 1 Start", Span(LevelStartZ, PitOneStart), m.Floor, 6.6f);
            Block(parent, "Floor 2 Main", Span(PitOneEnd, PitTwoStart), m.Floor, 6.6f);
            Block(parent, "Floor 3 Goal", Span(PitTwoEnd, LevelEndZ), m.Floor, 6.6f);
            // Jump practice step beside the walking lane.
            var step = Cube(parent, "Low Step", new Vector3(-1.2f, 0.35f, 4.5f), new Vector3(2.4f, 0.7f, 1.2f), m.Step);
            step.isStatic = true;
            // Side walls reach below the floor so the pits read as pits; end walls close the level.
            var length = LevelEndZ - LevelStartZ;
            var centerZ = (LevelStartZ + LevelEndZ) * 0.5f;
            Cube(parent, "Wall Left", new Vector3(-WallX, 1f, centerZ), new Vector3(0.3f, 10f, length), m.Wall).isStatic = true;
            Cube(parent, "Wall Right", new Vector3(WallX, 1f, centerZ), new Vector3(0.3f, 10f, length), m.Wall).isStatic = true;
            Cube(parent, "Wall Start", new Vector3(0f, 1f, LevelStartZ - 0.15f), new Vector3(6.6f, 10f, 0.3f), m.Wall).isStatic = true;
            Cube(parent, "Wall End", new Vector3(0f, 1f, LevelEndZ + 0.15f), new Vector3(6.6f, 10f, 0.3f), m.Wall).isStatic = true;
        }

        private static GameObject BuildPlatform(Transform parent, Materials m, PlayerPlatformCarrier passenger)
        {
            var start = NewChild(parent, "Platform Start").transform;
            start.position = new Vector3(0f, 0.05f, 37.6f);
            var end = NewChild(parent, "Platform End").transform;
            end.position = new Vector3(0f, 0.05f, 43.4f);
            var root = NewChild(parent, "Rewind Platform");
            root.transform.position = start.position;
            var surface = Cube(root.transform, "Platform Surface and Collider", Vector3.zero, new Vector3(5f, 0.4f, 3f), m.Platform);
            var platform = root.AddComponent<RewindMovingPlatform>();
            Set(platform, "startPoint", start);
            Set(platform, "endPoint", end);
            Set(platform, "surface", surface.GetComponent<BoxCollider>());
            Set(platform, "passenger", passenger);
            var data = new SerializedObject(platform);
            data.FindProperty("speed").floatValue = 2.4f;
            data.FindProperty("rewindSpeed").floatValue = 1.5f;
            data.ApplyModifiedPropertiesWithoutUndo();
            root.AddComponent<ParkedPlatformAttemptReset>(); // restores and parks the platform out of reach after a reset
            return root;
        }

        private static RoomResetScope NewRoom(Transform parent, string name, LevelStats stats)
        {
            var room = NewChild(parent, name);
            var scope = room.AddComponent<RoomResetScope>();
            var counterObject = NewChild(room.transform, "Attempt Counter");
            var counter = counterObject.AddComponent<LevelStatsAttemptCounter>();
            Set(counter, "stats", stats);
            Append(scope, counter);
            return scope;
        }

        private static void NewCheckpoint(Transform parent, string name, Vector3 position,
            CheckpointManager manager, RoomResetScope room, Materials m)
        {
            var checkpoint = NewChild(parent, name);
            checkpoint.transform.position = position;
            var spawn = NewChild(checkpoint.transform, "Respawn Point").transform;
            spawn.localPosition = new Vector3(0f, 0.05f, 0f);
            var volume = checkpoint.AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.center = Vector3.up * 2f;
            volume.size = new Vector3(6f, 4f, 2f);
            var trigger = checkpoint.AddComponent<CheckpointTrigger>();
            Set(trigger, "manager", manager);
            Set(trigger, "room", room);
            Set(trigger, "spawnPoint", spawn);
            var marker = Cube(checkpoint.transform, "Checkpoint Marker", new Vector3(0f, 0.025f, 0f), new Vector3(5f, 0.05f, 1.5f), m.Checkpoint);
            UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
            Set(trigger, "indicator", marker.GetComponent<Renderer>());
            trigger.SetActive(false);
        }

        private static void NewHazard(GameObject prefab, Transform parent, string name,
            CheckpointManager manager, Vector3 position, Vector3 scale)
        {
            var instance = Instantiate(prefab, parent, name, position);
            instance.transform.localScale = scale;
            Set(instance.GetComponent<RespawnZone>(), "manager", manager);
        }

        private static void NewHint(GameObject prefab, Transform parent, TutorialHintDisplay display,
            string name, Vector3 center, Vector3 size, string message)
        {
            var instance = Instantiate(prefab, parent, name, center);
            var volume = instance.GetComponent<BoxCollider>();
            volume.size = size;
            var hint = instance.GetComponent<TutorialHint>();
            Set(hint, "message", message);
            Set(hint, "display", display);
        }

        // ---------------------------------------------------------------- HUD

        private static GameObject BuildHud(out TutorialHintDisplay hintDisplay, out GameObject completePanelObject)
        {
            var canvasObject = new GameObject("HUD Canvas", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var hintPanel = Panel(canvasObject.transform, "Hint Panel", new Color(0f, 0f, 0f, 0.65f));
            Anchor(hintPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(1300f, 120f));
            var hintLabel = Label(hintPanel.transform, "Hint Label", 38f, TextAlignmentOptions.Center);
            Stretch(hintLabel.rectTransform, 24f);
            hintDisplay = canvasObject.AddComponent<TutorialHintDisplay>();
            Set(hintDisplay, "panel", hintPanel.gameObject);
            Set(hintDisplay, "label", hintLabel);

            var endPanel = Panel(canvasObject.transform, "Level Complete Panel", new Color(0.05f, 0.08f, 0.12f, 0.92f));
            Anchor(endPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 520f));
            var title = Label(endPanel.transform, "Title", 72f, TextAlignmentOptions.Center);
            title.text = "Nivel completado";
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(900f, 110f));
            var attempts = Label(endPanel.transform, "Attempts", 42f, TextAlignmentOptions.Center);
            Anchor(attempts.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(900f, 70f));
            var rewinds = Label(endPanel.transform, "Rewinds", 42f, TextAlignmentOptions.Center);
            Anchor(rewinds.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(900f, 70f));
            var time = Label(endPanel.transform, "Time", 42f, TextAlignmentOptions.Center);
            Anchor(time.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(900f, 70f));
            endPanel.gameObject.SetActive(false);

            var complete = canvasObject.AddComponent<LevelCompletePanel>();
            Set(complete, "panel", endPanel.gameObject);
            Set(complete, "attemptsLabel", attempts);
            Set(complete, "rewindsLabel", rewinds);
            Set(complete, "timeLabel", time);
            hintPanel.gameObject.SetActive(false);
            completePanelObject = canvasObject;
            return canvasObject;
        }

        private static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go.GetComponent<RectTransform>();
        }

        private static TextMeshProUGUI Label(Transform parent, string name, float size, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static void Anchor(Component target, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)(target as RectTransform ?? target.transform);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin * 0.5f);
            rect.offsetMax = new Vector2(-margin, -margin * 0.5f);
        }

        // ---------------------------------------------------------------- helpers

        private static (float center, float length) Span(float from, float to) => ((from + to) * 0.5f, to - from);

        private static void Block(Transform parent, string name, (float center, float length) span, Material material, float width)
        {
            var block = Cube(parent, name, new Vector3(0f, -0.5f, span.center), new Vector3(width, 1f, span.length), material);
            block.isStatic = true;
        }

        private static GameObject Cube(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = scale;
            if (material != null) cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static GameObject NewChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject Instantiate(GameObject prefab, Transform parent, string name, Vector3 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            return instance;
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            var property = data.FindProperty(field) ?? throw new InvalidOperationException("Missing field " + field + " on " + target.GetType().Name);
            property.objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(UnityEngine.Object target, string field, string value)
        {
            var data = new SerializedObject(target);
            var property = data.FindProperty(field) ?? throw new InvalidOperationException("Missing field " + field + " on " + target.GetType().Name);
            property.stringValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Append(RoomResetScope room, MonoBehaviour participant)
        {
            var data = new SerializedObject(room);
            var list = data.FindProperty("participants");
            var index = list.arraySize;
            list.arraySize++;
            list.GetArrayElementAtIndex(index).objectReferenceValue = participant;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }

        private static void AddToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var existing in scenes)
                if (existing.path == ScenePath) { existing.enabled = true; EditorBuildSettings.scenes = scenes.ToArray(); return; }
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
