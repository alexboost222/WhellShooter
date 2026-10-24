using Engine.DI;
using Engine.Runtime;
using Features.Bots.Components;
using Features.Bots.Constants;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace Features.Bots.Systems
{
    /// <summary>
    /// Drives a bot's visual from its body (the animation contract, Docs/08 §Bots — animation):
    /// the model at the interpolated feet turned by the look, planar velocity in the bot's frame,
    /// grounded, aiming, and death with a variant picked from the entity and its hit count.
    /// </summary>
    /// <remarks>
    /// A view: reads the world, writes GameObjects. The death variant is a hash rather than a roll
    /// of <see cref="BotRandom"/> — a frame-rate view must not consume the simulation's stream.
    /// Drops <see cref="BotViewLink"/> the frame its object is gone.
    /// </remarks>
    public sealed class BotAnimatorSystem : ISystem
    {
        private const float MoveDamping = 0.1f;
        private const float AimDamping = 0.15f;

        [Inject] private Stash<BotViewLink> _botViewLink;
        [Inject] private Stash<BotAim> _botAim;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Look> _look;
        [Inject] private Stash<Dead> _dead;
        [Inject] private Stash<DamageTaken> _damageTaken;

        [Inject] private TimeService _time;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<BotViewLink>()
                .With<CharacterMotion>()
                .With<Look>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            var alpha = _time.InterpolationAlpha;

            foreach (var entity in _filter)
            {
                var view = _botViewLink.Get(entity).View;
                if (view == null)
                {
                    _botViewLink.Remove(entity);
                    continue;
                }

                var motion = _characterMotion.Get(entity);
                var yaw = _look.Get(entity).Yaw;
                var dead = _dead.Has(entity);

                var model = view.Model;
                if (model != null)
                {
                    var feet = math.lerp(motion.PreviousPosition, motion.Position, alpha);
                    model.SetPositionAndRotation(feet, Quaternion.Euler(0f, math.degrees(yaw), 0f));
                }

                var animator = view.Animator;
                if (animator == null)
                {
                    continue;
                }

                var forward = new float2(math.sin(yaw), math.cos(yaw));
                var right = new float2(math.cos(yaw), -math.sin(yaw));
                var velocity = motion.Velocity.xz;
                animator.SetFloat(BotAnimatorParams.MoveX, math.dot(velocity, right), MoveDamping, deltaTime);
                animator.SetFloat(BotAnimatorParams.MoveZ, math.dot(velocity, forward), MoveDamping, deltaTime);
                animator.SetBool(BotAnimatorParams.Grounded, motion.Grounded);

                var aiming = _botAim.Has(entity) && _botAim.Get(entity).Active && !dead;
                animator.SetFloat(BotAnimatorParams.Aim, aiming ? 1f : 0f, AimDamping, deltaTime);

                if (dead != view.WasDead)
                {
                    if (dead)
                    {
                        var hits = _damageTaken.Has(entity) ? _damageTaken.Get(entity).Hits : 0u;
                        var variant = (int)(math.hash(new int2(entity.ID.GetHashCode(), (int)hits)) %
                                            BotAnimatorParams.DeathVariantCount);
                        animator.SetInteger(BotAnimatorParams.DeathVariant, variant);
                    }

                    animator.SetBool(BotAnimatorParams.Dead, dead);
                    view.WasDead = dead;
                }
            }
        }
    }
}
