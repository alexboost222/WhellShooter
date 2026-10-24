using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.ViewmodelFeature.Components
{
    /// <summary>
    /// This frame's answer: where the weapon sits under the camera, how the camera is punched, and the
    /// field of view. Composed by <c>ViewmodelPoseSystem</c>, written to the rig by
    /// <c>ViewmodelApplySystem</c> and nothing else.
    /// </summary>
    public struct ViewmodelPose : IComponent
    {
        public float3 WeaponPosition;
        public quaternion WeaponRotation;
        public quaternion CameraRotation;
        public float FieldOfView;
    }
}
