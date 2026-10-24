using Engine.DI;
using Engine.Runtime;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.CharacterFeature.Systems
{
    /// <summary>
    /// Places every body's view each frame: the root at its feet interpolated between the last two
    /// fixed ticks, facing its heading, and the head at eye height above, rotated by <see cref="Look"/>.
    /// </summary>
    /// <remarks>
    /// <b>World space, from components, every frame</b> — never stepped at the tick rate, so the camera
    /// doesn't judder at any frame rate that is not the tick rate.
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
                var view = _characterViewLink.Get(entity).View;
                var look = _look.Get(entity);

                var feet = math.lerp(motion.PreviousPosition, motion.Position, alpha);
                view.transform.SetPositionAndRotation(feet, look.Heading);
                view.Head.SetPositionAndRotation(feet + new float3(0f, _config.EyeHeight, 0f), look.Rotation);
            }
        }
    }
}
