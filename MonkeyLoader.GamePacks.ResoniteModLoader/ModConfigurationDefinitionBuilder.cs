using FrooxEngine;
using HarmonyLib;
using MonkeyLoader.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ResoniteModLoader
{
    /// <summary>
    /// Represents a fluent configuration interface to define mod configurations.
    /// </summary>
    public class ModConfigurationDefinitionBuilder
    {
        private static readonly MethodInfo _addRangeComponentMethod = AccessTools.Method(typeof(ModConfigurationDefinitionBuilder), nameof(AddRangeComponent));
        private static readonly string[] _definitiveEnabledToggles = new[] { "enabled", "mod enabled", "mod_enabled", "is_enabled" };
        private static readonly string[] _indicativeEnabledToggles = new[] { "enabled" };
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

        internal bool TryGetEnabledToggle([NotNullWhen(true)] out DefiningConfigKey<bool>? enabledToggleKey, bool remove = true)
        {
            enabledToggleKey = null;
            ModConfigurationKey? enabledToggle = null;

            var potentialKeys = _keys.OfType<ModConfigurationKey<bool>>().ToArray();

            foreach (var definitiveEnabledToggle in _definitiveEnabledToggles)
            {
                if (potentialKeys.FirstOrDefault(key => key.Name.Equals(definitiveEnabledToggle, StringComparison.OrdinalIgnoreCase)) is ModConfigurationKey enabledKey)
                {
                    enabledToggle = enabledKey;
                    break;
                }
            }

            if (enabledToggle is null)
            {
                potentialKeys = potentialKeys
                    .Where(key => _indicativeEnabledToggles
                        .Any(name => key.Name.Contains(name, StringComparison.OrdinalIgnoreCase)))
                    .ToArray();

                if (potentialKeys.Length != 1)
                    return false;

                enabledToggle = potentialKeys[0];
            }

            enabledToggleKey = (DefiningConfigKey<bool>)enabledToggle.UntypedKey;

            if (remove)
                _keys.Remove(enabledToggle);

            return true;
        }

        private static void AddRangeComponent<T>(DefiningConfigKey<T> key, T min, T max)
        {
            var rangeComponent = new ConfigKeyRange<T>(min, max, null);
            key.Components.Add(rangeComponent);
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

            if (field.GetCustomAttribute<RangeAttribute>() is RangeAttribute rangeAttribute)
            {
                _owner.Logger.Info(() => $"Found FrooxEngine.RangeAttribute on field {field.Name}");
                var min = Convert.ChangeType(rangeAttribute.Min, fieldValue.ValueType());
                var max = Convert.ChangeType(rangeAttribute.Max, fieldValue.ValueType());
                _addRangeComponentMethod.MakeGenericMethod(fieldValue.ValueType()).Invoke(null, new object[] { fieldValue.UntypedKey, min, max });
                _owner.Logger.Info(() => $"Added ConfigKeyRange<{fieldValue.ValueType().Name}> component.");
            }

            _keys.Add(fieldValue);
        }
    }
}