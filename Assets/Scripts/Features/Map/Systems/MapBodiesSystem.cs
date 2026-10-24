using Engine.DI;
using Engine.Ecs;
using Features.Bodies.Components;
using Features.Bodies.Enums;
using Features.Map.Components;
using Features.Map.Configs;
using Features.Map.Utils;
using PhysR;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Map.Systems
{
    /// <summary>
    /// Keeps the arena's collision in phys-r (Docs/11): whenever <see cref="MapStorage.Version"/>
    /// moves, the old arena's <see cref="MapBody"/> entities are tagged <see cref="Destroy"/> and one
    /// entity per <see cref="MapCollision"/> box asks Bodies for a static body.
    /// </summary>
    /// <remarks>
    /// Runs right after <see cref="MapGenerateSystem"/>, so a new arena's bodies are created in the
    /// same tick's physics step and the old ones released there.
    /// </remarks>
    public sealed class MapBodiesSystem : ISystem
    {
        [Inject] private Stash<MapBody> _mapBody;
        [Inject] private Stash<BodyCreateRequest> _bodyCreateRequest;
        [Inject] private Stash<Destroy> _destroy;

        [Inject] private MapStorage _storage;
        [Inject] private MapConfig _config;

        public World World { get; set; }

        private Filter _filter;
        private int _builtVersion;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<MapBody>()
                .Without<Destroy>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            if (_storage.Version == _builtVersion)
            {
                return;
            }

            _builtVersion = _storage.Version;

            foreach (var entity in _filter)
            {
                _destroy.Set(entity);
            }

            if (_storage.Layout == null)
            {
                return;
            }

            foreach (var box in MapCollision.Build(_storage.Layout, _config.Settings))
            {
                var entity = World.CreateEntity();
                _mapBody.Set(entity);
                _bodyCreateRequest.Set(entity, new BodyCreateRequest
                {
                    Kind = BodyKind.Static,
                    Shape = BodyShape.Box,
                    Size = box.HalfExtents,
                    Position = box.Center,
                    Rotation = quaternion.RotateY(box.Yaw),
                });
            }
        }
    }
}
