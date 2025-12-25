using EnumerableToolkit;
using MonkeyLoader;
using MonkeyLoader.Configuration;
using System;
using System.Collections.Generic;

namespace ResoniteModLoader
{
    /// <summary>
    /// Represents an untyped mod configuration key.
    /// </summary>
    public abstract class ModConfigurationKey
    {
        /// <summary>
        /// Gets the human-readable description of this config item. Should be specified by the defining mod.
        /// </summary>
        public string? Description => DescriptionProxy;

        /// <summary>
        /// Gets whether only the owning mod should have access to this config item.
        /// </summary>
        public bool InternalAccessOnly => InternalAccessOnlyProxy;

        /// <summary>
        /// Gets the mod-unique name of this config item. Must be present.
        /// </summary>
        public string Name => NameProxy;

        /// <summary>
        /// Gets the proxied description from the <see cref="ModConfigurationKey{T}.Key"/>.
        /// </summary>
        internal abstract string? DescriptionProxy { get; }

        /// <summary>
        /// Gets the proxied internal access only value from the <see cref="ModConfigurationKey{T}.Key"/>.
        /// </summary>
        internal abstract bool InternalAccessOnlyProxy { get; }

        /// <summary>
        /// Gets the proxied name from the <see cref="ModConfigurationKey{T}.Key"/>.
        /// </summary>
        internal abstract string NameProxy { get; }

        internal abstract IDefiningConfigKey UntypedKey { get; }

        internal ModConfigurationKey()
        { }

        /// <summary>
        /// We only care about key name for non-defining keys.<br/>
        /// For defining keys all of the other properties (default, validator, etc.) also matter.
        /// </summary>
        /// <param name="obj">The other object to compare against.</param>
        /// <returns><c>true</c> if the other object is equal to this.</returns>
        public abstract override bool Equals(object? obj);

        /// <inheritdoc/>
        public abstract override int GetHashCode();

        /// <inheritdoc/>
        public override string ToString()
            => $"ConfigKey Name: {Name}, Description: {Description}, InternalAccessOnly: {InternalAccessOnly}, Type: {ValueType()}, Value: {UntypedKey.GetValue()}";

        /// <summary>
        /// Tries to compute the default value for this key, if a default provider was set.
        /// </summary>
        /// <param name="defaultValue">The computed default value if the return value is <c>true</c>. Otherwise <c>default</c>.</param>
        /// <returns><c>true</c> if the default value was successfully computed.</returns>
        public abstract bool TryComputeDefault(out object? defaultValue);

        /// <summary>
        /// Checks if a value is valid for this configuration item.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c> if the value is valid.</returns>
        public abstract bool Validate(object? value);

        /// <summary>
        /// Get the <see cref="Type"/> of this key's value.
        /// </summary>
        /// <returns>The <see cref="Type"/> of this key's value.</returns>
        public abstract Type ValueType();

        internal void FireOnChanged(object? newValue = null)
        {
            try
            {
                OnChanged?.TryInvokeAll(newValue!);
            }
            catch (AggregateException ex)
            {
                UntypedKey.Config.Logger.Error(() => ex.Format($"Some On Changed event handlers for key [{Name}] threw an exception:"));
            }
        }

        /// <summary>
        /// Called if this <see cref="ModConfigurationKey"/> changed.
        /// </summary>
        public event OnChangedHandler? OnChanged;

        /// <summary>
        /// Delegate for handling configuration changes.
        /// </summary>
        /// <param name="newValue">The new value of the <see cref="ModConfigurationKey"/>.</param>
        public delegate void OnChangedHandler(object? newValue);
    }

    /// <summary>
    /// Represents a typed mod configuration key.
    /// </summary>
    /// <typeparam name="T">The type of this key's value.</typeparam>
    public class ModConfigurationKey<T> : ModConfigurationKey
    {
        private static int _replacementCounter = 0;

        /// <summary>
        /// Gets the internal MonkeyLoader config key.
        /// </summary>
        public DefiningConfigKey<T> Key { get; }

        /// <summary>
        /// Gets or sets the value of this configuration key.
        /// </summary>
        /// <remarks>
        /// When getting, attempts to retrieve the current value assigned to this key, or <c>default(T)</c> if none is set.<br/>
        /// When setting, assigns the provided value to this key and notifies any <see cref="ModConfigurationKey.OnChanged">OnChanged</see> subscribers.
        /// </remarks>
        public T? Value
        {
            get => Key.GetValue();

            // In RML this bypasses the validation check, but ML doesn't let anything set an invalid value.
            // A debate could be had as to whether to use the version that throws or not,
            // but I'm using the one that throws to not let a mod continue with the old value if the new one is invalid.
            set => Key.SetValue(value!);
        }

        /// <inheritdoc/>
        internal override string? DescriptionProxy => Key.Description;

        /// <inheritdoc/>
        internal override bool InternalAccessOnlyProxy => Key.InternalAccessOnly;

        /// <inheritdoc/>
        internal override string NameProxy => Key.Id;

        internal override IDefiningConfigKey UntypedKey => Key;

        private ModConfiguration ModConfiguration => (ModConfiguration)Key.Section;

        /// <summary>
        /// Creates a new instance of the <see cref="ModConfigurationKey{T}"/> class with the given parameters.
        /// </summary>
        /// <param name="name">The mod-unique name of this config item.</param>
        /// <param name="description">The human-readable description of this config item.</param>
        /// <param name="computeDefault">The function that computes a default value for this key. Otherwise <c>default(<typeparamref name="T"/>)</c> will be used.</param>
        /// <param name="internalAccessOnly">If <c>true</c>, only the owning mod should have access to this config item.</param>
        /// <param name="valueValidator">The function that checks if the given value is valid for this configuration item. Otherwise everything will be accepted.</param>
        public ModConfigurationKey(string name, string? description = null, Func<T>? computeDefault = null, bool internalAccessOnly = false, Predicate<T?>? valueValidator = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ModLoader.Logger.Warn(() => $"ModConfigurationKey with description [{description}] has null or whitespace name - using Spacer name!");

                Key = new($"Spacer-{name?.GetHashCode() ?? description?.GetHashCode() ?? _replacementCounter++}",
                    description, computeDefault, internalAccessOnly, valueValidator);

                return;
            }

            Key = new(name, description, computeDefault, internalAccessOnly, valueValidator);

            Key.Changed += OnKeyChanged;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is ModConfigurationKey<T> other && Key.Equals(other.Key);

        /// <inheritdoc/>
        public override int GetHashCode() => Key.GetHashCode();

        /// <inheritdoc/>
        public override bool TryComputeDefault(out object? defaultValue)
            => ((IDefiningConfigKey)Key).TryComputeDefault(out defaultValue);

        /// <summary>
        /// Tries to compute the default value for this key, if a default provider was set.
        /// </summary>
        /// <param name="defaultValue">The computed default value if the return value is <c>true</c>. Otherwise <c>default(T)</c>.</param>
        /// <returns><c>true</c> if the default value was successfully computed.</returns>
        public bool TryComputeDefaultTyped(out T? defaultValue)
            => Key.TryComputeDefault(out defaultValue);

        /// <inheritdoc/>
        public override bool Validate(object? value)
            => ((IDefiningConfigKey)Key).Validate(value);

        /// <summary>
        /// Checks if a value is valid for this configuration item.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c> if the value is valid.</returns>
        public bool ValidateTyped(T? value)
            => Key.Validate(value!);

        /// <inheritdoc/>
        public override Type ValueType() => Key.ValueType;

        private void OnKeyChanged(object sender, ConfigKeyChangedEventArgs<T> configKeyChangedEventArgs)
        {
            FireOnChanged(configKeyChangedEventArgs.NewValue);
            ModConfiguration.FireConfigurationChangedEvent(this, configKeyChangedEventArgs.Label);
        }
    }
}