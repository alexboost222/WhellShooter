using System;
using Engine.DI;

namespace Engine.Runtime
{
    /// <summary>
    /// A feature's single entry point. Meta lists features in order; nothing else in the
    /// project knows a feature's internals (Docs/01).
    /// </summary>
    /// <remarks>
    /// <b>Order in the list is the contract.</b> A feature builds its systems during
    /// <see cref="Register"/>, so anything it injects from another feature must already have
    /// been registered — that feature has to appear earlier in <c>Features.cs</c>. A miss
    /// throws at boot naming both the feature and the dependency.
    /// </remarks>
    public abstract class Feature
    {
        /// <summary>Available from <see cref="Register"/> onward.</summary>
        protected Container Container { get; private set; }

        /// <summary>Available from <see cref="Register"/> onward.</summary>
        protected Runner Runner { get; private set; }

        /// <summary>
        /// Create this feature's services and systems and hand them to the Runner. Called once
        /// at boot, in list order.
        /// </summary>
        public abstract void Register();

        /// <summary>
        /// Binds the feature and runs <see cref="Register"/>. Called once per feature by the
        /// composition root and nowhere else.
        /// </summary>
        public void Install(Container container, Runner runner)
        {
            Container = container;
            Runner = runner;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            FeatureManifest.Begin(ToString());
#endif

            try
            {
                Register();
            }
            catch (DependencyException exception)
            {
                throw new DependencyException(
                    $"{this} failed to register. If the missing dependency belongs to another feature, " +
                    $"that feature must come earlier in Features.cs.\n{exception.Message}",
                    exception.TargetType, exception.Missing);
            }
            finally
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // In a finally, so a feature that throws still closes its record rather than
                // silently collecting the next feature's registrations.
                FeatureManifest.End();
#endif
            }
        }

        /// <summary>Names this feature in boot logs and failure messages.</summary>
        public override string ToString() => GetType().Name;
    }
}
