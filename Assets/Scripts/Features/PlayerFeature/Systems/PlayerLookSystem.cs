using Engine.DI;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Utils;
using Features.Network.Systems;
using Features.Network.Utils;
using Features.PlayerFeature.Components;
using Features.PlayerFeature.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.PlayerFeature.Systems
{
    /// <summary>
    /// Spends <see cref="PlayerLookInput"/> on the body's <see cref="Look"/>: yaw wrapped, pitch
    /// clamped. Every frame, straight after input — never on the tick. The result is also the look
    /// the next tick sends (<see cref="LocalInputBuffer"/>).
    /// </summary>
    public sealed class PlayerLookSystem : ISystem
    {
        [Inject] private Stash<PlayerLookInput> _playerLookInput;
        [Inject] private Stash<Look> _look;

        [Inject] private PlayerConfig _config;
        [Inject] private PlayerSettings _settings;
        [Inject] private LocalInputBuffer _buffer;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PlayerLookInput>()
                .With<Look>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            var sensitivity = math.radians(_settings.LookSensitivity);
            var limit = math.radians(_config.PitchLimit);

            foreach (var entity in _filter)
            {
                ref var input = ref _playerLookInput.Get(entity);
                ref var look = ref _look.Get(entity);

                // Pointer up is +y; looking up is negative pitch.
                look.Yaw = LookMath.WrapAngle(look.Yaw + input.Delta.x * sensitivity);
                look.Pitch = math.clamp(look.Pitch - input.Delta.y * sensitivity, -limit, limit);

                input.Delta = float2.zero;

                // The look the next tick sends: absolute, so the server aims where the client does.
                _buffer.WriteLook(LookCodec.EncodeYaw(look.Yaw), LookCodec.EncodePitch(look.Pitch));
            }
        }
    }
}
