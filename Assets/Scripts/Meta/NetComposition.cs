using System;
using Engine.Diagnostics.Logs;
using Engine.Net;
using Features.Network.Configs;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Multiplayer.PlayMode;
#endif

namespace Meta
{
    /// <summary>Decides this process's role and builds its transport (Docs/05).</summary>
    public static class NetComposition
    {
        public const string ServerArg = "-server";
        public const string ClientArg = "-client";
        public const string ServerTag = "Server";
        public const string ClientTag = "Client";

        public static INetTransport CreateTransport(NetConfig config)
        {
            var isServer = IsServer();
            INetTransport transport = new LiteNetTransport(isServer, config.Address, config.Port, config.ConnectionKey);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (config.SimulateBadNetwork)
            {
                transport = new LinkConditioner(transport, config.Conditions, Environment.TickCount);
                Log.Warning(LogChannels.Net, $"Link conditioner on: {config.Conditions} each way.");
            }
#endif

            Log.Info(LogChannels.Net, $"Role: {(isServer ? "server" : "client")}.");
            return transport;
        }

        /// <summary>
        /// Command line first, then the MPPM player tag, then: main editor = server, virtual
        /// players = clients; a player build is a server only in batch mode.
        /// </summary>
        private static bool IsServer()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, ServerArg) >= 0)
            {
                return true;
            }

            if (Array.IndexOf(args, ClientArg) >= 0)
            {
                return false;
            }

#if UNITY_EDITOR
            var tags = CurrentPlayer.ReadOnlyTags();
            if (Array.IndexOf(tags, ServerTag) >= 0)
            {
                return true;
            }

            if (Array.IndexOf(tags, ClientTag) >= 0)
            {
                return false;
            }

            return CurrentPlayer.IsMainEditor;
#else
            return Application.isBatchMode;
#endif
        }
    }
}
