using System.Collections.Generic;
using Engine.DI;
using Engine.Ecs;
using Engine.Messages;
using Features.Bots.Components;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Features.CharacterFeature.Utils;
using Features.Combat.Components;
using Features.Combat.Configs;
using Features.Combat.Messages;
using Features.Combat.Systems;
using Features.Map.Systems;
using Features.Replication.Components;
using Features.Replication.Enums;
using Features.Replication.Utils;
using Features.WeaponsFeature.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Replication.Systems
{
    /// <summary>
    /// Client: every body but the local one, drawn from snapshots at the render tick (Docs/05
    /// §Remote bodies). Created when the newest snapshot has it, destroyed — view too — when it does
    /// not; each step its motion moves to the render tick's state so views interpolate as for any
    /// body. A rising shot counter becomes tracers from its eye.
    /// </summary>
    public sealed class RemoteBodySystem : ISystem
    {
        // How far either side of a missing render tick a neighbour is looked for.
        private const int SearchTicks = 4;
        // Tracers drawn for one counter jump at most: a long gap is not a burst.
        private const int MaxTracersPerStep = 3;

        [Inject] private Stash<NetEntity> _netEntity;
        [Inject] private Stash<RemoteBody> _remoteBody;
        [Inject] private Stash<Avatar> _avatar;
        [Inject] private Stash<Bot> _bot;
        [Inject] private Stash<BotAim> _botAim;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Look> _look;
        [Inject] private Stash<Health> _health;
        [Inject] private Stash<Dead> _dead;
        [Inject] private Stash<DamageTaken> _damageTaken;
        [Inject] private Stash<Team> _team;
        [Inject] private Stash<CharacterViewLink> _characterViewLink;
        [Inject] private Stash<Destroy> _destroy;

        [Inject] private IPublisher<ShotTraced> _shotTraced;
        [Inject] private IShotOccluder _occluder;
        [Inject] private SnapshotBuffer _buffer;
        [Inject] private MapStorage _map;
        [Inject] private CharacterConfig _characterConfig;
        [Inject] private CombatConfig _combatConfig;
        [Inject] private WeaponsConfig _weaponsConfig;

        public World World { get; set; }

        private readonly Dictionary<int, Entity> _entities = new Dictionary<int, Entity>();
        private readonly List<int> _gone = new List<int>();
        private Filter _bodies;

        public void OnAwake()
        {
            _bodies = World.Filter
                .With<NetEntity>()
                .Build();
        }

        public void Dispose()
        {
            _entities.Clear();
        }

        public void OnUpdate(float deltaTime)
        {
            var newest = _buffer.Newest;
            if (newest == null || _map.Layout == null || _map.Layout.Seed != newest.Seed)
            {
                return;
            }

            Sweep(newest);
            for (var i = 0; i < newest.Count; i++)
            {
                var id = newest.Bodies[i].Id;
                if (id == newest.You)
                {
                    continue;
                }

                if (!_entities.TryGetValue(id, out var entity) || entity.IsDisposed())
                {
                    entity = Create(newest.Bodies[i]);
                    _entities[id] = entity;
                }

                Apply(entity, Sample(id, newest));
            }
        }

        private void Sweep(SnapshotFrame newest)
        {
            _gone.Clear();
            foreach (var pair in _entities)
            {
                if (pair.Key == newest.You || !newest.TryFind(pair.Key, out _) || pair.Value.IsDisposed())
                {
                    _gone.Add(pair.Key);
                }
            }

            for (var i = 0; i < _gone.Count; i++)
            {
                var entity = _entities[_gone[i]];
                _entities.Remove(_gone[i]);
                if (entity.IsDisposed())
                {
                    continue;
                }

                if (_characterViewLink.Has(entity) && _characterViewLink.Get(entity).View != null)
                {
                    UnityEngine.Object.Destroy(_characterViewLink.Get(entity).View.gameObject);
                }

                _destroy.Set(entity);
            }
        }

        private Entity Create(BodySnapshot body)
        {
            var entity = World.CreateEntity();
            _netEntity.Set(entity, new NetEntity { Id = body.Id });
            _remoteBody.Set(entity, new RemoteBody { Shots = body.Shots });
            _avatar.Set(entity);
            if (body.Has(SnapshotFlags.Bot))
            {
                _bot.Set(entity, new Bot());
            }

            _characterMotion.Set(entity, new CharacterMotion
            {
                Position = body.Feet,
                PreviousPosition = body.Feet,
            });
            _look.Set(entity, new Look { Yaw = body.Yaw, Pitch = body.Pitch });
            _damageTaken.Set(entity, new DamageTaken { Hits = body.Hits });
            _team.Set(entity, new Team { Index = body.Team });
            return entity;
        }

        /// <summary>The body at the render tick: exact, else lerped between the nearest neighbours, else the newest.</summary>
        private BodySnapshot Sample(int id, SnapshotFrame newest)
        {
            var render = _buffer.RenderTick;
            if (_buffer.TryGet(render, out var exact) && exact.TryFind(id, out var body))
            {
                return body;
            }

            var hasBefore = false;
            var hasAfter = false;
            BodySnapshot before = default;
            BodySnapshot after = default;
            var beforeTick = 0;
            var afterTick = 0;
            for (var k = 1; k <= SearchTicks; k++)
            {
                if (!hasBefore && _buffer.TryGet(render - k, out var older) && older.TryFind(id, out before))
                {
                    hasBefore = true;
                    beforeTick = render - k;
                }

                if (!hasAfter && _buffer.TryGet(render + k, out var newer) && newer.TryFind(id, out after))
                {
                    hasAfter = true;
                    afterTick = render + k;
                }
            }

            if (hasBefore && hasAfter)
            {
                return Lerp(before, after, (float)(render - beforeTick) / (afterTick - beforeTick));
            }

            if (hasBefore)
            {
                return before;
            }

            if (hasAfter)
            {
                return after;
            }

            newest.TryFind(id, out body);
            return body;
        }

        private static BodySnapshot Lerp(BodySnapshot a, BodySnapshot b, float t)
        {
            var result = b;
            result.Feet = math.lerp(a.Feet, b.Feet, t);
            result.Velocity = math.lerp(a.Velocity, b.Velocity, t);
            result.Yaw = LookMath.WrapAngle(a.Yaw + LookMath.DeltaAngle(a.Yaw, b.Yaw) * t);
            result.Pitch = math.lerp(a.Pitch, b.Pitch, t);
            return result;
        }

        private void Apply(Entity entity, BodySnapshot body)
        {
            ref var motion = ref _characterMotion.Get(entity);
            motion.PreviousPosition = motion.Position;
            motion.Position = body.Feet;
            motion.Velocity = body.Velocity;
            motion.Grounded = body.Has(SnapshotFlags.Grounded);

            _look.Set(entity, new Look { Yaw = body.Yaw, Pitch = body.Pitch });
            _health.Set(entity, new Health { Current = body.Health, Max = _combatConfig.MaxHealth });
            _team.Set(entity, new Team { Index = body.Team });

            ref var taken = ref _damageTaken.Get(entity);
            taken.Hits += (ushort)(body.Hits - (ushort)taken.Hits);

            var dead = body.Has(SnapshotFlags.Dead);
            if (dead && !_dead.Has(entity))
            {
                _dead.Set(entity, new Dead { Killer = EntityOf(body.Killer) });
            }
            else if (!dead && _dead.Has(entity))
            {
                _dead.Remove(entity);
            }

            var aiming = body.Has(SnapshotFlags.Aiming) && !dead;
            if (aiming || _botAim.Has(entity))
            {
                _botAim.Set(entity, new BotAim { Active = aiming });
            }

            Trace(entity, body);
        }

        private void Trace(Entity entity, BodySnapshot body)
        {
            ref var remote = ref _remoteBody.Get(entity);
            var fired = (ushort)(body.Shots - remote.Shots);
            remote.Shots = body.Shots;
            if (fired == 0 || fired > short.MaxValue || !_weaponsConfig.TryGet(_weaponsConfig.DefaultWeaponId, out var weapon))
            {
                return;
            }

            var look = new Look { Yaw = body.Yaw, Pitch = body.Pitch };
            var origin = body.Feet + new float3(0f, _characterConfig.EyeHeight, 0f);
            var direction = look.Forward;
            var reach = _occluder.Raycast(origin, direction, weapon.Range, out var wall) ? wall : weapon.Range;
            for (var i = 0; i < math.min((int)fired, MaxTracersPerStep); i++)
            {
                _shotTraced.Publish(new ShotTraced
                {
                    Shooter = entity.ID,
                    Origin = origin,
                    End = origin + direction * reach,
                    HitBody = false,
                });
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
