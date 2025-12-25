using System;
using System.Collections.Generic;
using EnumerableToolkit;
using MonkeyLoader;

namespace ResoniteModLoader
{
    /// <summary>
    /// The configuration for a mod. Each mod has zero or one configuration. The configuration object will never be reassigned once initialized.
    /// </summary>
    public class ModConfiguration : IModConfigurationDefinition
    {
        internal ModConfigurationDefinition Definition { get; }

        /// <summary>
        /// Gets the internal <see cref="MonkeyLoader.Configuration.ConfigSection"/>
        /// that actually handles storing the data for this.
        /// </summary>
        internal RmlModConfigSection ConfigSection { get; }

        /// <inheritdoc/>
        public Version Version => Definition.Version;

        /// <inheritdoc/>
        public ISet<ModConfigurationKey> ConfigurationItemDefinitions => Definition.ConfigurationItemDefinitions;

        /// <inheritdoc/>
        public ResoniteModBase Owner => Definition.Owner;

        internal ModConfiguration(ModConfigurationDefinition definition)
        {
            Definition = definition;
            ConfigSection = new(this);
        }

        /// <summary>
        /// Get a value, throwing a <see cref="KeyNotFoundException"/> if the key is not found.
        /// </summary>
        /// <param name="key">The key to get the value for.</param>
        /// <returns>The value for the key.</returns>
        /// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
        public object GetValue(ModConfigurationKey key)
            => ConfigSection.GetDefinedKey(key.UntypedKey).GetValue()!;

        /// <summary>
        /// Get a value, throwing a <see cref="KeyNotFoundException"/> if the key is not found.
        /// </summary>
        /// <typeparam name="T">The type of the key's value.</typeparam>
        /// <param name="key">The key to get the value for.</param>
        /// <returns>The value for the key.</returns>
        /// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
        public T? GetValue<T>(ModConfigurationKey<T> key)
            => ConfigSection.GetDefinedKey(key.Key).GetValue();

        /// <summary>
        /// Checks if the given key is defined in this config.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns><c>true</c> if the key is defined.</returns>
        public bool IsKeyDefined(ModConfigurationKey key)
            => ConfigSection.TryGetDefinedKey(key.UntypedKey, out _);

        /// <summary>
        /// Persist this configuration to disk.<br/>
        /// This method is not called automatically.
        /// </summary>
        /// <param name="saveDefaultValues">If <c>true</c>, default values will also be persisted.</param>
        /// <remarks>
        /// Saving too often may result in save calls being debounced, with only the latest save call being used after a delay.
        /// </remarks>
#pragma warning disable IDE0060 // Remove unused parameter

        public void Save(bool saveDefaultValues = false)
            => ConfigSection.Config.Save();

#pragma warning restore IDE0060 // Remove unused parameter

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
            => ConfigSection.GetDefinedKey(key.UntypedKey).SetValue(value, eventLabel);

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
            => ConfigSection.GetDefinedKey(key.Key).SetValue(value, eventLabel);

        /// <summary>
        /// Tries to get a value, returning <c>default</c> if the key is not found.
        /// </summary>
        /// <param name="key">The key to get the value for.</param>
        /// <param name="value">The value if the return value is <c>true</c>, or <c>default</c> if <c>false</c>.</param>
        /// <returns><c>true</c> if the value was read successfully.</returns>
        public bool TryGetValue(ModConfigurationKey key, out object? value)
        {
            if (ConfigSection.TryGetDefinedKey(key.UntypedKey, out var definingKey))
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
            if (ConfigSection.TryGetDefinedKey(key.Key, out var definingKey))
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
        public bool Unset(ModConfigurationKey key)
            => ConfigSection.GetDefinedKey(key.UntypedKey).Unset();

        internal void FireConfigurationChangedEvent(ModConfigurationKey key, string? label)
        {
            var eventData = new ConfigurationChangedEvent(this, key, label);

            try
            {
                OnAnyConfigurationChanged?.TryInvokeAll(eventData);
            }
            catch (AggregateException ex)
            {
                ConfigSection.Config.Logger.Error(() => ex.Format($"An OnAnyConfigurationChanged event subscriber threw an exception:"));
            }

            try
            {
                OnThisConfigurationChanged?.TryInvokeAll(eventData);
            }
            catch (AggregateException ex)
            {
                ConfigSection.Config.Logger.Error(() => ex.Format($"An OnThisConfigurationChanged event subscriber threw an exception:"));
            }
        }

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
}