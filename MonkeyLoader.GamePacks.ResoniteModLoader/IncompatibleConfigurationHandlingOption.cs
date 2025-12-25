using MonkeyLoader.Configuration;

namespace ResoniteModLoader
{
    /// <summary>
    /// Defines options for the handling of incompatible configuration versions.
    /// </summary>
    public enum IncompatibleConfigurationHandlingOption
    {
        /// <summary>
        /// Fail to read the config, and block saving over the config on disk.
        /// </summary>
        ERROR = IncompatibleConfigHandling.Error,

        /// <summary>
        /// Destroy the saved config and start over from scratch.
        /// </summary>
        CLOBBER = IncompatibleConfigHandling.Clobber,

        /// <summary>
        /// Ignore the version number and attempt to load the config from disk.
        /// </summary>
        FORCELOAD = IncompatibleConfigHandling.ForceLoad,
    }
}