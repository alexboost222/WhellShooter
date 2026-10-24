using System;

namespace Engine.Net
{
    /// <summary>What <see cref="LinkConditioner"/> does to each outgoing packet.</summary>
    [Serializable]
    public struct LinkConditions
    {
        /// <summary>One-way delay. "Ping 300" is 150 here on both ends.</summary>
        public float LatencyMs;

        /// <summary>Per-packet uniform ± around <see cref="LatencyMs"/>; packets reorder.</summary>
        public float JitterMs;

        /// <summary>0..100.</summary>
        public float LossPercent;

        public LinkConditions(float latencyMs, float jitterMs, float lossPercent)
        {
            LatencyMs = latencyMs;
            JitterMs = jitterMs;
            LossPercent = lossPercent;
        }

        public bool IsIdeal => LatencyMs <= 0f && JitterMs <= 0f && LossPercent <= 0f;

        public override string ToString() => $"{LatencyMs:0}±{JitterMs:0} ms, {LossPercent:0.#}% loss";
    }
}
