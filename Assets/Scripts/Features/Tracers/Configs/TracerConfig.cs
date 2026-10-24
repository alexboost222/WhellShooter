using Engine.Configs;
using UnityEngine;

namespace Features.Tracers.Configs
{
    /// <summary>How a shot's tracer looks (Docs/08 §Tracers). One asset, in <c>Resources/Configs</c>.</summary>
    [CreateAssetMenu(menuName = "WheelShooter/Configs/Tracers", fileName = nameof(TracerConfig))]
    public sealed class TracerConfig : Config
    {
        private const float DefaultLength = 5f;
        private const float DefaultWidth = 0.025f;
        private const float DefaultSpeed = 350f;

        [Tooltip("Unlit, vertex-coloured, alpha-blended — the streak is a LineRenderer.")]
        [SerializeField] private Material material;

        [Tooltip("Head colour; the tail fades to transparent.")]
        [SerializeField] private Color color = new Color(1f, 0.85f, 0.45f, 1f);

        [Tooltip("Streak length, metres.")]
        [SerializeField] private float length = DefaultLength;

        [Tooltip("Streak width, metres.")]
        [SerializeField] private float width = DefaultWidth;

        [Tooltip("How fast the streak flies, m/s. Far below a real bullet, so the eye catches it.")]
        [SerializeField] private float speed = DefaultSpeed;

        [Tooltip("Where the streak starts relative to the shooter's eye: x right, y up, z forward, metres. The local player's uses the x, everyone else's doesn't.")]
        [SerializeField] private Vector3 muzzleOffset = new Vector3(0.12f, -0.15f, 0.6f);

        public Material Material => material;
        public Color Color => color;
        public float Length => length > 0f ? length : DefaultLength;
        public float Width => width > 0f ? width : DefaultWidth;
        public float Speed => speed > 0f ? speed : DefaultSpeed;
        public Vector3 MuzzleOffset => muzzleOffset;
    }
}
