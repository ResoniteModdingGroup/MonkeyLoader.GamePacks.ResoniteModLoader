using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using MonkeyLoader;
using MonkeyLoader.Meta;
using MonkeyLoader.NuGet;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using MonkeyLoader.Resonite.Features.FrooxEngine;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ResoniteModLoader
{
    /// <summary>
    /// Contains the actual mod loader.
    /// </summary>
    [HarmonyPatchCategory(nameof(ModLoader))]
    [HarmonyPatch(typeof(EngineInitializer), nameof(EngineInitializer.InitializeFrooxEngine))]
    public sealed class ModLoader : ResoniteMonkey<ModLoader>
    {
        /// <summary>
        /// ResoniteModLoader's version
        /// </summary>
        public static readonly string VERSION = VERSION_CONSTANT;

        internal const string VERSION_CONSTANT = "4.0.0";

        private static readonly Lazy<bool> _isHeadless = new(()
            => AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.GetName().Name?.StartsWith("FrooxEngine") ?? false)
                .Any(assembly => assembly.DefinedTypes
                    .Any(type => type.Namespace == "FrooxEngine.Headless")));

        /// <summary>
        /// Gets whether this is running on a headless client.
        /// </summary>
        /// <value><c>true</c> if ResoniteModLoader was loaded by a headless; otherwise, <c>false</c>.</value>
        public static bool IsHeadless => _isHeadless.Value;

        /// <inheritdoc/>
        public override string Name { get; } = "ModLoader";

        /// <summary>
        /// Allows reading metadata for all loaded mods
        /// </summary>
        /// <returns>A new list containing each loaded mod</returns>
        public static IEnumerable<ResoniteModBase> Mods()
        {
            return Mod.Loader.RegularMods
                .OfType<RmlMod>()
                .SelectMany(rmlMod => rmlMod.Monkeys)
                .Cast<ResoniteModBase>();
        }

        /// <inheritdoc/>
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches()
        {
            yield return new FeaturePatch<EngineInitialization>(PatchCompatibility.HookOnly);
        }

        /// <inheritdoc/>
        protected override bool OnEngineInit()
        {
            return base.OnEngineInit();
        }

        /// <inheritdoc/>
        protected override bool OnEngineReady() => true;

        /// <inheritdoc/>
        protected override bool OnLoaded() => base.OnEngineReady();

        private static IEnumerable<string> GetAssemblyPaths(string root)
        {
            if (!Directory.Exists(root))
                yield break;

            foreach (var file in Directory.EnumerateFiles(root, "*.dll"))
            {
                if (Path.GetExtension(file).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                    yield return file;
            }
        }

        [HarmonyPostfix]
        private static async Task InitializeFrooxEnginePostfixAsync(Task __result)
        {
            await __result;

            Engine.Current?.InitProgress?.SetSubphase("Loading RML Libraries...", true);

            try
            {
                foreach (var file in GetAssemblyPaths("rml_libs"))
                {
                    Engine.Current?.InitProgress?.SetSubphase(Path.GetFileNameWithoutExtension(file), true);

                    try
                    {
                        var assembly = Mod.Loader.AssemblyLoadStrategy.LoadFile(Path.GetFullPath(file));
                        var name = assembly.GetName();

                        Mod.Loader.NuGet.Add(new LoadedNuGetPackage(new PackageIdentity(name.Name, new NuGetVersion(name.Version!)), NuGetHelper.Framework));

                        Logger.Info(() => $"Loaded library {name.Name}.{name.Version} from rml_libs: {file}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(() => ex.Format($"Failed to load library from rml_libs: {file}"));
                    }
                }

                Engine.Current?.InitProgress?.SetSubphase("Collecting RML Mods...", true);

                var rmlMods = await LoadModsAsync().ToArrayAsync();

                Engine.Current?.InitProgress?.SetSubphase("Running RML Mods...", true);
                await Task.Run(() => Mod.Loader.RunMods(rmlMods));
                Engine.Current?.InitProgress?.SetSubphase("Done with RML", true);
            }
            catch (Exception ex)
            {
                Logger.Error(() => ex.Format("Exception in execution hook!"));
                Engine.Current?.InitProgress?.SetSubphase("Error running RML Mods!", true);
            }
        }

        private static async IAsyncEnumerable<RmlMod> LoadModsAsync()
        {
            var modAssemblies = new List<Assembly>();

            foreach (var file in GetAssemblyPaths("rml_mods"))
            {
                try
                {
                    var modAssembly = await Task.Run(() => Mod.Loader.AssemblyLoadStrategy.LoadFile(Path.GetFullPath(file!)));
                    modAssemblies.Add(modAssembly);
                }
                catch (Exception ex)
                {
                    Logger.Warn(() => ex.Format($"Failed to load assembly from rml_mods: {file}"));
                }
            }

            foreach (var modAssembly in modAssemblies)
            {
                var fileName = Path.GetFileName(modAssembly.Location);
                Engine.Current?.InitProgress?.SetSubphase(modAssembly.GetName().Name!, true);

                RmlMod? rmlMod = null;
                var success = true;

                try
                {
                    rmlMod = new RmlMod(Mod.Loader, modAssembly);
                    Logger.Info(() => $"Loaded mod from rml_mods: {fileName}");

                    Mod.Loader.AddMod(rmlMod);
                }
                catch (Exception ex)
                {
                    success = false;
                    Logger.Warn(() => ex.Format($"Failed to load mod from rml_mods: {fileName}"));
                }

                if (success)
                    yield return rmlMod!;
            }
        }
    }
}