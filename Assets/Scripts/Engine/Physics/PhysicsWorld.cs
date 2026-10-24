using System;
using Engine.Diagnostics;
using PhysR;
using Unity.Mathematics;

namespace Engine.Physics
{
    /// <summary>
    /// The simulation's physics world (Docs/11): owns the native phys-r <see cref="PhysWorld"/>.
    /// An engine service — Bootstrap creates, registers and disposes it.
    /// </summary>
    /// <remarks>
    /// Stepped only by <see cref="PhysicsStep"/>, the Runner's physics hook. Systems may query it
    /// (raycasts) at any time between steps; everything that creates or moves native objects lives
    /// in the bodies feature.
    /// <para>
    /// <b>Native memory.</b> Domain reload is disabled, so a world that isn't disposed outlives
    /// play mode; <see cref="Dispose"/> is idempotent and every member throws afterwards.
    /// </para>
    /// </remarks>
    public sealed class PhysicsWorld : IDisposable
    {
        private PhysWorld _native;

        public PhysicsWorld(int tickRate) : this(tickRate, WorldSettings.Default)
        {
        }

        /// <param name="tickRate">Fixed ticks per second; must match the Runner's fixed step.</param>
        /// <param name="settings">phys-r settings; <c>Dt</c> is overwritten from <paramref name="tickRate"/>.</param>
        public PhysicsWorld(int tickRate, WorldSettings settings)
        {
            Guard.ThrowIfNotPositive(tickRate, nameof(tickRate));

            settings.Dt = FP.FromRatio(1, tickRate);
            _native = new PhysWorld(settings);
            TickRate = tickRate;
        }

        public int TickRate { get; }

        /// <summary>The tick length in seconds, as the Runner passes it.</summary>
        public float DeltaTime
        {
            get { return 1f / TickRate; }
        }

        /// <summary>Completed steps.</summary>
        public uint Tick
        {
            get { return Native.Tick; }
        }

        public bool IsDisposed
        {
            get { return _native == null; }
        }

        /// <summary>The raw binding: body / character creation, batched rays, rewinds, sweeps, snapshots.</summary>
        public PhysWorld Native
        {
            get
            {
                if (_native == null)
                {
                    throw new ObjectDisposedException(nameof(PhysicsWorld));
                }

                return _native;
            }
        }

        /// <summary>
        /// The tick rate a fixed step stands for, e.g. 0.02 → 50. Throws unless the step is a whole
        /// number of ticks per second: phys-r's tick is an exact ratio, a 0.0166… step would drift.
        /// </summary>
        public static int TickRateOf(float fixedDeltaTime)
        {
            Guard.ThrowIfNotPositive(fixedDeltaTime, nameof(fixedDeltaTime));

            var rate = (int)math.round(1f / fixedDeltaTime);
            Guard.ThrowIf(rate <= 0 || math.abs(rate * fixedDeltaTime - 1f) > 1e-4f,
                $"Fixed step {fixedDeltaTime} s is not a whole number of ticks per second.");

            return rate;
        }

        /// <summary>One fixed tick. Only <see cref="PhysicsStep"/> calls this.</summary>
        public void Step()
        {
            Native.Step();
        }

        /// <summary>Closest hit against everything (static and moving, no sensors).</summary>
        public bool Raycast(float3 origin, float3 direction, float maxDistance, out PhysicsRayHit hit)
        {
            return Raycast(origin, direction, maxDistance, QueryFilter.All, out hit);
        }

        /// <summary>Closest hit. <paramref name="direction"/> need not be normalised.</summary>
        public bool Raycast(float3 origin, float3 direction, float maxDistance, QueryFilter filter,
            out PhysicsRayHit hit)
        {
            if (!Native.Raycast(PhysicsMath.ToFP(origin), PhysicsMath.ToFP(math.normalizesafe(direction)),
                    PhysicsMath.ToFP(maxDistance), filter, out var native))
            {
                hit = default;
                return false;
            }

            hit = new PhysicsRayHit(native.Body.Value, native.Distance.ToFloat(), PhysicsMath.ToFloat3(native.Point),
                PhysicsMath.ToFloat3(native.Normal), PhysicsMath.ToFloat3(native.LocalPoint));
            return true;
        }

        public void Dispose()
        {
            _native?.Dispose();
            _native = null;
        }
    }
}
