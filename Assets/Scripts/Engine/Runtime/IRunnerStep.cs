using System;
using Engine.Diagnostics;

namespace Engine.Runtime
{
    /// <summary>
    /// One unit of work in a phase. An ECS systems group, a physics step, a small pump — the
    /// Runner treats them alike, so nothing is special-cased in the loop.
    /// </summary>
    public interface IRunnerStep
    {
        /// <summary>
        /// Called once by <see cref="Runner.Initialize"/> before the first tick. Boot is the
        /// validation pass (Docs/06), so a step that cannot start says so here rather than on
        /// frame one of gameplay.
        /// </summary>
        void Initialize();

        void Run(float deltaTime);
    }

    /// <summary>Wraps a delegate as a step — for the small pumps that aren't worth a class.</summary>
    /// <example><c>runner.Add(RunnerPhase.Update, RunnerStep.Create("Log.Flush", _ => UnityConsoleSink.Flush()));</c></example>
    public sealed class RunnerStep : IRunnerStep
    {
        private readonly string _name;
        private readonly Action<float> _run;
        private readonly Action _initialize;

        private RunnerStep(string name, Action<float> run, Action initialize)
        {
            _name = name;
            _run = run;
            _initialize = initialize;
        }

        public static RunnerStep Create(string name, Action<float> run, Action initialize = null)
        {
            Guard.ThrowIfNullOrEmpty(name, nameof(name));
            Guard.ThrowIfNull(run, nameof(run));

            return new RunnerStep(name, run, initialize);
        }

        public void Initialize() => _initialize?.Invoke();

        public void Run(float deltaTime) => _run(deltaTime);

        /// <summary>The Runner reports failures by this name, so make it recognisable.</summary>
        public override string ToString() => _name;
    }
}
