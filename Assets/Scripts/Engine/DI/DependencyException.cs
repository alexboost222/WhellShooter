using System;
using System.Collections.Generic;

namespace Engine.DI
{
    /// <summary>
    /// Thrown when the container cannot build or fill an object. Carries <em>every</em>
    /// unresolved dependency, not just the first — boot is the validation pass, so one run
    /// should tell you everything that's missing.
    /// </summary>
    public sealed class DependencyException : Exception
    {
        public DependencyException(string message) : base(message)
        {
            Missing = Array.Empty<Type>();
        }

        public DependencyException(string message, Type targetType, IReadOnlyList<Type> missing) : base(message)
        {
            TargetType = targetType;
            Missing = missing ?? (IReadOnlyList<Type>)Array.Empty<Type>();
        }

        /// <summary>The type being created or injected, when there was one.</summary>
        public Type TargetType { get; }

        /// <summary>Field types nothing resolved. Empty for failures that aren't about missing dependencies.</summary>
        public IReadOnlyList<Type> Missing { get; }
    }
}
