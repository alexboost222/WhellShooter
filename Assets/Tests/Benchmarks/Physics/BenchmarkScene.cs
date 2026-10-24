using System;
using System.Collections.Generic;
using UnityEngine;

namespace Benchmarks.Physics
{
    /// <summary>
    /// One engine-neutral scene description, built identically by every backend: a large static
    /// box level, a central arena with obstacles, falling props, walking characters and hitscan rays.
    /// </summary>
    /// <remarks>
    /// Layout comes from a seeded <see cref="System.Random"/>, so both engines get exactly the same
    /// boxes. Units are metres, the floor top is y = 0.
    /// </remarks>
    public sealed class BenchmarkScene
    {
        public const float Dt = 1f / 60f;
        public const float ArenaHalf = 10f;
        public const float PropHalf = 0.4f;
        public const float PropMass = 10f;
        public const float RayLength = 100f;

        public readonly List<StaticBox> Statics = new List<StaticBox>();
        public readonly List<Vector3> Props = new List<Vector3>();
        public readonly List<Vector3> Characters = new List<Vector3>();

        public string Name;
        public bool Sleep = true;
        public int RaysPerTick;

        public static BenchmarkScene Create(string name, int clutter, int props, int characters, int raysPerTick,
            bool sleep)
        {
            var scene = new BenchmarkScene
            {
                Name = name,
                Sleep = sleep,
                RaysPerTick = raysPerTick,
            };
            var random = new System.Random(1234);

            // 20x20 floor tiles, 4 m each: an 80x80 m level.
            for (var x = 0; x < 20; x++)
            {
                for (var z = 0; z < 20; z++)
                {
                    scene.Statics.Add(new StaticBox(new Vector3(-38f + x * 4f, -0.5f, -38f + z * 4f),
                        new Vector3(2f, 0.5f, 2f), 0f));
                }
            }

            // Level clutter outside the arena; a third of it rotated.
            var placed = 0;
            while (placed < clutter)
            {
                var x = Range(random, -39f, 39f);
                var z = Range(random, -39f, 39f);
                if (Mathf.Abs(x) < ArenaHalf + 2f && Mathf.Abs(z) < ArenaHalf + 2f)
                {
                    continue;
                }

                var half = new Vector3(Range(random, 0.25f, 1.5f), Range(random, 0.25f, 1.5f),
                    Range(random, 0.25f, 1.5f));
                var yaw = placed % 3 == 0 ? Range(random, 0f, Mathf.PI) : 0f;
                scene.Statics.Add(new StaticBox(new Vector3(x, half.y, z), half, yaw));
                placed++;
            }

            // Arena obstacles the characters and props actually touch.
            for (var i = 0; i < 24; i++)
            {
                var angle = i * Mathf.PI * 2f / 24f;
                var radius = i % 2 == 0 ? 4f : 7.5f;
                var half = new Vector3(0.5f, Range(random, 0.3f, 1.2f), 0.5f);
                scene.Statics.Add(new StaticBox(
                    new Vector3(Mathf.Cos(angle) * radius, half.y, Mathf.Sin(angle) * radius), half, angle));
            }

            // Props: columns of cubes dropped into the arena, spaced so nothing starts overlapping.
            for (var i = 0; i < props; i++)
            {
                var column = i % 64;
                var layer = i / 64;
                scene.Props.Add(new Vector3(-7f + column % 8 * 2f, 4f + layer * 1.2f, -7f + column / 8 * 2f));
            }

            // Characters on a ring, inside the arena.
            for (var i = 0; i < characters; i++)
            {
                var angle = i * Mathf.PI * 2f / Mathf.Max(1, characters);
                scene.Characters.Add(new Vector3(Mathf.Cos(angle) * 6f, 1.0f, Mathf.Sin(angle) * 6f));
            }

            return scene;
        }

        /// <summary>The scripted input of character <paramref name="index"/> on <paramref name="tick"/>.</summary>
        public static void CharacterInput(int index, int tick, out float yaw, out bool jump)
        {
            // Each one turns slowly in its own direction, so they cross paths and hit obstacles.
            var direction = index % 2 == 0 ? 1f : -1f;
            yaw = index * 0.7f + direction * tick * 0.02f;
            jump = (tick + index * 7) % 90 == 0;
        }

        /// <summary>Ray <paramref name="index"/> of <paramref name="tick"/>: from the arena, out across the level.</summary>
        public static void Ray(int index, int tick, out Vector3 origin, out Vector3 direction)
        {
            var angle = index * 2.399963f + tick * 0.013f;
            origin = new Vector3(Mathf.Cos(angle) * 3f, 1.6f, Mathf.Sin(angle) * 3f);
            var pitch = -0.02f - index % 5 * 0.01f;
            direction = new Vector3(Mathf.Cos(angle), pitch, Mathf.Sin(angle)).normalized;
        }

        private static float Range(System.Random random, float min, float max)
        {
            return (float)(min + random.NextDouble() * (max - min));
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public readonly struct StaticBox
    {
        public readonly Vector3 Center;
        public readonly Vector3 HalfExtents;
        public readonly float Yaw;

        public StaticBox(Vector3 center, Vector3 halfExtents, float yaw)
        {
            Center = center;
            HalfExtents = halfExtents;
            Yaw = yaw;
        }
    }

    /// <summary>One physics engine running a <see cref="BenchmarkScene"/>.</summary>
    public interface IPhysicsBackend : IDisposable
    {
        string Name { get; }

        void Build(BenchmarkScene scene);

        /// <summary>One fixed tick: apply character input, then simulate.</summary>
        void Step(int tick);

        /// <summary>Casts the tick's rays — one call each, or as one batch — and returns how many hit.</summary>
        int CastRays(int tick, bool batched);

        /// <summary>Average height of the props, to check the engines did the same work.</summary>
        float AveragePropHeight();

        int SleepingProps();
    }
}
