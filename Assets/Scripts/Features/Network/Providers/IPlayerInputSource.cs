using Features.Network.Components;

namespace Features.Network.Providers
{
    /// <summary>Where the client's input for a tick comes from. Registered by Meta; tests script it.</summary>
    public interface IPlayerInputSource
    {
        /// <summary>Called once per produced tick (a jump reuses one sample for every tick it fills).</summary>
        PlayerInput Sample();
    }
}
