using System.Collections.Generic;
using Engine.Diagnostics;
using Engine.Runtime;
using Scellecs.Morpeh;

namespace Engine.Ecs
{
    /// <summary>
    /// A <see cref="Feature"/> that owns ECS systems.
    /// </summary>
    /// <remarks>
    /// <code>
    /// public sealed class GameFlowFeature : EcsFeature
    /// {
    ///     public override void Register()
    ///     {
    ///         AddInitializer(Container.NewAndRegister&lt;GameStateStorage&gt;());
    ///         AddSystem(RunnerPhase.FixedUpdate, Container.New&lt;GameStateSystem&gt;());
    ///     }
    /// }
    /// </code>
    /// Groups are created on demand — one per phase, plus one for initializers — so a feature
    /// just lists what it has and the grouping takes care of itself.
    /// </remarks>
    public abstract class EcsFeature : Feature
    {
        private readonly Dictionary<RunnerPhase, SystemsGroup> _groups = new Dictionary<RunnerPhase, SystemsGroup>();

        private SystemsGroup _initializers;

        /// <summary>
        /// Registers a storage or service for <c>OnAwake</c>. <b>No phase:</b> initializers wake
        /// once and are never ticked, which is also why this is where world setup belongs —
        /// nothing mutates the world from outside the ECS lifecycle.
        /// </summary>
        protected void AddInitializer(IInitializer initializer)
        {
            Guard.ThrowIfNull(initializer, nameof(initializer));

            if (_initializers == null)
            {
                var world = Container.Resolve<World>();
                _initializers = world.CreateSystemsGroup();
                Runner.AddInitializer(new SystemsGroupStep(_initializers, $"{this}/initializers"));
            }

            _initializers.AddInitializer(initializer);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            FeatureManifest.Note(FeatureFact.Initializer, initializer.GetType().Name);
#endif
        }

        /// <summary>Adds a system to a phase. Systems run in the order they are added.</summary>
        protected void AddSystem(RunnerPhase phase, ISystem system)
        {
            Guard.ThrowIfNull(system, nameof(system));

            if (!_groups.TryGetValue(phase, out var group))
            {
                var world = Container.Resolve<World>();
                group = world.CreateSystemsGroup();
                _groups.Add(phase, group);
                Runner.Add(phase, new SystemsGroupStep(group, $"{this}/{phase}"));
            }

            group.AddSystem(system);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            FeatureManifest.Note(FeatureFact.Step, $"{phase}: {system.GetType().Name}");
#endif
        }
    }
}
