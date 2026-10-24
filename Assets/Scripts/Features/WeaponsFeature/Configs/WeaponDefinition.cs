using System;
using UnityEngine;

namespace Features.WeaponsFeature.Configs
{
    /// <summary>One row of <see cref="WeaponsConfig"/>. Numbers and why: Docs/08 §Weapons.</summary>
    [Serializable]
    public sealed class WeaponDefinition
    {
        [Tooltip("Stable name — its hash is the id components store. Renaming is a content break.")]
        public string Name = "Rifle";

        [Header("Firing")]
        [Tooltip("Rounds per minute.")]
        public float RoundsPerMinute = 600f;

        [Tooltip("Holding the trigger keeps firing. Off: one shot per press.")]
        public bool Automatic = true;

        [Header("Hit")]
        [Tooltip("Damage per hit. Read by Combat's hitscan.")]
        public float Damage = 20f;

        [Tooltip("Hitscan range, metres. Read by Combat's hitscan.")]
        public float Range = 120f;

        [Header("First person")]
        [Tooltip("The model shown in the player's hands. Parented to the viewmodel socket; no colliders, no scripts.")]
        public GameObject ViewPrefab;

        public WeaponViewmodel Viewmodel = new WeaponViewmodel();

        /// <summary>Seconds between shots.</summary>
        public float ShotInterval => 60f / Mathf.Max(RoundsPerMinute, 1f);
    }
}
