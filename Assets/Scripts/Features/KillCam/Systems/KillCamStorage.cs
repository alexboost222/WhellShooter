using System.Collections.Generic;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.KillCam.Systems
{
    /// <summary>
    /// The last few seconds of every body, one frame per fixed tick, and the shots fired in them —
    /// what the kill cam replays (Docs/08 §Kill cam). Also the replay's status for the HUD.
    /// </summary>
    /// <remarks>
    /// A ring of <see cref="Capacity"/> ticks, filled by <see cref="KillCamRecordSystem"/> right after
    /// the physics step. View data only: nothing in the simulation reads it.
    /// </remarks>
    public sealed class KillCamStorage : IInitializer
    {
        /// <summary>Ticks kept: 5 s at 50 Hz.</summary>
        public const int Capacity = 250;

        private readonly List<BodySample>[] _frames = new List<BodySample>[Capacity];
        private readonly List<ShotSample> _shots = new List<ShotSample>();

        public World World { get; set; }

        /// <summary>Tick of the newest frame, −1 before the first.</summary>
        public int LatestTick { get; private set; } = -1;

        /// <summary>Frames held, newest is <see cref="LatestTick"/>, contiguous.</summary>
        public int Count { get; private set; }

        public int OldestTick
        {
            get { return LatestTick - Count + 1; }
        }

        /// <summary>Off while a replay plays, so the history it plays from stays put.</summary>
        public bool Recording { get; set; } = true;

        /// <summary>A replay is on screen.</summary>
        public bool Playing { get; set; }

        public IReadOnlyList<ShotSample> Shots
        {
            get { return _shots; }
        }

        public void OnAwake()
        {
            for (var i = 0; i < Capacity; i++)
            {
                _frames[i] = new List<BodySample>(16);
            }
        }

        public void Dispose()
        {
            _shots.Clear();
            Count = 0;
            LatestTick = -1;
        }

        /// <summary>Starts the next tick's frame and returns it to fill.</summary>
        internal List<BodySample> BeginFrame()
        {
            LatestTick++;
            Count = math.min(Count + 1, Capacity);

            var frame = _frames[Slot(LatestTick)];
            frame.Clear();

            // Oldest first, so the expired ones are a prefix; no lambda, nothing allocated per tick.
            var oldest = OldestTick;
            var expired = 0;
            while (expired < _shots.Count && _shots[expired].Tick < oldest)
            {
                expired++;
            }

            if (expired > 0)
            {
                _shots.RemoveRange(0, expired);
            }

            return frame;
        }

        internal void AddShot(ShotSample shot)
        {
            _shots.Add(shot);
        }

        /// <summary>The frame of <paramref name="tick"/>, if it is still held.</summary>
        public bool TryGetFrame(int tick, out List<BodySample> frame)
        {
            frame = null;
            if (Count == 0 || tick < OldestTick || tick > LatestTick)
            {
                return false;
            }

            frame = _frames[Slot(tick)];
            return true;
        }

        private static int Slot(int tick)
        {
            return ((tick % Capacity) + Capacity) % Capacity;
        }
    }

    /// <summary>One body in one recorded tick.</summary>
    public struct BodySample
    {
        public EntityId Id;

        /// <summary>Feet.</summary>
        public float3 Position;

        public float3 Velocity;
        public float Yaw;
        public float Pitch;
        public bool Grounded;
        public bool Dead;
    }

    /// <summary>One shot, stamped with the tick it was seen in.</summary>
    public struct ShotSample
    {
        public int Tick;
        public EntityId Shooter;
        public float3 Origin;
        public float3 End;
    }
}
