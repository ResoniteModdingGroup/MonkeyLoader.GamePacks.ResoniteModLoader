﻿using HarmonyLib;
using MonkeyLoader;
using MonkeyLoader.Configuration;
using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using MonkeyLoader.Patching;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResoniteModLoader
{
    public abstract class ResoniteModBase : IMonkey
    {
        private Mod _mod;

        Mod INestedIdentifiable<Mod>.Parent => _mod;

        IIdentifiable INestedIdentifiable.Parent => _mod;

        /// <inheritdoc/>
        public AssemblyName AssemblyName { get; }

        /// <summary>
        /// Gets the mod's author.
        /// </summary>
        public abstract string Author { get; }

        /// <inheritdoc/>
        public bool CanBeDisabled => false;

        /// <inheritdoc/>
        public Config Config => Mod.Config;

        /// <inheritdoc/>
        public bool Enabled
        {
            get => true;
            set { }
        }

        /// <inheritdoc/>
        public bool Failed { get; private set; }

        /// <inheritdoc/>
        public IEnumerable<IFeaturePatch> FeaturePatches { get; }

        /// <inheritdoc/>
        public string FullId { get; }

        /// <inheritdoc/>
        public Harmony Harmony { get; }

        /// <inheritdoc/>
        public string Id => Name;

        /// <summary>
        /// Gets an optional hyperlink to the mod's homepage.
        /// </summary>
        public virtual string? Link { get; }

        /// <inheritdoc/>
        public Logger Logger => _mod.Logger;

        /// <inheritdoc/>
        public Mod Mod
        {
            get => _mod;

            [MemberNotNull(nameof(_mod))]
            internal set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));

                if (ReferenceEquals(_mod, value))
                    return;

                if (_mod is not null)
                    throw new InvalidOperationException("Can't assign a different mod to a monkey!");

                _mod = value;
            }
        }

        /// <summary>
        /// Gets the mod's name. This must be unique.
        /// </summary>
        public abstract string Name { get; }

        /// <inheritdoc/>
        public bool Ran { get; private set; }

        /// <inheritdoc/>
        public bool ShutdownFailed { get; private set; }

        /// <inheritdoc/>
        public bool ShutdownRan { get; private set; }

        /// <inheritdoc/>
        public Type Type { get; }

        /// <summary>
        /// Gets the mod's semantic version.
        /// </summary>
        public abstract string Version { get; }

        protected abstract ModConfiguration? Configuration { get; }

        protected ResoniteModBase()
        {
            Type = GetType();
            AssemblyName = new(Type.Assembly.GetName().Name);

            FullId = $"RML.{Name}";
        }

        /// <inheritdoc/>
        public int CompareTo(IMonkey other) => Monkey.AscendingComparer.Compare(this, other);

        /// <summary>
        /// Gets this mod's current <see cref="ModConfiguration" />.
        /// <para />
        /// This will always be the same instance.
        /// </summary>
        /// <returns>This mod's current configuration.</returns>
        public ModConfiguration? GetConfiguration() => Configuration;

        /// <inheritdoc/>
        public abstract bool Run();

        /// <inheritdoc/>
        public bool Shutdown(bool applicationExiting)
        {
            if (ShutdownRan)
                throw new InvalidOperationException("A monkey's Shutdown() method must only be called once!");

            ShutdownRan = true;

            Logger.Debug(() => "Running OnShutdown!");
            OnShuttingDown(applicationExiting);

            Logger.Trace(() => $"RML Mods don't do anything on Shutdown");

            OnShutdownDone(applicationExiting);
            Logger.Debug(() => "OnShutdown done!");

            return !ShutdownFailed;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <i>Format:</i> <c>{<see cref="Mod">Mod</see>.<see cref="Mod.Title">Title</see>}/{<see cref="Name">Name</see>}</c>
        /// </remarks>
        public override string ToString() => $"{Mod.Title}/{Name}";

        private void OnShutdownDone(bool applicationExiting)
        {
            try
            {
                ShutdownDone?.TryInvokeAll(this, applicationExiting);
            }
            catch (AggregateException ex)
            {
                Logger.Error(() => ex.Format($"Some {nameof(ShutdownDone)} event subscriber(s) threw an exception:"));
            }
        }

        private void OnShuttingDown(bool applicationExiting)
        {
            try
            {
                ShuttingDown?.TryInvokeAll(this, applicationExiting);
            }
            catch (AggregateException ex)
            {
                Logger.Error(() => ex.Format($"Some {nameof(ShuttingDown)} event subscriber(s) threw an exception:"));
            }
        }

        /// <inheritdoc/>
        public event ShutdownHandler? ShutdownDone;

        /// <inheritdoc/>
        public event ShutdownHandler? ShuttingDown;
    }
}