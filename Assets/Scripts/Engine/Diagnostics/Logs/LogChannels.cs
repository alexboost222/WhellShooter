namespace Engine.Diagnostics.Logs
{
    /// <summary>
    /// Engine's built-in channels. Game code declares its own the same way — a static
    /// readonly field per channel, e.g. <c>static readonly LogChannel Shooting = LogChannel.Get("Shooting");</c>
    /// </summary>
    public static class LogChannels
    {
        /// <summary>Default channel: everything logged without naming one. Index 0.</summary>
        public static readonly LogChannel General = LogChannel.Get("General");

        /// <summary>Engine plumbing: DI, disposables, configs.</summary>
        public static readonly LogChannel Core = LogChannel.Get("Core");

        /// <summary>Boot, game state flow, the tick loop.</summary>
        public static readonly LogChannel Runtime = LogChannel.Get("Runtime");

        /// <summary>The message bus.</summary>
        public static readonly LogChannel Messages = LogChannel.Get("Messages");

        /// <summary>Worlds, features, systems.</summary>
        public static readonly LogChannel Ecs = LogChannel.Get("Ecs");

        /// <summary>Router, view models, bindings.</summary>
        public static readonly LogChannel Ui = LogChannel.Get("Ui");

        /// <summary>Saves, prefs.</summary>
        public static readonly LogChannel Save = LogChannel.Get("Save");

        /// <summary>Cameras, render targets, anything drawing outside the main view.</summary>
        public static readonly LogChannel Render = LogChannel.Get("Render");

        /// <summary>The mixer, the one-shot pool, bus levels.</summary>
        /// <remarks>
        /// Its own channel rather than <see cref="Render"/> because almost everything it has to
        /// say is about an <i>asset</i> being mis-authored — a group renamed, a parameter not
        /// exposed — which is one person's problem for an afternoon and nobody else's ever.
        /// </remarks>
        public static readonly LogChannel Audio = LogChannel.Get("Audio");

        /// <summary>String tables, missing keys, locale switches.</summary>
        /// <remarks>
        /// Its own channel rather than <see cref="Ui"/> because a table with gaps in it is
        /// noisy in a specific, mutable way — a translator mid-pass wants it muted, and
        /// everyone else wants it loud.
        /// </remarks>
        public static readonly LogChannel Localization = LogChannel.Get("Localization");

        /// <summary>Transport, connections, tick sync, input traffic.</summary>
        public static readonly LogChannel Net = LogChannel.Get("Net");

        /// <summary>Touches every field, forcing registration. Tooling calls this so the
        /// channel list is complete before anything has logged.</summary>
        public static void Touch()
        {
            _ = General;
            _ = Core;
            _ = Runtime;
            _ = Messages;
            _ = Ecs;
            _ = Ui;
            _ = Save;
            _ = Render;
            _ = Audio;
            _ = Localization;
            _ = Net;
        }
    }
}
