using System;
using System.Collections.Generic;
using UnityEngine;

namespace Engine.Configs
{
    /// <summary>
    /// One row of a config that holds a table: its authored name, the id that name hashes to, and
    /// the definition object itself.
    /// </summary>
    /// <remarks>
    /// <b><see cref="Value"/> is deliberately <c>object</c>.</b> Every consumer of it is doing
    /// reflection anyway — the drawer looking for <c>[ConfigRef]</c> fields, the validator walking
    /// them — and a generic <c>ConfigEntry&lt;T&gt;</c> would make <see cref="Config.Entries"/>
    /// impossible to declare on the base class, which is the whole point of it.
    /// </remarks>
    public readonly struct ConfigEntry
    {
        public ConfigEntry(int index, string name, object value)
        {
            Index = index;
            Name = name;
            Id = ConfigIds.Hash(name);
            Value = value;
        }

        /// <summary>Position in the config's array — what an inspector needs to point at the row.</summary>
        public int Index { get; }

        /// <summary>
        /// The authored stable name. May be null or empty: a half-finished row is a finding for the
        /// validator, not a reason for enumeration to throw.
        /// </summary>
        public string Name { get; }

        /// <summary>What <see cref="ConfigIds.Hash"/> makes of <see cref="Name"/>. Zero for an empty name.</summary>
        public int Id { get; }

        /// <summary>The definition object.</summary>
        public object Value { get; }

        public override string ToString() => string.IsNullOrEmpty(Name) ? $"[{Index}] (unnamed)" : Name;
    }

    /// <summary>
    /// Base for every config ScriptableObject. Subclass it, add
    /// <c>[CreateAssetMenu]</c>, and author exactly one asset per subclass in
    /// <c>Assets/Resources/Configs/</c>.
    /// </summary>
    /// <remarks>
    /// Nearly empty — it exists so <see cref="ConfigCatalog"/> can find configs by type
    /// without knowing any of them, and so `Resources/Configs` has a single admission rule.
    /// <para>
    /// <b>One asset per type.</b> A config that describes many things (every weapon, every
    /// archetype) is one asset holding a list, not many assets. Entities then store the id of
    /// an entry, never a reference to the asset (Docs/03).
    /// </para>
    /// </remarks>
    public abstract class Config : ScriptableObject
    {
        /// <summary>
        /// The rows this config holds, for tooling and validation. Empty for a config that is a bag
        /// of numbers rather than a table.
        /// </summary>
        /// <remarks>
        /// <b>The config's own knowledge, and therefore the config's own method.</b> The alternative
        /// — an editor-side helper reflecting over serialized arrays — would have to <i>guess</i>
        /// which array is the table and which field is the name, a heuristic that breaks silently the
        /// first time a config grows a second array. Silent is precisely the failure mode the whole
        /// id scheme exists to prevent.
        /// <para>
        /// <b>Three consumers want this list and only one is an editor:</b> the <c>[ConfigRef]</c>
        /// drawer at authoring time, the project validator at authoring and build time, and the
        /// entity inspector's id-to-name map while the game is running. Putting it in the editor
        /// would strand the third.
        /// </para>
        /// <para>
        /// Every list config already builds a dictionary from exactly this array, so an override is
        /// a <c>yield</c> over the same loop.
        /// </para>
        /// </remarks>
        public virtual IEnumerable<ConfigEntry> Entries => Array.Empty<ConfigEntry>();

        /// <summary>
        /// Appends a row with this name, and says whether it could. False by default: a config whose
        /// rows need more than a name to be meaningful cannot be created from one.
        /// </summary>
        /// <remarks>
        /// <b>The config's own method, for the same reason <see cref="Entries"/> is.</b> Tooling that
        /// offers "add the entry you just named" has to know which array is the table and which field
        /// is the name, and a helper reflecting over serialized arrays would have to <i>guess</i> —
        /// the guess that breaks silently the first time a config grows a second array. Here the one
        /// object that knows answers, or declines.
        /// <para>
        /// <b>Declining is the honest default and most configs should keep it.</b> A level needs a
        /// scene, a recipe needs both sides; a row conjured from a name alone would be a half-authored
        /// entry that validates as real. Only a config whose rows genuinely <i>are</i> a name — the
        /// vocabulary kind — should override this.
        /// </para>
        /// <para>
        /// <b>It does not save the asset.</b> Marking dirty and writing belong to the editor code
        /// that called this; a runtime type has no business doing either, and the caller usually
        /// wants to batch anyway.
        /// </para>
        /// </remarks>
        public virtual bool TryAddEntry(string name)
        {
            return false;
        }

        /// <summary>
        /// Whether <see cref="TryAddEntry"/> is ever worth calling. Tooling asks this to decide
        /// whether to show the affordance at all, rather than offering a button that declines.
        /// </summary>
        public virtual bool CanAddEntries => false;
    }
}
