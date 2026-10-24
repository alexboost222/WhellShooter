using Engine.DI;
using Features.PlayerFeature.Systems;
using UnityEngine;

namespace Features.PlayerFeature.Providers
{
    /// <summary>
    /// "The player starts here, facing this way." Registers itself into
    /// <see cref="PlayerSpawnPointStorage"/>; a scene without one has no player.
    /// </summary>
    /// <remarks>
    /// Registers in <c>Start</c>, not <c>Awake</c>: a scene loaded after boot is injected on
    /// <c>sceneLoaded</c>, which Unity raises after <c>Awake</c> and before <c>Start</c>.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        [Inject] private PlayerSpawnPointStorage _storage;

        private void Start()
        {
            _storage.Register(transform);
        }

        private void OnDestroy()
        {
            _storage?.Unregister(transform);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.4f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.9f, transform.position + Vector3.up * 0.9f + transform.forward);
        }
    }
}
