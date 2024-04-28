using MonkeyLoader;
using MonkeyLoader.Meta;
using MonkeyLoader.NuGet;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zio;
using Zio.FileSystems;

namespace ResoniteModLoader
{
    internal sealed class RmlMod : Mod
    {
        private static readonly Type _resoniteModType = typeof(ResoniteMod);
        private static readonly Uri _rmlIconUrl = new("https://avatars.githubusercontent.com/u/145755526");

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
        public RmlMod(MonkeyLoader.MonkeyLoader loader, string? location, bool isGamePack)
            : base(loader, location, isGamePack)
        {
            FileSystem = new MemoryFileSystem() { Name = $"Dummy FileSystem for {Path.GetFileNameWithoutExtension(location)}" };

            var assembly = Assembly.LoadFile(location);
            var modType = assembly.GetTypes().Single(_resoniteModType.IsAssignableFrom);
            var resoniteMod = (ResoniteMod)Activator.CreateInstance(modType);

            NuGetVersion version;
            if (!NuGetVersion.TryParse(resoniteMod.Version, out version!))
                version = new(1, 0, 0);

            Identity = new PackageIdentity(resoniteMod.AssemblyName, version);

            resoniteMod.Mod = this;

            if (Uri.TryCreate(resoniteMod.Link, UriKind.Absolute, out var projectUrl))
                ProjectUrl = projectUrl;

            authors.Add(resoniteMod.Author);
            monkeys.Add(resoniteMod);
        }

        protected override bool OnLoadEarlyMonkeys() => true;

        protected override bool OnLoadMonkeys() => true;
    }
}