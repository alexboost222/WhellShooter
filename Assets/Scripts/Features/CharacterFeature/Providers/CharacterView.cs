using UnityEngine;

namespace Features.CharacterFeature.Providers
{
    /// <summary>
    /// Binding provider on a body prefab's root: the hierarchy handles the character systems need.
    /// No logic.
    /// </summary>
    /// <remarks>
    /// The root carries the <see cref="CharacterController"/>, which the motor moves on the fixed
    /// tick. The <see cref="Head"/> is placed every frame from the interpolated position and
    /// <c>Look</c> — in world space, so it never inherits the root's tick-rate stepping.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CharacterView : MonoBehaviour
    {
        [Tooltip("The capsule the motor moves. On this object.")]
        [SerializeField] private CharacterController controller;

        [Tooltip("Placed at eye height and rotated by Look every frame. The player's camera lives under it.")]
        [SerializeField] private Transform head;

        public CharacterController Controller => controller;
        public Transform Head => head;
    }
}
