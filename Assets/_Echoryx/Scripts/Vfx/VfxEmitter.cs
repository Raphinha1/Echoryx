using UnityEngine;

namespace Echoryx.Vfx
{
    /// <summary>
    /// One reusable particle system: bursts are emitted by code into a fixed-size pool, so playing an effect
    /// never creates objects. Simulation uses scaled time, so effects freeze with the pause.
    /// </summary>
    public sealed class VfxEmitter
    {
        public ParticleSystem System { get; }
        /// <summary>Particles requested since creation (for checks).</summary>
        public int Emitted { get; private set; }
        public int Alive => System.particleCount;
        public Color LastColor { get; private set; }
        public Vector3 LastVelocity { get; private set; }

        public VfxEmitter(Transform parent, string name, Material material, int capacity,
            ParticleSystemRenderMode mode, float startScale, float endScale, float drag = 0f, float gravity = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            System = go.AddComponent<ParticleSystem>();
            System.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = System.main;
            main.playOnAwake = false;
            main.loop = true;
            main.maxParticles = capacity;
            main.startSpeed = 0f;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = System.emission;
            emission.enabled = false;
            var shape = System.shape;
            shape.enabled = false;

            var size = System.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, startScale, 1f, endScale));

            var color = System.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;

            if (drag > 0f)
            {
                var limit = System.limitVelocityOverLifetime;
                limit.enabled = true;
                limit.drag = drag;
                limit.limit = 100f;
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = mode;
            if (mode == ParticleSystemRenderMode.Stretch)
            {
                renderer.velocityScale = 0.09f;
                renderer.lengthScale = 1.5f;
            }
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.maxParticleSize = 3f; // big flashes close to the camera
            System.Play();
        }

        public void Emit(Vector3 position, Vector3 velocity, Color color, float size, float lifetime)
        {
            var particle = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = velocity,
                startColor = color,
                startSize = size,
                startLifetime = lifetime,
                applyShapeToPosition = false
            };
            System.Emit(particle, 1);
            Emitted++;
            LastColor = color;
            LastVelocity = velocity;
        }

        public void Clear() => System.Clear();
    }
}
