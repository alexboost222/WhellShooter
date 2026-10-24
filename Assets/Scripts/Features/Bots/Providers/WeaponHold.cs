using Features.Bots.Constants;
using UnityEngine;

namespace Features.Bots.Providers
{
    /// <summary>
    /// Holds the weapon in both hands. Sits on the Animator's GameObject; the weapon is its sibling
    /// subtree, not a child of a hand bone, and is posed here every frame.
    /// </summary>
    /// <remarks>
    /// Mixamo clips retargeted onto another rig don't keep a rifle rigid in the right hand — between
    /// clips the hand-to-hand line swings by 15–25° in the right hand's frame — so a gun bolted to the
    /// right hand points crooked in most clips. Instead, each frame:
    /// <list type="number">
    /// <item>two-handed (rifle): the weapon is laid along the animated right-wrist → left-wrist line
    /// (what the clip means the gun to be), kept upright to the body, the right wrist on its grip;
    /// one-handed hold (pistol): the weapon rides the right hand rigidly;</item>
    /// <item>the left hand is IK'd onto its grip on the weapon.</item>
    /// </list>
    /// While the Animator's <see cref="BotAnimatorParams.Dead"/> is set it fades (~0.25 s) to the
    /// rigid right-hand hold with the left hand free, so death clips play untouched.
    /// <para>
    /// View-only: reads the Animator it sits beside, writes IK and the weapon's pose. The grip
    /// numbers are solved by <c>Game.Editor.Bots.BotPrefabBuilder</c>; needs IK Pass on the base
    /// layer.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(Animator))]
    public sealed class WeaponHold : MonoBehaviour
    {
        // Weight change per second: ~0.25 s to let go on death or grab again on respawn.
        private const float FadeSpeed = 4f;

        [SerializeField] private Transform _weapon;
        [SerializeField] private bool _twoHanded;

        [Tooltip("The weapon's pose in the right hand's frame — the rigid hold.")]
        [SerializeField] private Vector3 _rigidPosition;
        [SerializeField] private Quaternion _rigidRotation = Quaternion.identity;

        [Tooltip("Where the wrists sit on the weapon, in the weapon's frame.")]
        [SerializeField] private Vector3 _rightWrist;
        [SerializeField] private Vector3 _leftWrist;

        private Animator _animator;
        private Transform _rightHand;
        private float _weight = 1f;
        private bool _aimedValid;
        private Vector3 _aimedPosition;
        private Quaternion _aimedRotation;

        public Transform Weapon => _weapon;
        public bool TwoHanded => _twoHanded;

        /// <summary>Written by the prefab builder.</summary>
        public void Configure(Transform weapon, bool twoHanded, Vector3 rigidPosition, Quaternion rigidRotation, Vector3 rightWrist, Vector3 leftWrist)
        {
            _weapon = weapon;
            _twoHanded = twoHanded;
            _rigidPosition = rigidPosition;
            _rigidRotation = rigidRotation;
            _rightWrist = rightWrist;
            _leftWrist = leftWrist;
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_weapon == null || _animator == null)
            {
                return;
            }

            var target = _animator.GetBool(BotAnimatorParams.Dead) ? 0f : 1f;
            _weight = Mathf.MoveTowards(_weight, target, FadeSpeed * Time.deltaTime);

            // A one-handed hold leaves the left hand to the clip (on a pistol it wraps the right).
            if (!_twoHanded)
            {
                return;
            }

            // This frame's animated wrists, before any IK.
            var right = _animator.GetIKPosition(AvatarIKGoal.RightHand);
            var left = _animator.GetIKPosition(AvatarIKGoal.LeftHand);

            var along = _leftWrist - _rightWrist;
            var inWeapon = Quaternion.LookRotation(along, Vector3.ProjectOnPlane(Vector3.up, along));
            var inWorld = Quaternion.LookRotation(left - right, transform.up);

            _aimedRotation = inWorld * Quaternion.Inverse(inWeapon);
            _aimedPosition = right - _aimedRotation * _rightWrist;
            _aimedValid = true;

            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _weight);
            _animator.SetIKPosition(AvatarIKGoal.LeftHand, _aimedPosition + _aimedRotation * _leftWrist);
        }

        private void LateUpdate()
        {
            if (_weapon == null || _rightHand == null)
            {
                return;
            }

            var rigidPosition = _rightHand.TransformPoint(_rigidPosition);
            var rigidRotation = _rightHand.rotation * _rigidRotation;

            if (!_twoHanded || !_aimedValid)
            {
                _weapon.SetPositionAndRotation(rigidPosition, rigidRotation);
                return;
            }

            _weapon.SetPositionAndRotation(
                Vector3.Lerp(rigidPosition, _aimedPosition, _weight),
                Quaternion.Slerp(rigidRotation, _aimedRotation, _weight));
        }
    }
}
