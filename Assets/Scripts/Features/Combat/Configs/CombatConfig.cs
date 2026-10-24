using Engine.Configs;
using UnityEngine;

namespace Features.Combat.Configs
{
    /// <summary>Health and damage rules (Docs/08 §Combat).</summary>
    [CreateAssetMenu(menuName = "WheelShooter/Configs/Combat", fileName = nameof(CombatConfig))]
    public sealed class CombatConfig : Config
    {
        private const float DefaultMaxHealth = 200f;

        [Tooltip("Hit points every body starts and revives with.")]
        [SerializeField] private float _maxHealth = DefaultMaxHealth;

        [Tooltip("Whether a shot hurts a team-mate. Free-for-all bodies are always fair game.")]
        [SerializeField] private bool _friendlyFire;

        public float MaxHealth => _maxHealth > 0f ? _maxHealth : DefaultMaxHealth;

        public bool FriendlyFire => _friendlyFire;

        /// <summary>For tests and tooling: a config built in code rather than loaded.</summary>
        internal void Set(float maxHealth, bool friendlyFire)
        {
            _maxHealth = maxHealth;
            _friendlyFire = friendlyFire;
        }
    }
}
