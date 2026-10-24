using Features.ViewmodelFeature.Configs;
using Features.ViewmodelFeature.Utils;
using Features.WeaponsFeature.Configs;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.EditMode.Features.Viewmodel
{
    public sealed class ViewmodelMathTests
    {
        private ViewmodelConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ViewmodelConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void TurningRight_LeavesTheWeaponPointingAndTrailingLeft()
        {
            ViewmodelMath.MotionTargets(new float2(180f, 0f), float3.zero, _config, out var position, out var rotation);

            Assert.That(rotation.y, Is.LessThan(0f), "yaw lags left");
            Assert.That(position.x, Is.LessThan(0f), "trails left");
        }

        [Test]
        public void LookingUp_LeavesTheWeaponPointingDownAndLow()
        {
            // Looking up is pitch decreasing: a negative pitch rate.
            ViewmodelMath.MotionTargets(new float2(0f, -180f), float3.zero, _config, out var position, out var rotation);

            Assert.That(rotation.x, Is.GreaterThan(0f), "positive pitch is nose down");
            Assert.That(position.y, Is.LessThan(0f));
        }

        [Test]
        public void AFlick_IsCapped()
        {
            ViewmodelMath.MotionTargets(new float2(100000f, 100000f), float3.zero, _config, out var position, out var rotation);

            Assert.That(math.abs(math.degrees(rotation.y)), Is.LessThanOrEqualTo(_config.MaxSwayRotation + 1e-3f));
            Assert.That(math.abs(position.x), Is.LessThanOrEqualTo(_config.MaxSwayPosition + 1e-5f));
        }

        [Test]
        public void StrafingRight_LeansRight_AndFallingLiftsTheWeapon()
        {
            ViewmodelMath.MotionTargets(float2.zero, new float3(4f, -8f, 0f), _config, out var position, out var rotation);

            Assert.That(rotation.z, Is.LessThan(0f), "negative roll tips the top right");
            Assert.That(position.y, Is.GreaterThan(0f));
            Assert.That(position.y, Is.LessThanOrEqualTo(_config.MaxFallLag + 1e-5f));
        }

        [Test]
        public void Bob_IsAFigureOfEight_LowOnEachFootfall()
        {
            // Footfalls are at φ = π/2 + kπ: weapon out to the side and at its lowest.
            ViewmodelMath.Bob(math.PI / 2f, 1f, _config, out var left, out _);
            ViewmodelMath.Bob(3f * math.PI / 2f, 1f, _config, out var right, out _);
            ViewmodelMath.Bob(0f, 1f, _config, out var between, out _);

            Assert.That(left.x, Is.GreaterThan(0f));
            Assert.That(right.x, Is.LessThan(0f));
            Assert.That(left.y, Is.LessThan(between.y));
            Assert.That(right.y, Is.LessThan(between.y));
        }

        [Test]
        public void Bob_AtZeroAmplitude_IsStill()
        {
            ViewmodelMath.Bob(1.3f, 0f, _config, out var position, out var rotation);

            Assert.That(position, Is.EqualTo(float3.zero));
            Assert.That(rotation, Is.EqualTo(float3.zero));
        }

        [Test]
        public void Recoil_IsTheSameForTheSameShot_AndDiffersBetweenShots()
        {
            var weapon = new WeaponViewmodel();

            ViewmodelMath.RecoilKick(weapon, 7, 1f, out var a, out var aRotation, out var aCamera);
            ViewmodelMath.RecoilKick(weapon, 7, 1f, out var b, out var bRotation, out var bCamera);
            ViewmodelMath.RecoilKick(weapon, 8, 1f, out _, out var cRotation, out _);

            Assert.That(b, Is.EqualTo(a));
            Assert.That(bRotation, Is.EqualTo(aRotation));
            Assert.That(bCamera, Is.EqualTo(aCamera));
            Assert.That(cRotation, Is.Not.EqualTo(aRotation));
        }

        [Test]
        public void Recoil_KicksBackAndMuzzleUp_AndScales()
        {
            var weapon = new WeaponViewmodel();

            ViewmodelMath.RecoilKick(weapon, 1, 1f, out var full, out var rotation, out var camera);
            ViewmodelMath.RecoilKick(weapon, 1, 0.5f, out var half, out _, out _);

            Assert.That(full.z, Is.LessThan(0f), "towards the face");
            Assert.That(rotation.x, Is.LessThan(0f), "muzzle up");
            Assert.That(camera.x, Is.LessThan(0f), "view punched up");
            Assert.That(half.z, Is.EqualTo(full.z * 0.5f).Within(1e-6f));
        }

        [Test]
        public void WrapPhase_StaysInZeroToTwoPi()
        {
            Assert.That(ViewmodelMath.WrapPhase(7f * math.PI), Is.EqualTo(math.PI).Within(1e-4f));
            Assert.That(ViewmodelMath.WrapPhase(-0.5f), Is.EqualTo(2f * math.PI - 0.5f).Within(1e-4f));
        }
    }
}
