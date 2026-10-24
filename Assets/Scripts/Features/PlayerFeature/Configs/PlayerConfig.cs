using Engine.Configs;
using Features.CharacterFeature.Providers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Features.PlayerFeature.Configs
{
    /// <summary>
    /// What makes a body the player's: its view prefab, the input that drives it and how the view
    /// turns. How the body moves is <c>CharacterConfig</c>'s, shared with bots. One asset, in
    /// <c>Resources/Configs</c>. Reasons: Docs/08 §Player.
    /// </summary>
    [CreateAssetMenu(menuName = "WheelShooter/Player Config", fileName = "PlayerConfig")]
    public sealed class PlayerConfig : Config
    {
        private const float DefaultLookSensitivity = 0.08f;
        private const float DefaultPitchLimit = 85f;
        private const float DefaultRespawnDelay = 5f;

        [Tooltip("The player's body and camera: a prefab with a CharacterView on the root. " +
                 "Content/Player/Prefabs/Player.")]
        [SerializeField] private CharacterView viewPrefab;

        [Tooltip("The input actions. The 'Player' map is used: Move, Look, Jump, Sprint, Attack, Aim.")]
        [SerializeField] private InputActionAsset inputActions;

        [Tooltip("Degrees turned per pixel of pointer movement.")]
        [SerializeField] private float lookSensitivity = DefaultLookSensitivity;

        [Tooltip("How far up or down the view can go, degrees from level.")]
        [SerializeField] private float pitchLimit = DefaultPitchLimit;

        [Tooltip("Seconds dead before the player comes back on the spawn point. Covers the kill cam (3.5 s) plus a beat of the death screen.")]
        [SerializeField] private float respawnDelay = DefaultRespawnDelay;

        public CharacterView ViewPrefab => viewPrefab;
        public InputActionAsset InputActions => inputActions;

        // Zero is meaningless for these, and is what a field added after the asset was written reads.
        public float LookSensitivity => lookSensitivity > 0f ? lookSensitivity : DefaultLookSensitivity;
        public float PitchLimit => pitchLimit > 0f ? pitchLimit : DefaultPitchLimit;
        public float RespawnDelay => respawnDelay > 0f ? respawnDelay : DefaultRespawnDelay;
    }
}
