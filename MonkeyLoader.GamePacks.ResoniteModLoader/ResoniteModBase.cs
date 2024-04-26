using HarmonyLib;
using MonkeyLoader;
using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using MonkeyLoader.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using Zio;
using Zio.FileSystems;

namespace ResoniteModLoader
{
    /// <summary>
    /// Contains public metadata about a mod.
    /// </summary>
    public abstract class ResoniteModBase : Mod, IMonkey
    {
        private static MonkeyLoader.MonkeyLoader _monkeyLoader = null!;

        private readonly Lazy<string> _fullId;

        /// <inheritdoc/>
        public AssemblyName AssemblyName { get; }

        /// <summary>
        /// Gets the mod's author.
        /// </summary>
        public abstract string Author { get; }

        public bool CanBeDisabled => false;

        /// <inheritdoc/>
        public override string Description => "ResoniteModLoader Mods don't have a description.";

        public bool Enabled
        { get => true; set { } }

        /// <inheritdoc/>
        public bool Failed { get; } = false;

        /// <inheritdoc/>
        public IEnumerable<IFeaturePatch> FeaturePatches => Enumerable.Empty<IFeaturePatch>();

        public override IFileSystem FileSystem { get; } = new MemoryFileSystem()
        {
            Name = "ResoniteModLoader Mod FileSystem"
        };

        public string FullId { get; }

        public Harmony Harmony { get; }

        /// <summary>
        /// Gets an optional hyperlink to the mod's homepage.
        /// </summary>
        public virtual string? Link { get; }

        /// <inheritdoc/>
        public Mod Mod => this;

        /// <summary>
        /// Gets the mod's name. This must be unique.
        /// </summary>
        public abstract string Name { get; }

        /// <inheritdoc/>
        public override Uri? ProjectUrl { get; }

        /// <inheritdoc/>
        public bool Ran { get; } = false;

        public Type Type { get; }

        /// <summary>
        /// Gets the mod's semantic version.
        /// </summary>
        public abstract string Version { get; }

        internal new static Logger Logger { get; private set; } = null!;

        internal static MonkeyLoader.MonkeyLoader MonkeyLoader
        {
            get => _monkeyLoader;
            set
            {
                _monkeyLoader = value;
                Logger = new(value.Logger, "ResoniteModLoader");
            }
        }

        protected ResoniteModBase() : base(MonkeyLoader, false)
        {
            monkeys.Add(this);
            authors.Add(Author);
            Loca
            FullId = $"ResoniteModLoader.{Name}";

            if (Uri.TryCreate(Link, UriKind.Absolute, out var projectUrl))
                ProjectUrl = projectUrl;
        }

        /// <inheritdoc/>
        public int CompareTo(IMonkey other) => Comparer<IMonkey>.Default.Compare(this, other);

        /// <summary>
        /// Gets this mod's current <see cref="ModConfiguration"/>.
        /// <para/>
        /// This will always be the same instance.
        /// </summary>
        /// <returns>This mod's current configuration.</returns>
        public ModConfiguration? GetConfiguration()
        {
            if (!FinishedLoading)
                throw new ModConfigurationException($"GetConfiguration() was called before {Name} was done initializing. Consider calling GetConfiguration() from within OnEngineInit()");

            return loadedResoniteMod?.ModConfiguration;
        }

        /// <inheritdoc/>
        bool IRun.Run() => Run();

        protected abstract bool Run();
    }
}