using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode.Meta
{
    /// <summary>
    /// What a headless world cannot prove: that entering play mode really runs Bootstrap and that
    /// Unity's loop really drives the Runner.
    /// </summary>
    public sealed class BootstrapTests
    {
        [UnityTest]
        public IEnumerator EnteringPlayMode_BootsAndDrivesTheRunner()
        {
            Assert.That(global::Meta.Bootstrap.Container, Is.Not.Null, "Bootstrap runs BeforeSceneLoad");
            Assert.That(global::Meta.Bootstrap.Runner, Is.Not.Null);

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(Object.FindAnyObjectByType<Engine.Runtime.UnityRunnerDriver>(), Is.Not.Null,
                "the driver is what forwards Unity's loop into the Runner");
            Assert.That(Physics.simulationMode, Is.EqualTo(SimulationMode.Script));
        }
    }
}
