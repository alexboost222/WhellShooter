namespace Features.PlayerFeature.Messages
{
    /// <summary>
    /// The settings screen changed the mouse sensitivity. Consumed by <c>PlayerSettingsSystem</c>,
    /// which applies and saves it.
    /// </summary>
    public struct SetLookSensitivity
    {
        /// <summary>Degrees turned per pixel of pointer movement.</summary>
        public float DegreesPerPixel;
    }
}
