using System;
using System.Collections.Generic;
using Engine.Disposables;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Engine.Tests.EditMode.Disposables
{
    [TestFixture]
    public sealed class AddToTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _created.Count; i++)
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);

            _created.Clear();
        }

        [Test]
        public void AddTo_Bag_ReturnsTheSameDisposable()
        {
            var bag = new DisposableBag();
            var disposable = Disposable.Create(() => { });

            var returned = disposable.AddTo(bag);

            Assert.That(returned, Is.SameAs(disposable));
            Assert.That(bag.Count, Is.EqualTo(1));
        }

        [Test]
        public void AddTo_Bag_RejectsNullBag()
        {
            Assert.Throws<ArgumentNullException>(() => Disposable.Empty.AddTo((DisposableBag)null));
        }

        [Test]
        public void AddTo_GameObject_DisposesWhenTheObjectIsDestroyed()
        {
            var gameObject = NewGameObject();
            var disposed = false;
            Disposable.Create(() => disposed = true).AddTo(gameObject);

            Assert.That(disposed, Is.False, "must survive until the object actually dies");

            Object.DestroyImmediate(gameObject);

            Assert.That(disposed, Is.True);
        }

        [Test]
        public void AddTo_GameObject_AttachesOneTriggerHoldingEverything()
        {
            var gameObject = NewGameObject();

            Disposable.Empty.AddTo(gameObject);
            Disposable.Empty.AddTo(gameObject);
            Disposable.Empty.AddTo(gameObject);

            var triggers = gameObject.GetComponents<DestroyTrigger>();
            Assert.That(triggers.Length, Is.EqualTo(1), "the trigger is reused, not stacked");
            Assert.That(triggers[0].Count, Is.EqualTo(3));
        }

        [Test]
        public void AddTo_GameObject_AddsNoTriggerUntilSomethingIsAttached()
        {
            var gameObject = NewGameObject();

            Assert.That(gameObject.GetComponent<DestroyTrigger>(), Is.Null);
        }

        [Test]
        public void AddTo_GameObject_RejectsDestroyedTarget()
        {
            var gameObject = NewGameObject();
            Object.DestroyImmediate(gameObject);

            // Destroyed but not null in C# terms — attaching here would never fire OnDestroy.
            Assert.Throws<ArgumentNullException>(() => Disposable.Empty.AddTo(gameObject));
        }

        [Test]
        public void AddTo_GameObject_RejectsNullTarget()
        {
            Assert.Throws<ArgumentNullException>(() => Disposable.Empty.AddTo((GameObject)null));
        }

        [Test]
        public void AddTo_Component_UsesItsGameObject()
        {
            var gameObject = NewGameObject();
            var component = gameObject.AddComponent<BoxCollider>();
            var disposed = false;

            Disposable.Create(() => disposed = true).AddTo(component);

            Assert.That(gameObject.GetComponent<DestroyTrigger>(), Is.Not.Null);

            Object.DestroyImmediate(gameObject);
            Assert.That(disposed, Is.True);
        }

        [Test]
        public void AddTo_Component_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => Disposable.Empty.AddTo((Component)null));
        }

        [Test]
        public void AddTo_GameObject_DisposesEachAttachedDisposableExactlyOnce()
        {
            var gameObject = NewGameObject();
            var counts = new int[3];
            for (var i = 0; i < counts.Length; i++)
            {
                var index = i;
                Disposable.Create(() => counts[index]++).AddTo(gameObject);
            }

            Object.DestroyImmediate(gameObject);

            Assert.That(counts, Is.EqualTo(new[] { 1, 1, 1 }));
        }

        private GameObject NewGameObject()
        {
            var gameObject = new GameObject("AddToTests");
            _created.Add(gameObject);
            return gameObject;
        }
    }
}
