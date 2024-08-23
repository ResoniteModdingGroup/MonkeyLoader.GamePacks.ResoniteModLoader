using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EnumerableToolkit;
using FrooxEngine;
using HarmonyLib;
using MonkeyLoader;
using MonkeyLoader.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ResoniteModLoader
{
    /// <summary>
    /// Represents an interface for mod configurations.
    /// </summary>
    public interface IModConfigurationDefinition
    {
        /// <summary>
        /// Gets the set of configuration keys defined in this configuration definition.
        /// </summary>
        ISet<ModConfigurationKey> ConfigurationItemDefinitions { get; }

        /// <summary>
        /// Gets the mod that owns this configuration definition.
        /// </summary>
        ResoniteModBase Owner { get; }

        /// <summary>
        /// Gets the semantic version for this configuration definition. This is used to check if the defined and saved configs are compatible.
        /// </summary>
        Version Version { get; }
    }

    /// <summary>
    /// Defines options for the handling of incompatible configuration versions.
    /// </summary>
    public enum IncompatibleConfigurationHandlingOption
    {
        /// <summary>
        /// Fail to read the config, and block saving over the config on disk.
        /// </summary>
        ERROR,

        /// <summary>
        /// Destroy the saved config and start over from scratch.
        /// </summary>
        CLOBBER,

        /// <summary>
        /// Ignore the version number and attempt to load the config from disk.
        /// </summary>
        FORCELOAD,
    }

    /// <summary>
    /// The configuration for a mod. Each mod has zero or one configuration. The configuration object will never be reassigned once initialized.
    /// </summary>
    public class ModConfiguration : ConfigSection, IModConfigurationDefinition
    {
        private readonly ModConfigurationDefinition _definition;

        /// <inheritdoc/>
        public ISet<ModConfigurationKey> ConfigurationItemDefinitions => _definition.ConfigurationItemDefinitions;

        /// <inheritdoc/>
        public override string Description => "RML Mod Config";

        /// <inheritdoc/>
        public override string Id => "values";

        /// <inheritdoc/>
        public ResoniteModBase Owner => _definition.Owner;

        /// <inheritdoc/>
        public override Version Version => _definition.Version;

        internal ModConfiguration(ModConfigurationDefinition definition)
        {
            _definition = definition;
        }

        /// <summary>
        /// Get a value, throwing a <see cref="KeyNotFoundException"/> if the key is not found.
        /// </summary>
        /// <param name="key">The key to get the value for.</param>
        /// <returns>The value for the key.</returns>
        /// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
        public object GetValue(ModConfigurationKey key) => GetDefinedKey(key.UntypedKey).GetValue()!;

        /// <summary>
        /// Get a value, throwing a <see cref="KeyNotFoundException"/> if the key is not found.
        /// </summary>
        /// <typeparam name="T">The type of the key's value.</typeparam>
        /// <param name="key">The key to get the value for.</param>
        /// <returns>The value for the key.</returns>
        /// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
        public T? GetValue<T>(ModConfigurationKey<T> key) => GetDefinedKey(key.Key).GetValue();

        /// <summary>
        /// Checks if the given key is defined in this config.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns><c>true</c> if the key is defined.</returns>
        public bool IsKeyDefined(ModConfigurationKey key) => TryGetDefinedKey(key.UntypedKey, out _);

        /// <summary>
        /// Persist this configuration to disk.<br/>
        /// This method is not called automatically.
        /// </summary>
        /// <param name="saveDefaultValues">If <c>true</c>, default values will also be persisted.</param>
        /// <remarks>
        /// Saving too often may result in save calls being debounced, with only the latest save call being used after a delay.
        /// </remarks>
        public void Save(bool saveDefaultValues = false) => Config.Save();

        /// <summary>
        /// Sets a configuration value for the given key, throwing a <see cref="KeyNotFoundException"/> if the key is not found
        /// or an <see cref="ArgumentException"/> if the value is not valid for it.
        /// </summary>
        /// <param name="key">The key to get the value for.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="eventLabel">A custom label you may assign to this change event.</param>
        /// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
        /// <exception cref="ArgumentException">The new value is not valid for the given key.</exception>
        public void Set(ModConfigurationKey key, object? value, string? eventLabel = null)
            => GetDefinedKey(key.UntypedKey).SetValue(value, eventLabel);

        /// <summary>
        /// Sets a configuration value for the given key, throwing a <see cref="KeyNotFoundException"/> if the key is not found
        /// or an <see cref="ArgumentException"/> if the value is not valid for it.
        /// </summary>
        /// <typeparam name="T">The type of the key's value.</typeparam>
        /// <param name="key">The key to get the value for.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="eventLabel">A custom label you may assign to this change event.</param>
        /// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
        /// <exception cref="ArgumentException">The new value is not valid for the given key.</exception>
        public void Set<T>(ModConfigurationKey<T> key, T value, string? eventLabel = null)
            => GetDefinedKey(key.Key).SetValue(value, eventLabel);

        /// <summary>
        /// Tries to get a value, returning <c>default</c> if the key is not found.
        /// </summary>
        /// <param name="key">The key to get the value for.</param>
        /// <param name="value">The value if the return value is <c>true</c>, or <c>default</c> if <c>false</c>.</param>
        /// <returns><c>true</c> if the value was read successfully.</returns>
        public bool TryGetValue(ModConfigurationKey key, out object? value)
        {
            if (TryGetDefinedKey(key.UntypedKey, out var definingKey))
            {
                value = definingKey.GetValue();
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Tries to get a value, returning <c>default(<typeparamref name="T"/>)</c> if the key is not found.
        /// </summary>
        /// <param name="key">The key to get the value for.</param>
        /// <param name="value">The value if the return value is <c>true</c>, or <c>default</c> if <c>false</c>.</param>
        /// <returns><c>true</c> if the value was read successfully.</returns>
        public bool TryGetValue<T>(ModConfigurationKey<T> key, out T? value)
        {
            if (TryGetDefinedKey(key.Key, out var definingKey))
            {
                value = definingKey.GetValue();
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Removes a configuration value, throwing a <see cref="KeyNotFoundException"/> if the key is not found.
        /// </summary>
        /// <param name="key">The key to remove the value for.</param>
        /// <returns><c>true</c> if a value was successfully found and removed, <c>false</c> if there was no value to remove.</returns>
        /// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
        public bool Unset(ModConfigurationKey key) => GetDefinedKey(key.UntypedKey).Unset();

        internal void FireConfigurationChangedEvent(ModConfigurationKey key, string? label)
        {
            var eventData = new ConfigurationChangedEvent(this, key, label);

            try
            {
                OnAnyConfigurationChanged?.TryInvokeAll(eventData);
            }
            catch (AggregateException ex)
            {
                Config.Logger.Error(() => ex.Format($"An OnAnyConfigurationChanged event subscriber threw an exception:"));
            }

            try
            {
                OnThisConfigurationChanged?.TryInvokeAll(eventData);
            }
            catch (AggregateException ex)
            {
                Config.Logger.Error(() => ex.Format($"An OnThisConfigurationChanged event subscriber threw an exception:"));
            }
        }

        /// <inheritdoc/>
        protected override IEnumerable<IDefiningConfigKey> GetConfigKeys()
            => _definition.ConfigurationItemDefinitions.Select(item => item.UntypedKey);

        /// <summary>
        /// Called if any config value for any mod changed.
        /// </summary>
        public static event ConfigurationChangedHandler? OnAnyConfigurationChanged;

        /// <summary>
        /// The delegate that is called for configuration change events.
        /// </summary>
        /// <param name="configurationChangedEvent">The event containing details about the configuration change</param>
        public delegate void ConfigurationChangedHandler(ConfigurationChangedEvent configurationChangedEvent);

        /// <summary>
        /// Called if one of the values in this mod's config changed.
        /// </summary>
        public event ConfigurationChangedHandler? OnThisConfigurationChanged;
    }

    /// <summary>
    /// Defines a mod configuration. This should be defined by a <see cref="ResoniteMod"/> using the <see cref="ResoniteMod.DefineConfiguration(ModConfigurationDefinitionBuilder)"/> method.
    /// </summary>
    public class ModConfigurationDefinition : IModConfigurationDefinition
    {
        internal readonly HashSet<ModConfigurationKey> ConfigurationItems;
        internal bool AutoSave;

        /// <inheritdoc/>
        public ISet<ModConfigurationKey> ConfigurationItemDefinitions
        {
            // clone the collection because I don't trust giving public API users shallow copies one bit
            get => new HashSet<ModConfigurationKey>(ConfigurationItems);
        }

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
            ConfigurationItems = new(keys);
            AutoSave = autoSaveConfig;
        }
    }

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