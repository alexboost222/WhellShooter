using Scellecs.Morpeh;

namespace Features.Map.Components
{
    /// <summary>
    /// Request: build the arena for <see cref="Seed"/>. Consumed (read-then-remove) by
    /// <c>MapGenerateSystem</c>; if several arrive in one tick (with <c>GenerateMap</c> messages), the last one wins.
    /// </summary>
    public struct MapGenerateRequest : IComponent
    {
        public int Seed;
    }
}
