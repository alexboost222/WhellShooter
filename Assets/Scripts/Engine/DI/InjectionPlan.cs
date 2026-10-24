using System;
using System.Collections.Generic;
using System.Reflection;

namespace Engine.DI
{
    /// <summary>
    /// The cached list of <see cref="InjectAttribute"/> fields for one type. Reflection runs
    /// once per type for the life of the domain; injection itself is then just a loop.
    /// </summary>
    /// <remarks>
    /// The cache is an unsynchronised dictionary — the container is main-thread only and
    /// asserts it, so there is nothing to race with.
    /// </remarks>
    internal sealed class InjectionPlan
    {
        private const BindingFlags DeclaredInstance =
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const BindingFlags DeclaredStatic =
            BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<Type, InjectionPlan> Cache = new Dictionary<Type, InjectionPlan>();

        internal readonly FieldInfo[] Fields;

        private InjectionPlan(FieldInfo[] fields) => Fields = fields;

        internal static InjectionPlan For(Type type)
        {
            if (Cache.TryGetValue(type, out var cached)) return cached;

            var plan = new InjectionPlan(Collect(type));
            Cache.Add(type, plan);
            return plan;
        }

        private static FieldInfo[] Collect(Type type)
        {
            var fields = new List<FieldInfo>();

            // Walk the hierarchy by hand. GetFields on a derived type does NOT return private
            // fields declared on its bases, and a base field left silently null is exactly the
            // failure injection is supposed to make impossible.
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                var declared = new List<FieldInfo>();

                foreach (var field in current.GetFields(DeclaredInstance))
                {
                    if (!field.IsDefined(typeof(InjectAttribute), false)) continue;

                    if (field.IsInitOnly)
                        throw new DependencyException(
                            $"{Describe(field)} is readonly — an [Inject] field has to be assignable.");

                    declared.Add(field);
                }

                // Caught explicitly: BindingFlags.Instance would just skip these, and a field
                // that silently never gets injected is worse than a loud error.
                foreach (var field in current.GetFields(DeclaredStatic))
                {
                    if (field.IsDefined(typeof(InjectAttribute), false))
                        throw new DependencyException(
                            $"{Describe(field)} is static — [Inject] works on instance fields only.");
                }

                // Base-first, so a failure list reads from the root of the hierarchy down.
                fields.InsertRange(0, declared);
            }

            return fields.Count == 0 ? Array.Empty<FieldInfo>() : fields.ToArray();
        }

        private static string Describe(FieldInfo field) => $"{field.DeclaringType?.Name}.{field.Name}";
    }
}
