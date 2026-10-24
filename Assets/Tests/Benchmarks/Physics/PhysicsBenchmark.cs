using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace Benchmarks.Physics
{
    /// <summary>
    /// PhysX vs phys-r on the same scenes. Not a correctness test: it measures and writes a table to
    /// <c>Temp/physics-benchmark.md</c> (and the console).
    /// </summary>
    /// <remarks>
    /// Lives in its own assembly (<c>Game.Benchmarks</c>, category <c>Benchmark</c>) and takes a
    /// minute or two; the Pipeline test runner does not run <c>[Explicit]</c> tests, so it is not
    /// marked explicit — scope PlayMode runs with a filter to skip it. Numbers from the editor
    /// are indicative only (Mono JIT on the C# side, editor overhead); a device build is the real
    /// measurement.
    /// </remarks>
    [Category("Benchmark")]
    public sealed class PhysicsBenchmark
    {
        private const int Clutter = 4600;
        private const int WarmupTicks = 30;
        private const int MeasuredTicks = 600;

        [UnityTest]
        [Timeout(1200000)]
        public IEnumerator PhysXVsPhysR()
        {
            var scenes = new[]
            {
                BenchmarkScene.Create("Level only (5k statics)", Clutter, 0, 0, 0, true),
                BenchmarkScene.Create("200 props, sleep on", Clutter, 200, 0, 0, true),
                BenchmarkScene.Create("500 props, sleep off", Clutter, 500, 0, 0, false),
                BenchmarkScene.Create("16 characters", Clutter, 0, 16, 0, true),
                BenchmarkScene.Create("64 characters", Clutter, 0, 64, 0, true),
                BenchmarkScene.Create("FPS tick: 16 chars + 64 props + 32 rays", Clutter, 64, 16, 32, true),
                BenchmarkScene.Create("256 rays (16 chars)", Clutter, 0, 16, 256, true),
            };

            var report = new StringBuilder();
            report.AppendLine("# PhysX vs phys-r");
            report.AppendLine();
            report.AppendLine($"{SystemInfo.processorType}, {SystemInfo.processorCount} cores, " +
                              $"Unity {Application.unityVersion}, {(Application.isEditor ? "Editor" : "Player")}. " +
                              $"{WarmupTicks} warm-up + {MeasuredTicks} measured ticks at 60 Hz. Times in µs.");
            report.AppendLine();
            report.AppendLine("| Scene | Engine | Build ms | Step mean | Step median | Step p95 | Step max " +
                              "| Rays single | Rays batch | Avg prop y | Sleeping |");
            report.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

            foreach (var scene in scenes)
            {
                var backends = new IPhysicsBackend[]
                {
                    new PhysXBackend(),
                    new PhysRBackend(1),
                    new PhysRBackend(0),
                };

                foreach (var backend in backends)
                {
                    var row = Measure(scene, backend);
                    report.AppendLine(row);

                    // Lets the unloaded PhysX scene actually go away before the next one.
                    yield return null;
                }
            }

            var text = report.ToString();
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "physics-benchmark.md"));
            File.WriteAllText(path, text);
            Debug.Log(text + "\nWritten to " + path);
        }

        private static string Measure(BenchmarkScene scene, IPhysicsBackend backend)
        {
            try
            {
                var build = Stopwatch.StartNew();
                backend.Build(scene);
                build.Stop();

                for (var tick = 0; tick < WarmupTicks; tick++)
                {
                    backend.Step(tick);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                var steps = new List<double>(MeasuredTicks);
                var singleRays = new List<double>(MeasuredTicks);
                var batchRays = new List<double>(MeasuredTicks);
                for (var tick = WarmupTicks; tick < WarmupTicks + MeasuredTicks; tick++)
                {
                    var start = Stopwatch.GetTimestamp();
                    backend.Step(tick);
                    steps.Add(Micros(start));

                    if (scene.RaysPerTick > 0)
                    {
                        start = Stopwatch.GetTimestamp();
                        backend.CastRays(tick, false);
                        singleRays.Add(Micros(start));

                        start = Stopwatch.GetTimestamp();
                        backend.CastRays(tick, true);
                        batchRays.Add(Micros(start));
                    }
                }

                steps.Sort();
                singleRays.Sort();
                batchRays.Sort();

                var props = scene.Props.Count > 0
                    ? backend.AveragePropHeight().ToString("0.00") : "—";
                var sleeping = scene.Props.Count > 0
                    ? backend.SleepingProps() + "/" + scene.Props.Count : "—";

                return $"| {scene.Name} | {backend.Name} | {build.Elapsed.TotalMilliseconds:0} " +
                       $"| {Mean(steps):0.0} | {Percentile(steps, 0.5):0.0} | {Percentile(steps, 0.95):0.0} " +
                       $"| {steps[steps.Count - 1]:0.0} | {Mean(singleRays, "—")} | {Mean(batchRays, "—")} " +
                       $"| {props} | {sleeping} |";
            }
            finally
            {
                backend.Dispose();
            }
        }

        private static double Micros(long start)
        {
            return (Stopwatch.GetTimestamp() - start) * 1_000_000.0 / Stopwatch.Frequency;
        }

        private static double Mean(List<double> values)
        {
            var sum = 0.0;
            for (var i = 0; i < values.Count; i++)
            {
                sum += values[i];
            }

            return values.Count == 0 ? 0.0 : sum / values.Count;
        }

        private static string Mean(List<double> values, string empty)
        {
            return values.Count == 0 ? empty : Mean(values).ToString("0.0");
        }

        private static double Percentile(List<double> sorted, double p)
        {
            if (sorted.Count == 0)
            {
                return 0.0;
            }

            var index = (int)Math.Round(p * (sorted.Count - 1));
            return sorted[index];
        }
    }
}
