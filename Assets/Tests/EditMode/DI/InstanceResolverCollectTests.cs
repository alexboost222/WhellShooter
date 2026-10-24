using System.Collections.Generic;
using Engine.DI;
using NUnit.Framework;

namespace Engine.Tests.EditMode.DI
{
    /// <summary>
    /// <see cref="InstanceResolver.CollectContracts"/> — what the Container window lists.
    /// </summary>
    /// <remarks>
    /// The property worth asserting is that it hands out descriptions rather than the objects. A
    /// window that held live services across a play session would hold services belonging to a
    /// container the next one replaced, and would answer questions about a disposed world quite
    /// confidently — which is the failure the whole design of this method is arranged to make
    /// impossible rather than merely unlikely.
    /// </remarks>
    [TestFixture]
    public sealed class InstanceResolverCollectTests
    {
        private InstanceResolver _resolver;
        private List<ContractBinding> _bindings;

        [SetUp]
        public void SetUp()
        {
            _resolver = new InstanceResolver();
            _bindings = new List<ContractBinding>();
        }

        [Test]
        public void EveryRegistrationIsListed()
        {
            _resolver.Register(typeof(Service), new Service());
            _resolver.Register(typeof(IContract), new Implementation());

            _resolver.CollectContracts(_bindings);

            Assert.That(_bindings, Has.Count.EqualTo(2));
        }

        [Test]
        public void TheContractAndTheImplementationAreBothReported()
        {
            _resolver.Register(typeof(IContract), new Implementation());
            _resolver.CollectContracts(_bindings);

            Assert.That(_bindings[0].Contract, Is.EqualTo(typeof(IContract)));
            Assert.That(_bindings[0].Implementation, Is.EqualTo(typeof(Implementation)),
                "'who satisfies this' is a different question from 'what was registered'");
        }

        /// <summary>The whole point: no live reference escapes.</summary>
        [Test]
        public void OnlyADescriptionComesOut()
        {
            _resolver.Register(typeof(Service), new Service());
            _resolver.CollectContracts(_bindings);

            Assert.That(_bindings[0].Description, Is.EqualTo("a service"));
        }

        [Test]
        public void BindingsAreSortedByContractName()
        {
            _resolver.Register(typeof(Service), new Service());
            _resolver.Register(typeof(IContract), new Implementation());

            _resolver.CollectContracts(_bindings);

            Assert.That(_bindings[0].Contract.Name, Is.EqualTo(nameof(IContract)));
            Assert.That(_bindings[1].Contract.Name, Is.EqualTo(nameof(Service)));
        }

        [Test]
        public void TheCallersListIsCleared()
        {
            _bindings.Add(new ContractBinding(typeof(Service), typeof(Service), "stale"));
            _resolver.CollectContracts(_bindings);

            Assert.That(_bindings, Is.Empty);
        }

        private sealed class Service
        {
            public override string ToString() => "a service";
        }

        private interface IContract
        {
        }

        private sealed class Implementation : IContract
        {
        }
    }
}
