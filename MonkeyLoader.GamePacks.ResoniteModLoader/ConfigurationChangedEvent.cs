namespace ResoniteModLoader
{
    /// <summary>
    /// Represents the data for the <see cref="ModConfiguration.OnThisConfigurationChanged"/> and <see cref="ModConfiguration.OnAnyConfigurationChanged"/> events.
    /// </summary>
    public class ConfigurationChangedEvent
    {
        /// <summary>
        /// The <see cref="ModConfiguration"/> in which the change occured.
        /// </summary>
        public ModConfiguration Config { get; }

        /// <summary>
        /// The specific <see cref="ModConfigurationKey{T}"/> who's value changed.
        /// </summary>
        public ModConfigurationKey Key { get; }

        /// <summary>
        /// A custom label that may be set by whoever changed the configuration.
        /// </summary>
        public string? Label { get; }

        internal ConfigurationChangedEvent(ModConfiguration config, ModConfigurationKey key, string? label)
        {
            Config = config;
            Key = key;
            Label = label;
        }
    }
}