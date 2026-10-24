using System;
using Engine.Disposables;
using NUnit.Framework;

namespace Engine.Tests.EditMode.Disposables
{
    [TestFixture]
    public sealed class DisposableTests
    {
        [Test]
        public void Create_RunsTheActionOnDispose()
        {
            var ran = false;

            Disposable.Create(() => ran = true).Dispose();

            Assert.That(ran, Is.True);
        }

        [Test]
        public void Create_RunsTheActionOnlyOnce()
        {
            var count = 0;
            var disposable = Disposable.Create(() => count++);

            disposable.Dispose();
            disposable.Dispose();
            disposable.Dispose();

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Create_DoesNotRunTheActionEagerly()
        {
            var ran = false;

            Disposable.Create(() => ran = true);

            Assert.That(ran, Is.False);
        }

        [Test]
        public void Create_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => Disposable.Create(null));
        }

        [Test]
        public void Empty_IsSafeToDisposeRepeatedly()
        {
            Assert.DoesNotThrow(() =>
            {
                Disposable.Empty.Dispose();
                Disposable.Empty.Dispose();
            });
        }
    }
}
