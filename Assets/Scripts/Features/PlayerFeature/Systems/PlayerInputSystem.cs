using Engine.DI;
using Engine.Messages;
using Features.Network.Enums;
using Features.Network.Systems;
using Features.PlayerFeature.Components;
using Features.PlayerFeature.Messages;
using Features.PlayerFeature.Utils;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace Features.PlayerFeature.Systems
{
    /// <summary>
    /// Client: reads the devices every frame — move and buttons into <see cref="LocalInputBuffer"/>,
    /// which the next tick samples and sends (Docs/05 §Input), pointer movement into the body's
    /// <see cref="PlayerLookInput"/>. Reads only — what the input means is decided by the systems
    /// that consume it.
    /// </summary>
    /// <remarks>
    /// <b>Separate from <c>PlayerLookSystem</c> on purpose:</b> this half touches real devices and
    /// cannot run in an edit-mode test; the half with rules in it takes its input from a component
    /// and can.
    /// </remarks>
    public sealed class PlayerInputSystem : ISystem
    {
        [Inject] private Stash<PlayerLookInput> _playerLookInput;

        [Inject] private PlayerInputService _input;
        [Inject] private LocalInputBuffer _buffer;
        [Inject] private SubscriberQueue<ResumePlay> _resumePlay;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Player>()
                .With<PlayerLookInput>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var _ in _resumePlay)
            {
                _input.Resume();
            }

            _input.UpdateCursor();

            float2 move = (Vector2)_input.Move.ReadValue<Vector2>();
            var held = InputButtons.None;
            if (_input.Sprint.IsPressed())
            {
                held |= InputButtons.Sprint;
            }

            if (_input.Fire.IsPressed())
            {
                held |= InputButtons.Fire;
            }

            if (_input.Aim.IsPressed())
            {
                held |= InputButtons.Aim;
            }

            // Latches: kept by the buffer until a tick samples them.
            var pressed = InputButtons.None;
            if (_input.Jump.WasPressedThisFrame())
            {
                pressed |= InputButtons.Jump;
            }

            if (_input.Fire.WasPressedThisFrame())
            {
                pressed |= InputButtons.FirePressed;
            }

            _buffer.WriteMove(PlayerInputCodec.EncodeAxis(move.x), PlayerInputCodec.EncodeAxis(move.y), held, pressed);

            if (!_input.LookCaptured)
            {
                return;
            }

            foreach (var entity in _filter)
            {
                _playerLookInput.Get(entity).Delta += (float2)(Vector2)_input.Look.ReadValue<Vector2>();
            }
        }
    }
}
