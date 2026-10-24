using System;

namespace Engine.DI
{
    /// <summary>
    /// One source of dependencies. The container consults its resolvers **in registration
    /// order and takes the first match**, so ordering is the priority mechanism.
    /// </summary>
    /// <remarks>
    /// Deliberately the whole contract: a resolver decides for itself whether it recognises a
    /// type — a hand-registered singleton, a `Stash&lt;T&gt;` off the world, a config loaded
    /// from Resources, a lazily-created message pipe. The container never knows which.
    /// </remarks>
    public interface IResolver
    {
        /// <summary>
        /// Produces an instance assignable to <paramref name="type"/>, or returns false to pass.
        /// Returning true with a null or mismatched value is a bug and the container says so.
        /// </summary>
        bool TryResolve(Type type, out object value);
    }
}
