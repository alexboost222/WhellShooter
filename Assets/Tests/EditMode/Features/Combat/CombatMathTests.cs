using Features.Combat.Components;
using Features.Combat.Utils;
using NUnit.Framework;
using Unity.Mathematics;

namespace Game.Tests.EditMode.Features.Combat
{
    /// <summary>The pure pieces of combat: who is hostile to whom, and ray against a body's capsule.</summary>
    public sealed class CombatMathTests
    {
        private const float Height = 1.8f;
        private const float Radius = 0.35f;

        [Test]
        public void Hostility_FreeForAllHatesEveryone_TeamsHateOnlyTheOtherTeam()
        {
            Assert.That(CombatRules.AreHostile(Team.FreeForAll, Team.FreeForAll), Is.True);
            Assert.That(CombatRules.AreHostile(Team.FreeForAll, 0), Is.True);
            Assert.That(CombatRules.AreHostile(0, 1), Is.True);
            Assert.That(CombatRules.AreHostile(1, 1), Is.False);
        }

        [Test]
        public void RayThroughTheChest_HitsAtTheSurface()
        {
            var feet = new float3(0f, 0f, 10f);
            var hit = CapsuleRay.Intersect(new float3(0f, 1.2f, 0f), new float3(0f, 0f, 1f), feet, Height, Radius,
                out var distance);

            Assert.That(hit, Is.True);
            Assert.That(distance, Is.EqualTo(10f - Radius).Within(1e-4f));
        }

        [Test]
        public void RayPassingBeside_OrOverTheHead_Misses()
        {
            var feet = new float3(0f, 0f, 10f);
            Assert.That(CapsuleRay.Intersect(new float3(Radius + 0.05f, 1.2f, 0f), new float3(0f, 0f, 1f), feet, Height,
                Radius, out _), Is.False);
            Assert.That(CapsuleRay.Intersect(new float3(0f, Height + 0.05f, 0f), new float3(0f, 0f, 1f), feet, Height,
                Radius, out _), Is.False);
        }

        [Test]
        public void RayIntoTheTopCap_Hits()
        {
            var feet = new float3(0f, 0f, 10f);
            var hit = CapsuleRay.Intersect(new float3(0f, Height - 0.05f, 0f), new float3(0f, 0f, 1f), feet, Height,
                Radius, out var distance);

            Assert.That(hit, Is.True);
            Assert.That(distance, Is.GreaterThan(10f - Radius).And.LessThan(10f));
        }

        [Test]
        public void BodyBehindTheMuzzle_IsNotHit()
        {
            var feet = new float3(0f, 0f, -10f);
            Assert.That(CapsuleRay.Intersect(new float3(0f, 1.2f, 0f), new float3(0f, 0f, 1f), feet, Height, Radius,
                out _), Is.False);
        }

        [Test]
        public void MuzzleInsideTheBody_HitsAtZero()
        {
            var hit = CapsuleRay.Intersect(new float3(0f, 1f, 0f), new float3(0f, 0f, 1f), float3.zero, Height, Radius,
                out var distance);

            Assert.That(hit, Is.True);
            Assert.That(distance, Is.EqualTo(0f));
        }
    }
}
