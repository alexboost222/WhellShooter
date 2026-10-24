using Engine.DI;
using Engine.Messages;
using Features.Combat.Messages;
using Features.PlayerFeature.Components;
using Features.Tracers.Configs;
using Features.Tracers.Utils;
using Scellecs.Morpeh;
using EntityId = Scellecs.Morpeh.EntityId;

namespace Features.Tracers.Systems
{
    /// <summary>
    /// Draws every live <see cref="ShotTraced"/> as a streak flying from the shooter's weapon to the
    /// hit point (Docs/08 §Tracers). A view: a <see cref="TracerPool"/>, nothing read back.
    /// </summary>
    public sealed class TracerSystem : ISystem
    {
        [Inject] private Stash<Player> _player;

        [Inject] private SubscriberQueue<ShotTraced> _shotTraced;
        [Inject] private TracerConfig _config;

        public World World { get; set; }

        private TracerPool _pool;

        public void OnAwake()
        {
            _pool = new TracerPool(_config, "Tracers");
        }

        public void Dispose()
        {
            _pool?.Dispose();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var shot in _shotTraced)
            {
                _pool.Spawn(shot.Origin, shot.End, IsLocalPlayer(shot.Shooter));
            }

            _pool.Update(deltaTime);
        }

        private bool IsLocalPlayer(EntityId shooter)
        {
            return World.TryGetEntity(shooter, out var entity) && _player.Has(entity);
        }
    }
}
