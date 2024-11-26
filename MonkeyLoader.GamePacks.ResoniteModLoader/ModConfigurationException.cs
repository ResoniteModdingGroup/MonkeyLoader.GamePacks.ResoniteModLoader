using System;

namespace ResoniteModLoader
{
    /// <summary>
    /// Represents an <see cref="Exception"/> encountered while loading a mod's configuration file.
    /// </summary>
    public class ModConfigurationException : Exception
    {
        internal ModConfigurationException(string message) : base(message)
        { }

        internal ModConfigurationException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}