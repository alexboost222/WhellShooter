using Engine.DI;
using Engine.Ecs;
using Features.Combat.Components;
using Features.Combat.Configs;
using Features.Combat.Systems;
using NUnit.Framework;
using Scellecs.Morpeh;
using UnityEngine;

namespace Game.Tests.EditMode.Features.Combat
{
    /// <summary>
    /// <see cref="DamageApplySystem"/> and <see cref="ReviveSystem"/> against a headless world ticked
    /// by hand.
    /// </summary>
    public sealed class DamageTests
    {
        private World _world;
        private CombatConfig _config;
        private SystemsGroup _group;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
            _config = ScriptableObject.CreateInstance<CombatConfig>();
            _config.Set(100f, false);

            var container = new Container();
            container.Register(_world);
            container.Register(_config);
            container.AddResolver(new StashResolver(_world));

            _group = _world.CreateSystemsGroup();
            _group.AddSystem(container.New<ReviveSystem>());
            _group.AddSystem(container.New<DamageApplySystem>());
            _group.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Damage_LowersHealth_RecordsTheHit_AndIsConsumed()
        {
            var shooter = Body(0);
            var victim = Body(1);
            var hit = Hit(shooter, victim, 30f);

            Tick();

            Assert.That(_world.GetStash<Health>().Get(victim).Current, Is.EqualTo(70f));
            var taken = _world.GetStash<DamageTaken>().Get(victim);
            Assert.That(taken.Hits, Is.EqualTo(1u));
            Assert.That(taken.LastSource, Is.EqualTo(shooter.ID));
            Assert.That(_world.GetStash<Destroy>().Has(hit), Is.True);
        }

        [Test]
        public void SeveralHitsInOneTick_AllLand_AndKill()
        {
            var shooter = Body(0);
            var victim = Body(1);
            Hit(shooter, victim, 60f);
            Hit(shooter, victim, 60f);

            Tick();

            Assert.That(_world.GetStash<Health>().Get(victim).Current, Is.EqualTo(0f));
            Assert.That(_world.GetStash<Dead>().Has(victim), Is.True);
            Assert.That(_world.GetStash<Dead>().Get(victim).Killer, Is.EqualTo(shooter.ID));
        }

        [Test]
        public void TeamMate_IsNotHurt_WithFriendlyFireOff()
        {
            var shooter = Body(0);
            var mate = Body(0);
            Hit(shooter, mate, 50f);

            Tick();

            Assert.That(_world.GetStash<Health>().Get(mate).Current, Is.EqualTo(100f));
        }

        [Test]
        public void FreeForAll_HurtsAnyone()
        {
            var shooter = Body(Team.FreeForAll);
            var other = Body(Team.FreeForAll);
            Hit(shooter, other, 50f);

            Tick();

            Assert.That(_world.GetStash<Health>().Get(other).Current, Is.EqualTo(50f));
        }

        [Test]
        public void TheDead_TakeNoMoreDamage()
        {
            var shooter = Body(0);
            var victim = Body(1);
            Hit(shooter, victim, 200f);
            Tick();

            Hit(shooter, victim, 10f);
            Tick();

            Assert.That(_world.GetStash<DamageTaken>().Get(victim).Hits, Is.EqualTo(1u));
        }

        [Test]
        public void Revive_RefillsHealth_AndClearsDead()
        {
            var shooter = Body(0);
            var victim = Body(1);
            Hit(shooter, victim, 200f);
            Tick();

            _world.GetStash<ReviveRequest>().Set(victim);
            Tick();

            Assert.That(_world.GetStash<Dead>().Has(victim), Is.False);
            Assert.That(_world.GetStash<Health>().Get(victim).Current, Is.EqualTo(100f));
            Assert.That(_world.GetStash<ReviveRequest>().Has(victim), Is.False);
        }

        private Entity Body(int team)
        {
            var entity = _world.CreateEntity();
            _world.GetStash<Health>().Set(entity, new Health { Current = 100f, Max = 100f });
            _world.GetStash<Team>().Set(entity, new Team { Index = team });
            _world.Commit();
            return entity;
        }

        private Entity Hit(Entity source, Entity target, float amount)
        {
            var entity = _world.CreateEntity();
            _world.GetStash<Damage>().Set(entity, new Damage { Source = source.ID, Target = target.ID, Amount = amount });
            _world.Commit();
            return entity;
        }

        private void Tick()
        {
            _group.Update(0.02f);
            _world.Commit();
        }
    }
}
