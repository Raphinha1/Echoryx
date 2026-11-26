using System.Collections.Generic;
using UnityEngine;

namespace Echoryx.Flow
{
    /// <summary>Gentle endless motion of the menu background: floating, slowly turning scenery and a swaying camera.</summary>
    public sealed class MenuMotion : MonoBehaviour
    {
        public const string SceneryRootName = "Menu Scenery";
        private const float BobMetres = 0.45f;

        private struct Item
        {
            public Transform Transform;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Axis;
            public float Phase;
            public float BobSpeed;
            public float Degrees;
        }

        private readonly List<Item> items = new List<Item>();
        private Transform view;
        private Vector3 viewPosition;
        private Quaternion viewRotation;
        private float time;

        private void Start()
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (root.name != SceneryRootName) continue;
                foreach (Transform child in root.transform)
                {
                    var i = items.Count;
                    items.Add(new Item
                    {
                        Transform = child, Position = child.localPosition, Rotation = child.localRotation,
                        Axis = new Vector3(0.25f * (i % 3 - 1), 1f, 0.2f * ((i + 1) % 3 - 1)).normalized,
                        Phase = i * 1.9f, BobSpeed = 0.55f + 0.07f * i,
                        Degrees = (i % 2 == 0 ? 1f : -1f) * (2.2f + 0.6f * i)
                    });
                }
            }
            var main = Camera.main;
            if (main == null) return;
            view = main.transform;
            viewPosition = view.localPosition;
            viewRotation = view.localRotation;
        }

        private void Update()
        {
            time += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            foreach (var item in items)
            {
                if (item.Transform == null) continue;
                var bob = Mathf.Sin(time * item.BobSpeed + item.Phase) * BobMetres;
                item.Transform.localPosition = item.Position + Vector3.up * bob;
                item.Transform.localRotation = Quaternion.AngleAxis(item.Degrees * time, item.Axis) * item.Rotation;
            }
            if (view == null) return;
            view.localPosition = viewPosition + new Vector3(Mathf.Sin(time * 0.21f) * 0.35f, Mathf.Sin(time * 0.29f) * 0.18f, 0f);
            view.localRotation = viewRotation * Quaternion.Euler(Mathf.Sin(time * 0.17f) * 0.5f, Mathf.Sin(time * 0.13f) * 0.9f, 0f);
        }
    }
}
