using UnityEngine;

namespace Features.Bots.Providers
{
    /// <summary>
    /// On the bot view prefab's root, beside the body's <c>CharacterView</c>: the Animator the
    /// animation contract drives. A view only — <c>BotAnimatorSystem</c> writes it, nothing reads
    /// gameplay back from it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BotView : MonoBehaviour
    {
        [Tooltip("The nested visual (the Bot_Rifle instance). Placed every frame at the interpolated feet, " +
                 "turned by the look's yaw — the body itself moves on the fixed tick and never turns.")]
        [SerializeField] private Transform _model;

        [Tooltip("The humanoid model's Animator (Bot_Rifle → Model). Found in children when left empty.")]
        [SerializeField] private Animator _animator;

        /// <summary>Whether the Animator was last told the bot is dead — the view system rolls a death clip on the change.</summary>
        public bool WasDead { get; set; }

        public Transform Model => _model;

        public Animator Animator
        {
            get
            {
                if (_animator == null)
                {
                    _animator = GetComponentInChildren<Animator>();
                }

                return _animator;
            }
        }
    }
}
