using System;

namespace Engine.Diagnostics.Logs
{
    /// <summary>
    /// One log record, handed to every <see cref="ILogSink"/>. Passed by <c>in</c> — sinks
    /// must not hold onto it beyond the call.
    /// </summary>
    public readonly struct LogEntry
    {
        public readonly LogLevel Level;
        public readonly LogChannel Channel;
        public readonly string Message;

        /// <summary>The exception this entry reports, or null.</summary>
        public readonly Exception Exception;

        /// <summary>Wall-clock time of the call. Never a simulation timestamp — see TimeService for those.</summary>
        public readonly DateTime TimestampUtc;

        public LogEntry(LogLevel level, LogChannel channel, string message, Exception exception, DateTime timestampUtc)
        {
            Level = level;
            Channel = channel;
            Message = message;
            Exception = exception;
            TimestampUtc = timestampUtc;
        }
    }
}
