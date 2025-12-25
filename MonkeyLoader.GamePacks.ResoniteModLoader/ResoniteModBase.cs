using EnumerableToolkit;
using HarmonyLib;
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
    /// <summary>
    /// Base class for <see cref="ResoniteMod"/>s.
    /// </summary>
    public abstract class ResoniteModBase : IMonkey
    {
        private readonly string _fullId;
        private readonly Harmony _harmony;
        private readonly Type _type;
        private bool _failed;
        private Mod _mod = null!;
        private bool _ran;
        private ShutdownHandler? _shutdownDone;
        private bool _shutdownRan;
        private ShutdownHandler? _shuttingDown;

        AssemblyName IMonkey.AssemblyName => AssemblyName;

        /// <summary>
        /// Gets the mod's author.
        /// </summary>
        public abstract string Author { get; }

        IEnumerable<string> IAuthorable.Authors => Author.Yield();

        bool IMonkey.CanBeDisabled => CanBeDisabled;

        Config IMonkey.Config => Config;

        string? IDisplayable.Description => "None";

        bool IMonkey.Enabled
        {
            get => !CanBeDisabled || EnabledToggle.GetValue();
            set
            {
                if (CanBeDisabled)
                {
                    EnabledToggle.SetValue(value, "SetMonkeyEnabled");
                    return;
                }

                if (!value)
                    throw new NotSupportedException("This monkey can't be disabled!");
            }
        }

        IDefiningConfigKey<bool>? IMonkey.EnabledToggle => EnabledToggle;

        bool IRun.Failed => _failed;

        IEnumerable<IFeaturePatch> IMonkey.FeaturePatches { get; } = [];

        string IIdentifiable.FullId => _fullId;

        Harmony IMonkey.Harmony => _harmony;

        bool IDisplayable.HasDescription => false;

        string IIdentifiable.Id => Name;

        /// <summary>
        /// Gets an optional hyperlink to the mod's homepage.
        /// </summary>
        public virtual string? Link { get; }

        Logger IMonkey.Logger => _mod.Logger;

        Mod IMonkey.Mod => _mod;

        /// <summary>
        /// Gets the mod's name. This must be unique.
        /// </summary>
        public abstract string Name { get; }

        Mod INestedIdentifiable<Mod>.Parent => _mod;

        IIdentifiable INestedIdentifiable.Parent => _mod;

        bool IRun.Ran => _ran;

        bool IShutdown.ShutdownFailed => false;

        bool IShutdown.ShutdownRan => _shutdownRan;

        Type IMonkey.Type => _type;

        /// <summary>
        /// Gets the mod's semantic version.
        /// </summary>
        public abstract string Version { get; }

        /// <inheritdoc/>
        internal AssemblyName AssemblyName { get; }

        /// <inheritdoc/>
        internal Config Config => Mod.Config;

        /// <summary>
        /// Gets the Mod's configuration.
        /// </summary>
        internal abstract ModConfiguration? Configuration { get; }

        internal IDefiningConfigKey<bool>? EnabledToggle { get; set; }

        /// <inheritdoc cref="IMonkey.Logger"/>
        internal Logger Logger => _mod.Logger;

        /// <inheritdoc cref="IMonkey.Mod"/>
        internal Mod Mod
        {
            get => _mod;

            [MemberNotNull(nameof(_mod))]
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                if (ReferenceEquals(_mod, value))
                    return;

                if (_mod is not null)
                    throw new InvalidOperationException("Can't assign a different mod to a monkey!");

                _mod = value;
            }
        }

        /// <inheritdoc cref="IMonkey.CanBeDisabled"/>
        [MemberNotNullWhen(true, nameof(EnabledToggle))]
        private bool CanBeDisabled => EnabledToggle is not null;

        /// <summary>
        /// Creates a new Resonite Mod instance.
        /// </summary>
        protected ResoniteModBase()
        {
            _type = GetType();
            AssemblyName = new(_type.Assembly.GetName().Name!);

            _fullId = $"RML.{Name}";
            _harmony = new(_fullId);
        }

        int IComparable<IMonkey>.CompareTo(IMonkey? other)
            => Monkey.AscendingComparer.Compare(this, other);

        /// <summary>
        /// Gets this mod's current <see cref="ModConfiguration" />.
        /// <para />
        /// This will always be the same instance.
        /// </summary>
        /// <returns>This mod's current configuration.</returns>
        public ModConfiguration? GetConfiguration() => Configuration;

        bool IAuthorable.HasAuthor(string name)
            => string.Equals(name, Author, StringComparison.InvariantCultureIgnoreCase);

        bool IRun.Run()
        {
            _ran = true;
            _failed = !Run();

            return !_failed;
        }

        bool IShutdown.Shutdown(bool applicationExiting)
        {
            if (_shutdownRan)
                throw new InvalidOperationException("A monkey's Shutdown() method must only be called once!");

            _shutdownRan = true;

            Logger.Debug(() => "Running OnShutdown!");
            OnShuttingDown(applicationExiting);

            Logger.Trace(() => $"RML Mods don't do anything on Shutdown");

            OnShutdownDone(applicationExiting);
            Logger.Debug(() => "OnShutdown done!");

            return true;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <i>Format:</i> <c>{<see cref="Mod">Mod</see>.<see cref="Mod.Title">Title</see>}/{<see cref="Name">Name</see>}</c>
        /// </remarks>
        public override string ToString() => $"{Mod.Title}/{Name}";

        /// <inheritdoc cref="IRun.Run"/>
        internal abstract bool Run();

        private void OnShutdownDone(bool applicationExiting)
        {
            try
            {
                _shutdownDone?.TryInvokeAll(this, applicationExiting);
            }
            catch (AggregateException ex)
            {
                Logger.Error(() => ex.Format($"Some ShutdownDone event subscriber(s) threw an exception:"));
            }
        }

        private void OnShuttingDown(bool applicationExiting)
        {
            try
            {
                _shuttingDown?.TryInvokeAll(this, applicationExiting);
            }
            catch (AggregateException ex)
            {
                Logger.Error(() => ex.Format($"Some ShuttingDown event subscriber(s) threw an exception:"));
            }
        }

        event ShutdownHandler? IShutdown.ShutdownDone
        {
            add => _shutdownDone += value;
            remove => _shutdownDone -= value;
        }

        event ShutdownHandler? IShutdown.ShuttingDown
        {
            add => _shuttingDown += value;
            remove => _shuttingDown -= value;
        }
    }
}