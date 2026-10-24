using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Engine.Diagnostics;

namespace Engine.DI
{
    /// <summary>
    /// The one scope. Holds an ordered list of <see cref="IResolver"/>s and fills
    /// <see cref="InjectAttribute"/> fields from them.
    /// </summary>
    /// <remarks>
    /// No hierarchy, no child scopes, nothing disposes anything (Docs/02). Everything is built
    /// at boot in the composition root, which makes startup the validation pass: an unresolved
    /// dependency throws on frame one, listing every miss at once.
    /// <para>
    /// No auto-wiring: an unregistered type is a miss, never "construct one for them".
    /// <see cref="Create(Type)"/> builds exactly one object and fills its fields — it does not
    /// recurse. That is what keeps first-match-wins comprehensible.
    /// </para>
    /// <para>
    /// Beyond its built-in <see cref="InstanceResolver"/>, the container knows nothing about
    /// where objects come from — configs, stashes and message pipes all arrive through
    /// resolvers added by the composition root.
    /// </para>
    /// </remarks>
    public sealed class Container
    {
        private readonly List<IResolver> _resolvers = new List<IResolver>();
        private readonly InstanceResolver _instances = new InstanceResolver();

        public Container()
        {
            // Always first: hand-registered singletons outrank anything computed.
            _resolvers.Add(_instances);
        }

        /// <summary>Resolvers in the order they are consulted.</summary>
        public IReadOnlyList<IResolver> Resolvers => _resolvers;

        /// <summary>Appends a resolver. Later resolvers are consulted only if earlier ones pass.</summary>
        public void AddResolver(IResolver resolver)
        {
            Guard.ThrowIfNull(resolver, nameof(resolver));
            Guard.ThrowIfNotMainThread();

            _resolvers.Add(resolver);
        }

        /// <summary>Registers an already-built instance under <typeparamref name="T"/>.</summary>
        public T Register<T>(T instance) where T : class
        {
            _instances.Register(typeof(T), instance);
            return instance;
        }

        /// <summary>Constructs <typeparamref name="T"/> and fills its [Inject] fields, or throws.</summary>
        public T New<T>() => (T)New(typeof(T));

        /// <summary>
        /// <see cref="New{T}"/> then <see cref="Register{T}"/> — the composition root's usual
        /// line: <c>container.NewAndRegister&lt;MessageBus&gt;();</c>
        /// </summary>
        public T NewAndRegister<T>() where T : class => Register(New<T>());

        /// <summary>
        /// Builds <typeparamref name="TImpl"/> but registers it under <typeparamref name="TContract"/>,
        /// so consumers inject the interface and never name the implementation.
        /// </summary>
        public TContract NewAndRegister<TContract, TImpl>() where TContract : class where TImpl : class, TContract =>
            Register<TContract>(New<TImpl>());

        /// <inheritdoc cref="New{T}"/>
        public object New(Type type)
        {
            Guard.ThrowIfNull(type, nameof(type));
            Guard.ThrowIfNotMainThread();

            object instance;
            try
            {
                // nonPublic: systems and view models may keep their constructors private.
                instance = Activator.CreateInstance(type, nonPublic: true);
            }
            catch (Exception exception)
            {
                throw new DependencyException(
                    $"Cannot construct {Pretty(type)} — it needs an accessible parameterless constructor " +
                    $"({exception.GetType().Name}: {exception.Message}).");
            }

            InjectInto(instance, type);
            return instance;
        }

        /// <summary>
        /// Fills the [Inject] fields of an object built elsewhere, and returns it so it can be
        /// used inline: <c>group.AddSystem(container.Inject(new GrowthSystem()))</c>.
        /// </summary>
        public T Inject<T>(T target) where T : class
        {
            Guard.ThrowIfNull(target, nameof(target));
            Guard.ThrowIfNotMainThread();

            InjectInto(target, target.GetType());
            return target;
        }

        /// <summary>Resolves a single dependency, or throws.</summary>
        public T Resolve<T>() => (T)Resolve(typeof(T));

        /// <inheritdoc cref="Resolve{T}"/>
        public object Resolve(Type type)
        {
            Guard.ThrowIfNull(type, nameof(type));

            if (TryResolve(type, out var value)) return value;

            throw new DependencyException(
                $"Nothing resolves {Pretty(type)}. {ResolverSummary()}", type, new[] { type });
        }

        /// <summary>Asks each resolver in order and takes the first match.</summary>
        public bool TryResolve(Type type, out object value)
        {
            Guard.ThrowIfNull(type, nameof(type));

            for (var i = 0; i < _resolvers.Count; i++)
            {
                if (!_resolvers[i].TryResolve(type, out value)) continue;

                // A resolver that claims a type owes us a usable instance. Catching this here
                // names the culprit, instead of a NullReferenceException three systems later.
                var resolver = _resolvers[i].GetType().Name;
                if (value == null)
                    throw new DependencyException($"{resolver} claimed {Pretty(type)} but produced null.");
                if (!type.IsInstanceOfType(value))
                    throw new DependencyException(
                        $"{resolver} produced {Pretty(value.GetType())} for {Pretty(type)}, which is not assignable to it.");

                return true;
            }

            value = null;
            return false;
        }

        private void InjectInto(object target, Type type)
        {
            var fields = InjectionPlan.For(type).Fields;
            if (fields.Length == 0) return;

            var values = new object[fields.Length];
            List<int> misses = null;

            for (var i = 0; i < fields.Length; i++)
            {
                if (TryResolve(fields[i].FieldType, out var value)) values[i] = value;
                else (misses ??= new List<int>()).Add(i);
            }

            if (misses != null) throw Unresolved(type, fields, misses);

            // All-or-nothing, and only after every field resolved: a caller who handed us an
            // object they already own gets it back untouched on failure, never half-wired.
            for (var i = 0; i < fields.Length; i++) fields[i].SetValue(target, values[i]);
        }

        private DependencyException Unresolved(Type type, FieldInfo[] fields, List<int> misses)
        {
            var missing = new Type[misses.Count];
            var message = new StringBuilder()
                .Append("Cannot build ").Append(Pretty(type))
                .Append(" — ").Append(misses.Count).Append(" of ").Append(fields.Length)
                .Append(" [Inject] fields unresolved:");

            for (var i = 0; i < misses.Count; i++)
            {
                var field = fields[misses[i]];
                missing[i] = field.FieldType;
                message.Append("\n  ").Append(Pretty(field.FieldType)).Append(' ').Append(field.Name);
            }

            message.Append('\n').Append(ResolverSummary());
            return new DependencyException(message.ToString(), type, missing);
        }

        private string ResolverSummary()
        {
            if (_resolvers.Count == 0) return "No resolvers are registered.";

            var summary = new StringBuilder("Resolvers, in order: ");
            for (var i = 0; i < _resolvers.Count; i++)
            {
                if (i > 0) summary.Append(", ");
                // InstanceResolver reports how much it holds; that count is usually the clue.
                summary.Append(_resolvers[i] is InstanceResolver instances
                    ? instances.ToString()
                    : _resolvers[i].GetType().Name);
            }

            return summary.ToString();
        }

        /// <summary>`Stash&lt;Health&gt;` rather than ``Stash`1[Health]`` — these strings are read under pressure.</summary>
        private static string Pretty(Type type)
        {
            if (!type.IsGenericType) return type.Name;

            var name = type.Name;
            var tick = name.IndexOf('`');
            if (tick >= 0) name = name.Substring(0, tick);

            var arguments = type.GetGenericArguments();
            var pretty = new StringBuilder(name).Append('<');
            for (var i = 0; i < arguments.Length; i++)
            {
                if (i > 0) pretty.Append(", ");
                pretty.Append(Pretty(arguments[i]));
            }

            return pretty.Append('>').ToString();
        }
    }
}
