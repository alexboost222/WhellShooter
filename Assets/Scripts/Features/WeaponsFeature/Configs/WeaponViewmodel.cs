using System;
using UnityEngine;

namespace Features.WeaponsFeature.Configs
{
    /// <summary>
    /// How one weapon sits and kicks in first person — the per-weapon half of the procedural
    /// animation. The shared half (sway, bob, breathing, spring stiffness) is <c>ViewmodelConfig</c>.
    /// </summary>
    /// <remarks>
    /// Poses are the weapon model's local position (metres) and rotation (degrees, Euler) under the
    /// camera. <b>Kicks are authored as distances and angles</b>; the viewmodel turns them into a
    /// spring velocity, so a kick reads roughly as "how far it jumps" whatever the spring's stiffness.
    /// </remarks>
    [Serializable]
    public sealed class WeaponViewmodel
    {
        [Header("Poses")]
        [Tooltip("Hip-fire position under the camera, metres.")]
        public Vector3 HipPosition = new Vector3(0.16f, -0.2f, 0.42f);
        public Vector3 HipRotation = new Vector3(0f, -2f, 0f);

        [Tooltip("Aiming down sights: the sight on the camera's centre line.")]
        public Vector3 AimPosition = new Vector3(0f, -0.158f, 0.3f);
        public Vector3 AimRotation = Vector3.zero;

        [Tooltip("Sprinting: lowered and canted.")]
        public Vector3 SprintPosition = new Vector3(0.1f, -0.25f, 0.36f);
        public Vector3 SprintRotation = new Vector3(18f, -32f, 12f);

        [Header("Aiming")]
        [Tooltip("Seconds from hip to sights.")]
        public float AimTime = 0.18f;

        [Tooltip("Camera vertical field of view while aimed, degrees.")]
        public float AimFieldOfView = 50f;

        [Header("Recoil (per shot)")]
        [Tooltip("Weapon kick: x side, y up, z back (negative = towards the face), metres.")]
        public Vector3 KickPosition = new Vector3(0f, 0.006f, -0.045f);

        [Tooltip("Weapon kick: x pitch (negative = muzzle up), degrees.")]
        public Vector3 KickRotation = new Vector3(-4f, 0f, 0f);

        [Tooltip("Random spread added to the kick each shot: ± yaw, ± roll, degrees.")]
        public Vector2 KickRandom = new Vector2(1.2f, 2.5f);

        [Tooltip("Camera punch: degrees of pitch up per shot.")]
        public float CameraKick = 0.8f;

        [Tooltip("Camera punch: ± random yaw per shot, degrees.")]
        public float CameraKickRandom = 0.35f;

        [Tooltip("Recoil multiplier while aimed (0..1). Aiming steadies the weapon.")]
        [Range(0f, 1f)]
        public float AimKickScale = 0.45f;
    }
}
