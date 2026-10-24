using System.Collections.Generic;
using Engine.Configs;
using UnityEngine;

namespace Features.Bots.Configs
{
    /// <summary>
    /// Bot tuning (Docs/08 §Bots): skill profiles, the brain's weights and timings, and the view
    /// prefab. The only place these numbers live.
    /// </summary>
    [CreateAssetMenu(menuName = "WheelShooter/Configs/Bots", fileName = nameof(BotsConfig))]
    public sealed class BotsConfig : Config
    {
        [SerializeField] private List<BotProfile> _profiles = new List<BotProfile> { new BotProfile() };

        [Tooltip("Profile used when a request names none.")]
        [SerializeField] private string _defaultProfile = "Regular";

        [Header("Brain")]
        [Tooltip("Seconds between decisions. Executors still run every tick.")]
        [SerializeField] private float _thinkInterval = 0.2f;

        [Tooltip("Seconds between sight scans.")]
        [SerializeField] private float _scanInterval = 0.15f;

        [Tooltip("Bonus the running action gets, so the brain doesn't flicker between near-equal bids.")]
        [SerializeField] private float _commitment = 0.1f;

        [Tooltip("Score of roaming when nothing else is going on.")]
        [SerializeField] private float _wanderScore = 0.1f;

        [Tooltip("Score of fighting a visible enemy (before distance shaping).")]
        [SerializeField] private float _attackScore = 0.8f;

        [Tooltip("Score of hunting a just-lost enemy (fades over the profile's memory).")]
        [SerializeField] private float _chaseScore = 0.5f;

        [Header("Movement")]
        [Tooltip("Within this distance of the destination counts as arrived, metres.")]
        [SerializeField] private float _arriveDistance = 0.6f;

        [Tooltip("Seconds between periodic re-paths while moving.")]
        [SerializeField] private float _repathInterval = 1.5f;

        [Tooltip("A bot that moved less than this in one progress check is stuck, metres.")]
        [SerializeField] private float _stuckDistance = 0.3f;

        [Tooltip("Seconds per progress check.")]
        [SerializeField] private float _stuckCheckInterval = 1f;

        [Tooltip("Stuck checks in a row before the move is given up.")]
        [SerializeField] private int _stuckLimit = 3;

        [Header("Life")]
        [Tooltip("Seconds a dead bot stays down.")]
        [SerializeField] private float _respawnDelay = 4f;

        [Header("View")]
        [Tooltip("Root has CharacterView (CharacterController + Head) and BotView; the animated model is nested.")]
        [SerializeField] private GameObject _viewPrefab;

        private Dictionary<int, BotProfile> _byId;

        public float ThinkInterval => _thinkInterval;
        public float ScanInterval => _scanInterval;
        public float Commitment => _commitment;
        public float WanderScore => _wanderScore;
        public float AttackScore => _attackScore;
        public float ChaseScore => _chaseScore;
        public float ArriveDistance => _arriveDistance;
        public float RepathInterval => _repathInterval;
        public float StuckDistance => _stuckDistance;
        public float StuckCheckInterval => _stuckCheckInterval;
        public int StuckLimit => _stuckLimit;
        public float RespawnDelay => _respawnDelay;
        public GameObject ViewPrefab => _viewPrefab;

        public int DefaultProfileId => ConfigIds.Hash(_defaultProfile);

        public override IEnumerable<ConfigEntry> Entries
        {
            get
            {
                for (var i = 0; i < _profiles.Count; i++)
                {
                    yield return new ConfigEntry(i, _profiles[i].Name, _profiles[i]);
                }
            }
        }

        /// <summary>The profile for an id; falls back to the default, then to the first row.</summary>
        public BotProfile GetProfile(int id)
        {
            if (_byId == null)
            {
                _byId = new Dictionary<int, BotProfile>();
                foreach (var profile in _profiles)
                {
                    _byId[ConfigIds.Hash(profile.Name)] = profile;
                }
            }

            if (_byId.TryGetValue(id, out var found) || _byId.TryGetValue(DefaultProfileId, out found))
            {
                return found;
            }

            return _profiles.Count > 0 ? _profiles[0] : new BotProfile();
        }

        private void OnValidate()
        {
            _byId = null;
        }

        /// <summary>For tests and tooling: a config built in code rather than loaded.</summary>
        internal void Set(List<BotProfile> profiles, GameObject viewPrefab)
        {
            _profiles = profiles;
            _defaultProfile = profiles.Count > 0 ? profiles[0].Name : _defaultProfile;
            _viewPrefab = viewPrefab;
            _byId = null;
        }
    }
}
