namespace Engine.Configs
{
    /// <summary>
    /// Turns an authored name into the integer a component stores. Components hold config ids and
    /// never references (Docs/03); this is where the id comes from.
    /// </summary>
    /// <remarks>
    /// <b>A hash of the name, not an index into the list.</b> An index changes when somebody
    /// reorders a config's array, and every save file and every entity in flight would then mean
    /// something else — a silent break with no error to notice. The name is what the author
    /// actually chose, so hashing it makes the id as stable as the name is, and renaming an entry
    /// becomes the deliberate content break it should be.
    /// <para>
    /// FNV-1a, written out rather than <c>string.GetHashCode()</c>, which is randomised per process
    /// on .NET Core and therefore cannot appear in a save file or on the wire.
    /// </para>
    /// <para>
    /// Collisions are the one hazard, and they are checked where the ids are collected — a config
    /// that finds two entries hashing alike says so at load rather than serving the wrong prefab.
    /// </para>
    /// </remarks>
    public static class ConfigIds
    {
        private const uint Offset = 2166136261u;
        private const uint Prime = 16777619u;

        /// <summary>Zero for an empty name, which is how "no id" is spelled everywhere else.</summary>
        public static int Hash(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return 0;
            }

            var hash = Offset;
            for (var i = 0; i < name.Length; i++)
            {
                hash ^= name[i];
                hash *= Prime;
            }

            return unchecked((int)hash);
        }
    }
}
