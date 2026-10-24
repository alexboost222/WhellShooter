using Engine.Diagnostics;
using Engine.Runtime;
using Scellecs.Morpeh;

namespace Engine.Ecs
{
    /// <summary>
    /// Runs a Morpeh <see cref="SystemsGroup"/> as one Runner step. The adapter lives here
    /// rather than in Engine/Runtime so the Runner itself stays Morpeh-free.
    /// </summary>
    /// <remarks>
    /// <b>Nobody calls <c>World.Commit()</c> by hand.</b> Morpeh's own <c>SystemsGroup.Update</c>
    /// commits after every system, and <c>Initialize</c> commits after every <c>OnAwake</c>, so
    /// structural changes are already visible to whatever runs next. A commit here would just
    /// repeat the one Morpeh made a moment earlier.
    /// <para>
    /// Morpeh also has its own <c>Update</c>/<c>FixedUpdate</c>/<c>LateUpdate</c> dispatch for
    /// the scheduler we deliberately don't use. Everything here is a plain <c>ISystem</c> and
    /// the <see cref="Runner"/> decides when it runs, so this always calls <c>Update</c> — the
    /// phase is our concept, not Morpeh's.
    /// </para>
    /// </remarks>
    public sealed class SystemsGroupStep : IRunnerStep
    {
        private readonly SystemsGroup _group;
        private readonly string _name;

        public SystemsGroupStep(SystemsGroup group, string name)
        {
            Guard.ThrowIfNull(group, nameof(group));
            Guard.ThrowIfNullOrEmpty(name, nameof(name));

            _group = group;
            _name = name;
        }

        public void Initialize() => _group.Initialize();

        public void Run(float deltaTime) => _group.Update(deltaTime);

        /// <summary>The Runner reports failures by this name.</summary>
        public override string ToString() => _name;
    }
}
