using Features.ViewmodelFeature.Providers;
using Scellecs.Morpeh;
using UnityEngine;

namespace Features.ViewmodelFeature.Components
{
    /// <summary>
    /// Local-only link to the first-person rig and the weapon model built into it. Present means
    /// both are live (Docs/03) — <c>ViewmodelSweepSystem</c> keeps that true.
    /// </summary>
    public struct ViewmodelLink : IComponent
    {
        public ViewmodelRig Rig;

        /// <summary>The model under <see cref="ViewmodelRig.Socket"/>.</summary>
        public GameObject Model;

        /// <summary>The weapon the model was built for; a different <c>Weapon.ConfigId</c> rebuilds it.</summary>
        public int WeaponId;
    }
}
