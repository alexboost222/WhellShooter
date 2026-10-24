using Engine.Ecs;
using Engine.Runtime;
using Features.Network.Systems;
using Features.Replication.Systems;

namespace Features.Replication
{
    /// <summary>
    /// Server-authoritative state for clients (Docs/05 §Replication and prediction). Server: net ids
    /// and one <c>Snapshot</c> per client per tick, after the physics step. Client: snapshots into
    /// the buffer, the server's arena, the local body (created, corrected) and the remote ones
    /// (drawn in the past).
    /// </summary>
    /// <remarks>
    /// Listed after the gameplay features: the server's snapshot carries this tick's result, and on a
    /// client the corrections are applied in this tick's physics step.
    /// </remarks>
    public sealed class ReplicationFeature : EcsFeature
    {
        public override void Register()
        {
            if (Container.Resolve<NetRole>().IsServer)
            {
                AddSystem(RunnerPhase.AfterPhysics, Container.New<NetEntityIdSystem>());
                AddSystem(RunnerPhase.AfterPhysics, Container.New<SnapshotSendSystem>());
                return;
            }

            AddInitializer(Container.NewAndRegister<SnapshotBuffer>());

            AddSystem(RunnerPhase.FixedUpdate, Container.New<SnapshotReceiveSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<LocalBodySystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<RemoteBodySystem>());
            AddSystem(RunnerPhase.AfterPhysics, Container.New<PredictionRecordSystem>());
        }
    }
}
