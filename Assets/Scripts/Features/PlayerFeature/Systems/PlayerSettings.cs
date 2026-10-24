using Engine.DI;
using Features.PlayerFeature.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.PlayerFeature.Systems
{
    /// <summary>
    /// The player's own settings, as they stand now: mouse sensitivity. Starts at
    /// <see cref="PlayerConfig"/>'s defaults; <c>PlayerSettingsSystem</c> loads the saved values and
    /// applies changes from the settings screen. Read by <c>PlayerLookSystem</c> and the UI.
    /// </summary>
    public sealed class PlayerSettings : IInitializer
    {
        public const float MinLookSensitivity = 0.01f;
        public const float MaxLookSensitivity = 0.4f;

        [Inject] private PlayerConfig _config;

        public World World { get; set; }

        /// <summary>Degrees turned per pixel of pointer movement.</summary>
        public float LookSensitivity { get; private set; }

        public void OnAwake()
        {
            LookSensitivity = _config.LookSensitivity;
        }

        public void Dispose()
        {
        }

        internal void SetLookSensitivity(float degreesPerPixel)
        {
            LookSensitivity = math.clamp(degreesPerPixel, MinLookSensitivity, MaxLookSensitivity);
        }

        /// <summary>Tests only: the change <c>PlayerSettingsSystem</c> would make, without saving.</summary>
        public void SetLookSensitivityForTests(float degreesPerPixel)
        {
            SetLookSensitivity(degreesPerPixel);
        }
    }
}
