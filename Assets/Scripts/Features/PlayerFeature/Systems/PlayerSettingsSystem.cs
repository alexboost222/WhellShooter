using Engine.DI;
using Engine.Messages;
using Features.PlayerFeature.Constants;
using Features.PlayerFeature.Messages;
using Scellecs.Morpeh;
using UnityEngine;

namespace Features.PlayerFeature.Systems
{
    /// <summary>
    /// Loads the saved settings into <see cref="PlayerSettings"/> at boot and applies
    /// <see cref="SetLookSensitivity"/> from the settings screen, saving it (<c>PlayerPrefs</c>,
    /// <see cref="PlayerPrefKeys"/>).
    /// </summary>
    public sealed class PlayerSettingsSystem : ISystem
    {
        [Inject] private SubscriberQueue<SetLookSensitivity> _setLookSensitivity;
        [Inject] private PlayerSettings _settings;

        public World World { get; set; }

        public void OnAwake()
        {
            if (PlayerPrefs.HasKey(PlayerPrefKeys.LookSensitivity))
            {
                _settings.SetLookSensitivity(PlayerPrefs.GetFloat(PlayerPrefKeys.LookSensitivity));
            }
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            var changed = false;
            foreach (var message in _setLookSensitivity)
            {
                _settings.SetLookSensitivity(message.DegreesPerPixel);
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            PlayerPrefs.SetFloat(PlayerPrefKeys.LookSensitivity, _settings.LookSensitivity);
            PlayerPrefs.Save();
        }
    }
}
