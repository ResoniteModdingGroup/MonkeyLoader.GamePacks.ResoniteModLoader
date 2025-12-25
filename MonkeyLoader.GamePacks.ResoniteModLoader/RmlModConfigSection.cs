using System;
using System.Collections.Generic;
using System.Linq;
using MonkeyLoader.Configuration;

namespace ResoniteModLoader
{
    /// <summary>
    /// Maps between the RML <see cref="ResoniteModLoader.ModConfiguration"/> and ML <see cref="ConfigSection"/>.
    /// </summary>
    /// <remarks>
    /// This intermediate step is necessary to hide ML's implementation details from RML consumers.
    /// </remarks>
    internal sealed class RmlModConfigSection : ConfigSection
    {
        public override string Description => "RML Mod Config";

        public override string Id => "values";

        public override Version Version => ModConfiguration.Definition.Version;

        /// <summary>
        /// Gets the RML Mod Configuration that uses this as a mapper.
        /// </summary>
        internal ModConfiguration ModConfiguration { get; }

        internal RmlModConfigSection(ModConfiguration modConfiguration)
        {
            ModConfiguration = modConfiguration;
        }

        protected override IEnumerable<IDefiningConfigKey> GetConfigKeys()
            => ModConfiguration.Definition.ConfigurationItemDefinitions.Select(item => item.UntypedKey);

        protected override IncompatibleConfigHandling HandleIncompatibleVersions(Version serializedVersion)
        {
            if (ModConfiguration.Definition.Owner is ResoniteMod resoniteMod)
                return (IncompatibleConfigHandling)resoniteMod.HandleIncompatibleConfigurationVersions(serializedVersion, Version);

            return base.HandleIncompatibleVersions(serializedVersion);
        }
    }
}