using System;
using System.Collections.Generic;
using System.Threading;
using Engine.Diagnostics;

namespace Engine.Diagnostics.Logs
{
    /// <summary>
    /// An interned log category. A channel is a value type wrapping a registry index, so
    /// passing one around is free and comparing two is an int compare.
    /// <c>default(LogChannel)</c> is <see cref="LogChannels.General"/>.
    /// </summary>
    /// <remarks>
    /// Declare channels as static readonly fields — Engine's live in <see cref="LogChannels"/>,
    /// game code declares its own the same way. Names are interned: <c>LogChannel.Get("Net")</c>
    /// twice returns the same channel.
    /// </remarks>
    public readonly struct LogChannel : IEquatable<LogChannel>
    {
        internal readonly int Index;

        internal LogChannel(int index) => Index = index;

        public string Name => LogChannelRegistry.NameOf(Index);

        /// <summary>
        /// Threshold for this channel, or <c>null</c> to inherit <see cref="Log.MinLevel"/>.
        /// An override wins in both directions: a channel may be noisier than the global
        /// default (<see cref="LogLevel.Trace"/>) or muted while everything else keeps talking
        /// (<see cref="LogLevel.Off"/>).
        /// </summary>
        public LogLevel? MinLevel
        {
            get => LogChannelRegistry.LevelOf(Index);
            set => LogChannelRegistry.SetLevel(Index, value);
        }

        /// <summary>Threshold actually applied to this channel right now.</summary>
        public LogLevel EffectiveMinLevel => LogChannelRegistry.LevelOf(Index) ?? Log.MinLevel;

        public static LogChannel Get(string name) => new LogChannel(LogChannelRegistry.GetOrAdd(name));

        /// <summary>Every channel registered so far, in registration order. For tooling.</summary>
        public static IReadOnlyList<LogChannel> All => LogChannelRegistry.All();

        public bool Equals(LogChannel other) => Index == other.Index;
        public override bool Equals(object obj) => obj is LogChannel other && Equals(other);
        public override int GetHashCode() => Index;
        public override string ToString() => Name;

        public static bool operator ==(LogChannel left, LogChannel right) => left.Index == right.Index;
        public static bool operator !=(LogChannel left, LogChannel right) => left.Index != right.Index;
    }

    /// <summary>
    /// Backing store for interned channels. Registration is locked; reads are lock-free
    /// because logging happens on any thread and must stay cheap. The arrays are only ever
    /// swapped for larger copies, and an index is handed out only after its slot is filled,
    /// so a reader holding an index can always dereference it.
    /// </summary>
    internal static class LogChannelRegistry
    {
        private const int InitialCapacity = 16;

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, int> Indices = new Dictionary<string, int>(StringComparer.Ordinal);

        private static string[] _names = new string[InitialCapacity];
        private static LogLevel?[] _levels = new LogLevel?[InitialCapacity];
        private static int _count;

        static LogChannelRegistry()
        {
            // Index 0 is claimed here rather than by LogChannels so that default(LogChannel)
            // is General no matter which channel some static field initializer touches first.
            GetOrAdd("General");
        }

        internal static int GetOrAdd(string name)
        {
            Guard.ThrowIfNullOrEmpty(name, nameof(name));

            lock (Sync)
            {
                if (Indices.TryGetValue(name, out var existing)) return existing;

                var index = _count;
                if (index == _names.Length) Grow();

                _names[index] = name;
                _levels[index] = null;
                Indices.Add(name, index);
                Volatile.Write(ref _count, index + 1);
                return index;
            }
        }

        internal static string NameOf(int index)
        {
            var names = Volatile.Read(ref _names);
            return index >= 0 && index < names.Length ? names[index] : null;
        }

        internal static LogLevel? LevelOf(int index)
        {
            var levels = Volatile.Read(ref _levels);
            return index >= 0 && index < levels.Length ? levels[index] : null;
        }

        internal static void SetLevel(int index, LogLevel? level)
        {
            lock (Sync)
            {
                if (index >= 0 && index < _count) _levels[index] = level;
            }
        }

        internal static LogChannel[] All()
        {
            lock (Sync)
            {
                var all = new LogChannel[_count];
                for (var i = 0; i < all.Length; i++) all[i] = new LogChannel(i);
                return all;
            }
        }

        /// <summary>Drops every per-channel override back to "inherit". Channels themselves persist.</summary>
        internal static void ResetLevels()
        {
            lock (Sync)
            {
                for (var i = 0; i < _count; i++) _levels[i] = null;
            }
        }

        private static void Grow()
        {
            var capacity = _names.Length * 2;

            var names = new string[capacity];
            Array.Copy(_names, names, _count);
            var levels = new LogLevel?[capacity];
            Array.Copy(_levels, levels, _count);

            // Publish both arrays before any index into them escapes GetOrAdd.
            Volatile.Write(ref _names, names);
            Volatile.Write(ref _levels, levels);
        }
    }
}
