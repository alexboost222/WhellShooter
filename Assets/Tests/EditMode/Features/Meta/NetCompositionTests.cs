using Features.Network.Enums;
using Meta;
using NUnit.Framework;
using Unity.Multiplayer;

namespace Game.Tests.EditMode.Features.Meta
{
    public sealed class NetCompositionTests
    {
        [Test]
        public void Address_FromCommandLine()
        {
            var args = new[] { "game.exe", "-client", "-address", "192.168.1.5" };

            Assert.AreEqual("192.168.1.5", NetComposition.Address(args, "127.0.0.1"));
        }

        [Test]
        public void Address_FallsBackToConfig()
        {
            Assert.AreEqual("127.0.0.1", NetComposition.Address(new[] { "game.exe" }, "127.0.0.1"));
        }

        [Test]
        public void Address_WithoutValue_FallsBackToConfig()
        {
            Assert.AreEqual("127.0.0.1", NetComposition.Address(new[] { "game.exe", "-address" }, "127.0.0.1"));
        }

        [Test]
        public void Mode_CommandLine_BeatsRoleAndConfig()
        {
            var args = new[] { "game.exe", "-client" };

            Assert.AreEqual(NetMode.Client, NetComposition.Mode(args, MultiplayerRoleFlags.Server, NetMode.Server));
        }

        [TestCase(MultiplayerRoleFlags.Server, NetMode.Server)]
        [TestCase(MultiplayerRoleFlags.Client, NetMode.Client)]
        public void Mode_MppmRole_BeatsConfig(MultiplayerRoleFlags role, NetMode expected)
        {
            var configMode = expected == NetMode.Server ? NetMode.Client : NetMode.Server;

            Assert.AreEqual(expected, NetComposition.Mode(new[] { "game.exe" }, role, configMode));
        }

        [TestCase(NetMode.Server)]
        [TestCase(NetMode.Client)]
        public void Mode_NoRole_UsesConfig(NetMode configMode)
        {
            Assert.AreEqual(configMode, NetComposition.Mode(new[] { "game.exe" }, MultiplayerRoleFlags.ClientAndServer, configMode));
        }
    }
}
