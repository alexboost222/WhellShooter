using Engine.Motion;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.ViewmodelFeature.Components
{
    /// <summary>
    /// The viewmodel's springs. Rotations are small Euler offsets in radians (x pitch, y yaw, z roll),
    /// positions metres, both in camera space.
    /// </summary>
    /// <remarks>
    /// Three families with three stiffnesses, because they are different feels: <b>motion</b> (sway,
    /// lag, landing) is soft and chases <see cref="MotionTargetPosition"/>/<see cref="MotionTargetRotation"/>;
    /// <b>recoil</b> is stiff and rests at zero, kicked by shots; <b>camera</b> rests at zero, kicked
    /// by shots and landings.
    /// </remarks>
    public struct ViewmodelSprings : IComponent
    {
        public float3 MotionTargetPosition;
        public float3 MotionTargetRotation;

        public Spring3 MotionPosition;
        public Spring3 MotionRotation;
        public Spring3 RecoilPosition;
        public Spring3 RecoilRotation;
        public Spring3 CameraRotation;
    }
}
