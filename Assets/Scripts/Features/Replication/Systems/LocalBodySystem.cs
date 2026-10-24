using Engine.DI;
using Engine.Diagnostics.Logs;
using Features.Bodies.Components;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Features.Combat.Configs;
using Features.Map.Systems;
using Features.Network.Configs;
using Features.Network.Constants;
using Features.PlayerFeature.Components;
using Features.Replication.Components;
using Features.Replication.Enums;
using Features.Replication.Utils;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Replication.Systems
{
    /// <summary>
    /// Client: the local body (Docs/05 §Prediction). Created where the server has it once a snapshot
    /// names it and the arena is built; afterwards its health, death and hits follow the newest
    /// snapshot, and its predicted motion is checked against the server's for the same tick —
    /// shifted by a small error, teleported by a large one.
    /// </summary>
    public sealed class LocalBodySystem : ISystem
    {
        [Inject] private Stash<NetEntity> _netEntity;
        [Inject] private Stash<Prediction> _prediction;
        [Inject] private Stash<Player> _player;
        [Inject] private Stash<PlayerLookInput> _playerLookInput;
        [Inject] private Stash<Character> _character;
        [Inject] private Stash<CharacterIntent> _characterIntent;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<CharacterTeleportRequest> _characterTeleportRequest;
        [Inject] private Stash<Look> _look;
        [Inject] private Stash<Health> _health;
        [Inject] private Stash<Dead> _dead;
        [Inject] private Stash<DamageTaken> _damageTaken;
        [Inject] private Stash<PhysicsCharacterCorrection> _physicsCharacterCorrection;

        [Inject] private SnapshotBuffer _buffer;
        [Inject] private MapStorage _map;
        [Inject] private NetConfig _config;
        [Inject] private CombatConfig _combatConfig;

        public World World { get; set; }

        private Filter _filter;
        private Filter _bodies;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Player>()
                .With<NetEntity>()
                .With<Prediction>()
                .With<CharacterMotion>()
                .Build();
            _bodies = World.Filter
                .With<NetEntity>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            var newest = _buffer.Newest;
            if (newest == null || newest.You == NetEntity.None || !newest.TryFind(newest.You, out var body))
            {
                return;
            }

            var local = _filter.FirstOrDefault();
            if (local == null)
            {
                if (_map.Layout != null && _map.Layout.Seed == newest.Seed)
                {
                    Create(newest.You, body);
                }

                return;
            }

            ApplyState(local, body);
            Check(local, newest.Tick, body);
        }

        private void Create(int id, BodySnapshot body)
        {
            var entity = World.CreateEntity();
            _netEntity.Set(entity, new NetEntity { Id = id });
            _player.Set(entity);
            _playerLookInput.Set(entity, new PlayerLookInput());
            _character.Set(entity);
            _characterIntent.Set(entity, new CharacterIntent());
            _look.Set(entity, new Look { Yaw = body.Yaw, Pitch = body.Pitch });
            _characterMotion.Set(entity, new CharacterMotion
            {
                Position = body.Feet,
                PreviousPosition = body.Feet,
            });

            var ticks = new int[Prediction.Capacity];
            for (var i = 0; i < ticks.Length; i++)
            {
                ticks[i] = NetConstants.NoTick;
            }

            _prediction.Set(entity, new Prediction
            {
                Ticks = ticks,
                Feet = new float3[Prediction.Capacity],
                Velocity = new float3[Prediction.Capacity],
                CheckedTick = NetConstants.NoTick,
            });
            Log.Info(LogChannels.Net, $"[Client] my body is #{id}, at {body.Feet}.");
        }

        private void ApplyState(Entity local, BodySnapshot body)
        {
            _health.Set(local, new Health { Current = body.Health, Max = _combatConfig.MaxHealth });

            if (!_damageTaken.Has(local))
            {
                _damageTaken.Set(local, new DamageTaken());
            }

            ref var taken = ref _damageTaken.Get(local);
            taken.Hits += (ushort)(body.Hits - (ushort)taken.Hits);

            var dead = body.Has(SnapshotFlags.Dead);
            if (dead && !_dead.Has(local))
            {
                _dead.Set(local, new Dead { Killer = EntityOf(body.Killer) });
            }
            else if (!dead && _dead.Has(local))
            {
                _dead.Remove(local);
            }
        }

        private void Check(Entity local, int tick, BodySnapshot body)
        {
            ref var prediction = ref _prediction.Get(local);
            if (tick <= prediction.CheckedTick)
            {
                return;
            }

            prediction.CheckedTick = tick;
            var slot = tick & Prediction.Mask;
            if (prediction.Ticks[slot] != tick)
            {
                return;
            }

            var error = body.Feet - prediction.Feet[slot];
            var distance = math.length(error);
            if (distance <= _config.CorrectionEpsilon)
            {
                return;
            }

            if (distance > _config.SnapDistance)
            {
                _characterTeleportRequest.Set(local, new CharacterTeleportRequest
                {
                    Position = body.Feet,
                    Yaw = _look.Get(local).Yaw,
                });
                for (var i = 0; i < Prediction.Capacity; i++)
                {
                    prediction.Ticks[i] = NetConstants.NoTick;
                }

                return;
            }

            _physicsCharacterCorrection.Set(local, new PhysicsCharacterCorrection
            {
                FeetDelta = error,
                VelocityDelta = body.Velocity - prediction.Velocity[slot],
            });
            for (var i = 0; i < Prediction.Capacity; i++)
            {
                if (prediction.Ticks[i] > tick)
                {
                    prediction.Feet[i] += error;
                }
            }
        }

        private EntityId EntityOf(int id)
        {
            if (id != NetEntity.None)
            {
                foreach (var entity in _bodies)
                {
                    if (_netEntity.Get(entity).Id == id)
                    {
                        return entity.ID;
                    }
                }
            }

            return default;
        }
    }
}
