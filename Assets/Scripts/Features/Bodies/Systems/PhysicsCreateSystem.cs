using Engine.DI;
using Engine.Diagnostics.Logs;
using Engine.Physics;
using Features.Bodies.Components;
using PhysR;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bodies.Systems
{
    /// <summary>
    /// Consumes <see cref="BodyCreateRequest"/> and <see cref="CharacterCreateRequest"/>
    /// (read-then-remove): creates the native object and links it to the entity. Runs inside the
    /// physics step, so a request made this tick is simulated this tick.
    /// </summary>
    public sealed class PhysicsCreateSystem : ISystem
    {
        [Inject] private Stash<BodyCreateRequest> _bodyCreateRequest;
        [Inject] private Stash<CharacterCreateRequest> _characterCreateRequest;
        [Inject] private Stash<PhysicsBody> _physicsBody;
        [Inject] private Stash<PhysicsCharacter> _physicsCharacter;
        [Inject] private Stash<BodyPose> _bodyPose;
        [Inject] private Stash<PhysicsCharacterState> _physicsCharacterState;

        [Inject] private BodiesStorage _storage;
        [Inject] private PhysicsWorld _physics;

        public World World { get; set; }

        private Filter _filter;
        private Filter _characters;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<BodyCreateRequest>()
                .Build();

            _characters = World.Filter
                .With<CharacterCreateRequest>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                var request = _bodyCreateRequest.Get(entity);
                _bodyCreateRequest.Remove(entity);

                if (_physicsBody.Has(entity) || _physicsCharacter.Has(entity))
                {
                    Log.Error(LogChannels.Ecs, $"{entity} already has a physics body; the BodyCreateRequest is dropped.");
                    continue;
                }

                CreateBody(entity, request);
            }

            foreach (var entity in _characters)
            {
                var request = _characterCreateRequest.Get(entity);
                _characterCreateRequest.Remove(entity);

                if (_physicsBody.Has(entity) || _physicsCharacter.Has(entity))
                {
                    Log.Error(LogChannels.Ecs, $"{entity} already has a physics body; the CharacterCreateRequest is dropped.");
                    continue;
                }

                CreateCharacter(entity, request);
            }
        }

        private void CreateBody(Entity entity, BodyCreateRequest request)
        {
            var native = _physics.Native;
            var rotation = math.lengthsq(request.Rotation.value) > 0f ? request.Rotation : quaternion.identity;

            var desc = BodyDesc.Default;
            desc.Kind = request.Kind;
            desc.Shape = _storage.GetShape(native, request.Shape, PhysicsMath.ToFP(request.Size));
            desc.Position = PhysicsMath.ToFP(request.Position);
            desc.Rotation = PhysicsMath.ToFP(rotation);
            if (request.Mass > 0f)
            {
                desc.Mass = PhysicsMath.ToFP(request.Mass);
            }

            var handle = native.CreateBody(desc);
            if (!handle.IsValid)
            {
                Log.Error(LogChannels.Ecs, $"phys-r refused a {request.Kind} {request.Shape} body for {entity}.");
                return;
            }

            _physicsBody.Set(entity, new PhysicsBody { Handle = handle.Value });
            _storage.Track(entity.ID, handle.Value, false);

            if (request.Kind != BodyKind.Static)
            {
                _bodyPose.Set(entity, new BodyPose
                {
                    Position = request.Position,
                    Rotation = rotation,
                    PreviousPosition = request.Position,
                    PreviousRotation = rotation,
                });
            }
        }

        private void CreateCharacter(Entity entity, CharacterCreateRequest request)
        {
            var native = _physics.Native;

            var desc = CharacterDesc.Default;
            Override(ref desc.Radius, request.Radius);
            Override(ref desc.Height, request.Height);
            Override(ref desc.StepHeight, request.StepHeight);

            ref var movement = ref desc.Movement;
            Override(ref movement.WalkSpeed, request.WalkSpeed);
            Override(ref movement.SprintSpeed, request.SprintSpeed);
            Override(ref movement.CrouchSpeed, request.CrouchSpeed);
            Override(ref movement.GroundAccel, request.GroundAccel);
            Override(ref movement.AirAccel, request.AirAccel);
            Override(ref movement.Gravity, request.Gravity);
            Override(ref movement.JumpSpeed, request.JumpSpeed);
            if (request.JumpHeight > 0f)
            {
                var gravity = movement.Gravity.ToFloat();
                movement.JumpSpeed = PhysicsMath.ToFP(math.sqrt(2f * gravity * request.JumpHeight));
            }

            // phys-r places the capsule by its centre; the contract is the feet.
            desc.Position = PhysicsMath.ToFP(request.Feet) + FPVector3.Up * (desc.Height / 2);

            var handle = native.CreateCharacter(desc);
            if (!handle.IsValid)
            {
                Log.Error(LogChannels.Ecs, $"phys-r refused a character for {entity}.");
                return;
            }

            var state = native.GetState(handle);
            _physicsCharacter.Set(entity, new PhysicsCharacter { Handle = handle.Value, Body = state.Body.Value });
            _storage.Track(entity.ID, handle.Value, true);

            var feet = PhysicsMath.ToFloat3(state.Feet);
            _physicsCharacterState.Set(entity, new PhysicsCharacterState
            {
                Feet = feet,
                PreviousFeet = feet,
                Height = state.Height.ToFloat(),
            });
        }

        /// <summary>0 in a request means "phys-r's default".</summary>
        private static void Override(ref FP target, float value)
        {
            if (value > 0f)
            {
                target = PhysicsMath.ToFP(value);
            }
        }
    }
}
