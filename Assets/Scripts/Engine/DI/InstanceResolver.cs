using System;
using System.Collections.Generic;
using Engine.Diagnostics;

namespace Engine.DI
{
    /// <summary>
    /// Hand-registered singletons, keyed by the contract they were registered under —
    /// MessageBus, UiRouter, TimeService, the Morpeh World.
    /// </summary>
    /// <remarks>
    /// Lives in Engine/DI rather than with the other resolvers because it has no dependencies
    /// of its own and because <see cref="Container.Register{T}"/> and
    /// <see cref="Container.NewAndRegister{T}"/> need somewhere to put things. Every container
    /// owns one as its first resolver.
    /// </remarks>
    public sealed class InstanceResolver : IResolver
    {
        private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();

        public int Count => _instances.Count;

        /// <summary>
        /// Registers <paramref name="instance"/> under <paramref name="contract"/>. Registering
        /// the same contract twice throws — there is one scope and one composition root, so a
        /// second registration is a bug rather than an override.
        /// </summary>
        public void Register(Type contract, object instance)
        {
            Guard.ThrowIfNull(contract, nameof(contract));
            Guard.ThrowIfNull(instance, nameof(instance));
            Guard.ThrowIfNotMainThread();

            if (!contract.IsInstanceOfType(instance))
                throw new DependencyException(
                    $"{instance.GetType().Name} is not assignable to {contract.Name}.");

            if (_instances.ContainsKey(contract))
                throw new DependencyException(
                    $"{contract.Name} is already registered. One scope, one registration — " +
                    "if this is intentional, the composition root is doing something it should not.");

            _instances.Add(contract, instance);
        }

        public bool TryResolve(Type type, out object value) => _instances.TryGetValue(type, out value);

        /// <summary>
        /// What is registered, as descriptions, into the caller's list — which is cleared first.
        /// </summary>
        /// <remarks>
        /// <b>Descriptions and not the instances, deliberately.</b> A window that held the live
        /// services would be holding objects belonging to a container the next play session
        /// replaced — the exact failure <c>RuntimeWindow</c>'s reference check exists to prevent,
        /// made structurally impossible here instead of guarded against. The cost is a
        /// <c>ToString</c> per binding at the moment somebody asks, which is not a cost.
        /// <para>
        /// <b>Contracts, not implementations, are what the container is keyed by</b> — so this
        /// reports both, since "who satisfies <c>ISceneLoader</c>" is a different question from
        /// "what did the composition root register" and only the pair answers either.
        /// </para>
        /// </remarks>
        public void CollectContracts(List<ContractBinding> into)
        {
            Guard.ThrowIfNull(into, nameof(into));

            into.Clear();

            foreach (var pair in _instances)
            {
                into.Add(new ContractBinding(pair.Key, pair.Value.GetType(), pair.Value.ToString()));
            }

            into.Sort(static (a, b) => string.CompareOrdinal(a.Contract.Name, b.Contract.Name));
        }

        public override string ToString() => $"InstanceResolver ({_instances.Count} registered)";
    }
}
