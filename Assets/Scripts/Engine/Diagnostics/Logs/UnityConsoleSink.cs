using System.Collections.Generic;
using System.Text;
using Engine.Diagnostics;
using UnityEngine;

namespace Engine.Diagnostics.Logs
{
    /// <summary>
    /// Routes entries to the Unity console — the only file in the project that is allowed to
    /// call <c>UnityEngine.Debug.Log*</c> in anger. Anything hooked into
    /// <c>Application.logMessageReceived</c> (the in-game debug console, crash reporters)
    /// picks entries up from here for free.
    /// </summary>
    /// <remarks>
    /// <b>Off-thread entries are queued and printed on the main thread.</b> <c>Debug.Log</c>
    /// invokes every <c>Application.logMessageReceived</c> subscriber <i>synchronously, on the
    /// calling thread</i>, and those subscribers are not ours — some editor packages call
    /// main-thread-only APIs from theirs. The queue is why the fix lives here rather than in every
    /// caller that might log from a worker thread.
    /// <para>
    /// Someone has to pump it — <see cref="Flush"/>, once a frame, installed by Bootstrap. The
    /// queue is bounded and drops oldest, so a missing pump costs old messages rather than memory.
    /// Deferred entries therefore appear slightly after the main-thread entries around them; the
    /// timestamp on <see cref="LogEntry"/> is the ordering of record, not console position.
    /// </para>
    /// </remarks>
    public sealed class UnityConsoleSink : ILogSink
    {
        /// <summary>Entries held for the main thread before the oldest start being dropped.</summary>
        public const int MaxPending = 512;

        // Static because there is one Unity console per process, exactly as there is one main
        // thread: two sinks would otherwise each need their own pump.
        private static readonly object Gate = new object();
        private static readonly List<LogEntry> Pending = new List<LogEntry>();

        // Only ever touched inside Flush, on the main thread.
        private static readonly List<LogEntry> Flushing = new List<LogEntry>();

        // Mirrors Pending.Count so the main thread can check for work without taking the lock on
        // every single log call.
        private static volatile int _pendingCount;
        private static int _dropped;

        [System.ThreadStatic] private static StringBuilder _builder;

        /// <summary>Entries waiting for the main thread.</summary>
        public static int PendingCount => _pendingCount;

        /// <summary>
        /// Entries below this are not printed. <see cref="LogLevel.Trace"/> — print everything —
        /// outside a test run. Queueing is unaffected: an entry from another thread is still
        /// deferred and still counted, it simply may not be printed when its turn comes.
        /// </summary>
        /// <remarks>
        /// <b>A gate on the console, deliberately not on the threshold.</b> The obvious way to
        /// quieten a test run is to raise <see cref="Log.MinLevel"/>, and it does not work: a
        /// handful of fixtures call <see cref="Log.Reset"/> in setup or teardown, and the first one
        /// that does puts the threshold back for every test after it. Worse, <c>LogTests</c> exists
        /// to assert on exactly that filtering, so moving the threshold under it would fail tests
        /// that are doing their job.
        /// <para>
        /// Gating here instead is both immune to <see cref="Log.Reset"/> — this is static, and
        /// Reset installs a new sink rather than clearing it — and invisible to those tests, which
        /// assert against their own fake sinks and never look at the console. What it does change
        /// is the one thing worth changing: a flood of entries a session that nobody reads.
        /// </para>
        /// <para>
        /// Never set above <see cref="LogLevel.Warning"/>. The test framework reads the console —
        /// <c>UnityLogCheckDelegatingCommand</c> fails a test on an unexpected error, and
        /// <c>LogAssert.Expect</c> matches entries that were actually printed — so warnings and
        /// errors have to keep going through.
        /// </para>
        /// </remarks>
        public static LogLevel MinPrintedLevel { get; set; } = LogLevel.Trace;

        public void Write(in LogEntry entry)
        {
            if (!MainThread.IsMainThread)
            {
                Defer(in entry);
                return;
            }

            // Opportunistic, so the queue still drains in a context that installs no per-frame
            // pump — an edit-mode test, or a tool. It also keeps deferred entries next to the
            // main-thread entries they belong between.
            if (_pendingCount > 0) Flush();

            Print(in entry);
        }

        /// <summary>
        /// Prints everything logged from other threads since the last call. Main thread only —
        /// calling it from anywhere else is the bug this whole class exists to prevent.
        /// </summary>
        public static void Flush()
        {
            if (!MainThread.IsMainThread) return;

            int dropped;
            lock (Gate)
            {
                if (Pending.Count == 0 && _dropped == 0) return;

                Flushing.Clear();
                Flushing.AddRange(Pending);
                Pending.Clear();
                _pendingCount = 0;
                dropped = _dropped;
                _dropped = 0;
            }

            for (var i = 0; i < Flushing.Count; i++) Print(Flushing[i]);
            Flushing.Clear();

            if (dropped > 0)
                UnityEngine.Debug.LogWarning(
                    $"[WARN][{LogChannels.General.Name}] {dropped} log entries from other threads were dropped: " +
                    $"more than {MaxPending} arrived between frames.");
        }

        /// <summary>Throws the queue away. For tests, and for shutdown.</summary>
        public static void DiscardPending()
        {
            lock (Gate)
            {
                Pending.Clear();
                _pendingCount = 0;
                _dropped = 0;
            }
        }

        private static void Defer(in LogEntry entry)
        {
            lock (Gate)
            {
                if (Pending.Count >= MaxPending)
                {
                    // Oldest go first: when a background thread is misbehaving, the last thing it
                    // said is what explains it.
                    var excess = Pending.Count - MaxPending + 1;
                    Pending.RemoveRange(0, excess);
                    _dropped += excess;
                }

                Pending.Add(entry);
                _pendingCount = Pending.Count;
            }
        }

        private static void Print(in LogEntry entry)
        {
            // Here rather than in Write, so suppression costs the console its output and nothing
            // else: entries from other threads are still queued and still counted, because the
            // queue is this class's actual contract and what its tests pin down.
            if (entry.Level < MinPrintedLevel) return;

            var text = Format(in entry);

            switch (entry.Level)
            {
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(text);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(text);
                    break;
                default:
                    UnityEngine.Debug.Log(text);
                    break;
            }
        }

        private static string Format(in LogEntry entry)
        {
            var builder = _builder ?? (_builder = new StringBuilder(256));
            builder.Clear();

            builder.Append('[').Append(Tag(entry.Level)).Append(']');

            // General is the default channel; naming it in every line would be noise.
            if (entry.Channel != LogChannels.General)
                builder.Append('[').Append(entry.Channel.Name).Append(']');

            builder.Append(' ').Append(entry.Message);

            if (entry.Exception != null)
                builder.Append('\n').Append(entry.Exception);

            return builder.ToString();
        }

        private static string Tag(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace: return "TRACE";
                case LogLevel.Debug: return "DEBUG";
                case LogLevel.Info: return "INFO";
                case LogLevel.Warning: return "WARN";
                case LogLevel.Error: return "ERROR";
                default: return level.ToString().ToUpperInvariant();
            }
        }
    }
}
