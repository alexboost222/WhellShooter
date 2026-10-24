using Features.CharacterFeature.Components;
using Features.PlayerFeature.Components;
using Features.PlayerFeature.Configs;
using Features.PlayerFeature.Systems;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.EditMode.Features.Player
{
    /// <summary>
    /// <see cref="PlayerSettings"/> as the look system sees it. The saving half
    /// (<c>PlayerSettingsSystem</c>, <c>PlayerPrefs</c>) is left out on purpose: a test must not
    /// overwrite the developer's own saved sensitivity.
    /// </summary>
    public sealed class PlayerSettingsTests
    {
        private EcsTestWorld _world;
        private PlayerSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _world = new EcsTestWorld(ScriptableObject.CreateInstance<PlayerConfig>());
            _settings = _world.AddInitializer(_world.Container.New<PlayerSettings>());
            _world.Add<PlayerLookSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void StartsAtTheConfigDefault_AndClampsChanges()
        {
            _world.Tick(0.016f);
            Assert.That(_settings.LookSensitivity, Is.EqualTo(0.08f).Within(1e-6f));

            _settings.SetLookSensitivityForTests(10f);
            Assert.That(_settings.LookSensitivity, Is.EqualTo(PlayerSettings.MaxLookSensitivity));

            _settings.SetLookSensitivityForTests(-1f);
            Assert.That(_settings.LookSensitivity, Is.EqualTo(PlayerSettings.MinLookSensitivity));
        }

        [Test]
        public void TheLook_TurnsByTheSettingsSensitivity()
        {
            _world.Tick(0.016f);
            _settings.SetLookSensitivityForTests(0.2f);

            var body = _world.World.CreateEntity();
            _world.Set(body, new Look());
            _world.Set(body, new PlayerLookInput { Delta = new float2(100f, 0f) });
            _world.Tick(0.016f);

            Assert.That(_world.Get<Look>(body).Yaw, Is.EqualTo(math.radians(20f)).Within(1e-4f));
        }
    }
}
