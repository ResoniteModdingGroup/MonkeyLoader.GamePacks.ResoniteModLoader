using System;
using System.Collections.Generic;

namespace ResoniteModLoader
{
    /// <summary>
    /// Defines a mod configuration. This should be defined by a <see cref="ResoniteMod"/> using the <see cref="ResoniteMod.DefineConfiguration(ModConfigurationDefinitionBuilder)"/> method.
    /// </summary>
    public class ModConfigurationDefinition : IModConfigurationDefinition
    {
        internal readonly HashSet<ModConfigurationKey> ConfigurationItems;
        internal bool AutoSave;

        /// <inheritdoc/>
        // clone the collection because I don't trust giving public API users shallow copies one bit
        public ISet<ModConfigurationKey> ConfigurationItemDefinitions
            => new HashSet<ModConfigurationKey>(ConfigurationItems);

        /// <inheritdoc/>
        public ResoniteModBase Owner { get; private set; }

        /// <inheritdoc/>
        public Version Version { get; private set; }

        /// <summary>
        /// Creates a new <see cref="ModConfiguration"/> definition.
        /// </summary>
        /// <param name="owner">The mod owning the config.</param>
        /// <param name="configVersion">The version of the config.</param>
        /// <param name="keys">The config keys for the config.</param>
        /// <param name="autoSaveConfig">Whether to automatically save the config.</param>
        public ModConfigurationDefinition(ResoniteModBase owner, Version configVersion, HashSet<ModConfigurationKey> keys, bool autoSaveConfig)
        {
            Owner = owner;
            Version = configVersion;
            ConfigurationItems = [.. keys];
            AutoSave = autoSaveConfig;
        }
    }
}