using Engine.DI;
using Engine.Messages;
using Features.Bots.Components;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Features.Combat.Messages;
using Features.KillCam.Systems;
using Features.PlayerFeature.Components;
using Features.WeaponsFeature.Components;
using Scellecs.Morpeh;
using UI.Hud.Configs;
using Unity.Mathematics;
using UnityEngine;
using EntityId = Scellecs.Morpeh.EntityId;
using UnityEngine.UIElements;

namespace UI.Hud.Systems
{
    /// <summary>
    /// The HUD (Docs/08 §HUD), raw UI Toolkit: builds one <see cref="UIDocument"/> from
    /// <see cref="HudConfig"/> and every frame writes the local player's state into its named
    /// elements — health, crosshair, hit marker, damage flash, the kill cam strip, death screen.
    /// </summary>
    /// <remarks>
    /// Read-only towards the world. The document is rebuilt if a scene change took it away.
    /// </remarks>
    public sealed class HudSystem : ISystem
    {
        private const float HitMarkerTime = 0.15f;
        private const float DamageFlashTime = 0.4f;
        private const float LowHealth = 0.3f;

        [Inject] private Stash<Health> _health;
        [Inject] private Stash<Dead> _dead;
        [Inject] private Stash<DamageTaken> _damageTaken;
        [Inject] private Stash<PlayerRespawn> _playerRespawn;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Weapon> _weapon;
        [Inject] private Stash<Bot> _bot;

        [Inject] private SubscriberQueue<ShotTraced> _shotTraced;
        [Inject] private KillCamStorage _killCam;
        [Inject] private HudConfig _config;

        public World World { get; set; }

        private Filter _filter;

        private UIDocument _document;
        private VisualElement _hud;
        private VisualElement _crosshair;
        private VisualElement _hitMarkerElement;
        private VisualElement _damageFlashElement;
        private VisualElement _healthElement;
        private VisualElement _healthFill;
        private Label _healthText;
        private VisualElement _killcamElement;
        private Label _killcamKiller;
        private VisualElement _death;
        private Label _deathKiller;
        private Label _deathRespawn;

        private float _hitMarkerLeft;
        private float _damageFlashLeft;
        private uint _hitsSeen;
        private EntityId _seenPlayer;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Player>()
                .With<Health>()
                .Build();
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

            var player = _filter.FirstOrDefault();
            _hud.style.display = player != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (player == null)
            {
                return;
            }

            if (player.ID != _seenPlayer)
            {
                // A new body: its hits so far are not news.
                _seenPlayer = player.ID;
                _hitsSeen = _damageTaken.Has(player) ? _damageTaken.Get(player).Hits : 0u;
            }

            var dead = _dead.Has(player);
            UpdateHealth(player);
            UpdateHits(player, deltaTime);
            UpdateCrosshair(player, dead);
            UpdateDeath(player, dead);
        }

        private void UpdateHealth(Entity player)
        {
            var health = _health.Get(player);
            var fraction = math.saturate(health.Fraction);
            _healthText.text = Mathf.CeilToInt(math.max(0f, health.Current)).ToString();
            _healthFill.style.width = Length.Percent(fraction * 100f);
            _healthFill.EnableInClassList("health__fill--low", fraction < LowHealth);
        }

        private void UpdateHits(Entity player, float deltaTime)
        {
            foreach (var shot in _shotTraced)
            {
                if (shot.HitBody && shot.Shooter == player.ID)
                {
                    _hitMarkerLeft = HitMarkerTime;
                }
            }

            var hits = _damageTaken.Has(player) ? _damageTaken.Get(player).Hits : 0u;
            if (hits != _hitsSeen)
            {
                _hitsSeen = hits;
                _damageFlashLeft = DamageFlashTime;
            }

            _hitMarkerLeft = math.max(0f, _hitMarkerLeft - deltaTime);
            _damageFlashLeft = math.max(0f, _damageFlashLeft - deltaTime);
            _hitMarkerElement.style.display = _hitMarkerLeft > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            _damageFlashElement.style.opacity = _damageFlashLeft / DamageFlashTime;
        }

        private void UpdateCrosshair(Entity player, bool dead)
        {
            var aiming = _weapon.Has(player) && _weapon.Get(player).Aiming;
            var sprinting = _characterMotion.Has(player) && _characterMotion.Get(player).Sprinting;
            _crosshair.style.display = dead || aiming || sprinting ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void UpdateDeath(Entity player, bool dead)
        {
            var replay = dead && _killCam.Playing;
            _killcamElement.style.display = replay ? DisplayStyle.Flex : DisplayStyle.None;
            _death.style.display = dead && !replay ? DisplayStyle.Flex : DisplayStyle.None;
            _healthElement.style.display = replay ? DisplayStyle.None : DisplayStyle.Flex;
            if (!dead)
            {
                return;
            }

            var killer = KillerText(player, _dead.Get(player).Killer);
            _killcamKiller.text = killer.Length > 0 ? "Killed " + killer : "";
            _deathKiller.text = killer;
            _deathRespawn.text = _playerRespawn.Has(player)
                ? $"Respawn in {math.max(0f, _playerRespawn.Get(player).Remaining):0.0}"
                : "Respawning…";
        }

        private string KillerText(Entity player, EntityId killer)
        {
            if (!World.TryGetEntity(killer, out var entity) || entity.ID == player.ID)
            {
                return "";
            }

            var who = _bot.Has(entity) ? "a bot" : "an enemy";
            if (!_characterMotion.Has(entity) || !_characterMotion.Has(player))
            {
                return $"by {who}";
            }

            var distance = math.distance(_characterMotion.Get(entity).Position, _characterMotion.Get(player).Position);
            return $"by {who} · {distance:0} m";
        }

        private bool EnsureDocument()
        {
            if (_document != null)
            {
                return true;
            }

            if (_config.PanelSettings == null || _config.Layout == null)
            {
                return false;
            }

            var go = new GameObject("Hud");
            go.SetActive(false);
            _document = go.AddComponent<UIDocument>();
            _document.panelSettings = _config.PanelSettings;
            _document.visualTreeAsset = _config.Layout;
            go.SetActive(true);

            var root = _document.rootVisualElement;
            _hud = root.Q<VisualElement>("hud");
            _crosshair = root.Q<VisualElement>("crosshair");
            _hitMarkerElement = root.Q<VisualElement>("hit-marker");
            _damageFlashElement = root.Q<VisualElement>("damage-flash");
            _healthElement = root.Q<VisualElement>("health");
            _healthFill = root.Q<VisualElement>("health-fill");
            _healthText = root.Q<Label>("health-text");
            _killcamElement = root.Q<VisualElement>("killcam");
            _killcamKiller = root.Q<Label>("killcam-killer");
            _death = root.Q<VisualElement>("death");
            _deathKiller = root.Q<Label>("death-killer");
            _deathRespawn = root.Q<Label>("death-respawn");
            _hitMarkerElement.style.display = DisplayStyle.None;
            return true;
        }
    }
}
