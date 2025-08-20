using MonkeyLoader.Meta;
using MonkeyLoader.NuGet;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Zio;
using Zio.FileSystems;
using AssemblyName = MonkeyLoader.AssemblyName;

namespace ResoniteModLoader
{
    internal sealed class RmlMod : Mod
    {
        /// <summary>
        /// Map of Assembly to ResoniteMod, used for logging purposes
        /// </summary>
        internal static readonly Dictionary<Assembly, ResoniteMod> AssemblyLookupMap = new();

        private static readonly Type _resoniteModType = typeof(ResoniteMod);
        private static readonly Uri _rmlIconUrl = new("https://avatars.githubusercontent.com/u/145755526");

        private readonly Assembly _modAssembly;

        ///<inheritdoc/>
        public override string Description => "RML Mods don't have descriptions.";

        ///<inheritdoc/>
        public override IFileSystem FileSystem { get; }

        ///<inheritdoc/>
        public override UPath? IconPath => null;

        ///<inheritdoc/>
        public override Uri? IconUrl => _rmlIconUrl;

        ///<inheritdoc/>
        public override PackageIdentity Identity { get; }

        ///<inheritdoc/>
        public override Uri? ProjectUrl { get; }

        ///<inheritdoc/>
        public override string? ReleaseNotes => null;

        ///<inheritdoc/>
        public override bool SupportsHotReload => false;

        ///<inheritdoc/>
        public override NuGetFramework TargetFramework => NuGetHelper.Framework;

        ///<inheritdoc/>
        public RmlMod(MonkeyLoader.MonkeyLoader loader, string location, bool isGamePack)
            : base(loader, location, isGamePack)
        {
            FileSystem = new MemoryFileSystem() { Name = $"Dummy FileSystem for {Path.GetFileNameWithoutExtension(location)}" };

            _modAssembly = loader.AssemblyLoadStrategy.LoadFile(Path.GetFullPath(location!));
            var modType = _modAssembly.GetTypes().Single(_resoniteModType.IsAssignableFrom);
            var resoniteMod = (ResoniteMod)Activator.CreateInstance(modType)!;

            AssemblyLookupMap.Add(_modAssembly, resoniteMod);

            NuGetVersion version;
            if (!NuGetVersion.TryParse(resoniteMod.Version, out version!))
                version = new(1, 0, 0);

            Identity = new PackageIdentity(resoniteMod.AssemblyName, version);

            resoniteMod.Mod = this;

            if (Uri.TryCreate(resoniteMod.Link, UriKind.Absolute, out var projectUrl))
                ProjectUrl = projectUrl;

            authors.Add(resoniteMod.Author);
            monkeys.Add(resoniteMod);

            resoniteMod.GetConfiguration();

            // Add dependencies after refactoring MKL
            //foreach (var referencedAssembly in assembly.GetReferencedAssemblies())
            //    dependencies.Add(referencedAssembly.Name, new DependencyReference())
        }

        public override bool TryResolveAssembly(AssemblyName assemblyName, [NotNullWhen(true)] out Assembly? assembly)
        {
            if (assemblyName.Name != _modAssembly.GetName().FullName)
            {
                assembly = null;
                return false;
            }

            Logger.Debug(() => $"Resolving assembly {assemblyName.Name} to {_modAssembly.FullName} through RmlMod");
            assembly = _modAssembly;
            return true;
        }

        ///<inheritdoc/>
        protected override bool OnLoadEarlyMonkeys() => true;

        ///<inheritdoc/>
        protected override bool OnLoadMonkeys() => true;
    }
}