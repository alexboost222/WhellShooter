using Engine.Configs;
using Engine.DI;
using Engine.Ecs;
using Engine.Messages;
using Features.Bots.Components;
using Features.Bots.Configs;
using Features.Bots.Systems;
using Features.Bots.Utils;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Features.Combat.Components;
using Features.Combat.Configs;
using Features.Combat.Systems;
using Features.Map.Components;
using Features.Map.Enums;
using Features.Map.Systems;
using Features.Map.Utils;
using Features.Navigation.Systems;
using Features.Navigation.Utils;
using Scellecs.Morpeh;
using Unity.Mathematics;
using Body = global::Features.CharacterFeature.Components.Character;
using UnityEngine;

namespace Game.Tests.EditMode.Features.Bots
{
    /// <summary>
    /// A headless world with the whole bot pipeline, Combat's damage/revive/dead-intent steps, a
    /// hand-built arena, and a stand-in motor that moves bodies by their intent — enough to watch a
    /// bot decide, walk, turn and pull the trigger without a scene.
    /// </summary>
    internal sealed class BotTestWorld
    {
        public const float Delta = 0.02f;
        public const float Speed = 4.5f;

        public readonly World World;
        public readonly MapLayout Layout;
        public readonly BotsConfig Bots;
        public readonly CharacterConfig Character;
        public readonly CombatConfig Combat;
        public readonly NavStorage Nav;

        private readonly SystemsGroup _group;

        public BotTestWorld(MapLayout layout)
        {
            World = World.Create();
            Layout = layout;
            Bots = ScriptableObject.CreateInstance<BotsConfig>();
            Character = ScriptableObject.CreateInstance<CharacterConfig>();
            Combat = ScriptableObject.CreateInstance<CombatConfig>();
            Combat.Set(100f, false);

            var bus = new MessageBus();
            var container = new Container();
            var catalog = new ConfigCatalog(new Config[] { Bots, Character, Combat });
            container.Register(World);
            container.Register(bus);
            container.AddResolver(new ConfigResolver(catalog));
            container.AddResolver(new StashResolver(World));
            container.AddResolver(new MessagePipeResolver(bus));

            var map = container.Register(new MapStorage());
            map.Set(layout);
            Nav = container.Register(new NavStorage());
            Nav.Set(new NavGrid(layout));

            _group = World.CreateSystemsGroup();
            _group.AddInitializer(container.NewAndRegister<BotActionSet>());
            _group.AddInitializer(container.NewAndRegister<BotSpawnSelector>());
            _group.AddSystem(container.New<BotSpawnSystem>());
            _group.AddSystem(container.New<BotRespawnSystem>());
            _group.AddSystem(container.New<BotSensesSystem>());
            _group.AddSystem(container.New<BotThinkSystem>());
            _group.AddSystem(container.New<BotWanderScoreSystem>());
            _group.AddSystem(container.New<BotAttackScoreSystem>());
            _group.AddSystem(container.New<BotChaseScoreSystem>());
            _group.AddSystem(container.New<BotDecideSystem>());
            _group.AddSystem(container.New<BotWanderSystem>());
            _group.AddSystem(container.New<BotAttackSystem>());
            _group.AddSystem(container.New<BotChaseSystem>());
            _group.AddSystem(container.New<BotPathSystem>());
            _group.AddSystem(container.New<BotSteerSystem>());
            _group.AddSystem(container.New<BotAimSystem>());
            _group.AddSystem(container.New<ReviveSystem>());
            _group.AddSystem(container.New<DamageApplySystem>());
            _group.AddSystem(container.New<DeadIntentSystem>());
            _group.Initialize();
        }

        public void Dispose()
        {
            World.Dispose();
            Object.DestroyImmediate(Bots);
            Object.DestroyImmediate(Character);
            Object.DestroyImmediate(Combat);
        }

        /// <summary>Runs <paramref name="seconds"/> of fixed ticks: the bot pipeline, then the stand-in motor.</summary>
        public void Run(float seconds)
        {
            var ticks = (int)math.ceil(seconds / Delta);
            for (var i = 0; i < ticks; i++)
            {
                _group.Update(Delta);
                World.Commit();
                Move();
            }
        }

        /// <summary>A bot standing at a cell centre, facing <paramref name="yawDegrees"/>.</summary>
        public Entity Bot(int2 cell, float yawDegrees, int team, uint seed = 1)
        {
            var entity = Body(cell, yawDegrees, team);
            World.GetStash<Bot>().Set(entity, new Bot { ProfileId = Bots.DefaultProfileId });
            World.GetStash<BotSenses>().Set(entity, default);
            World.GetStash<BotBrain>().Set(entity, default);
            World.GetStash<BotMove>().Set(entity, default);
            World.GetStash<BotPath>().Set(entity, new BotPath { Corners = new float3[16] });
            World.GetStash<BotAim>().Set(entity, default);
            World.GetStash<BotRandom>().Set(entity, new BotRandom { Value = new Unity.Mathematics.Random(seed) });
            World.Commit();
            return entity;
        }

        /// <summary>A plain body (a stand-in for the player) at a cell centre.</summary>
        public Entity Body(int2 cell, float yawDegrees, int team)
        {
            var entity = World.CreateEntity();
            var position = Layout.CellCenter(cell);
            World.GetStash<Body>().Set(entity);
            World.GetStash<CharacterIntent>().Set(entity, default);
            World.GetStash<CharacterMotion>().Set(entity, new CharacterMotion
            {
                Position = position,
                PreviousPosition = position,
                Grounded = true,
            });
            World.GetStash<Look>().Set(entity, new Look { Yaw = math.radians(yawDegrees) });
            World.GetStash<Health>().Set(entity, new Health { Current = 100f, Max = 100f });
            World.GetStash<Team>().Set(entity, new Team { Index = team });
            World.Commit();
            return entity;
        }

        public void SpawnPoint(int2 cell, int team)
        {
            World.GetStash<SpawnPoint>().Set(World.CreateEntity(), new SpawnPoint
            {
                Position = Layout.CellCenter(cell),
                Team = team,
            });
            World.Commit();
        }

        public void Hurt(Entity source, Entity target, float amount)
        {
            World.GetStash<Damage>().Set(World.CreateEntity(), new Damage
            {
                Source = source.ID,
                Target = target.ID,
                Amount = amount,
                Origin = World.GetStash<CharacterMotion>().Get(source).Position,
            });
            World.Commit();
        }

        public T Get<T>(Entity entity) where T : struct, IComponent
        {
            return World.GetStash<T>().Get(entity);
        }

        public bool Has<T>(Entity entity) where T : struct, IComponent
        {
            return World.GetStash<T>().Has(entity);
        }

        /// <summary>Angle between where the body looks (yaw only) and the direction to a point, degrees.</summary>
        public float YawErrorTo(Entity entity, float3 point)
        {
            var motion = Get<CharacterMotion>(entity);
            var look = Get<Look>(entity);
            return math.degrees(math.abs(BotMath.DeltaAngle(look.Yaw, BotMath.YawOf(point - motion.Position))));
        }

        /// <summary>The stand-in motor: local intent → world velocity at a fixed speed, no collisions.</summary>
        private void Move()
        {
            var intents = World.GetStash<CharacterIntent>();
            var motions = World.GetStash<CharacterMotion>();
            var looks = World.GetStash<Look>();
            var teleports = World.GetStash<CharacterTeleportRequest>();
            var filter = World.Filter.With<Body>().With<CharacterIntent>().With<CharacterMotion>().With<Look>().Build();

            foreach (var entity in filter)
            {
                ref var motion = ref motions.Get(entity);
                if (teleports.Has(entity))
                {
                    var teleport = teleports.Get(entity);
                    teleports.Remove(entity);
                    motion.Position = teleport.Position;
                    motion.PreviousPosition = teleport.Position;
                    motion.Velocity = float3.zero;
                    looks.Get(entity) = new Look { Yaw = teleport.Yaw };
                    continue;
                }

                var intent = intents.Get(entity);
                var yaw = looks.Get(entity).Yaw;
                var forward = new float3(math.sin(yaw), 0f, math.cos(yaw));
                var right = new float3(math.cos(yaw), 0f, -math.sin(yaw));
                var velocity = (right * intent.Move.x + forward * intent.Move.y) * Speed;

                motion.PreviousPosition = motion.Position;
                motion.Position += velocity * Delta;
                motion.Velocity = velocity;
                intents.Get(entity).FirePressed = false;
            }

            World.Commit();
        }

        /// <summary>An empty walled arena.</summary>
        public static MapLayout OpenLayout(int width, int depth)
        {
            var layout = new MapLayout(7, width, depth, 2.5f);
            for (var x = 0; x < width; x++)
            {
                layout.SetEdgeX(x, 0, MapEdgeKind.Border);
                layout.SetEdgeX(x, depth, MapEdgeKind.Border);
            }

            for (var z = 0; z < depth; z++)
            {
                layout.SetEdgeZ(0, z, MapEdgeKind.Border);
                layout.SetEdgeZ(width, z, MapEdgeKind.Border);
            }

            return layout;
        }
    }
}
