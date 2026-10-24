using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bots.Components
{
    /// <summary>
    /// The bot's current path, as corners. A reused buffer: the array grows when a longer path
    /// needs it and is never shrunk (Docs/03 allows an array component for homogeneous data).
    /// </summary>
    public struct BotPath : IComponent
    {
        public float3[] Corners;
        public int Count;

        /// <summary>The corner being walked to.</summary>
        public int Next;

        /// <summary>The destination this path was computed for.</summary>
        public float3 Destination;

        /// <summary><c>NavStorage.Version</c> the path was computed under; another version = stale.</summary>
        public int NavVersion;

        public bool Valid;

        /// <summary>Seconds until a periodic re-path.</summary>
        public float Repath;

        /// <summary>Progress watchdog: where the bot was at the last check, and how long it's been stuck.</summary>
        public float3 ProgressPosition;
        public float ProgressTimer;
        public int StuckCount;
    }
}
