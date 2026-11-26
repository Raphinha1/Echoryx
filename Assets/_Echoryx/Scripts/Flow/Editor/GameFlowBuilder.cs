using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Echoryx.Flow.Editor
{
    /// <summary>Builds the MainMenu scene and the level catalog, and orders Build Settings from the catalog.</summary>
    public static class GameFlowBuilder
    {
        public const string MenuScenePath = "Assets/_Echoryx/Scenes/MainMenu.unity";
        public const string CatalogPath = "Assets/_Echoryx/Resources/LevelCatalog.asset";
        private const string ActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string FirstLevelPath = "Assets/_Echoryx/Scenes/Level_01_Tutorial.unity";
        private const string Materials = "Assets/_Echoryx/Materials/Art/";
        private const string Modular = "Assets/_Echoryx/Art/Environment/kenney_modular-space-kit_1.0/Models/FBX format/";
        private const string Station = "Assets/_Echoryx/Art/Environment/kenney_space-station-kit/Models/FBX format/";

        [MenuItem("Echoryx/Flow/1 Build Main Menu")]
        public static void BuildMainMenu()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureCatalog();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 55f;
            cameraObject.AddComponent<AudioListener>();

            // Same sky, ambient and warm key light as the Level_01 art pass.
            RenderSettings.skybox = Load<Material>(Materials + "ArtSky.mat");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.55f, 0.88f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.42f, 0.62f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.20f, 0.34f);
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.82f, 0.64f);
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

            // Distant scenery around the edges; the centre stays clear for the buttons.
            var scenery = new GameObject("Menu Scenery").transform;
            Spawn(Modular, "room-large", scenery, new Vector3(-23f, -7f, 38f), new Vector3(8f, 35f, 10f), 1.1f, "ArtWall");
            Spawn(Modular, "room-small", scenery, new Vector3(21f, -9f, 34f), new Vector3(-8f, -40f, 12f), 1.2f, "ArtWall");
            Spawn(Station, "rocks", scenery, new Vector3(17f, 9f, 30f), new Vector3(20f, 47f, 13f), 3.5f, "ArtAccent");
            Spawn(Station, "skip-rocks", scenery, new Vector3(-18f, 11f, 36f), new Vector3(60f, 120f, 25f), 4f, "ArtAccent");
            Spawn(Station, "container-tall", scenery, new Vector3(-11f, -8f, 22f), new Vector3(15f, 60f, 25f), 2.5f, "ArtAccent");

            new GameObject("Main Menu").AddComponent<MainMenuController>();

            if (!EditorSceneManager.SaveScene(scene, MenuScenePath)) throw new InvalidOperationException("Could not save " + MenuScenePath);
            SyncBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("Main menu built.");
        }

        /// <summary>Menu first, then the catalog levels in order, then any other scene already listed.</summary>
        [MenuItem("Echoryx/Flow/2 Sync Build Settings From Catalog")]
        public static void SyncBuildSettings()
        {
            var catalog = EnsureCatalog();
            var problems = catalog.Validate();
            if (problems.Count > 0) throw new InvalidOperationException("Fix the level catalog first:\n" + string.Join("\n", problems));
            var ordered = new List<string> { catalog.MainMenuScenePath };
            ordered.AddRange(catalog.Levels.Select(level => level.ScenePath));
            var missing = ordered.Where(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null).ToArray();
            if (missing.Length > 0) throw new InvalidOperationException("Missing scenes: " + string.Join(", ", missing));
            var others = EditorBuildSettings.scenes.Where(s => !ordered.Contains(s.path));
            EditorBuildSettings.scenes = ordered.Select(path => new EditorBuildSettingsScene(path, true)).Concat(others).ToArray();
            Debug.Log("Build Settings: " + string.Join(" | ", EditorBuildSettings.scenes.Select(s => s.path)));
        }

        /// <summary>Creates the catalog with the shipped levels once; an existing catalog is never overwritten.</summary>
        private static LevelCatalog EnsureCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            if (catalog != null) return catalog;
            if (!AssetDatabase.IsValidFolder("Assets/_Echoryx/Resources")) AssetDatabase.CreateFolder("Assets/_Echoryx", "Resources");
            catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            catalog.Configure(MenuScenePath, Load<InputActionAsset>(ActionsPath),
                new LevelEntry("level-01-tutorial", "Tutorial", FirstLevelPath));
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing asset: " + path);

        private static void Spawn(string kit, string model, Transform parent, Vector3 position, Vector3 euler, float scale, string material)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(kit + model + ".fbx"), parent.gameObject.scene);
            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            instance.transform.localScale = Vector3.one * scale;
            var shared = Load<Material>(Materials + material + ".mat");
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials = Enumerable.Repeat(shared, renderer.sharedMaterials.Length).ToArray();
        }
    }
}
