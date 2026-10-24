using Features.Bots.Constants;
using UnityEngine;

namespace Features.Bots.Providers
{
    /// <summary>
    /// Puts the support (left) hand on the weapon's handguard. The weapon rides the right hand, and
    /// a Mixamo clip only roughly agrees with itself where the left hand goes, so without this the
    /// hand floats off the gun or the gun hangs crooked between the two hands.
    /// </summary>
    /// <remarks>
    /// View-only: it reads the Animator it sits beside and writes IK, nothing else. The hand lets go
    /// while the Animator's <see cref="BotAnimatorParams.Dead"/> is set, so death clips play
    /// untouched. Needs IK Pass on the base layer (the bot controllers have it).
    /// </remarks>
    [RequireComponent(typeof(Animator))]
    public sealed class WeaponHandIK : MonoBehaviour
    {
        // Weight change per second: ~0.25 s to let go on death or grab again on respawn.
        private const float FadeSpeed = 4f;

        [SerializeField] private Transform _leftHandTarget;

        private Animator _animator;
        private float _weight = 1f;

        public Transform LeftHandTarget
        {
            get => _leftHandTarget;
            set => _leftHandTarget = value;
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_leftHandTarget == null || _animator == null)
            {
                return;
            }

            var target = _animator.GetBool(BotAnimatorParams.Dead) ? 0f : 1f;
            _weight = Mathf.MoveTowards(_weight, target, FadeSpeed * Time.deltaTime);

            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _weight);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, _weight);
            _animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandTarget.position);
            _animator.SetIKRotation(AvatarIKGoal.LeftHand, _leftHandTarget.rotation);
        }
    }
}
