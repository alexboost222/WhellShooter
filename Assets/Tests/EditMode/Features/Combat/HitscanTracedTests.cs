using System.Collections.Generic;
using Engine.Messages;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Features.Combat.Components;
using Features.Combat.Messages;
using Features.Combat.Systems;
using Features.WeaponsFeature.Components;
using Features.WeaponsFeature.Configs;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;
using CharacterTag = Features.CharacterFeature.Components.Character;

namespace Game.Tests.EditMode.Features.Combat
{
    /// <summary>Every resolved shot is published as <see cref="ShotTraced"/> for the views (Docs/08 §Combat).</summary>
    public sealed class HitscanTracedTests
    {
        private EcsTestWorld _world;
        private WeaponsConfig _weapons;
        private MessageBus _bus;
        private SubscriberQueue<ShotTraced> _traced;
        private Entity _shooter;

        [SetUp]
        public void SetUp()
        {
            _weapons = ScriptableObject.CreateInstance<WeaponsConfig>();
            _weapons.SetWeapons("Rifle", new WeaponDefinition { Name = "Rifle", Damage = 20f, Range = 100f });

            _world = new EcsTestWorld(_weapons, ScriptableObject.CreateInstance<CharacterConfig>());
            _world.Container.Register<IShotOccluder>(new WallAt(30f));
            _world.Add<HitscanSystem>();

            _bus = _world.Container.Resolve<MessageBus>();
            _traced = _bus.Queue<ShotTraced>(MessagePhase.Update);

            _shooter = _world.World.CreateEntity();
            _world.Set(_shooter, new CharacterTag());
            _world.Set(_shooter, new CharacterMotion());
            _world.Set(_shooter, new Health { Current = 100f, Max = 100f });
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void AMiss_IsTracedToTheWall()
        {
            var shots = Fire();

            Assert.That(shots.Count, Is.EqualTo(1));
            Assert.That(shots[0].Shooter, Is.EqualTo(_shooter.ID));
            Assert.That(shots[0].HitBody, Is.False);
            Assert.That(shots[0].End.z, Is.EqualTo(30f).Within(1e-3f));
        }

        [Test]
        public void ABodyHit_IsTracedToTheBody()
        {
            var victim = _world.World.CreateEntity();
            _world.Set(victim, new CharacterTag());
            _world.Set(victim, new CharacterMotion { Position = new float3(0f, 0f, 10f) });
            _world.Set(victim, new Health { Current = 100f, Max = 100f });

            var shots = Fire();

            Assert.That(shots[0].HitBody, Is.True);
            Assert.That(shots[0].End.z, Is.LessThan(10f).And.GreaterThan(9f), "stops at the capsule's surface");
        }

        private List<ShotTraced> Fire()
        {
            _world.Set(_shooter, new ShotRequest
            {
                Origin = new float3(0f, 1.6f, 0f),
                Direction = new float3(0f, 0f, 1f),
                WeaponId = _weapons.DefaultWeaponId,
            });
            _world.Tick(0.02f);

            _bus.BeginPhase(MessagePhase.Update);
            var shots = new List<ShotTraced>();
            foreach (var shot in _traced)
            {
                shots.Add(shot);
            }

            return shots;
        }

        private sealed class WallAt : IShotOccluder
        {
            private readonly float _distance;

            public WallAt(float distance)
            {
                _distance = distance;
            }

            public bool Raycast(float3 origin, float3 direction, float maxDistance, out float distance)
            {
                distance = _distance;
                return _distance <= maxDistance;
            }
        }
    }
}
