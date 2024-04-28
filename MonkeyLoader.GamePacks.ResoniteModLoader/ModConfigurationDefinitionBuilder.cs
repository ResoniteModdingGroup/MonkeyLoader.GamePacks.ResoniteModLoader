using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ResoniteModLoader
{
    /// <summary>
    /// Represents a fluent configuration interface to define mod configurations.
    /// </summary>
    public class ModConfigurationDefinitionBuilder
    {
        private static readonly Type _modConfigKeyType = typeof(ModConfigurationKey);
        private readonly HashSet<ModConfigurationKey> _keys = new();
        private readonly ResoniteModBase _owner;
        private bool _autoSaveConfig = true;
        private Version _configVersion = new(1, 0, 0);

        internal ModConfigurationDefinitionBuilder(ResoniteModBase owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Sets the AutoSave property of this configuration definition. Default is <c>true</c>.
        /// </summary>
        /// <param name="autoSave">If <c>false</c>, the config will not be autosaved on Resonite close.</param>
        /// <returns>This builder.</returns>
        public ModConfigurationDefinitionBuilder AutoSave(bool autoSave)
        {
            _autoSaveConfig = autoSave;

            return this;
        }

        /// <summary>
        /// Adds a new key to this configuration definition.
        /// </summary>
        /// <param name="key">A configuration key.</param>
        /// <returns>This builder.</returns>
        public ModConfigurationDefinitionBuilder Key(ModConfigurationKey key)
        {
            _keys.Add(key);

            return this;
        }

        /// <summary>
        /// Sets the semantic version of this configuration definition. Default is 1.0.0.
        /// </summary>
        /// <param name="version">The config's semantic version.</param>
        /// <returns>This builder.</returns>
        public ModConfigurationDefinitionBuilder Version(Version version)
        {
            _configVersion = version;

            return this;
        }

        /// <summary>
        /// Sets the semantic version of this configuration definition. Default is 1.0.0.
        /// </summary>
        /// <param name="version">The config's semantic version, as a string.</param>
        /// <returns>This builder.</returns>
        public ModConfigurationDefinitionBuilder Version(string version)
        {
            _configVersion = new Version(version);

            return this;
        }

        internal ModConfigurationDefinition? Build()
        {
            if (_keys.Count > 0)
                return new ModConfigurationDefinition(_owner, _configVersion, _keys, _autoSaveConfig);

            return null;
        }

        internal void ProcessAttributes()
        {
            AccessTools.GetDeclaredFields(_owner.GetType())
                .Where(field => field.GetCustomAttribute<AutoRegisterConfigKeyAttribute>() is not null)
                .Do(ProcessField);
        }

        private void ProcessField(FieldInfo field)
        {
            if (!_modConfigKeyType.IsAssignableFrom(field.FieldType))
            {
                // wrong type
                _owner.Logger.Warn(() => $"{_owner.Name} had an [AutoRegisterConfigKey] field of the wrong type: {field}");
                return;
            }

            var fieldValue = (ModConfigurationKey)field.GetValue(field.IsStatic ? null : _owner);
            _keys.Add(fieldValue);
        }
    }
}