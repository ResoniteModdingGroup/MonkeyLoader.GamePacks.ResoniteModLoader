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
        internal static readonly Dictionary<Assembly, ResoniteMod> AssemblyLookupMap = [];

        private static readonly Type _resoniteModType = typeof(ResoniteMod);
        private static readonly Uri _rmlIconUrl = new("https://avatars.githubusercontent.com/u/145755526");

        private readonly Assembly _assembly;

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
        public RmlMod(MonkeyLoader.MonkeyLoader loader, Assembly assembly)
            : base(loader, assembly.Location, false)
        {
            FileSystem = new MemoryFileSystem() { Name = $"Dummy FileSystem for {assembly.GetName().Name}" };

            _assembly = assembly;
            var modType = assembly.GetTypes().Single(_resoniteModType.IsAssignableFrom);
            var resoniteMod = (ResoniteMod)Activator.CreateInstance(modType)!;

            AssemblyLookupMap.Add(assembly, resoniteMod);

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
            if (assemblyName.Name != _assembly.GetName().FullName)
            {
                assembly = null;
                return false;
            }

            Logger.Debug(() => $"Resolving assembly {assemblyName.Name} to {_assembly.FullName} through RmlMod");
            assembly = _assembly;
            return true;
        }

        ///<inheritdoc/>
        protected override bool OnLoadEarlyMonkeys() => true;

        ///<inheritdoc/>
        protected override bool OnLoadMonkeys() => true;
    }
}