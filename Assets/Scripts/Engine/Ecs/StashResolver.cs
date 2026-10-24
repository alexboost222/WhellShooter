using System;
using System.Collections.Generic;
using System.Reflection;
using Engine.DI;
using Engine.Diagnostics;
using Scellecs.Morpeh;

namespace Engine.Ecs
{
    /// <summary>
    /// Resolves <c>Stash&lt;T&gt;</c> to <c>world.GetStash&lt;T&gt;()</c>, so a system's first
    /// few lines are just the stashes it works with (Docs/03).
    /// </summary>
    /// <remarks>
    /// Against Morpeh 2024.1.1: <c>WorldExtensions.GetStash&lt;T&gt;(this World)</c>, constrained
    /// to <c>struct, IComponent</c>. Found by scanning rather than <c>GetMethod(name)</c>
    /// because <c>WorldExtensions</c> carries several overloads of that name.
    /// </remarks>
    public sealed class StashResolver : IResolver
    {
        private static readonly MethodInfo StashFactory = FindStashFactory();

        private readonly Dictionary<Type, object> _stashes = new Dictionary<Type, object>();
        private readonly object[] _arguments;

        public StashResolver(World world)
        {
            Guard.ThrowIfNull(world, nameof(world));

            _arguments = new object[] { world };
        }

        private static MethodInfo FindStashFactory()
        {
            foreach (var candidate in typeof(WorldExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static))
                if (candidate.Name == nameof(WorldExtensions.GetStash) &&
                    candidate.IsGenericMethodDefinition &&
                    candidate.GetParameters().Length == 1)
                    return candidate;

            throw new InvalidOperationException(
                "Morpeh's WorldExtensions.GetStash<T>(World) was not found — the package API has changed.");
        }

        public bool TryResolve(Type type, out object value)
        {
            if (_stashes.TryGetValue(type, out value)) return true;

            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Stash<>))
            {
                value = null;
                return false;
            }

            var component = type.GetGenericArguments()[0];
            try
            {
                value = StashFactory.MakeGenericMethod(component).Invoke(null, _arguments);
            }
            catch (ArgumentException exception)
            {
                throw new DependencyException(
                    $"Cannot get a Stash<{component.Name}> — {component.Name} must be a struct implementing " +
                    $"IComponent. ({exception.Message})");
            }

            _stashes.Add(type, value);
            return true;
        }

        public override string ToString() => $"StashResolver ({_stashes.Count} stashes)";
    }
}
