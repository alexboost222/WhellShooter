using System;
using System.Collections.Generic;
using Engine.Disposables;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Engine.Tests.EditMode.Disposables
{
    [TestFixture]
    public sealed class DisposableBagTests
    {
        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        [Test]
        public void Dispose_DisposesEverythingHeld()
        {
            var bag = new DisposableBag();
            var first = new Spy();
            var second = new Spy();
            bag.Add(first);
            bag.Add(second);

            bag.Dispose();

            Assert.That(first.DisposeCount, Is.EqualTo(1));
            Assert.That(second.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_UnwindsInReverseOrder()
        {
            var order = new List<string>();
            var bag = new DisposableBag();
            bag.Add(Disposable.Create(() => order.Add("first")));
            bag.Add(Disposable.Create(() => order.Add("second")));

            bag.Dispose();

            Assert.That(order, Is.EqualTo(new[] { "second", "first" }));
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var bag = new DisposableBag();
            var spy = new Spy();
            bag.Add(spy);

            bag.Dispose();
            bag.Dispose();

            Assert.That(spy.DisposeCount, Is.EqualTo(1));
            Assert.That(bag.IsDisposed, Is.True);
        }

        [Test]
        public void Add_AfterDispose_DisposesImmediatelyRatherThanLeaking()
        {
            var bag = new DisposableBag();
            bag.Dispose();
            var late = new Spy();

            bag.Add(late);

            Assert.That(late.DisposeCount, Is.EqualTo(1), "a late subscription must not be held by a dead bag");
            Assert.That(bag.Count, Is.Zero);
        }

        [Test]
        public void Clear_EmptiesButKeepsTheBagUsable()
        {
            var bag = new DisposableBag();
            var first = new Spy();
            bag.Add(first);

            bag.Clear();

            Assert.That(first.DisposeCount, Is.EqualTo(1));
            Assert.That(bag.Count, Is.Zero);
            Assert.That(bag.IsDisposed, Is.False);

            // The reopen case: a ViewModel closed and shown again reuses its bag.
            var second = new Spy();
            bag.Add(second);
            Assert.That(bag.Count, Is.EqualTo(1));

            bag.Dispose();
            Assert.That(second.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Count_TracksContents()
        {
            var bag = new DisposableBag();
            Assert.That(bag.Count, Is.Zero);

            bag.Add(Disposable.Empty);
            bag.Add(Disposable.Empty);

            Assert.That(bag.Count, Is.EqualTo(2));
        }

        [Test]
        public void ThrowingDisposable_DoesNotStrandTheRest()
        {
            LogAssert.ignoreFailingMessages = true;
            var bag = new DisposableBag();
            var survivor = new Spy();
            bag.Add(survivor);
            bag.Add(Disposable.Create(() => throw new InvalidOperationException("boom")));

            Assert.DoesNotThrow(() => bag.Dispose());

            Assert.That(survivor.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void DisposingDuringDispose_DoesNotCorruptIteration()
        {
            var bag = new DisposableBag();
            var late = new Spy();
            var survivor = new Spy();
            bag.Add(survivor);
            // Re-entrant add: the bag is already disposed by the time this runs.
            bag.Add(Disposable.Create(() => bag.Add(late)));

            Assert.DoesNotThrow(() => bag.Dispose());

            Assert.That(late.DisposeCount, Is.EqualTo(1), "re-entrant add during teardown must not leak");
            Assert.That(survivor.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Add_RejectsNull()
        {
            var bag = new DisposableBag();

            Assert.Throws<ArgumentNullException>(() => bag.Add(null));
        }

        [Test]
        public void Constructor_RejectsNegativeCapacity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DisposableBag(-1));
        }

        internal sealed class Spy : IDisposable
        {
            public int DisposeCount { get; private set; }

            public void Dispose() => DisposeCount++;
        }
    }
}
