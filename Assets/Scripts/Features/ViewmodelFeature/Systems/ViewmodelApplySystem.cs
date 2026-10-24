using Engine.DI;
using Features.ViewmodelFeature.Components;
using Scellecs.Morpeh;

namespace Features.ViewmodelFeature.Systems
{
    /// <summary>
    /// Writes <see cref="ViewmodelPose"/> to the rig. The only system here that touches a Transform.
    /// </summary>
    /// <remarks>
    /// Local transforms under the head, which the character feature has already placed this frame —
    /// so the order within Update (head, then this) is what keeps the weapon glued to the view.
    /// </remarks>
    public sealed class ViewmodelApplySystem : ISystem
    {
        [Inject] private Stash<ViewmodelPose> _viewmodelPose;
        [Inject] private Stash<ViewmodelLink> _viewmodelLink;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ViewmodelPose>()
                .With<ViewmodelLink>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                ref var pose = ref _viewmodelPose.Get(entity);
                var rig = _viewmodelLink.Get(entity).Rig;

                rig.Kick.localRotation = pose.CameraRotation;
                rig.Socket.SetLocalPositionAndRotation(pose.WeaponPosition, pose.WeaponRotation);
                rig.Camera.fieldOfView = pose.FieldOfView;
            }
        }
    }
}
