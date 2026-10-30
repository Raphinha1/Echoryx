using System;
using System.Linq;
using Echoryx.Player;
using Echoryx.Rewind.Platforms;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoryx.Game.Editor
{
    /// <summary>Creates isolated B4.2 resources through Unity APIs; never applies Player overrides.</summary>
    public static class S05PlayerAnimationLabSetup
    {
        public const string ScenePath = "Assets/_Echoryx/Scenes/PlayerAnimationIntegrationLab.unity";
        public const string VisualPath = "Assets/_Echoryx/Prefabs/Characters/SuperSoldierVisual.prefab";
        public const string ControllerPath = "Assets/_Echoryx/Art/Animations/SuperSoldier/PlayerLocomotion.controller";
        private const string Animations = "Assets/_Echoryx/Art/Animations/SuperSoldier/";
        private const string ModelPath = "Assets/_Echoryx/Art/Characters/SihaGamesSuperSoldier/Idle.fbx";

        [MenuItem("Echoryx/S05-B4.2/Build Isolated Player Lab")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Debug.Log("B4.2 lab already exists; no assets replaced.");
                return;
            }
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ?? CreateController();
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath) ?? CreateVisual(controller);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Box("Ground", new Vector3(0f, -0.15f, 0f), new Vector3(40f, 0.3f, 40f), "Graphite");
            Box("Wall Speed Check", new Vector3(0f, 2f, 10f), new Vector3(12f, 4f, 0.5f), "Titanium");
            Box("Platform Wall", new Vector3(12f, 2f, 4f), new Vector3(5f, 4f, 0.5f), "Titanium");
            var stripe = Box("Forward +Z", new Vector3(0f, 0.006f, 4f), new Vector3(0.12f, 0.01f, 6f), "EchoCyan");
            UnityEngine.Object.DestroyImmediate(stripe.GetComponent<Collider>());
            var player = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Echoryx/Prefabs/Player.prefab"), scene);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 0.05f, -4f);
            player.transform.Find("VisualRoot/Capsule Placeholder").gameObject.SetActive(false);
            var appearance = (GameObject)PrefabUtility.InstantiatePrefab(visual, player.transform.Find("VisualRoot"));
            var driver = player.AddComponent<PlayerAnimationDriver>();
            driver.SetTargets(player.GetComponent<PlayerMotor>(), appearance.GetComponentInChildren<Animator>());
            CameraRig(scene, player);
            var start = new GameObject("Platform Start").transform;
            start.position = new Vector3(12f, 0f, -4f);
            var end = new GameObject("Platform End").transform;
            end.position = new Vector3(12f, 0f, 2f);
            var platform = new GameObject("Transport Test Platform");
            platform.transform.position = start.position;
            var surface = platform.AddComponent<BoxCollider>();
            surface.center = Vector3.zero;
            surface.size = new Vector3(2.5f, 0.3f, 2.5f);
            var skin = Box("Platform Visual", start.position, surface.size, "RewindViolet");
            UnityEngine.Object.DestroyImmediate(skin.GetComponent<Collider>());
            skin.transform.SetParent(platform.transform, true);
            var mechanism = platform.AddComponent<RewindMovingPlatform>();
            var settings = new SerializedObject(mechanism);
            settings.FindProperty("startPoint").objectReferenceValue = start;
            settings.FindProperty("endPoint").objectReferenceValue = end;
            settings.FindProperty("surface").objectReferenceValue = surface;
            settings.FindProperty("passenger").objectReferenceValue = player.GetComponent<PlayerPlatformCarrier>();
            settings.FindProperty("speed").floatValue = 1.5f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var sun = new GameObject("Key Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 2f;
            sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.45f, 0.5f);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Cannot save B4.2 lab.");
            AssetDatabase.SaveAssets();
            Debug.Log("B4.2 created isolated lab, visual prefab and ground controller. Player prefab untouched.");
        }

        private static AnimatorController CreateController()
        {
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Animations + "IdleLoop.anim");
            var walk = Clip(Animations + "Mixamo/X Bot@Walking.fbx", "Walk");
            var run = Clip(Animations + "Mixamo/X Bot@Running.fbx", "Run");
            if (idle == null || walk == null || run == null) throw new InvalidOperationException("Missing approved clips.");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("LocomotionRate", AnimatorControllerParameterType.Float);
            var parameters = controller.parameters;
            parameters.Single(p => p.name == "LocomotionRate").defaultFloat = 1f;
            controller.parameters = parameters;
            var tree = new BlendTree { name = "Ground Speed", blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, walk.averageSpeed.magnitude);
            tree.AddChild(run, run.averageSpeed.magnitude);
            var state = controller.layers[0].stateMachine.AddState("Locomotion");
            state.motion = tree;
            state.iKOnFeet = true;
            state.writeDefaultValues = false;
            state.speedParameter = "LocomotionRate";
            state.speedParameterActive = true;
            controller.layers[0].stateMachine.defaultState = state;
            return controller;
        }

        private static GameObject CreateVisual(AnimatorController controller)
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Echoryx/Prefabs/Characters"))
                AssetDatabase.CreateFolder("Assets/_Echoryx/Prefabs", "Characters");
            var root = new GameObject("SuperSoldierVisual");
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), root.transform);
                model.name = "SuperSoldier";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one * 0.98656774f;
                var animator = model.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                return PrefabUtility.SaveAsPrefabAsset(root, VisualPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static AnimationClip Clip(string path, string name) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c => c.name == name);

        private static GameObject Box(string name, Vector3 position, Vector3 size, string material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Echoryx/Materials/Environment/" + material + ".mat");
            return box;
        }

        private static void CameraRig(Scene scene, GameObject player)
        {
            var prototype = EditorSceneManager.OpenScene("Assets/_Echoryx/Scenes/Prototype.unity", OpenSceneMode.Additive);
            try
            {
                var roots = prototype.GetRootGameObjects();
                GameObject Copy(string name)
                {
                    var clone = UnityEngine.Object.Instantiate(roots.Single(r => r.name == name));
                    clone.name = name;
                    SceneManager.MoveGameObjectToScene(clone, scene);
                    return clone;
                }
                var main = Copy("Main Camera");
                var orbit = Copy("Camera Orbit Target");
                var cm = Copy("Player Cinemachine Camera").GetComponent<CinemachineCamera>();
                orbit.GetComponent<PlayerCameraOrbit>().SetTargets(player.transform.Find("CameraAnchor"), player.GetComponent<PlayerInputReader>());
                orbit.transform.position = player.transform.Find("CameraAnchor").position;
                cm.Follow = orbit.transform;
                player.GetComponent<PlayerMotor>().SetCameraReference(orbit.transform);
                main.transform.SetPositionAndRotation(new Vector3(0.35f, 2f, -8f), Quaternion.Euler(12f, 0f, 0f));
            }
            finally { EditorSceneManager.CloseScene(prototype, true); SceneManager.SetActiveScene(scene); }
        }
    }
}
