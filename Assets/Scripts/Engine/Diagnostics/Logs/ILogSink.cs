namespace Engine.Diagnostics.Logs
{
    /// <summary>
    /// Destination for log entries. <see cref="UnityConsoleSink"/> is registered by default;
    /// a file sink, a network sink or a test recorder plug in the same way.
    /// </summary>
    /// <remarks>
    /// A sink is called on whatever thread logged, and a throwing sink is caught and skipped
    /// so one bad destination cannot take the others down with it.
    /// </remarks>
    public interface ILogSink
    {
        void Write(in LogEntry entry);
    }
}
