using UnityEngine;

namespace Features.CharacterFeature.Providers
{
    /// <summary>
    /// Binding provider on a body prefab's root: the hierarchy handles the character systems need.
    /// No logic.
    /// </summary>
    /// <remarks>
    /// No collider: the body is a phys-r capsule (Docs/11). The root and the <see cref="Head"/> are
    /// placed every frame from the interpolated position and <c>Look</c>, in world space.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CharacterView : MonoBehaviour
    {
        [Tooltip("Placed at eye height and rotated by Look every frame. The player's camera lives under it.")]
        [SerializeField] private Transform head;

        public Transform Head => head;
    }
}
