using System;
using System.Runtime.CompilerServices;
using Object = UnityEngine.Object;

namespace Engine.Diagnostics
{
    /// <summary>
    /// Invariant checks that throw. Deliberately <b>not</b> compiled out of release builds:
    /// a named exception at the broken invariant beats silent
    /// corruption three systems later. Every check is a predictable branch whose throw path
    /// lives in a separate non-inlined method, so the cost at a passing call site is nil.
    /// </summary>
    /// <remarks>
    /// Guard throws, it does not log — the exception is the report. Logging here would double
    /// up with Unity's uncaught-exception handler and would turn every guard test into a
    /// LogAssert dance.
    /// <para>
    /// Unity 6000.3 is C# 9, so there is no <c>[CallerArgumentExpression]</c>: pass
    /// <c>nameof(x)</c> explicitly.
    /// </para>
    /// </remarks>
    public static class Guard
    {
        /// <summary>Throws <see cref="ArgumentNullException"/> if <paramref name="value"/> is null.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNull<T>(T value, string paramName) where T : class
        {
            if (value is null) ThrowNull(paramName);
        }

        /// <summary>
        /// Unity-object flavour of <see cref="ThrowIfNull{T}"/>: also catches the "destroyed but
        /// not null" state that a plain null check walks straight past.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfDestroyed(Object value, string paramName)
        {
            if (value == null) ThrowDestroyed(paramName);
        }

        /// <summary>Throws if the string is null or empty.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNullOrEmpty(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value)) ThrowNullOrEmpty(value, paramName);
        }

        /// <summary>Throws <see cref="InvalidOperationException"/> when the condition holds.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIf(bool condition, string message)
        {
            if (condition) ThrowInvalid(message);
        }

        /// <summary>Throws <see cref="InvalidOperationException"/> unless the condition holds.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNot(bool condition, string message)
        {
            if (!condition) ThrowInvalid(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNegative(int value, string paramName)
        {
            if (value < 0) ThrowOutOfRange(paramName, value, "must be zero or greater");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNegative(float value, string paramName)
        {
            if (value < 0f) ThrowOutOfRange(paramName, value, "must be zero or greater");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNotPositive(int value, string paramName)
        {
            if (value <= 0) ThrowOutOfRange(paramName, value, "must be greater than zero");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNotPositive(float value, string paramName)
        {
            if (value <= 0f) ThrowOutOfRange(paramName, value, "must be greater than zero");
        }

        /// <summary>Bounds check for an index into a collection of <paramref name="count"/> items.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfOutOfRange(int index, int count, string paramName)
        {
            if ((uint)index >= (uint)count) ThrowOutOfRange(paramName, index, $"must be in [0, {count})");
        }

        /// <summary>
        /// Asserts the caller is on Unity's main thread. Cheap enough to leave in hot paths —
        /// an int compare against <see cref="MainThread.Id"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNotMainThread([CallerMemberName] string member = null)
        {
            if (!MainThread.IsMainThread) ThrowOffMainThread(member);
        }

        /// <summary>
        /// The branch that cannot happen. Returns the exception so call sites read
        /// <c>throw Guard.Unreachable("...")</c> and the compiler sees the flow end.
        /// </summary>
        public static Exception Unreachable(string message) =>
            new InvalidOperationException($"Unreachable: {message}");

        /// <summary>Same shape as <see cref="Unreachable"/>, for a branch that is merely wrong rather than impossible.</summary>
        public static Exception Fail(string message) =>
            new InvalidOperationException(message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNull(string paramName) =>
            throw new ArgumentNullException(paramName);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowDestroyed(string paramName) =>
            throw new ArgumentNullException(paramName, "Unity object is null or has been destroyed.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNullOrEmpty(string value, string paramName)
        {
            if (value is null) throw new ArgumentNullException(paramName);
            throw new ArgumentException("Value must not be empty.", paramName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalid(string message) =>
            throw new InvalidOperationException(message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowOutOfRange(string paramName, object value, string requirement) =>
            throw new ArgumentOutOfRangeException(paramName, value, $"Value {requirement}.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowOffMainThread(string member) =>
            throw new InvalidOperationException(
                $"{member} is main-thread only, but ran on thread {Environment.CurrentManagedThreadId} " +
                $"(main is {MainThread.Id}).");
    }
}
