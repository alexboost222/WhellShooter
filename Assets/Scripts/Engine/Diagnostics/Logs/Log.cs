using System;
using System.Diagnostics;
using System.Threading;
using Engine.Diagnostics;
using UnityEngine;

namespace Engine.Diagnostics.Logs
{
    /// <summary>
    /// The project's logging front door. <c>UnityEngine.Debug.Log</c> is for throwaway
    /// debugging only — everything that ships goes through here.
    /// </summary>
    /// <remarks>
    /// Two filters, cheapest first:
    /// <list type="bullet">
    /// <item><see cref="Trace"/> and <see cref="Debug"/> are <c>[Conditional]</c> on editor and
    /// development builds, so in a release player the call <i>and its arguments</i> vanish —
    /// interpolated strings are never built.</item>
    /// <item>Everything else is filtered at runtime against the channel's threshold, which
    /// falls back to <see cref="MinLevel"/>.</item>
    /// </list>
    /// Entries fan out to the registered <see cref="ILogSink"/>s; <see cref="UnityConsoleSink"/>
    /// is installed by default.
    /// </remarks>
    public static class Log
    {
        // The editor sits at Info rather than Debug on purpose: every console entry costs a managed
        // stack walk and is retained for the rest of the session, which is what makes domain reload
        // slow down over an afternoon. A development build has no console holding onto anything, so
        // it keeps Debug. A default, not a ceiling — Log.SetLevel overrides per channel.
#if UNITY_EDITOR
        private const LogLevel DefaultMinLevel = LogLevel.Info;
#elif DEVELOPMENT_BUILD
        private const LogLevel DefaultMinLevel = LogLevel.Debug;
#else
        private const LogLevel DefaultMinLevel = LogLevel.Info;
#endif

        private static readonly object Sync = new object();
        private static ILogSink[] _sinks = Array.Empty<ILogSink>();

        static Log() => Reset();

        /// <summary>
        /// Threshold for channels that have no override of their own. Defaults to
        /// <see cref="LogLevel.Debug"/> in a development build and <see cref="LogLevel.Info"/>
        /// everywhere else, including the editor.
        /// </summary>
        public static LogLevel MinLevel { get; set; } = DefaultMinLevel;

        /// <summary>
        /// The build's compiled-in default, for callers that raise <see cref="MinLevel"/> for a
        /// while and need to put it back — the test-run silencer is the one that does.
        /// </summary>
        public static LogLevel DefaultLevel => DefaultMinLevel;

        /// <summary>Sets a channel's threshold. <c>null</c> restores inheritance from <see cref="MinLevel"/>.</summary>
        public static void SetLevel(LogChannel channel, LogLevel? level) =>
            LogChannelRegistry.SetLevel(channel.Index, level);

        /// <summary>Silences a channel outright, whatever the global threshold is.</summary>
        public static void Mute(LogChannel channel) => SetLevel(channel, LogLevel.Off);

        /// <summary>Drops a channel's override so it follows <see cref="MinLevel"/> again.</summary>
        public static void Unmute(LogChannel channel) => SetLevel(channel, null);

        /// <summary>
        /// Whether an entry would survive filtering. Worth checking before building an
        /// expensive message that isn't already inside a <c>[Conditional]</c> call.
        /// </summary>
        public static bool IsEnabled(LogLevel level, LogChannel channel = default) =>
            level >= (channel.MinLevel ?? MinLevel);

        public static void AddSink(ILogSink sink)
        {
            Guard.ThrowIfNull(sink, nameof(sink));
            lock (Sync)
            {
                var sinks = new ILogSink[_sinks.Length + 1];
                Array.Copy(_sinks, sinks, _sinks.Length);
                sinks[_sinks.Length] = sink;
                Volatile.Write(ref _sinks, sinks);
            }
        }

        public static bool RemoveSink(ILogSink sink)
        {
            Guard.ThrowIfNull(sink, nameof(sink));
            lock (Sync)
            {
                var index = Array.IndexOf(_sinks, sink);
                if (index < 0) return false;

                var sinks = new ILogSink[_sinks.Length - 1];
                Array.Copy(_sinks, sinks, index);
                Array.Copy(_sinks, index + 1, sinks, index, sinks.Length - index);
                Volatile.Write(ref _sinks, sinks);
                return true;
            }
        }

        public static void ClearSinks()
        {
            lock (Sync) Volatile.Write(ref _sinks, Array.Empty<ILogSink>());
        }

        /// <summary>Back to factory settings: one Unity console sink, default threshold, no channel overrides.</summary>
        public static void Reset()
        {
            MinLevel = DefaultMinLevel;
            LogChannelRegistry.ResetLevels();
            lock (Sync) Volatile.Write(ref _sinks, new ILogSink[] { new UnityConsoleSink() });
        }

        /// <summary>
        /// Reinstalls the default sink at boot. Deliberately leaves thresholds alone: with
        /// domain reload off, statics survive between play sessions, and editor tooling applies
        /// channel overrides before this runs.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            lock (Sync) Volatile.Write(ref _sinks, new ILogSink[] { new UnityConsoleSink() });
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Trace(string message) => Write(LogLevel.Trace, default, message, null);

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Trace(LogChannel channel, string message) => Write(LogLevel.Trace, channel, message, null);

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Debug(string message) => Write(LogLevel.Debug, default, message, null);

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Debug(LogChannel channel, string message) => Write(LogLevel.Debug, channel, message, null);

        public static void Info(string message) => Write(LogLevel.Info, default, message, null);

        public static void Info(LogChannel channel, string message) => Write(LogLevel.Info, channel, message, null);

        public static void Warning(string message) => Write(LogLevel.Warning, default, message, null);

        public static void Warning(LogChannel channel, string message) => Write(LogLevel.Warning, channel, message, null);

        public static void Error(string message) => Write(LogLevel.Error, default, message, null);

        public static void Error(LogChannel channel, string message) => Write(LogLevel.Error, channel, message, null);

        public static void Error(string message, Exception exception) => Write(LogLevel.Error, default, message, exception);

        public static void Error(LogChannel channel, string message, Exception exception) =>
            Write(LogLevel.Error, channel, message, exception);

        public static void Error(Exception exception) =>
            Write(LogLevel.Error, default, exception != null ? exception.Message : "null exception", exception);

        /// <summary>
        /// Reports at <see cref="LogLevel.Error"/> and hands back the exception to throw, so a
        /// call site reads <c>throw Log.Fail(channel, "…")</c> and the compiler sees the flow end.
        /// </summary>
        /// <remarks>
        /// <b>For the failures that are both fatal and worth a report</b> — a boot that cannot
        /// continue is the case it was written for. <see cref="Guard"/> is the other half of this
        /// pair and stays as it is: a guard throws and does not log, because a broken argument is
        /// reported by the exception reaching whoever passed it. This logs <i>as well</i> because
        /// the throw usually escapes into Unity's own handler, where the channel — the thing that
        /// says which subsystem died — would be lost.
        /// <para>
        /// The cost is that a sink watching errors and a crash reporter watching unhandled
        /// exceptions each see the same failure once. That is the right trade for a process that
        /// is ending anyway, and the reason this is not the shape of every throw in the codebase.
        /// </para>
        /// </remarks>
        public static Exception Fail(LogChannel channel, string message, Exception inner = null)
        {
            Write(LogLevel.Error, channel, message, inner);

            return inner == null
                ? new InvalidOperationException(message)
                : new InvalidOperationException(message, inner);
        }

        private static void Write(LogLevel level, LogChannel channel, string message, Exception exception)
        {
            if (!IsEnabled(level, channel)) return;

            var entry = new LogEntry(level, channel, message, exception, DateTime.UtcNow);
            var sinks = Volatile.Read(ref _sinks);

            for (var i = 0; i < sinks.Length; i++)
            {
                try
                {
                    sinks[i].Write(in entry);
                }
                catch (Exception e)
                {
                    // A broken sink must not silence the others, and must not recurse into Log.
                    UnityEngine.Debug.LogException(e);
                }
            }
        }
    }
}
