using Engine.DI;
using Engine.Messages;
using Features.Map.Messages;
using UnityEngine;

namespace Features.Map.Providers
{
    /// <summary>
    /// Put in a scene to get an arena when it starts: publishes <see cref="GenerateMap"/> once.
    /// </summary>
    public sealed class MapGenerateOnStart : MonoBehaviour
    {
        [Tooltip("The arena to build. 0 = a fresh seed every time.")]
        [SerializeField] private int _seed;

        [Inject] private IPublisher<GenerateMap> _generateMap;

        private void Start()
        {
            // The seed is picked here, outside the simulation, so generation itself stays deterministic.
            var seed = _seed != 0 ? _seed : new System.Random().Next(1, int.MaxValue);
            _generateMap.Publish(new GenerateMap { Seed = seed });
        }
    }
}
