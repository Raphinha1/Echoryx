using System;
using System.IO;
using System.Linq;
using Echoryx.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Echoryx.Game.Editor
{
    /// <summary>Builds standard static components and prefab wrappers in an isolated scene.</summary>
    public static class S05ReferenceRoomBuilder
    {
        private const string Source = "Assets/_Echoryx/Art/Environment/Quaternius/ModularSciFiMegaKitStandard/Models/";
        private const string Prefabs = "Assets/_Echoryx/Prefabs/Environment";
        private const string RoomPath = "Assets/_Echoryx/Scenes/VisualReferenceRoom.unity";
        private const string Materials = "Assets/_Echoryx/Materials/Environment/";

        [MenuItem("Echoryx/S05-A2/Build Reference Room")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RoomPath) != null)
            {
                Debug.Log("S05-A2 room exists; no resources replaced. Use Validate Reference Room.");
                return;
            }
            if (!AssetDatabase.IsValidFolder(Prefabs)) AssetDatabase.CreateFolder("Assets/_Echoryx/Prefabs", "Environment");
            EnsurePrefab("Floor4", MakeFloor);
            EnsurePrefab("Wall4", MakeWall);
            EnsurePrefab("InnerCorner4", MakeCorner);
            EnsurePrefab("Column5", MakeColumn);
            EnsurePrefab("DoorFramePanel", MakeDoor);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var room = new GameObject("Visual Reference Room").transform;
            var floors = Child(room, "Floors");
            for (var x = -6; x <= 6; x += 4)
                for (var z = -6; z <= 6; z += 4) Place("Floor4", floors, new Vector3(x, 0f, z));
            Place("Floor4", floors, new Vector3(-2f, 0f, -10f));
            Place("Floor4", floors, new Vector3(2f, 0f, -10f));
            var walls = Child(room, "Walls and Corners");
            Place("InnerCorner4", walls, new Vector3(8f, 0f, -8f));
            Place("InnerCorner4", walls, new Vector3(-8f, 0f, -8f), 90f);
            Place("InnerCorner4", walls, new Vector3(8f, 0f, 8f), -90f);
            Place("InnerCorner4", walls, new Vector3(-8f, 0f, 8f), 180f);
            foreach (var z in new[] { -2f, 2f })
            {
                Place("Wall4", walls, new Vector3(-8f, 0f, z), 90f);
                Place("Wall4", walls, new Vector3(8f, 0f, z), -90f);
            }
            // The frame is 4.85 m wide, not a four-metre wall tile. Only these edge strips vary.
            var frameWidth = 4.85345f;
            var strip = 4f - frameWidth * 0.5f;
            foreach (var z in new[] { -8f, 8f })
                foreach (var sign in new[] { -1f, 1f })
                {
                    var wall = Place("Wall4", walls, new Vector3(sign * (frameWidth * 0.5f + strip * 0.5f), 0f, z), z < 0f ? 0f : 180f);
                    wall.transform.localScale = new Vector3(strip / 4f, 1f, 1f);
                    wall.name = "Door edge infill";
                }
            var doors = Child(room, "Static Doors");
            var entrance = Place("DoorFramePanel", doors, new Vector3(0f, 0f, -8f));
            entrance.name = "Open Entrance";
            entrance.transform.Find("Panel").gameObject.SetActive(false);
            var closed = Place("DoorFramePanel", doors, new Vector3(0f, 0f, 8f), 180f);
            closed.name = "Closed Door Exhibit";
            // Short vestibule has a continuous floor and boundary, not a fall/respawn mechanic.
            Place("Wall4", walls, new Vector3(-4f, 0f, -10f), 90f);
            Place("Wall4", walls, new Vector3(4f, 0f, -10f), -90f);
            Place("Wall4", walls, new Vector3(-2f, 0f, -12f));
            Place("Wall4", walls, new Vector3(2f, 0f, -12f));
            var columns = Child(room, "Columns");
            foreach (var x in new[] { -6f, 6f })
                foreach (var z in new[] { -6f, 6f }) Place("Column5", columns, new Vector3(x, 0f, z));
            AddLightingAndSigns(room);
            AddPlayerAndCamera(scene);
            if (!EditorSceneManager.SaveScene(scene, RoomPath)) throw new InvalidOperationException("Cannot save reference room.");
            AssetDatabase.SaveAssets();
            Validate();
        }

        private static GameObject MakeFloor()
        {
            var root = new GameObject("Floor4");
            Model(root.transform, "Platforms/Platform_Metal", Vector3.zero);
            Solid(root.transform, "Floor Collision", new Vector3(0f, -0.15f, 0f), new Vector3(4f, 0.3f, 4f));
            Block(root.transform, "Floor thickness", new Vector3(0f, -0.155f, 0f), new Vector3(4f, 0.3f, 4f), "Graphite");
            return root;
        }

        private static GameObject MakeWall()
        {
            var root = new GameObject("Wall4");
            var visuals = Child(root.transform, "Visuals");
            // Canonical front faces local +Z; root is at the middle of the floor-level seam.
            Model(visuals, "Walls/WallAstra_Straight_Flat", Vector3.forward * 2f, 90f);
            Model(visuals, "Walls/TopSimple_Straight", Vector3.forward * 2f, 90f);
            Model(visuals, "Walls/BottomSimple_Straight", Vector3.forward * 2f, 90f);
            Block(visuals, "Opaque wall backing", new Vector3(0f, 2.5f, -0.16f), new Vector3(4f, 5f, 0.2f), "Graphite");
            Solid(root.transform, "Wall Collision", new Vector3(0f, 2.5f, -0.1f), new Vector3(4f, 5f, 0.3f));
            return root;
        }

        private static GameObject MakeCorner()
        {
            var root = new GameObject("InnerCorner4");
            var visuals = Child(root.transform, "Visuals");
            // Native planes x=4 and z=-4 meet here; arms extend -X and +Z.
            Model(visuals, "Walls/WallAstra_Corner_Square_Inner", new Vector3(-4f, 0f, 4f));
            Model(visuals, "Walls/TopSimple_Straight", new Vector3(-2f, 0f, 2f), 90f);
            Model(visuals, "Walls/TopSimple_Straight", new Vector3(-2f, 0f, 2f));
            Block(visuals, "West arm backing", new Vector3(-2f, 2.5f, -0.16f), new Vector3(4f, 5f, 0.2f), "Graphite");
            Block(visuals, "North arm backing", new Vector3(0.16f, 2.5f, 2f), new Vector3(0.2f, 5f, 4f), "Graphite");
            // Two arms, never a solid box over the empty corner interior. Slightly conservative for ornaments.
            Solid(root.transform, "West Arm Collision", new Vector3(-1.9f, 2.5f, -0.03f), new Vector3(4.8f, 5f, 0.9f));
            Solid(root.transform, "North Arm Collision", new Vector3(0.03f, 2.5f, 1.9f), new Vector3(0.9f, 5f, 4.8f));
            return root;
        }

        private static GameObject MakeColumn()
        {
            var root = new GameObject("Column5");
            Model(root.transform, "Columns/Column_Simple", new Vector3(0f, 0f, 0.01878f));
            Solid(root.transform, "Column Collision", new Vector3(0f, 2.5f, 0f), new Vector3(0.51f, 5f, 0.98f));
            return root;
        }

        private static GameObject MakeDoor()
        {
            var root = new GameObject("DoorFramePanel");
            var frame = Child(root.transform, "Frame");
            Model(frame, "Platforms/Door_Frame_Square", Vector3.zero);
            Solid(frame, "Left Post Collision", new Vector3(-1.83f, 1.8f, 0f), new Vector3(1.2f, 3.6f, 0.51f));
            Solid(frame, "Right Post Collision", new Vector3(1.83f, 1.8f, 0f), new Vector3(1.2f, 3.6f, 0.51f));
            Solid(frame, "Header Collision", new Vector3(0f, 4.1f, 0f), new Vector3(4.86f, 1.8f, 0.51f));
            var panel = Child(root.transform, "Panel");
            // Different source modules need a local fit. Leave the original FBX/pivot untouched.
            var leaf = Model(panel, "Platforms/Door_DarkMetal", new Vector3(-1.3160756f, 0f, 0f));
            leaf.localScale = new Vector3(1.25f, 0.9f, 1f);
            Solid(panel, "Panel Collision", new Vector3(0f, 1.822f, 0f), new Vector3(2.633f, 3.644f, 0.21f));
            return root;
        }

        private static void AddLightingAndSigns(Transform room)
        {
            var lights = Child(room, "Lighting and Temporal Signs");
            Directional(lights, "Neutral key", Quaternion.Euler(50f, -30f, 0f), 1.8f);
            Directional(lights, "Soft fill", Quaternion.Euler(35f, 135f, 0f), 0.65f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.45f, 0.52f);
            foreach (var side in new[] { -1f, 1f })
            {
                var mat = side < 0 ? "EchoCyan" : "RewindViolet";
                for (var z = -4f; z <= 4f; z += 4f)
                {
                    Block(lights, mat + " wall marker", new Vector3(side * 7.7f, 1.65f, z), new Vector3(0.04f, 0.9f, 0.12f), mat);
                    Block(lights, mat + " floor marker", new Vector3(side * 6.8f, 0.015f, z), new Vector3(0.08f, 0.02f, 0.7f), mat);
                }
                var accent = Child(lights, mat + " accent light").gameObject.AddComponent<Light>();
                accent.type = LightType.Point;
                accent.transform.localPosition = new Vector3(side * 6.8f, 2.8f, 0f);
                accent.color = Mat(mat).GetColor("_BaseColor");
                accent.intensity = 2f;
                accent.range = 7f;
                accent.shadows = LightShadows.None;
            }
            // A few neutral overhead luminaires establish human scale without filling the room with signs.
            foreach (var z in new[] { -4f, 4f })
            {
                Block(lights, "Overhead titanium beam", new Vector3(0f, 5.1f, z), new Vector3(16f, 0.18f, 0.25f), "Titanium");
                Solid(lights, "Overhead beam collision", new Vector3(0f, 5.1f, z), new Vector3(16f, 0.18f, 0.25f));
            }
        }

        private static void AddPlayerAndCamera(Scene roomScene)
        {
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Echoryx/Prefabs/Player.prefab"), roomScene);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 0.05f, -10f);
            // Copy the actual rig configuration read-only; never save or alter Prototype.
            var prototype = EditorSceneManager.OpenScene("Assets/_Echoryx/Scenes/Prototype.unity", OpenSceneMode.Additive);
            try
            {
                var roots = prototype.GetRootGameObjects();
                GameObject Copy(string name)
                {
                    var clone = UnityEngine.Object.Instantiate(roots.Single(r => r.name == name));
                    clone.name = name;
                    SceneManager.MoveGameObjectToScene(clone, roomScene);
                    return clone;
                }
                var main = Copy("Main Camera").GetComponent<Camera>();
                var rig = Copy("Camera Orbit Target");
                var cm = Copy("Player Cinemachine Camera").GetComponent<CinemachineCamera>();
                rig.GetComponent<PlayerCameraOrbit>().SetTargets(player.transform.Find("CameraAnchor"), player.GetComponent<PlayerInputReader>());
                rig.transform.position = player.transform.Find("CameraAnchor").position;
                cm.Follow = rig.transform;
                player.GetComponent<PlayerMotor>().SetCameraReference(rig.transform);
                main.transform.SetPositionAndRotation(new Vector3(0.35f, 2f, -11.5f), Quaternion.Euler(12f, 0f, 0f));
            }
            finally { EditorSceneManager.CloseScene(prototype, true); SceneManager.SetActiveScene(roomScene); }
        }

        [MenuItem("Echoryx/S05-A2/Validate Reference Room")]
        public static void Validate()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(RoomPath, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var floorChecks = 0;
            for (var x = -7.5f; x <= 7.5f; x += 1f)
                for (var z = -7.5f; z <= 7.5f; z += 1f)
                {
                    Require(Physics.Raycast(new Vector3(x, 0.1f, z), Vector3.down, out var hit, 0.5f)
                        && Mathf.Abs(hit.point.y) < 0.002f, "Floor gap at " + x + ", " + z);
                    floorChecks++;
                }
            foreach (var z in new[] { -7f, -3f, 1f, 5f, 7f })
                foreach (var sign in new[] { -1f, 1f })
                    Require(Physics.Raycast(new Vector3(0f, 2f, z), Vector3.right * sign, 8.2f), "Missing side wall collision.");
            for (var x = -7f; x <= 7f; x += 2f)
            {
                Require(Physics.Raycast(new Vector3(x, 2f, 0f), Vector3.forward, 8.2f), "Missing north wall/door collision.");
                if (Mathf.Abs(x) > 1.23f)
                    Require(Physics.Raycast(new Vector3(x, 2f, 0f), Vector3.back, 8.2f), "Gap beside entrance frame.");
            }
            Require(!Physics.Linecast(new Vector3(0f, 1f, -7f), new Vector3(0f, 1f, -9f)), "Open frame blocks passage.");
            Require(Physics.Linecast(new Vector3(0f, 1f, 7f), new Vector3(0f, 1f, 9f)), "Closed panel is not solid.");
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                Require(renderer.sharedMaterials.All(m => m != null && m.shader != null), "Missing renderer material.");
            Require(UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Length == 0, "Use simple colliders.");
            var cm = GameObject.Find("Player Cinemachine Camera").GetComponent<CinemachineCamera>();
            Require(cm.Follow == GameObject.Find("Camera Orbit Target").transform, "Missing camera target.");
            var text = "Unity " + Application.unityVersion + ": " + floorChecks + " floor probes, 24 perimeter rays, open/closed door rays passed.\n";
            File.WriteAllText("docs/visual-reference-room-validation.txt", text);
            Debug.Log("S05-A2: " + text.Trim());
        }

        [MenuItem("Echoryx/S05-A2/Render Reference Room")]
        public static void RenderPreview()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("Graphics device required.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(RoomPath, OpenSceneMode.Single);
            var camera = new GameObject("Temporary survey camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 17f, -15f);
            camera.transform.LookAt(new Vector3(0f, 1f, 0f));
            camera.orthographic = true;
            camera.orthographicSize = 12f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.055f);
            var target = new RenderTexture(1600, 1000, 24);
            var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("docs/images");
                File.WriteAllBytes("docs/images/s05-a2-reference-room.png", pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(pixels);
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }
            Debug.Log("S05-A2 room rendered on " + SystemInfo.graphicsDeviceName);
        }

        private static void EnsurePrefab(string name, Func<GameObject> create)
        {
            var path = Prefabs + "/" + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var root = create();
            try { if (PrefabUtility.SaveAsPrefabAsset(root, path) == null) throw new InvalidOperationException("Cannot save " + path); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static GameObject Place(string name, Transform parent, Vector3 position, float yaw = 0f)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + name + ".prefab"));
            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            return instance;
        }
        private static Transform Model(Transform parent, string name, Vector3 offset, float yaw = 0f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Source + name + ".fbx");
            if (asset == null) throw new InvalidOperationException("Missing selected FBX: " + name);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = offset;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return instance.transform;
        }
        private static Transform Child(Transform parent, string name)
        {
            var result = new GameObject(name).transform;
            result.SetParent(parent, false);
            return result;
        }
        private static void Solid(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var box = Child(parent, name).gameObject.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
        }
        private static void Block(Transform parent, string name, Vector3 center, Vector3 size, string material)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = center;
            item.transform.localScale = size;
            UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
            item.GetComponent<Renderer>().sharedMaterial = Mat(material);
        }
        private static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(Materials + name + ".mat");
        private static void Directional(Transform parent, string name, Quaternion rotation, float intensity)
        {
            var light = Child(parent, name).gameObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = rotation;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
