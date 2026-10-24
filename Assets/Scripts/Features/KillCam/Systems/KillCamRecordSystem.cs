using Engine.DI;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Scellecs.Morpeh;

namespace Features.KillCam.Systems
{
    /// <summary>
    /// Right after the physics step (<c>RunnerPhase.AfterPhysics</c>): one frame of every body into
    /// <see cref="KillCamStorage"/> — feet, velocity, look, grounded, dead. Remote bodies on a
    /// client count too: they have motion and look but no <c>Character</c>.
    /// </summary>
    public sealed class KillCamRecordSystem : ISystem
    {
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Look> _look;
        [Inject] private Stash<Dead> _dead;

        [Inject] private KillCamStorage _storage;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<CharacterMotion>()
                .With<Look>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            if (!_storage.Recording)
            {
                return;
            }

            var frame = _storage.BeginFrame();
            foreach (var entity in _filter)
            {
                var motion = _characterMotion.Get(entity);
                var look = _look.Get(entity);
                frame.Add(new BodySample
                {
                    Id = entity.ID,
                    Position = motion.Position,
                    Velocity = motion.Velocity,
                    Yaw = look.Yaw,
                    Pitch = look.Pitch,
                    Grounded = motion.Grounded,
                    Dead = _dead.Has(entity),
                });
            }
        }
    }
}
