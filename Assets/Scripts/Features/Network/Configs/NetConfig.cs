using Engine.Configs;
using Engine.Net;
using UnityEngine;

namespace Features.Network.Configs
{
    /// <summary>Connection and tick-sync tuning (Docs/05).</summary>
    [CreateAssetMenu(menuName = "Configs/Net Config", fileName = nameof(NetConfig))]
    public sealed class NetConfig : Config
    {
        [Header("Connection")]
        public string Address = "127.0.0.1";
        public int Port = 7777;
        public string ConnectionKey = "WheelShooter";

        [Header("Input redundancy")]
        [Tooltip("Unacked inputs sent per packet, newest kept when over.")]
        public int MaxInputsPerPacket = 64;

        [Tooltip("Server drops inputs for ticks further ahead of its tick than this; the client never aims past it.")]
        public float MaxInputLeadMs = 300f;

        [Header("Tick sync")]
        [Tooltip("Sync packets measured before the client picks its first tick.")]
        public int SyncSamples = 10;

        [Tooltip("Slack aimed for until loss and jitter are measured; also the start lead.")]
        public int StartSlack = 6;

        [Tooltip("Lowest slack the client ever aims for, in ticks.")]
        public int MinSlack = 1;

        [Tooltip("Highest slack the loss margin may ask for, in ticks.")]
        public int MaxSlack = 10;

        [Tooltip("Accepted per-tick chance of an input missing its tick; sets the loss margin.")]
        public float TargetMissChance = 1e-6f;

        [Tooltip("Ticks of slack above the need before the client sheds lead.")]
        public int Hysteresis = 1;

        [Tooltip("Slack samples per window; at most one hold per window.")]
        public int SlackWindow = 100;

        [Tooltip("Client packets per loss-rate window on the server.")]
        public int LossWindow = 100;

        [Header("Logging")]
        public bool LogEveryInput;
        public int StatsIntervalTicks = 50;

        [Header("Link conditioner (editor / dev only)")]
        public bool SimulateBadNetwork;

        [Tooltip("Applied to each end's outgoing packets: ping 300 = latency 150.")]
        public LinkConditions Conditions = new LinkConditions(150f, 50f, 10f);
    }
}
