using Engine.Diagnostics.Logs;
using Engine.DI;
using Features.Bots.Components;
using Features.Bots.Configs;
using Features.Bots.Constants;
using Features.CharacterFeature.Components;
using Scellecs.Morpeh;
using UnityEngine;

namespace Features.Bots.Systems
{
    /// <summary>
    /// Hydrates bot bodies: builds the view prefab where the body stands and adds the two links.
    /// A view only — the body is simulated whether or not it has one.
    /// </summary>
    public sealed class BotViewSpawnSystem : ISystem
    {
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<CharacterViewLink> _characterViewLink;
        [Inject] private Stash<BotViewLink> _botViewLink;
        [Inject] private Stash<Look> _look;

        [Inject] private BotViewFactory _factory;
        [Inject] private BotsConfig _config;

        public World World { get; set; }

        private Filter _filter;
        private bool _reported;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Bot>()
                .With<CharacterMotion>()
                .With<Look>()
                .Without<CharacterViewLink>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                var position = (Vector3)_characterMotion.Get(entity).Position;
                var rotation = Quaternion.Euler(0f, Mathf.Rad2Deg * _look.Get(entity).Yaw, 0f);
                if (!_factory.TryBuild(_config.ViewPrefab, position, rotation, out var body, out var bot))
                {
                    if (!_reported)
                    {
                        _reported = true;
                        Log.Error(BotLog.Channel,
                            "BotsConfig.ViewPrefab is missing or lacks CharacterView + BotView on its root; bots stay invisible and still.");
                    }

                    return;
                }

                _characterViewLink.Set(entity, new CharacterViewLink { View = body });
                _botViewLink.Set(entity, new BotViewLink { View = bot });
            }
        }
    }
}
