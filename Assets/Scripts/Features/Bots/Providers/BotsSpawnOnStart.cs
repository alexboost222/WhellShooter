using Engine.Configs;
using Engine.DI;
using Engine.Messages;
using Features.Bots.Messages;
using UnityEngine;

namespace Features.Bots.Providers
{
    /// <summary>
    /// Put in a scene to get bots when it starts: publishes <see cref="SpawnBots"/> once. The bots
    /// wait for the arena's spawn points, so order against <c>MapGenerateOnStart</c> doesn't matter.
    /// </summary>
    public sealed class BotsSpawnOnStart : MonoBehaviour
    {
        [Tooltip("How many bots.")]
        [SerializeField, Range(0, 16)] private int _count = 6;

        [Tooltip("Two teams instead of free-for-all.")]
        [SerializeField] private bool _teams;

        [Tooltip("BotsConfig profile name; empty = the default profile.")]
        [SerializeField] private string _profile;

        [Inject] private IPublisher<SpawnBots> _spawnBots;

        private void Start()
        {
            _spawnBots.Publish(new SpawnBots
            {
                Count = _count,
                Teams = _teams,
                ProfileId = string.IsNullOrEmpty(_profile) ? 0 : ConfigIds.Hash(_profile),
            });
        }
    }
}
