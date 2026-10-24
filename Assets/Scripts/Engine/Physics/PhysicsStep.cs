using Engine.Diagnostics;
using Engine.Runtime;
using Unity.Mathematics;

namespace Engine.Physics
{
    /// <summary>
    /// The Runner's physics hook (Docs/11): <i>before</i> → <see cref="PhysicsWorld.Step"/> →
    /// <i>after</i>, once per fixed tick, after every FixedUpdate step.
    /// </summary>
    /// <remarks>
    /// The two steps are where whoever bridges the world (an ECS feature) pushes its requests and
    /// inputs in and reads results back out — so they always see the world exactly at the step.
    /// </remarks>
    public sealed class PhysicsStep : IRunnerStep
    {
        private readonly PhysicsWorld _world;
        private readonly IRunnerStep _before;
        private readonly IRunnerStep _after;

        public PhysicsStep(PhysicsWorld world, IRunnerStep before = null, IRunnerStep after = null)
        {
            Guard.ThrowIfNull(world, nameof(world));

            _world = world;
            _before = before;
            _after = after;
        }

        public void Initialize()
        {
            _before?.Initialize();
            _after?.Initialize();
        }

        public void Run(float deltaTime)
        {
            // A changed fixed step needs a restart, not a world quietly ticking at the old rate.
            Guard.ThrowIf(math.abs(deltaTime - _world.DeltaTime) > 1e-5f,
                $"Physics ticks at {_world.TickRate} Hz but the Runner passed {deltaTime} s.");

            _before?.Run(deltaTime);
            _world.Step();
            _after?.Run(deltaTime);
        }

        public override string ToString()
        {
            return "Physics.Step";
        }
    }
}
