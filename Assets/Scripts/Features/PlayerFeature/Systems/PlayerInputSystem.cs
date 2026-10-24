using Engine.DI;
using Features.CharacterFeature.Components;
using Features.PlayerFeature.Components;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace Features.PlayerFeature.Systems
{
    /// <summary>
    /// Reads the devices into the player body's <see cref="CharacterIntent"/> and
    /// <see cref="PlayerLookInput"/>, every frame. Reads only — what the input means is decided by
    /// the systems that consume it.
    /// </summary>
    /// <remarks>
    /// <b>Separate from <c>PlayerLookSystem</c> on purpose:</b> this half touches real devices and
    /// cannot run in an edit-mode test; the half with rules in it takes its input from a component
    /// and can.
    /// </remarks>
    public sealed class PlayerInputSystem : ISystem
    {
        [Inject] private Stash<CharacterIntent> _characterIntent;
        [Inject] private Stash<PlayerLookInput> _playerLookInput;

        [Inject] private PlayerInputService _input;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Player>()
                .With<CharacterIntent>()
                .With<PlayerLookInput>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            _input.UpdateCursor();

            foreach (var entity in _filter)
            {
                ref var intent = ref _characterIntent.Get(entity);

                float2 move = (Vector2)_input.Move.ReadValue<Vector2>();
                intent.Move = math.lengthsq(move) > 1f ? math.normalize(move) : move;

                intent.Sprint = _input.Sprint.IsPressed();
                intent.Fire = _input.Fire.IsPressed();
                intent.Aim = _input.Aim.IsPressed();

                // Latches: set here, cleared by whoever acts on them.
                intent.JumpPressed |= _input.Jump.WasPressedThisFrame();
                intent.FirePressed |= _input.Fire.WasPressedThisFrame();

                if (_input.LookCaptured)
                {
                    _playerLookInput.Get(entity).Delta += (float2)(Vector2)_input.Look.ReadValue<Vector2>();
                }
            }
        }
    }
}
