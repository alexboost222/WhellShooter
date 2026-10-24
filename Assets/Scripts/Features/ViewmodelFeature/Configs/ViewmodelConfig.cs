using Engine.Configs;
using UnityEngine;

namespace Features.ViewmodelFeature.Configs
{
    /// <summary>
    /// The shared feel of the first-person view: springs, sway, bob, breathing, landing. Per-weapon
    /// poses and recoil are <c>WeaponsConfig</c>'s. One asset, in <c>Resources/Configs</c>.
    /// Numbers and why: Docs/08 §Viewmodel.
    /// </summary>
    /// <remarks>
    /// <b>Zero switches an effect off</b> rather than falling back to a default — every amount here is
    /// a legitimate thing to turn off while tuning. A spring at 0 Hz snaps to its target.
    /// Angles are degrees, distances metres; the systems convert.
    /// </remarks>
    [CreateAssetMenu(menuName = "WheelShooter/Viewmodel Config", fileName = "ViewmodelConfig")]
    public sealed class ViewmodelConfig : Config
    {
        [Header("Camera")]
        [Tooltip("Vertical field of view from the hip, degrees.")]
        [SerializeField] private float hipFieldOfView = 70f;

        [Tooltip("Field of view added at full sprint, degrees. A little speed feel.")]
        [SerializeField] private float sprintFieldOfView = 4f;

        [Header("Springs (Hz, damping ratio: <1 overshoots, 1 settles, >1 creeps)")]
        [Tooltip("Sway, movement lag and landing — soft, a little overshoot.")]
        [SerializeField] private float motionFrequency = 5f;
        [SerializeField] private float motionDamping = 0.55f;

        [Tooltip("Weapon recoil — stiff and lively, so shots read individually.")]
        [SerializeField] private float recoilFrequency = 9f;
        [SerializeField] private float recoilDamping = 0.45f;

        [Tooltip("Camera punch from shots and landings.")]
        [SerializeField] private float cameraFrequency = 6f;
        [SerializeField] private float cameraDamping = 0.6f;

        [Header("Look sway")]
        [Tooltip("Degrees of lag per degree/second of turning. 0.02 = 3.6° behind at a 180°/s turn.")]
        [SerializeField] private float lookSwayRotation = 0.02f;

        [Tooltip("Degrees of roll per degree/second of yaw.")]
        [SerializeField] private float lookSwayRoll = 0.012f;

        [Tooltip("Metres of lag per degree/second of turning.")]
        [SerializeField] private float lookSwayPosition = 0.00008f;

        [Tooltip("Caps, so a flick does not throw the weapon off screen. Degrees / metres.")]
        [SerializeField] private float maxSwayRotation = 6f;
        [SerializeField] private float maxSwayPosition = 0.03f;

        [Header("Movement lag")]
        [Tooltip("Degrees of roll per m/s of strafing — leaning into the move.")]
        [SerializeField] private float strafeRoll = 1.1f;

        [Tooltip("Metres the weapon trails per m/s of movement.")]
        [SerializeField] private float moveLag = 0.004f;

        [Tooltip("Metres the weapon rises per m/s of falling (and dips per m/s of rising), capped.")]
        [SerializeField] private float fallLag = 0.003f;
        [SerializeField] private float maxFallLag = 0.04f;

        [Header("Bob")]
        [Tooltip("Ground speed at which bob reaches full amplitude, m/s.")]
        [SerializeField] private float bobFullSpeed = 4.5f;

        [Tooltip("Metres covered per footstep, walking / sprinting. One bob cycle is two steps.")]
        [SerializeField] private float walkStepLength = 0.85f;
        [SerializeField] private float sprintStepLength = 1.25f;

        [Tooltip("Walk bob: x side-to-side, y up-and-down, metres.")]
        [SerializeField] private Vector2 bobPosition = new Vector2(0.011f, 0.008f);

        [Tooltip("Walk bob: x pitch, y yaw, z roll, degrees.")]
        [SerializeField] private Vector3 bobRotation = new Vector3(0.6f, 0.5f, 1.0f);

        [Tooltip("Bob multiplier at full sprint.")]
        [SerializeField] private float sprintBobScale = 2.2f;

        [Tooltip("How fast bob fades in and out with speed. Higher is snappier.")]
        [SerializeField] private float bobSharpness = 8f;

        [Header("Breathing (idle)")]
        [Tooltip("Breaths per second.")]
        [SerializeField] private float breathRate = 0.28f;

        [Tooltip("Metres of rise, degrees of pitch.")]
        [SerializeField] private float breathPosition = 0.0025f;
        [SerializeField] private float breathRotation = 0.35f;

        [Header("Jump & land")]
        [Tooltip("Landing speed that gives the full landing kick, m/s. Softer landings scale down.")]
        [SerializeField] private float landReferenceSpeed = 9f;

        [Tooltip("Full landing kick: weapon dip (m), weapon nose-down (°), camera nose-down (°).")]
        [SerializeField] private float landPosition = 0.06f;
        [SerializeField] private float landRotation = 5f;
        [SerializeField] private float landCamera = 2.5f;

        [Tooltip("Take-off kick: weapon dip (m) and nose-up (°).")]
        [SerializeField] private float jumpPosition = 0.025f;
        [SerializeField] private float jumpRotation = 2.5f;

        [Header("Aiming & sprint")]
        [Tooltip("Sway, lag and bob multiplier while aimed. Aiming steadies the weapon.")]
        [Range(0f, 1f)]
        [SerializeField] private float aimMotionScale = 0.15f;

        [Tooltip("Breathing multiplier while aimed.")]
        [Range(0f, 1f)]
        [SerializeField] private float aimBreathScale = 0.4f;

        [Tooltip("Seconds into and out of the sprint pose.")]
        [SerializeField] private float sprintBlendTime = 0.22f;

        public float HipFieldOfView => hipFieldOfView;
        public float SprintFieldOfView => sprintFieldOfView;
        public float MotionFrequency => motionFrequency;
        public float MotionDamping => motionDamping;
        public float RecoilFrequency => recoilFrequency;
        public float RecoilDamping => recoilDamping;
        public float CameraFrequency => cameraFrequency;
        public float CameraDamping => cameraDamping;
        public float LookSwayRotation => lookSwayRotation;
        public float LookSwayRoll => lookSwayRoll;
        public float LookSwayPosition => lookSwayPosition;
        public float MaxSwayRotation => maxSwayRotation;
        public float MaxSwayPosition => maxSwayPosition;
        public float StrafeRoll => strafeRoll;
        public float MoveLag => moveLag;
        public float FallLag => fallLag;
        public float MaxFallLag => maxFallLag;
        public float BobFullSpeed => bobFullSpeed;
        public float WalkStepLength => walkStepLength;
        public float SprintStepLength => sprintStepLength;
        public Vector2 BobPosition => bobPosition;
        public Vector3 BobRotation => bobRotation;
        public float SprintBobScale => sprintBobScale;
        public float BobSharpness => bobSharpness;
        public float BreathRate => breathRate;
        public float BreathPosition => breathPosition;
        public float BreathRotation => breathRotation;
        public float LandReferenceSpeed => landReferenceSpeed;
        public float LandPosition => landPosition;
        public float LandRotation => landRotation;
        public float LandCamera => landCamera;
        public float JumpPosition => jumpPosition;
        public float JumpRotation => jumpRotation;
        public float AimMotionScale => aimMotionScale;
        public float AimBreathScale => aimBreathScale;
        public float SprintBlendTime => sprintBlendTime;
    }
}
