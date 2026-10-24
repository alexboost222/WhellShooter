using Engine.DI;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Engine.Messages;
using Features.Combat.Components;
using Features.Combat.Messages;
using Features.Combat.Utils;
using Features.WeaponsFeature.Components;
using Features.Network.Systems;
using Features.WeaponsFeature.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Combat.Systems
{
    /// <summary>
    /// Consumes <see cref="ShotRequest"/> (read-then-remove): finds the first thing the shot meets —
    /// world geometry or a living body's capsule — and turns a body hit into a <see cref="Damage"/>.
    /// Every resolved shot is also published as <see cref="ShotTraced"/> for the views.
    /// </summary>
    /// <remarks>
    /// <b>Bodies are tested analytically</b>, ray against the capsule <c>CharacterConfig</c>
    /// describes standing on <see cref="CharacterMotion.Position"/>, so who got hit is decided the
    /// same in a headless test as in play. The world is asked through <see cref="IShotOccluder"/>.
    /// <para>
    /// Runs one tick after the shot was fired (Combat is listed before the weapon so it can silence
    /// the dead before the motor runs) — 20 ms at 50 Hz, and the same for every shooter.
    /// </para>
    /// <para>
    /// Net (Docs/05): a shot with <see cref="ShotRequest.RewindTicks"/> is tested against the bodies'
    /// <see cref="BodyHistory"/>; on a client it is traced only — no <see cref="Damage"/>.
    /// </para>
    /// </remarks>
    public sealed class HitscanSystem : ISystem
    {
        [Inject] private Stash<ShotRequest> _shotRequest;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<BodyHistory> _bodyHistory;
        [Inject] private Stash<Damage> _damage;

        [Inject] private IPublisher<ShotTraced> _shotTraced;
        [Inject] private NetRole _role;
        [Inject] private IShotOccluder _occluder;
        [Inject] private CharacterConfig _characterConfig;
        [Inject] private WeaponsConfig _weaponsConfig;

        public World World { get; set; }

        private Filter _filter;
        private Filter _bodies;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ShotRequest>()
                .Build();

            _bodies = World.Filter
                .With<Character>()
                .With<CharacterMotion>()
                .With<Health>()
                .Without<Dead>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var shooter in _filter)
            {
                var shot = _shotRequest.Get(shooter);
                _shotRequest.Remove(shooter);
                Resolve(shooter, shot);
            }
        }

        private void Resolve(Entity shooter, ShotRequest shot)
        {
            if (!_weaponsConfig.TryGet(shot.WeaponId, out var weapon) || math.lengthsq(shot.Direction) < 1e-8f)
            {
                return;
            }

            var direction = math.normalize(shot.Direction);
            var reach = weapon.Range;
            if (_occluder.Raycast(shot.Origin, direction, reach, out var wall))
            {
                reach = wall;
            }

            Entity victim = null;
            foreach (var body in _bodies)
            {
                if (body.ID == shooter.ID)
                {
                    continue;
                }

                var feet = FeetAt(body, shot.RewindTicks);
                if (CapsuleRay.Intersect(shot.Origin, direction, feet, _characterConfig.Height, _characterConfig.Radius,
                        out var distance) && distance < reach)
                {
                    reach = distance;
                    victim = body;
                }
            }

            _shotTraced.Publish(new ShotTraced
            {
                Shooter = shooter.ID,
                Origin = shot.Origin,
                End = shot.Origin + direction * reach,
                HitBody = victim != null,
            });

            // A client's shot is a prediction: the tracer is drawn, the server decides the hit.
            if (victim == null || _role.IsClient)
            {
                return;
            }

            _damage.Set(World.CreateEntity(), new Damage
            {
                Target = victim.ID,
                Source = shooter.ID,
                Amount = weapon.Damage,
                Origin = shot.Origin,
                Direction = direction,
            });
        }

        /// <summary>Where the body stood <paramref name="rewind"/> steps ago, as far back as its history goes.</summary>
        private float3 FeetAt(Entity body, int rewind)
        {
            if (rewind <= 0 || !_bodyHistory.Has(body))
            {
                return _characterMotion.Get(body).Position;
            }

            var history = _bodyHistory.Get(body);
            if (history.Count == 0)
            {
                return _characterMotion.Get(body).Position;
            }

            var back = math.min(rewind, history.Count - 1);
            var index = (history.Cursor - 1 - back + BodyHistory.Capacity) % BodyHistory.Capacity;
            return history.Feet[index];
        }
    }
}
