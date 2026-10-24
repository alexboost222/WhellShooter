using PhysR;
using Unity.Mathematics;

namespace Engine.Physics
{
    /// <summary>
    /// The float ↔ fixed-point boundary (Docs/11): <c>float3</c> / <c>quaternion</c> in ECS,
    /// <see cref="FP"/> in phys-r.
    /// </summary>
    /// <remarks>
    /// float → FP goes through <see cref="FP.FromFloat"/>, a native call that is exact on every
    /// platform. FP → float is managed and only ever feeds gameplay and views, never the simulation.
    /// </remarks>
    public static class PhysicsMath
    {
        public static FP ToFP(float value)
        {
            return FP.FromFloat(value);
        }

        public static FPVector3 ToFP(float3 value)
        {
            return FPVector3.FromFloat(value.x, value.y, value.z);
        }

        public static FPQuaternion ToFP(quaternion value)
        {
            return FPQuaternion.FromFloat(value.value.x, value.value.y, value.value.z, value.value.w);
        }

        public static float ToFloat(FP value)
        {
            return value.ToFloat();
        }

        public static float3 ToFloat3(FPVector3 value)
        {
            return new float3(value.X.ToFloat(), value.Y.ToFloat(), value.Z.ToFloat());
        }

        public static quaternion ToQuaternion(FPQuaternion value)
        {
            return new quaternion(value.X.ToFloat(), value.Y.ToFloat(), value.Z.ToFloat(), value.W.ToFloat());
        }
    }
}
