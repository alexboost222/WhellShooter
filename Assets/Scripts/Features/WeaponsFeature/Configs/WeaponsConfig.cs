using System;
using System.Collections.Generic;
using Engine.Configs;
using UnityEngine;

namespace Features.WeaponsFeature.Configs
{
    /// <summary>
    /// Every weapon, one row each: how it fires, what it does when it hits, and how it looks and
    /// kicks in first person. One asset, in <c>Resources/Configs</c>. Components hold
    /// <see cref="ConfigIds.Hash"/> of a row's name, never the row (Docs/03).
    /// </summary>
    [CreateAssetMenu(menuName = "WheelShooter/Weapons Config", fileName = "WeaponsConfig")]
    public sealed class WeaponsConfig : Config
    {
        [Tooltip("The weapon a body is given when it has none. A row name from the list below.")]
        [SerializeField] private string defaultWeapon = "Rifle";

        [SerializeField] private WeaponDefinition[] weapons = Array.Empty<WeaponDefinition>();

        private Dictionary<int, WeaponDefinition> _byId;

        public int DefaultWeaponId => ConfigIds.Hash(defaultWeapon);

        public IReadOnlyList<WeaponDefinition> Weapons => weapons;

        public override IEnumerable<ConfigEntry> Entries
        {
            get
            {
                for (var i = 0; i < weapons.Length; i++)
                {
                    yield return new ConfigEntry(i, weapons[i]?.Name, weapons[i]);
                }
            }
        }

        public bool TryGet(int id, out WeaponDefinition weapon)
        {
            if (_byId == null)
            {
                Index();
            }

            return _byId.TryGetValue(id, out weapon);
        }

        private void OnValidate()
        {
            // Edited in the inspector while playing: rebuild on next lookup.
            _byId = null;
        }

        private void Index()
        {
            _byId = new Dictionary<int, WeaponDefinition>();

            for (var i = 0; i < weapons.Length; i++)
            {
                var weapon = weapons[i];
                if (weapon == null || string.IsNullOrEmpty(weapon.Name))
                {
                    continue;
                }

                var id = ConfigIds.Hash(weapon.Name);
                if (_byId.TryGetValue(id, out var existing))
                {
                    throw new InvalidOperationException(
                        $"{name}: weapons '{existing.Name}' and '{weapon.Name}' hash to the same id. Rename one.");
                }

                _byId.Add(id, weapon);
            }
        }

        /// <summary>For tests and tooling: replaces the table.</summary>
        internal void SetWeapons(string defaultName, params WeaponDefinition[] rows)
        {
            defaultWeapon = defaultName;
            weapons = rows;
            _byId = null;
        }
    }
}
