using Features.Bots.Components;
using Features.Bots.Enums;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Features.Map.Enums;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using Body = global::Features.CharacterFeature.Components.Character;

namespace Game.Tests.EditMode.Features.Bots
{
    /// <summary>
    /// The bot pipeline end to end on a headless world: senses → brain → executors → path → steer →
    /// aim, with a stand-in motor. Each test sets up one situation and checks what a player would see
    /// the bot do.
    /// </summary>
    public sealed class BotBehaviourTests
    {
        private BotTestWorld _world;

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
            _world = null;
        }

        [Test]
        public void Alone_TheBotWanders_AndActuallyGoesSomewhere()
        {
            _world = new BotTestWorld(BotTestWorld.OpenLayout(16, 16));
            var bot = _world.Bot(new int2(8, 8), 0f, 0);
            var start = _world.Get<CharacterMotion>(bot).Position;

            _world.Run(4f);

            Assert.That(_world.Get<BotBrain>(bot).Action, Is.EqualTo(BotAction.Wander));
            Assert.That(math.distance(_world.Get<CharacterMotion>(bot).Position, start), Is.GreaterThan(3f));
            Assert.That(_world.Get<CharacterIntent>(bot).Fire, Is.False);
        }

        [Test]
        public void Wandering_StaysOnWalkableGround()
        {
            var layout = BotTestWorld.OpenLayout(16, 16);
            for (var z = 3; z < 13; z++)
            {
                layout.SetCell(new int2(8, z), MapCellKind.Blocked);
            }

            _world = new BotTestWorld(layout);
            var bot = _world.Bot(new int2(4, 8), 0f, 0, seed: 3);
            var grid = new global::Features.Navigation.Utils.NavGrid(layout);

            for (var i = 0; i < 40; i++)
            {
                _world.Run(0.5f);
                var cell = grid.CellOf(_world.Get<CharacterMotion>(bot).Position);
                Assert.That(grid.IsWalkable(cell), Is.True, $"bot walked into {cell} at step {i}");
            }
        }

        [Test]
        public void EnemyInFront_TheBotAttacks_TurnsOnIt_AndFiresAfterItsReaction()
        {
            _world = new BotTestWorld(BotTestWorld.OpenLayout(20, 20));
            var bot = _world.Bot(new int2(10, 4), 0f, 0);
            // Ahead and a little to the side, well inside the 120° field of view.
            var enemy = _world.Body(new int2(12, 10), 0f, 1);

            _world.Run(0.1f);
            Assert.That(_world.Get<CharacterIntent>(bot).Fire, Is.False, "no shot before the reaction time");

            _world.Run(1.5f);

            Assert.That(_world.Get<BotBrain>(bot).Action, Is.EqualTo(BotAction.Attack));
            Assert.That(_world.Get<BotSenses>(bot).Target, Is.EqualTo(enemy.ID));
            Assert.That(_world.YawErrorTo(bot, _world.Get<CharacterMotion>(enemy).Position), Is.LessThan(10f));
            Assert.That(_world.Get<CharacterIntent>(bot).Fire, Is.True);
        }

        [Test]
        public void TeamMate_IsIgnored()
        {
            _world = new BotTestWorld(BotTestWorld.OpenLayout(20, 20));
            var bot = _world.Bot(new int2(10, 4), 0f, 0);
            _world.Body(new int2(10, 10), 0f, 0);

            _world.Run(1.5f);

            Assert.That(_world.Get<BotSenses>(bot).HasTarget, Is.False);
            Assert.That(_world.Get<BotBrain>(bot).Action, Is.EqualTo(BotAction.Wander));
        }

        [Test]
        public void EnemyBehindAWall_IsNotSeen()
        {
            var layout = BotTestWorld.OpenLayout(20, 20);
            for (var x = 0; x < 20; x++)
            {
                layout.SetEdge(new int2(x, 7), new int2(0, 1), MapEdgeKind.Wall);
            }

            _world = new BotTestWorld(layout);
            var bot = _world.Bot(new int2(10, 4), 0f, 0);
            _world.Body(new int2(10, 11), 0f, 1);

            _world.Run(1f);

            Assert.That(_world.Get<BotSenses>(bot).HasTarget, Is.False);
        }

        [Test]
        public void EnemyBehind_IsNoticedOnlyWhenClose()
        {
            _world = new BotTestWorld(BotTestWorld.OpenLayout(20, 20));
            var bot = _world.Bot(new int2(10, 10), 0f, 0);
            // Directly behind, 10 m away: outside the field of view and the awareness radius.
            var far = _world.Body(new int2(10, 6), 0f, 1);

            _world.Run(0.5f);
            Assert.That(_world.Get<BotSenses>(bot).HasTarget, Is.False);

            // Same direction, one cell (2.5 m) away: inside the awareness radius.
            _world.World.GetStash<CharacterMotion>().Get(far).Position = _world.Layout.CellCenter(new int2(10, 9));
            _world.Run(0.5f);
            Assert.That(_world.Get<BotSenses>(bot).HasTarget, Is.True);
        }

        [Test]
        public void ShotFromBehind_TheBotTurnsRound_AndFindsTheShooter()
        {
            _world = new BotTestWorld(BotTestWorld.OpenLayout(20, 20));
            var bot = _world.Bot(new int2(10, 12), 0f, 0);
            var shooter = _world.Body(new int2(10, 5), 0f, 1);

            _world.Run(0.3f);
            Assert.That(_world.Get<BotSenses>(bot).HasTarget, Is.False);

            _world.Hurt(shooter, bot, 10f);
            _world.Run(1.5f);

            Assert.That(_world.Get<BotSenses>(bot).Target, Is.EqualTo(shooter.ID));
            Assert.That(_world.Get<BotSenses>(bot).Visible, Is.True);
            Assert.That(_world.Get<BotBrain>(bot).Action, Is.EqualTo(BotAction.Attack));
        }

        [Test]
        public void LostTarget_IsChasedToWhereItWasSeen_ThenForgotten()
        {
            var layout = BotTestWorld.OpenLayout(20, 20);
            _world = new BotTestWorld(layout);
            var bot = _world.Bot(new int2(10, 4), 0f, 0);
            var enemy = _world.Body(new int2(10, 10), 0f, 1);

            _world.Run(0.6f);
            Assert.That(_world.Get<BotBrain>(bot).Action, Is.EqualTo(BotAction.Attack));
            var lastSeen = _world.Get<CharacterMotion>(enemy).Position;

            // The enemy steps behind a fresh wall: the bot loses sight.
            for (var x = 0; x < 20; x++)
            {
                layout.SetEdge(new int2(x, 7), new int2(0, 1), x == 0 ? MapEdgeKind.Door : MapEdgeKind.Wall);
            }

            _world.World.GetStash<CharacterMotion>().Get(enemy).Position = layout.CellCenter(new int2(18, 18));
            RebuildNav(layout);
            _world.Run(0.6f);

            Assert.That(_world.Get<BotBrain>(bot).Action, Is.EqualTo(BotAction.Chase));
            Assert.That(_world.Get<BotSenses>(bot).TargetPosition, Is.EqualTo(lastSeen));

            _world.Run(10f);
            Assert.That(_world.Get<BotBrain>(bot).Action, Is.EqualTo(BotAction.Wander));
        }

        [Test]
        public void DeadBot_StandsStill_ThenRespawnsAtASpawnPoint()
        {
            _world = new BotTestWorld(BotTestWorld.OpenLayout(20, 20));
            _world.SpawnPoint(new int2(2, 2), 0);
            var bot = _world.Bot(new int2(10, 10), 0f, 0);
            var enemy = _world.Body(new int2(18, 18), 0f, 1);

            _world.Hurt(enemy, bot, 500f);
            _world.Run(0.1f);
            Assert.That(_world.Has<Dead>(bot), Is.True);
            Assert.That(_world.Get<CharacterIntent>(bot).Move, Is.EqualTo(float2.zero));

            _world.Run(_world.Bots.RespawnDelay + 0.2f);

            Assert.That(_world.Has<Dead>(bot), Is.False);
            Assert.That(_world.Get<Health>(bot).Current, Is.EqualTo(100f));
            Assert.That(math.distance(_world.Get<CharacterMotion>(bot).Position, _world.Layout.CellCenter(new int2(2, 2))),
                Is.LessThan(2.5f));
        }

        [Test]
        public void SpawnRequest_MakesBodies_AlternatingTeams_OnTheirSpawnPoints()
        {
            _world = new BotTestWorld(BotTestWorld.OpenLayout(20, 20));
            _world.SpawnPoint(new int2(2, 1), 0);
            _world.SpawnPoint(new int2(17, 18), 1);
            _world.World.GetStash<BotSpawnRequest>().Set(_world.World.CreateEntity(),
                new BotSpawnRequest { Count = 4, Teams = true });
            _world.World.Commit();

            _world.Run(BotTestWorld.Delta);

            var bots = _world.World.Filter.With<Bot>().With<Body>().Build();
            var count = 0;
            foreach (var bot in bots)
            {
                count++;
                var team = _world.Get<Team>(bot).Index;
                var expected = _world.Layout.CellCenter(team == 0 ? new int2(2, 1) : new int2(17, 18));
                Assert.That(math.distance(_world.Get<CharacterMotion>(bot).Position, expected), Is.LessThan(0.01f));
            }

            Assert.That(count, Is.EqualTo(4));
        }

        private void RebuildNav(global::Features.Map.Utils.MapLayout layout)
        {
            // The test edited the layout in place; give the bots a grid of the new walls.
            _world.Nav.Set(new global::Features.Navigation.Utils.NavGrid(layout));
        }
    }
}
