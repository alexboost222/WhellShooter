using System;
using System.Collections.Generic;
using Engine.DI;
using NUnit.Framework;

namespace Engine.Tests.EditMode.DI
{
    [TestFixture]
    public sealed class ContainerTests
    {
        private Container _container;
        private StubResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _container = new Container();
            _resolver = new StubResolver();
            _container.AddResolver(_resolver);
        }

        // --- construction + injection --------------------------------------------------

        [Test]
        public void New_ConstructsAndFillsInjectFields()
        {
            var service = new Service();
            _resolver.Add(service);

            var target = _container.New<NeedsService>();

            Assert.That(target, Is.Not.Null);
            Assert.That(target.Service, Is.SameAs(service));
        }

        [Test]
        public void New_LeavesFieldsWithoutTheAttributeAlone()
        {
            _resolver.Add(new Service());

            var target = _container.New<NeedsService>();

            Assert.That(target.NotInjected, Is.Null);
        }

        [Test]
        public void New_WorksForTypesWithNoInjectFields()
        {
            Assert.DoesNotThrow(() => _container.New<Service>());
        }

        [Test]
        public void New_UsesAPrivateConstructor()
        {
            _resolver.Add(new Service());

            var target = _container.New<PrivateConstructor>();

            Assert.That(target.Service, Is.Not.Null);
        }

        [Test]
        public void New_FillsPrivateFieldsDeclaredOnABaseClass()
        {
            var service = new Service();
            _resolver.Add(service);
            _resolver.Add(new OtherService());

            var target = _container.New<Derived>();

            Assert.That(target.BaseService, Is.SameAs(service), "a private [Inject] field on the base must be filled");
            Assert.That(target.Other, Is.Not.Null);
        }

        [Test]
        public void Inject_FillsAnObjectBuiltElsewhereAndReturnsIt()
        {
            var service = new Service();
            _resolver.Add(service);
            var target = new NeedsService();

            var returned = _container.Inject(target);

            Assert.That(returned, Is.SameAs(target));
            Assert.That(target.Service, Is.SameAs(service));
        }

        // --- resolver chain ------------------------------------------------------------

        [Test]
        public void Resolvers_AreConsultedInOrderAndFirstMatchWins()
        {
            var second = new StubResolver();
            var winner = new Service();
            var loser = new Service();
            _resolver.Add(winner);
            second.Add(loser);
            _container.AddResolver(second);

            Assert.That(_container.Resolve<Service>(), Is.SameAs(winner));
        }

        [Test]
        public void Resolvers_FallThroughToLaterOnesWhenEarlierOnesPass()
        {
            var second = new StubResolver();
            var other = new OtherService();
            second.Add(other);
            _container.AddResolver(second);

            Assert.That(_container.Resolve<OtherService>(), Is.SameAs(other));
        }

        [Test]
        public void TryResolve_ReturnsFalseWhenNothingMatches()
        {
            Assert.That(_container.TryResolve(typeof(Service), out var value), Is.False);
            Assert.That(value, Is.Null);
        }

        [Test]
        public void Resolve_ThrowsWhenNothingMatches()
        {
            var exception = Assert.Throws<DependencyException>(() => _container.Resolve<Service>());

            Assert.That(exception.Message, Does.Contain("Service"));
        }

        [Test]
        public void Resolve_FindsAnInstanceRegisteredUnderAnInterface()
        {
            var service = new Service();
            _resolver.Add<IService>(service);

            Assert.That(_container.Resolve<IService>(), Is.SameAs(service));
        }

        // --- fail fast -----------------------------------------------------------------

        [Test]
        public void New_ReportsEveryUnresolvedFieldAtOnce_NotJustTheFirst()
        {
            // Only one of the three dependencies is available.
            _resolver.Add(new Service());

            var exception = Assert.Throws<DependencyException>(() => _container.New<NeedsThree>());

            Assert.That(exception.Missing, Is.EquivalentTo(new[] { typeof(OtherService), typeof(IService) }));
            Assert.That(exception.TargetType, Is.EqualTo(typeof(NeedsThree)));
            Assert.That(exception.Message, Does.Contain("2 of 3"));
            Assert.That(exception.Message, Does.Contain("OtherService"));
            Assert.That(exception.Message, Does.Contain("IService"));
        }

        [Test]
        public void New_NamesTheResolversItAsked()
        {
            var exception = Assert.Throws<DependencyException>(() => _container.New<NeedsService>());

            Assert.That(exception.Message, Does.Contain("StubResolver"));
        }

        [Test]
        public void New_DoesNotAutoWireUnregisteredTypes()
        {
            // Service is perfectly constructible; the container still must not invent it.
            Assert.Throws<DependencyException>(() => _container.New<NeedsService>());
        }

        [Test]
        public void Inject_LeavesTheTargetUntouchedWhenAnythingIsMissing()
        {
            _resolver.Add(new Service());
            var target = new NeedsThree();

            Assert.Throws<DependencyException>(() => _container.Inject(target));

            Assert.That(target.Service, Is.Null, "injection is all-or-nothing, never half-wired");
        }

        [Test]
        public void New_ThrowsAReadableErrorForATypeWithNoParameterlessConstructor()
        {
            var exception = Assert.Throws<DependencyException>(() => _container.New<NeedsConstructorArgs>());

            Assert.That(exception.Message, Does.Contain("parameterless constructor"));
        }

        [Test]
        public void New_ThrowsForAnAbstractType()
        {
            Assert.Throws<DependencyException>(() => _container.New(typeof(AbstractThing)));
        }

        // --- resolver contract violations ----------------------------------------------

        [Test]
        public void Resolver_ClaimingATypeButProducingNull_IsCalledOut()
        {
            _container = new Container();
            _container.AddResolver(new NullProducingResolver());

            var exception = Assert.Throws<DependencyException>(() => _container.Resolve<Service>());

            Assert.That(exception.Message, Does.Contain("null"));
            Assert.That(exception.Message, Does.Contain("NullProducingResolver"));
        }

        [Test]
        public void Resolver_ProducingTheWrongType_IsCalledOut()
        {
            _container = new Container();
            _container.AddResolver(new WrongTypeResolver());

            var exception = Assert.Throws<DependencyException>(() => _container.Resolve<Service>());

            Assert.That(exception.Message, Does.Contain("not assignable"));
        }

        // --- attribute misuse ------------------------------------------------------------

        [Test]
        public void InjectOnAStaticField_Throws()
        {
            var exception = Assert.Throws<DependencyException>(() => _container.New<HasStaticInject>());

            Assert.That(exception.Message, Does.Contain("static"));
        }

        [Test]
        public void InjectOnAReadonlyField_Throws()
        {
            var exception = Assert.Throws<DependencyException>(() => _container.New<HasReadonlyInject>());

            Assert.That(exception.Message, Does.Contain("readonly"));
        }

        // --- registration ------------------------------------------------------------------

        [Test]
        public void Register_MakesAnInstanceInjectable()
        {
            var service = new Service();

            var returned = _container.Register(service);

            Assert.That(returned, Is.SameAs(service));
            Assert.That(_container.Resolve<Service>(), Is.SameAs(service));
            Assert.That(_container.New<NeedsService>().Service, Is.SameAs(service));
        }

        [Test]
        public void Register_IsConsultedBeforeAnyAddedResolver()
        {
            var registered = new Service();
            _resolver.Add(new Service());
            _container.Register(registered);

            Assert.That(_container.Resolve<Service>(), Is.SameAs(registered));
        }

        [Test]
        public void Register_ThrowsOnDuplicateContract()
        {
            _container.Register(new Service());

            var exception = Assert.Throws<DependencyException>(() => _container.Register(new Service()));

            Assert.That(exception.Message, Does.Contain("already registered"));
        }

        [Test]
        public void Register_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => _container.Register<Service>(null));
        }

        [Test]
        public void NewAndRegister_BuildsInjectsAndRegistersInOneStep()
        {
            _container.Register(new Service());

            var built = _container.NewAndRegister<NeedsService>();

            Assert.That(built.Service, Is.Not.Null, "it must be injected, not just constructed");
            Assert.That(_container.Resolve<NeedsService>(), Is.SameAs(built));
        }

        [Test]
        public void NewAndRegister_WithAContract_RegistersUnderTheContractOnly()
        {
            var built = _container.NewAndRegister<IService, Service>();

            Assert.That(_container.Resolve<IService>(), Is.SameAs(built));
            Assert.That(_container.TryResolve(typeof(Service), out _), Is.False,
                "registering under a contract must not also register the implementation");
        }

        // --- guards ------------------------------------------------------------------------

        [Test]
        public void AddResolver_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => _container.AddResolver(null));
        }

        [Test]
        public void Inject_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => _container.Inject<NeedsService>(null));
        }

        [Test]
        public void GenericNew_ReturnsTheRequestedType()
        {
            _resolver.Add(new Service());

            NeedsService target = _container.New<NeedsService>();

            Assert.That(target, Is.InstanceOf<NeedsService>());
        }

        // --- fixtures ----------------------------------------------------------------------

        // These fields are assigned by reflection, which the compiler cannot see.
#pragma warning disable 0649

        private interface IService
        {
        }

        private sealed class Service : IService
        {
        }

        private sealed class OtherService
        {
        }

        private sealed class NeedsService
        {
            [Inject] private Service _service;
            private Service _notInjected;

            public Service Service => _service;
            public Service NotInjected => _notInjected;
        }

        private sealed class NeedsThree
        {
            [Inject] private Service _service;
            [Inject] private OtherService _other;
            [Inject] private IService _contract;

            public Service Service => _service;
            public OtherService Other => _other;
            public IService Contract => _contract;
        }

        private sealed class PrivateConstructor
        {
            [Inject] private Service _service;

            private PrivateConstructor()
            {
            }

            public Service Service => _service;
        }

        private abstract class BaseWithInject
        {
            [Inject] private Service _service;

            public Service BaseService => _service;
        }

        private sealed class Derived : BaseWithInject
        {
            [Inject] private OtherService _other;

            public OtherService Other => _other;
        }

        private abstract class AbstractThing
        {
        }

        private sealed class NeedsConstructorArgs
        {
            public NeedsConstructorArgs(int required)
            {
                _ = required;
            }
        }

        private sealed class HasStaticInject
        {
            [Inject] private static Service _service;

            public static Service Service => _service;
        }

        private sealed class HasReadonlyInject
        {
            [Inject] private readonly Service _service;

            public Service Service => _service;
        }

        private sealed class StubResolver : IResolver
        {
            private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();

            public void Add<T>(T instance) => _instances[typeof(T)] = instance;

            public bool TryResolve(Type type, out object value) => _instances.TryGetValue(type, out value);
        }

        private sealed class NullProducingResolver : IResolver
        {
            public bool TryResolve(Type type, out object value)
            {
                value = null;
                return true;
            }
        }

        private sealed class WrongTypeResolver : IResolver
        {
            public bool TryResolve(Type type, out object value)
            {
                value = "not a Service";
                return true;
            }
        }

#pragma warning restore 0649
    }
}
