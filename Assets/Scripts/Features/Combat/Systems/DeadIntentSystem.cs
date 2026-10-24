using Engine.DI;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Scellecs.Morpeh;

namespace Features.Combat.Systems
{
    /// <summary>
    /// A dead body takes no input: its <see cref="CharacterIntent"/> is cleared every tick, after the
    /// drivers wrote it and before the motor and the weapon read it.
    /// </summary>
    /// <remarks>
    /// Clearing rather than having the motor skip <see cref="Dead"/> keeps the body feature free of
    /// any knowledge of combat — and the body still falls and can still be teleported.
    /// </remarks>
    public sealed class DeadIntentSystem : ISystem
    {
        [Inject] private Stash<CharacterIntent> _characterIntent;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Dead>()
                .With<CharacterIntent>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                _characterIntent.Set(entity, default);
            }
        }
    }
}
