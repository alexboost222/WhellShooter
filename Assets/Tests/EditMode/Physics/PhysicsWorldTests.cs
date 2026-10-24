using System;
using System.Collections.Generic;
using Engine.Physics;
using Engine.Runtime;
using NUnit.Framework;
using PhysR;
using Unity.Mathematics;

namespace Engine.Tests.EditMode.Physics
{
    /// <summary>The engine side of phys-r (Docs/11): the world service, its tick, the physics hook.</summary>
    [TestFixture]
    public sealed class PhysicsWorldTests
    {
        private PhysicsWorld _world;

        [SetUp]
        public void SetUp()
        {
            _world = new PhysicsWorld(50);
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void TickRateOf_TakesWholeTickRates()
        {
            Assert.That(PhysicsWorld.TickRateOf(0.02f), Is.EqualTo(50));
            Assert.That(PhysicsWorld.TickRateOf(1f / 60f), Is.EqualTo(60));
        }

        [Test]
        public void TickRateOf_RejectsAStepThatIsNotAWholeRate()
        {
            Assert.Throws<InvalidOperationException>(() => PhysicsWorld.TickRateOf(0.03f));
        }

        [Test]
        public void TheWorldTicksAtItsRate()
        {
            Assert.That(_world.DeltaTime, Is.EqualTo(0.02f).Within(1e-7f));
            Assert.That(_world.Native.Dt, Is.EqualTo(FP.FromRatio(1, 50)));

            var start = _world.Tick;
            _world.Step();
            _world.Step();

            Assert.That(_world.Tick, Is.EqualTo(start + 2));
        }

        [Test]
        public void Raycast_HitsAStaticBox_AndReportsItInFloats()
        {
            var body = AddStaticBox(new float3(0f, 0f, 10f), new float3(1f, 1f, 1f));

            var hit = _world.Raycast(float3.zero, new float3(0f, 0f, 1f), 100f, out var result);

            Assert.That(hit, Is.True);
            Assert.That(result.Body, Is.EqualTo(body.Value));
            Assert.That(result.Distance, Is.EqualTo(9f).Within(1e-3f));
            Assert.That(result.Point.z, Is.EqualTo(9f).Within(1e-3f));
            Assert.That(result.Normal.z, Is.EqualTo(-1f).Within(1e-3f));
        }

        [Test]
        public void ABodyIsNotQueryable_UntilTheNextStep()
        {
            var desc = BodyDesc.Default;
            desc.Kind = BodyKind.Static;
            desc.Shape = _world.Native.AddBox(PhysicsMath.ToFP(new float3(1f)));
            desc.Position = PhysicsMath.ToFP(new float3(0f, 0f, 10f));
            desc.Rotation = FPQuaternion.Identity;
            _world.Native.CreateBody(desc);

            Assert.That(_world.Raycast(float3.zero, new float3(0f, 0f, 1f), 100f, out _), Is.False);

            _world.Step();

            Assert.That(_world.Raycast(float3.zero, new float3(0f, 0f, 1f), 100f, out _), Is.True);
        }

        [Test]
        public void Raycast_MissesBeyondItsLength()
        {
            AddStaticBox(new float3(0f, 0f, 10f), new float3(1f, 1f, 1f));

            Assert.That(_world.Raycast(float3.zero, new float3(0f, 0f, 1f), 5f, out _), Is.False);
        }

        [Test]
        public void Dispose_IsIdempotent_AndTheWorldIsUnusableAfter()
        {
            _world.Dispose();
            _world.Dispose();

            Assert.That(_world.IsDisposed, Is.True);
            Assert.Throws<ObjectDisposedException>(() => _world.Step());
        }

        [Test]
        public void PhysicsStep_RunsBefore_ThenTheWorld_ThenAfter()
        {
            var order = new List<string>();
            var step = new PhysicsStep(_world,
                RunnerStep.Create("before", _ => order.Add("before:" + _world.Tick)),
                RunnerStep.Create("after", _ => order.Add("after:" + _world.Tick)));

            step.Run(0.02f);

            Assert.That(order, Is.EqualTo(new[] { "before:0", "after:1" }));
        }

        [Test]
        public void PhysicsStep_RefusesADeltaThatIsNotTheWorldsTick()
        {
            var step = new PhysicsStep(_world);

            Assert.Throws<InvalidOperationException>(() => step.Run(1f / 60f));
            Assert.That(_world.Tick, Is.Zero);
        }

        [Test]
        public void PhysicsMath_RoundTripsWithinFixedPointPrecision()
        {
            var value = new float3(1.25f, -3.5f, 1000.125f);

            Assert.That(PhysicsMath.ToFloat3(PhysicsMath.ToFP(value)), Is.EqualTo(value));

            var rotation = quaternion.RotateY(0.7f);
            var back = PhysicsMath.ToQuaternion(PhysicsMath.ToFP(rotation));
            Assert.That(math.abs(math.dot(back, rotation)), Is.EqualTo(1f).Within(1e-5f));
        }

        private BodyHandle AddStaticBox(float3 center, float3 halfExtents)
        {
            var desc = BodyDesc.Default;
            desc.Kind = BodyKind.Static;
            desc.Shape = _world.Native.AddBox(PhysicsMath.ToFP(halfExtents));
            desc.Position = PhysicsMath.ToFP(center);
            desc.Rotation = FPQuaternion.Identity;
            var body = _world.Native.CreateBody(desc);

            // New bodies join the query structures on the next step.
            _world.Step();
            return body;
        }
    }
}
