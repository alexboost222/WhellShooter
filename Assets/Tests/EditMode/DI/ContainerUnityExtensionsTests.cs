using System;
using System.Collections.Generic;
using Engine.DI;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Engine.Tests.EditMode.DI
{
    /// <summary>
    /// Injection into MonoBehaviours — the path Providers depend on to reach their Storage
    /// without a singleton (Docs/03, Docs/06).
    /// </summary>
    [TestFixture]
    public sealed class ContainerUnityExtensionsTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        private Container _container;
        private Service _service;

        [SetUp]
        public void SetUp()
        {
            _container = new Container();
            _service = new Service();
            _container.Register(_service);
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _created.Count; i++)
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);

            _created.Clear();
        }

        [Test]
        public void InjectGameObject_FillsInjectFieldsOnItsComponents()
        {
            var gameObject = NewGameObject();
            var provider = gameObject.AddComponent<NeedsServiceBehaviour>();

            _container.InjectGameObject(gameObject);

            Assert.That(provider.Service, Is.SameAs(_service));
        }

        [Test]
        public void InjectGameObject_FillsEveryComponentOnTheObject()
        {
            var gameObject = NewGameObject();
            var first = gameObject.AddComponent<NeedsServiceBehaviour>();
            var second = gameObject.AddComponent<AlsoNeedsServiceBehaviour>();

            _container.InjectGameObject(gameObject);

            Assert.That(first.Service, Is.SameAs(_service));
            Assert.That(second.Service, Is.SameAs(_service));
        }

        [Test]
        public void InjectGameObject_IgnoresChildren()
        {
            var parent = NewGameObject();
            var child = NewGameObject();
            child.transform.SetParent(parent.transform);
            var onChild = child.AddComponent<NeedsServiceBehaviour>();

            _container.InjectGameObject(parent);

            Assert.That(onChild.Service, Is.Null);
        }

        [Test]
        public void InjectHierarchy_ReachesChildren()
        {
            var parent = NewGameObject();
            var child = NewGameObject();
            child.transform.SetParent(parent.transform);
            var onParent = parent.AddComponent<NeedsServiceBehaviour>();
            var onChild = child.AddComponent<NeedsServiceBehaviour>();

            _container.InjectHierarchy(parent);

            Assert.That(onParent.Service, Is.SameAs(_service));
            Assert.That(onChild.Service, Is.SameAs(_service));
        }

        [Test]
        public void InjectHierarchy_ReachesInactiveObjects()
        {
            var parent = NewGameObject();
            var child = NewGameObject();
            child.transform.SetParent(parent.transform);
            child.SetActive(false);
            var onChild = child.AddComponent<NeedsServiceBehaviour>();

            _container.InjectHierarchy(parent);

            Assert.That(onChild.Service, Is.SameAs(_service),
                "a disabled view still needs its dependencies before it is switched on");
        }

        [Test]
        public void InjectHierarchy_ReturnsTheGameObjectForChaining()
        {
            var gameObject = NewGameObject();

            Assert.That(_container.InjectHierarchy(gameObject), Is.SameAs(gameObject));
        }

        [Test]
        public void Injection_IgnoresComponentsWithNoInjectFields()
        {
            var gameObject = NewGameObject();
            gameObject.AddComponent<PlainBehaviour>();

            Assert.DoesNotThrow(() => _container.InjectHierarchy(gameObject));
        }

        [Test]
        public void Injection_FailsLoudlyWhenAComponentDependencyIsMissing()
        {
            var container = new Container();
            var gameObject = NewGameObject();
            gameObject.AddComponent<NeedsServiceBehaviour>();

            var exception = Assert.Throws<DependencyException>(() => container.InjectHierarchy(gameObject));

            Assert.That(exception.TargetType, Is.EqualTo(typeof(NeedsServiceBehaviour)));
        }

        [Test]
        public void InjectGameObject_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => _container.InjectGameObject(null));
        }

        [Test]
        public void InjectHierarchy_RejectsADestroyedGameObject()
        {
            var gameObject = NewGameObject();
            Object.DestroyImmediate(gameObject);

            Assert.Throws<ArgumentNullException>(() => _container.InjectHierarchy(gameObject));
        }

        [Test]
        public void GenericInject_WorksOnASingleComponent()
        {
            var gameObject = NewGameObject();
            var provider = gameObject.AddComponent<NeedsServiceBehaviour>();

            _container.Inject(provider);

            Assert.That(provider.Service, Is.SameAs(_service));
        }

        // --- InstantiateInjected -----------------------------------------------------------
        //
        // The regression these exist for: Object.Instantiate runs Awake before it returns, so
        // "Instantiate then InjectHierarchy" leaves every Provider registering with null fields.
        // The bug is silent — a provider guarding with `?.` simply does nothing — and it went
        // unnoticed until the UiRouter became the first consumer of a registration (2026-07-30).

        [Test]
        public void InstantiateInjected_FillsDependenciesBeforeAwakeRuns()
        {
            var prefab = NewGameObject();
            prefab.AddComponent<RecordsAwakeBehaviour>();

            var instance = _container.InstantiateInjected(prefab);
            _created.Add(instance);

            var behaviour = instance.GetComponent<RecordsAwakeBehaviour>();
            Assert.That(behaviour.AwakeRan, Is.True, "Awake should have run by the time we get the instance back");
            Assert.That(behaviour.ServiceAtAwake, Is.SameAs(_service),
                "the dependency was still null when Awake ran — providers cannot register");
        }

        [Test]
        public void InstantiateInjected_ReachesChildrenToo()
        {
            var prefab = NewGameObject();
            var child = new GameObject("Child");
            child.transform.SetParent(prefab.transform);
            child.AddComponent<RecordsAwakeBehaviour>();

            var instance = _container.InstantiateInjected(prefab);
            _created.Add(instance);

            var behaviour = instance.GetComponentInChildren<RecordsAwakeBehaviour>();
            Assert.That(behaviour.ServiceAtAwake, Is.SameAs(_service));
        }

        [Test]
        public void InstantiateInjected_LeavesTheInstanceAtTheSceneRootAndActive()
        {
            var prefab = NewGameObject();
            prefab.AddComponent<NeedsServiceBehaviour>();

            var instance = _container.InstantiateInjected(prefab);
            _created.Add(instance);

            // DontDestroyOnLoad only accepts a root object, and the disabled holder must be gone.
            Assert.That(instance.transform.parent, Is.Null);
            Assert.That(instance.activeInHierarchy, Is.True);
            Assert.That(instance.name, Is.EqualTo(prefab.name), "the clone suffix should be stripped");
        }

        [Test]
        public void InstantiateInjected_CleansUpItsHolderWhenInjectionFails()
        {
            var prefab = NewGameObject();
            prefab.AddComponent<NeedsMissingServiceBehaviour>();

            var before = CountRootObjects();
            Assert.Throws<DependencyException>(() => _container.InstantiateInjected(prefab));

            // Otherwise a failed boot leaves a disabled object holding a half-built hierarchy.
            Assert.That(CountRootObjects(), Is.EqualTo(before));
        }

        [Test]
        public void InstantiateInjected_RejectsANullPrefab()
        {
            Assert.Throws<ArgumentNullException>(() => _container.InstantiateInjected(null));
        }

        private static int CountRootObjects() =>
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Length;

        private GameObject NewGameObject()
        {
            var gameObject = new GameObject("InjectionTests");
            _created.Add(gameObject);
            return gameObject;
        }

        // --- fixtures ----------------------------------------------------------------------

#pragma warning disable 0649

        private sealed class Service
        {
        }

        private sealed class NeedsServiceBehaviour : MonoBehaviour
        {
            [Inject] private Service _service;

            public Service Service => _service;
        }

        private sealed class AlsoNeedsServiceBehaviour : MonoBehaviour
        {
            [Inject] private Service _service;

            public Service Service => _service;
        }

        private sealed class PlainBehaviour : MonoBehaviour
        {
        }

        private sealed class NeedsMissingServiceBehaviour : MonoBehaviour
        {
            [Inject] private PlainBehaviour _neverRegistered;
        }

        /// <summary>
        /// Stands in for a Provider: it reads its injected field at exactly the moment a real one
        /// would call <c>storage.Register(this)</c>.
        /// </summary>
        /// <remarks>
        /// <c>[ExecuteAlways]</c> because edit mode does not run <c>Awake</c> otherwise, and the
        /// ordering between injection and <c>Awake</c> is the entire thing under test.
        /// </remarks>
        [ExecuteAlways]
        private sealed class RecordsAwakeBehaviour : MonoBehaviour
        {
            [Inject] private Service _service;

            public bool AwakeRan { get; private set; }

            public Service ServiceAtAwake { get; private set; }

            private void Awake()
            {
                AwakeRan = true;
                ServiceAtAwake = _service;
            }
        }

#pragma warning restore 0649
    }
}
