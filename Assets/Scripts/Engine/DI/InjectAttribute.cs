using System;

namespace Engine.DI
{
    /// <summary>
    /// Marks a field for injection by <see cref="Container"/>.
    /// </summary>
    /// <remarks>
    /// Fields, not constructor parameters: a system injects half a dozen stashes plus configs
    /// and queues, and a constructor parameter wall is unreadable (Docs/02). Works on private
    /// fields, including ones declared on a base class.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class InjectAttribute : Attribute
    {
    }
}
