namespace Engine.Diagnostics.Logs
{
    /// <summary>
    /// Severity of a log entry, also used as a filter threshold: a channel set to
    /// <see cref="Warning"/> emits warnings and errors only. Ordered — comparisons are
    /// load-bearing, so never reorder or renumber these.
    /// </summary>
    public enum LogLevel : byte
    {
        /// <summary>Per-tick spam: packets, system timings, state machine steps.</summary>
        Trace = 0,

        /// <summary>Development detail worth seeing while working on a feature.</summary>
        Debug = 1,

        /// <summary>Lifecycle milestones: boot, scene load, connect, save written.</summary>
        Info = 2,

        /// <summary>Recoverable, but someone should look at it.</summary>
        Warning = 3,

        /// <summary>A broken invariant we chose to survive rather than throw on.</summary>
        Error = 4,

        /// <summary>Threshold only — never the level of an actual entry. Silences everything.</summary>
        Off = 5
    }
}
