using System.Linq;
using Echoryx.Interactions;
using Echoryx.Rewind.Platforms;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Echoryx.Level.Tests
{
    /// <summary>The art pass is visual only: gameplay colliders stay, art carries none, assets come from the Kenney kits.</summary>
    public sealed class Level01ArtTests
    {
        private const string ScenePath = "Assets/_Echoryx/Scenes/Level_01_Tutorial.unity";
        private const string Prefabs = "Assets/_Echoryx/Prefabs/Level/";

        private static void WithScene(System.Action body)
        {
            var previous = SceneManager.GetActiveScene().path;
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                body();
            }
            finally
            {
                if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }

        [Test]
        public void GameplayGeometryKeepsItsCollidersWhileItsPlaceholderRendererIsHidden()
        {
            WithScene(() =>
            {
                var geometry = GameObject.Find("Level Geometry").transform;
                foreach (var name in new[] { "Floor 1 Start", "Floor 2 Main", "Floor 3 Goal", "Low Step", "Wall Left", "Wall Right", "Wall Start", "Wall End" })
                {
                    var block = geometry.Find(name);
                    Assert.That(block.GetComponent<BoxCollider>(), Is.Not.Null, name + " collider");
                    Assert.That(block.GetComponent<Renderer>().enabled, Is.False, name + " placeholder hidden");
                }
            });
        }

        [Test]
        public void ArtRootHasNoCollidersAndOnlyUsesKenneyMeshes()
        {
            WithScene(() =>
            {
                var art = GameObject.Find("Level Art");
                Assert.That(art, Is.Not.Null);
                Assert.That(art.GetComponentsInChildren<Collider>(true), Is.Empty);
                var meshes = art.GetComponentsInChildren<MeshFilter>(true).Select(f => AssetDatabase.GetAssetPath(f.sharedMesh));
                Assert.That(meshes, Is.All.Contains("kenney_"));
                Assert.That(meshes.Any(p => p.Contains("Quaternius")), Is.False);
            });
        }

        [Test]
        public void PlateIndicatorMovedToTheKitMeshAndDoorLeafFollowsTheLogicalPanel()
        {
            var plate = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "PressurePlate.prefab");
            var indicator = (Renderer)new SerializedObject(plate.GetComponent<PressurePlate>()).FindProperty("indicator").objectReferenceValue;
            Assert.That(indicator, Is.Not.Null);
            Assert.That(indicator.enabled, Is.True);
            Assert.That(AssetDatabase.GetAssetPath(indicator.GetComponent<MeshFilter>().sharedMesh), Does.Contain("kenney_"));

            var door = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Door.prefab");
            var panel = (Transform)new SerializedObject(door.GetComponent<DoorController>()).FindProperty("doorPanel").objectReferenceValue;
            Assert.That(panel.GetComponent<BoxCollider>(), Is.Not.Null, "Logical panel keeps its collider.");
            var follower = door.GetComponentInChildren<SmoothVisualFollow>(true);
            Assert.That(follower, Is.Not.Null);
            Assert.That(new SerializedObject(follower).FindProperty("source").objectReferenceValue, Is.SameAs(panel));
        }

        [Test]
        public void RewindPlatformHasGlowFeedbackWiredToItsMechanism()
        {
            WithScene(() =>
            {
                var platform = Object.FindFirstObjectByType<RewindMovingPlatform>();
                var feedback = platform.GetComponentInChildren<RewindMechanismFeedback>();
                Assert.That(feedback, Is.Not.Null);
                Assert.That(new SerializedObject(feedback).FindProperty("renderers").arraySize, Is.GreaterThan(0));
            });
        }

        [Test]
        public void MaterialsAreMatteUrpLit()
        {
            var paths = AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Echoryx/Materials/Art" }).Select(AssetDatabase.GUIDToAssetPath);
            // Sky and the two translucent effect planes (hazard, goal) are unlit by design.
            foreach (var path in paths.Where(p => !p.EndsWith("ArtSky.mat") && !p.EndsWith("ArtHazard.mat") && !p.EndsWith("ArtGoalField.mat")))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"), path);
                Assert.That(material.GetFloat("_Metallic"), Is.EqualTo(0f), path);
                Assert.That(material.GetFloat("_Smoothness"), Is.LessThanOrEqualTo(0.2f), path);
            }
        }

        [Test]
        public void AtmosphereHasSkyFogLightAndPostProcessing()
        {
            WithScene(() =>
            {
                Assert.That(RenderSettings.skybox, Is.Not.Null);
                Assert.That(RenderSettings.skybox.shader.name, Is.EqualTo("Echoryx/Gradient Skybox"));
                Assert.That(RenderSettings.fog, Is.True);
                var light = GameObject.Find("Directional Light").GetComponent<Light>();
                Assert.That(light.shadows, Is.EqualTo(LightShadows.Soft));
                var volume = Object.FindFirstObjectByType<Volume>();
                Assert.That(volume.isGlobal, Is.True);
                Assert.That(volume.sharedProfile.components.Count, Is.EqualTo(3), "Bloom, ColorAdjustments, Vignette");
                var names = volume.sharedProfile.components.Select(c => c.GetType().Name).ToArray();
                Assert.That(names, Is.EquivalentTo(new[] { "Bloom", "ColorAdjustments", "Vignette" }));
            });
        }

        [Test]
        public void RewindHintsDescribeMechanismRewindAndDoNotGiveTheSolutionAway()
        {
            WithScene(() =>
            {
                var hints = Object.FindObjectsByType<TutorialHint>(FindObjectsSortMode.None).Select(h => h.Message).ToArray();
                Assert.That(hints.Any(h => h.Contains("rebobinar su movimiento")), Is.True);
                Assert.That(hints.Any(h => h.ToLowerInvariant().Contains("deshace")), Is.False, "Rewind does not undo the player.");
                Assert.That(hints.Where(h => h.Contains("plataforma")).All(h => h.Contains("¿")), Is.True, "Room 3 hints are guiding questions.");
            });
        }

        [Test]
        public void MovingVisualsAreNeverStatic()
        {
            // Static batching bakes meshes at their authored position, so nothing that moves may be static.
            foreach (var name in new[] { "Door", "RewindPlatformVisual", "PressurePlate" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab");
                foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
                    Assert.That(GameObjectUtility.GetStaticEditorFlags(t.gameObject), Is.EqualTo((StaticEditorFlags)0), name + "/" + t.name);
            }
            WithScene(() =>
            {
                var platform = Object.FindFirstObjectByType<RewindMovingPlatform>();
                foreach (var t in platform.GetComponentsInChildren<Transform>(true))
                    Assert.That(t.gameObject.isStatic, Is.False, "platform/" + t.name);
                var door = Object.FindFirstObjectByType<DoorController>();
                foreach (var t in door.GetComponentsInChildren<Transform>(true))
                    Assert.That(t.gameObject.isStatic, Is.False, "door/" + t.name);
                var scenery = GameObject.Find("Level Art").GetComponentsInChildren<MeshRenderer>(true);
                Assert.That(scenery.All(r => r.gameObject.isStatic), Is.True, "Pure scenery stays batched.");
            });
        }

        [Test]
        public void CameraHasSaneDepthRangeAndObstacleAvoidanceAgainstGameplayColliders()
        {
            WithScene(() =>
            {
                var cm = Object.FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
                Assert.That(cm.Lens.NearClipPlane, Is.InRange(0.2f, 0.5f));
                Assert.That(cm.Lens.FarClipPlane, Is.LessThanOrEqualTo(500f));
                Assert.That(cm.Lens.FarClipPlane / cm.Lens.NearClipPlane, Is.LessThan(5000f), "Depth precision");
                var deoccluder = cm.GetComponent<Unity.Cinemachine.CinemachineDeoccluder>();
                Assert.That(deoccluder, Is.Not.Null);
                Assert.That(deoccluder.AvoidObstacles.Enabled, Is.True);
                Assert.That(deoccluder.AvoidObstacles.CameraRadius, Is.GreaterThanOrEqualTo(0.3f));
                Assert.That(deoccluder.CollideAgainst.value & (1 << 0), Is.Not.EqualTo(0), "Default layer (gameplay colliders)");
                Assert.That(deoccluder.IgnoreTag, Is.EqualTo("Player"));
            });
        }
    }
}
