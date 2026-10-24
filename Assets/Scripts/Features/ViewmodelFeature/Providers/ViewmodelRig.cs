using UnityEngine;

namespace Features.ViewmodelFeature.Providers
{
    /// <summary>
    /// Binding provider on the player prefab: the first-person rig's handles. No logic.
    /// </summary>
    /// <remarks>
    /// <code>
    /// Head            (CharacterView.Head — placed by the character feature)
    ///   Kick          ← camera punch, local rotation
    ///     Camera
    ///     Socket      ← weapon pose, local position + rotation
    ///       (weapon model, built from WeaponsConfig)
    /// </code>
    /// The socket hangs off the kick, so a camera punch carries the weapon with it and the weapon's
    /// own recoil plays on top.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ViewmodelRig : MonoBehaviour
    {
        [SerializeField] private Transform kick;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform socket;

        public Transform Kick => kick;
        public Camera Camera => viewCamera;
        public Transform Socket => socket;
    }
}
