using Engine.Configs;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Hud.Configs
{
    /// <summary>The in-game UI's assets — HUD and pause screen (Docs/08 §HUD, §Pause). One asset, in <c>Resources/Configs</c>.</summary>
    [CreateAssetMenu(menuName = "WheelShooter/Configs/Hud", fileName = nameof(HudConfig))]
    public sealed class HudConfig : Config
    {
        [Tooltip("The panel the HUD draws on (scale mode, sort order, theme).")]
        [SerializeField] private PanelSettings panelSettings;

        [Tooltip("Content/Hud/Hud.uxml.")]
        [SerializeField] private VisualTreeAsset layout;

        [Tooltip("Content/Pause/Pause.uxml — the pause / settings screen.")]
        [SerializeField] private VisualTreeAsset pauseLayout;

        public PanelSettings PanelSettings => panelSettings;
        public VisualTreeAsset Layout => layout;
        public VisualTreeAsset PauseLayout => pauseLayout;
    }
}
