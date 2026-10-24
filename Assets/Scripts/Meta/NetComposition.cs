using System;
using Engine.Diagnostics.Logs;
using Engine.Net;
using Features.Network.Configs;
using Features.Network.Enums;
using Unity.Multiplayer;

namespace Meta
{
    /// <summary>Decides this process's role and builds its transport (Docs/05).</summary>
    public static class NetComposition
    {
        public const string ServerArg = "-server";
        public const string ClientArg = "-client";
        public const string AddressArg = "-address";

        public static INetTransport CreateTransport(NetConfig config)
        {
            var isServer = IsServer(config.Mode);
            var address = Address(Environment.GetCommandLineArgs(), config.Address);
            INetTransport transport = new LiteNetTransport(isServer, address, config.Port, config.ConnectionKey);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (config.SimulateBadNetwork)
            {
                transport = new LinkConditioner(transport, config.Conditions, Environment.TickCount);
                Log.Warning(LogChannels.Net, $"Link conditioner on: {config.Conditions} each way.");
            }
#endif

            Log.Info(LogChannels.Net, isServer ? "Role: server." : $"Role: client of {address}:{config.Port}.");
            return transport;
        }

        /// <summary>Server address for a client: <c>-address &lt;ip&gt;</c> on the command line, else the config's.</summary>
        public static string Address(string[] args, string fallback)
        {
            var index = Array.IndexOf(args, AddressArg);
            if (index >= 0 && index + 1 < args.Length)
            {
                return args[index + 1];
            }

            return fallback;
        }

        /// <summary>
        /// Command line first; in the editor, the MPPM multiplayer role (Server / Client, set per
        /// instance by the play mode scenario); otherwise <see cref="NetConfig.Mode"/>.
        /// </summary>
        private static bool IsServer(NetMode configMode)
        {
#if UNITY_EDITOR
            var role = MultiplayerRolesManager.ActiveMultiplayerRoleMask;
#else
            var role = MultiplayerRoleFlags.ClientAndServer;
#endif
            return Mode(Environment.GetCommandLineArgs(), role, configMode) == NetMode.Server;
        }

        /// <summary>The role rule of <see cref="IsServer"/>; ClientAndServer means no MPPM role.</summary>
        public static NetMode Mode(string[] args, MultiplayerRoleFlags role, NetMode configMode)
        {
            if (Array.IndexOf(args, ServerArg) >= 0)
            {
                return NetMode.Server;
            }

            if (Array.IndexOf(args, ClientArg) >= 0)
            {
                return NetMode.Client;
            }

            if (role == MultiplayerRoleFlags.Server)
            {
                return NetMode.Server;
            }

            if (role == MultiplayerRoleFlags.Client)
            {
                return NetMode.Client;
            }

            return configMode;
        }
    }
}
