using System;
using UnityEngine;

namespace Engine.Diagnostics
{
    /// <summary>
    /// Remembers which thread is Unity's main thread, so <see cref="Guard.ThrowIfNotMainThread"/>
    /// can assert it. The message bus, the Runner and every ECS system are main-thread-only by
    /// design (see Docs/02) — asserted, never synchronized.
    /// </summary>
    public static class MainThread
    {
        private static int _id;

        static MainThread()
        {
            // Fallback for edit-mode tests and editor tooling, where RuntimeInitializeOnLoadMethod
            // never fires: whoever touches this type first is assumed to be the main thread. In a
            // player or in play mode, Capture below runs before any user code and settles it.
            _id = Environment.CurrentManagedThreadId;
        }

        public static int Id => _id;

        public static bool IsMainThread => Environment.CurrentManagedThreadId == _id;

        /// <summary>
        /// Declares the calling thread to be the main thread. Called automatically at
        /// subsystem registration; tests call it explicitly in setup.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Capture() => _id = Environment.CurrentManagedThreadId;
    }
}
