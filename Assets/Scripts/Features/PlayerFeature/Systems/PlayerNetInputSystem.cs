using Engine.DI;
using Engine.Ecs;
using Features.CharacterFeature.Components;
using Features.Network.Components;
using Features.PlayerFeature.Components;
using Features.PlayerFeature.Utils;
using Features.WeaponsFeature.Components;
using Scellecs.Morpeh;

namespace Features.PlayerFeature.Systems
{
    /// <summary>
    /// Server: every <see cref="NetPlayer"/>'s <see cref="ConsumedInput"/> for this tick → its
    /// <see cref="CharacterIntent"/>, <see cref="Look"/> and <see cref="ShotRewind"/> — decoded the
    /// way the client's prediction decodes it (Docs/05 §Input). A missed input repeats without its
    /// latches.
    /// </summary>
    public sealed class PlayerNetInputSystem : ISystem
    {
        [Inject] private Stash<NetClock> _netClock;
        [Inject] private Stash<ServerClient> _serverClient;
        [Inject] private Stash<ConsumedInput> _consumedInput;
        [Inject] private Stash<NetPlayer> _netPlayer;
        [Inject] private Stash<CharacterIntent> _characterIntent;
        [Inject] private Stash<Look> _look;
        [Inject] private Stash<ShotRewind> _shotRewind;

        public World World { get; set; }

        private Filter _filter;
        private Filter _clients;
        private Filter _clockFilter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<NetPlayer>()
                .With<CharacterIntent>()
                .With<Look>()
                .Without<Destroy>()
                .Build();
            _clients = World.Filter
                .With<ServerClient>()
                .With<ConsumedInput>()
                .Without<Destroy>()
                .Build();
            _clockFilter = World.Filter
                .With<NetClock>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            var tick = _netClock.Get(_clockFilter.First()).Tick;
            foreach (var entity in _filter)
            {
                var peer = _netPlayer.Get(entity).Peer;
                foreach (var client in _clients)
                {
                    if (_serverClient.Get(client).Peer != peer)
                    {
                        continue;
                    }

                    var consumed = _consumedInput.Get(client);
                    if (consumed.Tick == tick)
                    {
                        Apply(entity, consumed);
                    }

                    break;
                }
            }
        }

        private void Apply(Entity entity, ConsumedInput consumed)
        {
            PlayerInputCodec.ApplyIntent(consumed.Input, ref _characterIntent.Get(entity), !consumed.Missed);
            _look.Set(entity, PlayerInputCodec.DecodeLook(consumed.Input));
            if (_shotRewind.Has(entity))
            {
                _shotRewind.Get(entity).Ticks = consumed.Input.ViewDelay;
            }
        }
    }
}
