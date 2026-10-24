using Engine.DI;
using Engine.Physics;
using Features.Bodies.Components;
using PhysR;
using Scellecs.Morpeh;

namespace Features.Bodies.Systems
{
    /// <summary>
    /// First thing in the physics step: destroys every native body or character whose entity was
    /// disposed, or whose link component was removed or replaced, since the last step.
    /// </summary>
    /// <remarks>
    /// A sweep over <see cref="BodiesStorage"/> rather than a <c>Destroy</c> filter: the step runs
    /// after <c>EntitiesDestroyFeature</c>, so tagged entities are already gone by now. A few
    /// thousand records cost a few microseconds.
    /// </remarks>
    public sealed class PhysicsReleaseSystem : ISystem
    {
        [Inject] private Stash<PhysicsBody> _physicsBody;
        [Inject] private Stash<PhysicsCharacter> _physicsCharacter;

        [Inject] private BodiesStorage _storage;
        [Inject] private PhysicsWorld _physics;

        public World World { get; set; }

        public void OnAwake()
        {
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            var native = _physics.Native;
            for (var i = _storage.Count - 1; i >= 0; i--)
            {
                var record = _storage[i];
                if (IsLinked(record))
                {
                    continue;
                }

                if (record.Character)
                {
                    native.DestroyCharacter(new CharacterHandle(record.Handle));
                }
                else
                {
                    native.DestroyBody(new BodyHandle(record.Handle));
                }

                _storage.RemoveAt(i);
            }
        }

        private bool IsLinked(BodiesStorage.Record record)
        {
            if (!World.TryGetEntity(record.Entity, out var entity))
            {
                return false;
            }

            if (record.Character)
            {
                return _physicsCharacter.Has(entity) && _physicsCharacter.Get(entity).Handle == record.Handle;
            }

            return _physicsBody.Has(entity) && _physicsBody.Get(entity).Handle == record.Handle;
        }
    }
}
