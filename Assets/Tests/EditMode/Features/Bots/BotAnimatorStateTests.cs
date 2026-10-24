using Features.Bots.Constants;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode.Features.Bots
{
    /// <summary>
    /// The bot controllers' state machine, stepped by hand: each <see cref="BotAnimatorParams"/>
    /// input lands the Animator in the state the contract promises.
    /// </summary>
    [TestFixture("Assets/Content/Bots/Prefabs/Bot_Rifle.prefab")]
    [TestFixture("Assets/Content/Bots/Prefabs/Bot_Pistol.prefab")]
    public sealed class BotAnimatorStateTests
    {
        private const float Step = 0.05f;

        private readonly string _prefabPath;

        private GameObject _bot;
        private Animator _animator;

        public BotAnimatorStateTests(string prefabPath)
        {
            _prefabPath = prefabPath;
        }

        [SetUp]
        public void SetUp()
        {
            _bot = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath));
            _animator = _bot.GetComponentInChildren<Animator>();
            _animator.Rebind();
            _animator.Update(0f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_bot);
        }

        [Test]
        public void Starts_InLocomotion()
        {
            AssertState("Locomotion");
        }

        [Test]
        public void Crouch_SwitchesLocomotion_AndBack()
        {
            _animator.SetBool(BotAnimatorParams.Crouch, true);
            Advance(0.5f);
            AssertState("CrouchLocomotion");

            _animator.SetBool(BotAnimatorParams.Crouch, false);
            Advance(0.5f);
            AssertState("Locomotion");
        }

        [Test]
        public void Airborne_JumpsUp_Hangs_ThenLandsBackIntoLocomotion()
        {
            _animator.SetBool(BotAnimatorParams.Grounded, false);
            Advance(0.2f);
            AssertState("JumpUp");

            Advance(1f);
            AssertState("JumpLoop");

            _animator.SetBool(BotAnimatorParams.Grounded, true);
            Advance(0.2f);
            AssertState("JumpDown");

            Advance(1.5f);
            AssertState("Locomotion");
        }

        [Test]
        public void Dead_PlaysThePickedVariant_ForEveryVariant([NUnit.Framework.Range(0, BotAnimatorParams.DeathVariantCount - 1)] int variant)
        {
            _animator.SetInteger(BotAnimatorParams.DeathVariant, variant);
            _animator.SetBool(BotAnimatorParams.Dead, true);
            Advance(0.5f);
            AssertState("Death_" + variant);

            Advance(5f);
            AssertState("Death_" + variant);
        }

        [Test]
        public void Respawn_LeavesDeath_ForLocomotion()
        {
            _animator.SetBool(BotAnimatorParams.Dead, true);
            Advance(1f);

            _animator.SetBool(BotAnimatorParams.Dead, false);
            Advance(0.5f);
            AssertState("Locomotion");
        }

        [Test]
        public void Running_NeverMovesTheTransform()
        {
            var start = _bot.transform.position;
            _animator.SetFloat(BotAnimatorParams.MoveZ, 4.5f);
            _animator.SetFloat(BotAnimatorParams.MoveX, 2f);

            Advance(2f);

            Assert.That(Vector3.Distance(_bot.transform.position, start), Is.LessThan(1e-4f));
            Assert.That(Vector3.Distance(_animator.transform.localPosition, Vector3.zero), Is.LessThan(1e-4f));
        }

        private void Advance(float seconds)
        {
            for (var t = 0f; t < seconds; t += Step)
            {
                _animator.Update(Step);
            }
        }

        private void AssertState(string state)
        {
            var info = _animator.GetCurrentAnimatorStateInfo(0);
            Assert.That(info.IsName(state), Is.True, $"expected '{state}'");
        }
    }
}
