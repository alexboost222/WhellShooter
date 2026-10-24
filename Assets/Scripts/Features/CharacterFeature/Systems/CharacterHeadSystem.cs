using Engine.DI;
using Engine.Runtime;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.CharacterFeature.Systems
{
    /// <summary>
    /// Places every body's head each frame: its feet interpolated between the last two fixed ticks,
    /// plus eye height, rotated by <see cref="Look"/>.
    /// </summary>
    /// <remarks>
    /// <b>World space, from components, every frame.</b> Inheriting from the root would inherit the
    /// root's fixed-tick stepping (the controller moves it on the tick), and the camera would judder
    /// at any frame rate that is not the tick rate.
    /// </remarks>
    public sealed class CharacterHeadSystem : ISystem
    {
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Look> _look;
        [Inject] private Stash<CharacterViewLink> _characterViewLink;

        [Inject] private CharacterConfig _config;
        [Inject] private TimeService _time;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<CharacterMotion>()
                .With<Look>()
                .With<CharacterViewLink>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            var alpha = _time.InterpolationAlpha;

            foreach (var entity in _filter)
            {
                ref var motion = ref _characterMotion.Get(entity);
                var head = _characterViewLink.Get(entity).View.Head;

                var feet = math.lerp(motion.PreviousPosition, motion.Position, alpha);
                head.SetPositionAndRotation(feet + new float3(0f, _config.EyeHeight, 0f), _look.Get(entity).Rotation);
            }
        }
    }
}
