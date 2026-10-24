using System;

namespace Engine.DI
{
    /// <summary>One registration in the container, flattened for a reader.</summary>
    /// <remarks>
    /// <b><see cref="Description"/> is a string captured at collect time, not the object.</b> See
    /// <see cref="InstanceResolver.CollectContracts"/> — a reader that held the instance would hold a
    /// service attached to a world the next play session disposed, and would answer questions about
    /// it perfectly confidently.
    /// </remarks>
    public readonly struct ContractBinding
    {
        public ContractBinding(Type contract, Type implementation, string description)
        {
            Contract = contract;
            Implementation = implementation;
            Description = description;
        }

        /// <summary>What an <c>[Inject]</c> field asks for.</summary>
        public Type Contract { get; }

        /// <summary>What it actually gets. The same as <see cref="Contract"/> for most registrations.</summary>
        public Type Implementation { get; }

        /// <summary>The instance's <c>ToString</c> at the moment it was collected — often a count.</summary>
        public string Description { get; }

        public override string ToString() =>
            Contract == Implementation
                ? $"{Contract.Name} → {Description}"
                : $"{Contract.Name} → {Implementation.Name}: {Description}";
    }
}
