using Engine.DI;
using Engine.Motion;
using Features.ViewmodelFeature.Components;
using Features.ViewmodelFeature.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.ViewmodelFeature.Systems
{
    /// <summary>
    /// Steps every viewmodel spring once per frame, after this frame's targets and kicks are in.
    /// </summary>
    /// <remarks>
    /// One place for the stepping, so the order "set targets, add kicks, then step" cannot be got
    /// wrong by the systems that do the first two. The springs are exact (<see cref="Springs.Step"/>),
    /// so the feel is the same at any frame rate.
    /// </remarks>
    public sealed class ViewmodelSpringSystem : ISystem
    {
        [Inject] private Stash<ViewmodelSprings> _viewmodelSprings;

        [Inject] private ViewmodelConfig _config;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ViewmodelSprings>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                ref var springs = ref _viewmodelSprings.Get(entity);

                Springs.Step(ref springs.MotionPosition, springs.MotionTargetPosition,
                    _config.MotionFrequency, _config.MotionDamping, deltaTime);
                Springs.Step(ref springs.MotionRotation, springs.MotionTargetRotation,
                    _config.MotionFrequency, _config.MotionDamping, deltaTime);
                Springs.Step(ref springs.RecoilPosition, float3.zero,
                    _config.RecoilFrequency, _config.RecoilDamping, deltaTime);
                Springs.Step(ref springs.RecoilRotation, float3.zero,
                    _config.RecoilFrequency, _config.RecoilDamping, deltaTime);
                Springs.Step(ref springs.CameraRotation, float3.zero,
                    _config.CameraFrequency, _config.CameraDamping, deltaTime);
            }
        }
    }
}
