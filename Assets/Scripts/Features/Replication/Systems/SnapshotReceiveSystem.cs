using Engine.DI;
using Engine.Diagnostics.Logs;
using Engine.Net;
using Features.Map.Components;
using Features.Map.Systems;
using Features.Network.Components;
using Features.Network.Configs;
using Features.Network.Constants;
using Features.Network.Enums;
using Features.Network.Systems;
using Features.Replication.Utils;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Replication.Systems
{
    /// <summary>
    /// Client: the inbox's snapshots into <see cref="SnapshotBuffer"/> (read-then-cleared), the
    /// server's arena asked for when its seed is not the one built, and the render tick moved on
    /// (Docs/05 §Remote bodies). The view delay it ends at is what the next input reports.
    /// </summary>
    public sealed class SnapshotReceiveSystem : ISystem
    {
        // Off the target by more than this → snap to it.
        private const int SnapTicks = 3;

        [Inject] private Stash<NetClock> _netClock;
        [Inject] private Stash<MapGenerateRequest> _mapGenerateRequest;

        [Inject] private NetInbox _inbox;
        [Inject] private SnapshotBuffer _buffer;
        [Inject] private MapStorage _map;
        [Inject] private LocalInputBuffer _input;
        [Inject] private NetConfig _config;

        public World World { get; set; }

        private Filter _clockFilter;
        private int _requestedSeed;

        public void OnAwake()
        {
            _clockFilter = World.Filter
                .With<NetClock>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            for (var i = 0; i < _inbox.Packets.Count; i++)
            {
                var reader = new PacketReader(_inbox.Packets[i]);
                if ((PacketType)reader.ReadByte() == PacketType.Snapshot &&
                    SnapshotPacket.TryRead(ref reader, _buffer.Scratch))
                {
                    _buffer.Commit();
                }
            }

            _inbox.Packets.Clear();

            var newest = _buffer.Newest;
            if (newest == null)
            {
                return;
            }

            RequestArena(newest.Seed);
            AdvanceRenderTick(newest.Tick);

            var tick = _netClock.Get(_clockFilter.First()).Tick;
            var delay = tick == NetConstants.NoTick ? 0 : tick - _buffer.RenderTick;
            _input.WriteViewDelay((byte)math.clamp(delay, 0, byte.MaxValue));
        }

        private void RequestArena(int seed)
        {
            if (seed == 0 || seed == _requestedSeed || (_map.Layout != null && _map.Layout.Seed == seed))
            {
                return;
            }

            _requestedSeed = seed;
            _mapGenerateRequest.Set(World.CreateEntity(), new MapGenerateRequest { Seed = seed });
            Log.Info(LogChannels.Net, $"[Client] building the server's arena, seed {seed}.");
        }

        /// <summary>One tick per step, kept <see cref="NetConfig.InterpolationDelayTicks"/> behind the newest.</summary>
        private void AdvanceRenderTick(int newest)
        {
            var target = newest - _config.InterpolationDelayTicks;
            if (_buffer.RenderTick == NetConstants.NoTick)
            {
                _buffer.RenderTick = target;
                return;
            }

            var render = _buffer.RenderTick + 1;
            var off = target - render;
            if (math.abs(off) > SnapTicks)
            {
                render = target;
            }
            else if (off < -1)
            {
                // Ahead of the target: hold this step.
                render--;
            }

            _buffer.RenderTick = math.min(render, newest);
        }
    }
}
