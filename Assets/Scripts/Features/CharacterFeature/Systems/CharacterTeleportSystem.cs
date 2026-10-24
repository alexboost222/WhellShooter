using System.Collections.Generic;
using Engine.DI;
using Features.Bodies.Components;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Utils;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.CharacterFeature.Systems
{
    /// <summary>
    /// Consumes <see cref="CharacterTeleportRequest"/>: moves the body, levels its view, stops it,
    /// and snaps interpolation so the camera does not sweep across the map.
    /// </summary>
    /// <remarks>
    /// A body that already has its phys-r capsule is moved there by a
    /// <see cref="PhysicsTeleportRequest"/> in this tick's physics step; one that doesn't yet gets
    /// its capsule created at the new position.
    /// </remarks>
    public sealed class CharacterTeleportSystem : ISystem
    {
        [Inject] private Stash<CharacterTeleportRequest> _characterTeleportRequest;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Look> _look;
        [Inject] private Stash<PhysicsCharacter> _physicsCharacter;
        [Inject] private Stash<PhysicsTeleportRequest> _physicsTeleportRequest;

        public World World { get; set; }

        private readonly List<Entity> _done = new List<Entity>();

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<CharacterTeleportRequest>()
                .With<CharacterMotion>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            _done.Clear();

            foreach (var entity in _filter)
            {
                var request = _characterTeleportRequest.Get(entity);
                ref var motion = ref _characterMotion.Get(entity);

                motion.Position = request.Position;
                motion.PreviousPosition = request.Position;
                motion.Velocity = float3.zero;
                motion.Grounded = false;

                if (_look.Has(entity))
                {
                    _look.Set(entity, new Look { Yaw = LookMath.WrapAngle(request.Yaw) });
                }

                if (_physicsCharacter.Has(entity))
                {
                    _physicsTeleportRequest.Set(entity, new PhysicsTeleportRequest { Feet = request.Position });
                }

                _done.Add(entity);
            }

            for (var i = 0; i < _done.Count; i++)
            {
                _characterTeleportRequest.Remove(_done[i]);
            }
        }
    }
}
