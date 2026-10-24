using Engine.DI;
using Engine.Physics;
using Features.Bodies.Components;
using PhysR;
using Scellecs.Morpeh;

namespace Features.Bodies.Systems
{
    /// <summary>Right after the step: native pose of every dynamic / kinematic body → <see cref="BodyPose"/>.</summary>
    public sealed class BodyReadbackSystem : ISystem
    {
        [Inject] private Stash<PhysicsBody> _physicsBody;
        [Inject] private Stash<BodyPose> _bodyPose;

        [Inject] private PhysicsWorld _physics;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PhysicsBody>()
                .With<BodyPose>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            var native = _physics.Native;
            foreach (var entity in _filter)
            {
                var handle = new BodyHandle(_physicsBody.Get(entity).Handle);
                if (!native.TryGetState(handle, out BodyState state))
                {
                    continue;
                }

                ref var pose = ref _bodyPose.Get(entity);
                pose.PreviousPosition = pose.Position;
                pose.PreviousRotation = pose.Rotation;
                pose.Position = PhysicsMath.ToFloat3(state.Position);
                pose.Rotation = PhysicsMath.ToQuaternion(state.Rotation);
                pose.Sleeping = state.Sleeping != 0;
            }
        }
    }
}
