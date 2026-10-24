using Engine.DI;
using Engine.Messages;
using Features.PlayerFeature.Messages;
using Features.PlayerFeature.Systems;
using Scellecs.Morpeh;
using UI.Hud.Configs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace UI.Pause.Systems
{
    /// <summary>
    /// The pause / settings screen (Docs/08 §Pause), raw UI Toolkit: shown whenever the desktop
    /// cursor is free (Escape), with the mouse sensitivity slider and Resume.
    /// </summary>
    /// <remarks>
    /// Reads <see cref="PlayerSettings"/> and the cursor state; writes nothing but its own document.
    /// The slider and the button only <b>publish</b> — <see cref="SetLookSensitivity"/>,
    /// <see cref="ResumePlay"/> — and the player feature applies them.
    /// </remarks>
    public sealed class PauseSystem : ISystem
    {
        [Inject] private IPublisher<SetLookSensitivity> _setLookSensitivity;
        [Inject] private IPublisher<ResumePlay> _resumePlay;
        [Inject] private PlayerInputService _input;
        [Inject] private PlayerSettings _settings;
        [Inject] private HudConfig _config;

        public World World { get; set; }

        private UIDocument _document;
        private VisualElement _pause;
        private Slider _slider;
        private Label _value;
        private bool _wasOpen;

        public void OnAwake()
        {
        }

        public void Dispose()
        {
            if (_document != null)
            {
                Object.Destroy(_document.gameObject);
            }

            _document = null;
        }

        public void OnUpdate(float deltaTime)
        {
            if (!EnsureDocument())
            {
                return;
            }

            var open = !_input.LookCaptured && Touchscreen.current == null;
            _pause.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (open && !_wasOpen)
            {
                // Opened: show what is in effect now, without echoing it back as a change.
                _slider.SetValueWithoutNotify(_settings.LookSensitivity);
            }

            if (open)
            {
                _value.text = _slider.value.ToString("0.00");
            }

            _wasOpen = open;
        }

        private bool EnsureDocument()
        {
            if (_document != null)
            {
                return true;
            }

            if (_config.PanelSettings == null || _config.PauseLayout == null)
            {
                return false;
            }

            var go = new GameObject("Pause");
            go.SetActive(false);
            _document = go.AddComponent<UIDocument>();
            _document.panelSettings = _config.PanelSettings;
            _document.visualTreeAsset = _config.PauseLayout;
            _document.sortingOrder = 10;
            go.SetActive(true);

            var root = _document.rootVisualElement;
            _pause = root.Q<VisualElement>("pause");
            _value = root.Q<Label>("sensitivity-value");
            _slider = root.Q<Slider>("sensitivity");
            _slider.lowValue = PlayerSettings.MinLookSensitivity;
            _slider.highValue = PlayerSettings.MaxLookSensitivity;
            _slider.RegisterValueChangedCallback(OnSensitivity);
            root.Q<Button>("resume").clicked += OnResume;
            return true;
        }

        private void OnSensitivity(ChangeEvent<float> change)
        {
            _setLookSensitivity.Publish(new SetLookSensitivity { DegreesPerPixel = change.newValue });
        }

        private void OnResume()
        {
            _resumePlay.Publish(new ResumePlay());
        }
    }
}
