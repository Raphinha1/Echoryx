using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using B = Echoryx.Level.Editor.Level01TutorialBuilder;

namespace Echoryx.Level.Editor
{
    /// <summary>
    /// Closes the slits a modular level leaves to the outside: opaque backing panels behind the perimeter walls,
    /// corner pillars and overlapping pit linings. Purely visual: no colliders, so neither the player nor the
    /// camera deoccluder notice them. Shared by Level_01 (art pass) and Level_02 (builder).
    /// </summary>
    internal static class LevelEnclosure
    {
        public const string DarkMaterialPath = "Assets/_Echoryx/Materials/Level02/ArtWellWall.mat";
        public const string RootName = "Enclosure";

        /// <summary>Rebuilds a single "Enclosure" child of the art root: backing, corner pillars and pit linings.</summary>
        public static Transform Apply(Transform art, params (string name, float from, float to)[] pits)
        {
            var old = art.Find(RootName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root = new GameObject(RootName).transform;
            root.SetParent(art, false);
            var dark = DarkMaterial();
            Backing(art, root, dark);
            EnclosePits(root, dark, pits);
            return root;
        }

        public static Material DarkMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(DarkMaterialPath);
            if (material != null) return material;
            if (!AssetDatabase.IsValidFolder("Assets/_Echoryx/Materials/Level02")) AssetDatabase.CreateFolder("Assets/_Echoryx/Materials", "Level02");
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "ArtWellWall" };
            material.SetColor("_BaseColor", new Color(0.28f, 0.24f, 0.42f));
            material.SetFloat("_Smoothness", 0.1f);
            material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, DarkMaterialPath);
            return material;
        }

        /// <summary>
        /// Opaque backing behind every perimeter wall (measured from the spawned wall art, floor of the pits to the
        /// top of the walls, a little longer at each end) plus corner pillars that overlap both panels. Whatever
        /// slit the modular wall pieces leave, only these dark panels show through. No colliders: neither the
        /// player nor the camera deoccluder can touch them.
        /// </summary>
        internal static void Backing(Transform walls, Transform parent, Material dark)
        {
            Bounds Of(string prefix)
            {
                var bounds = new Bounds();
                var first = true;
                foreach (var renderer in walls.GetComponentsInChildren<Renderer>())
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
            Panel(parent, "Backing Left", new Vector3(outerLeft + thickness * 0.5f, middleY, (outerStart + outerEnd) * 0.5f),
                new Vector3(thickness, height, outerEnd - outerStart + 2f * over), dark);
            Panel(parent, "Backing Right", new Vector3(outerRight - thickness * 0.5f, middleY, (outerStart + outerEnd) * 0.5f),
                new Vector3(thickness, height, outerEnd - outerStart + 2f * over), dark);
            Panel(parent, "Backing Start", new Vector3((outerLeft + outerRight) * 0.5f, middleY, outerStart + thickness * 0.5f),
                new Vector3(outerRight - outerLeft + 2f * over, height, thickness), dark);
            Panel(parent, "Backing End", new Vector3((outerLeft + outerRight) * 0.5f, middleY, outerEnd - thickness * 0.5f),
                new Vector3(outerRight - outerLeft + 2f * over, height, thickness), dark);
            // Corner pillars: from inside the end wall's run to the outer backing, overlapping both.
            foreach (var (x0, x1) in new[] { (outerLeft, Mathf.Min(start.min.x, end.min.x) + 0.1f), (Mathf.Max(start.max.x, end.max.x) - 0.1f, outerRight) })
                foreach (var (z0, z1) in new[] { (outerStart, Mathf.Max(left.min.z, right.min.z) + 0.3f), (Mathf.Min(left.max.z, right.max.z) - 0.3f, outerEnd) })
                    Panel(parent, "Corner Pillar", new Vector3((x0 + x1) * 0.5f, middleY, (z0 + z1) * 0.5f), new Vector3(x1 - x0, height, z1 - z0), dark);
        }

        private static bool HasAncestor(Transform child, string prefix)
        {
            for (var t = child; t != null; t = t.parent)
                if (t.name.StartsWith(prefix)) return true;
            return false;
        }

        internal static void Panel(Transform art, string name, Vector3 position, Vector3 size, Material material)
        {
            var cube = B.Cube(art, name, position, size, material);
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Each pit is a closed box (side, end and bottom panels), so no gap shows the void below.</summary>
        internal static void EnclosePits(Transform art, Material dark, params (string name, float from, float to)[] pits)
        {
            foreach (var (name, from, to) in pits)
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

        // Linings overlap their neighbours by 6 cm at each end (the thin axis is kept), so corners have no slit.
        internal static void Lining(Transform art, string name, Vector3 position, Vector3 size, Material material)
        {
            const float overlap = 0.12f;
            size = new Vector3(size.x > 0.2f ? size.x + overlap : size.x, size.y > 0.2f ? size.y + overlap : size.y,
                size.z > 0.2f ? size.z + overlap : size.z);
            var cube = B.Cube(art, name, position, size, material);
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
        }

    }
}
