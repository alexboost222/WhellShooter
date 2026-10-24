using Engine.DI;
using Engine.Messages;
using Features.Map.Messages;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Development
{
    /// <summary>Dev tool: F5 rebuilds the arena with a fresh seed, and logs it so a good one can be kept.</summary>
    public sealed class MapRegenerateHotkey : MonoBehaviour
    {
        [Inject] private IPublisher<GenerateMap> _generateMap;

        private readonly System.Random _seeds = new System.Random();

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.f5Key.wasPressedThisFrame)
            {
                return;
            }

            var seed = _seeds.Next(1, int.MaxValue);
            Debug.Log($"[Map] Regenerating with seed {seed}.");
            _generateMap.Publish(new GenerateMap { Seed = seed });
        }
    }
}
