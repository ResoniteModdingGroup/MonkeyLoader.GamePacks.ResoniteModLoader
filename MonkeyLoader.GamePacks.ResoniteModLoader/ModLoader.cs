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
using System.Text;
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

        internal const string VERSION_CONSTANT = "3.0.0";

        private static readonly Lazy<bool> _isHeadless = new(() => AccessTools.AllTypes().Any(type => type.Namespace == "FrooxEngine.Headless"));

        /// <summary>
        /// Returns <c>true</c> if ResoniteModLoader was loaded by a headless
        /// </summary>
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
            LoadProgressReporter.AddFixedPhases(4);

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

            LoadProgressReporter.AdvanceFixedPhase("Loading RML Libraries...");

            try
            {
                foreach (var file in GetAssemblyPaths("rml_libs"))
                {
                    LoadProgressReporter.SetSubphase(Path.GetFileNameWithoutExtension(file));

                    try
                    {
                        var assembly = await Task.Run(() => Assembly.LoadFrom(file));
                        var name = assembly.GetName();

                        Mod.Loader.NuGet.Add(new LoadedNuGetPackage(new PackageIdentity(name.Name, new NuGetVersion(name.Version)), NuGetHelper.Framework));

                        Logger.Info(() => $"Loaded library {name.Name}.{name.Version} from rml_libs: {file}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(() => ex.Format($"Failed to load library from rml_libs: {file}"));
                    }

                    LoadProgressReporter.ExitSubphase();
                }

                LoadProgressReporter.AdvanceFixedPhase("Collecting RML Mods...");

                var rmlMods = await Task.Run(() => LoadMods().ToArray());

                LoadProgressReporter.AdvanceFixedPhase("Running RML Mods...");
                await Task.Run(() => Mod.Loader.RunMods(rmlMods));
                LoadProgressReporter.AdvanceFixedPhase("Done with RML");
            }
            catch (Exception ex)
            {
                Logger.Error(() => ex.Format("Exception in execution hook!"));
                LoadProgressReporter.AdvanceFixedPhase("Error running RML Mods!");
            }
        }

        private static IEnumerable<RmlMod> LoadMods()
        {
            foreach (var file in GetAssemblyPaths("rml_mods"))
            {
                LoadProgressReporter.SetSubphase(Path.GetFileNameWithoutExtension(file));

                RmlMod? rmlMod = null;
                var success = true;

                try
                {
                    rmlMod = new RmlMod(Mod.Loader, file, false);
                    Logger.Info(() => $"Loaded mod from rml_mods: {file}");

                    Mod.Loader.AddMod(rmlMod);
                }
                catch (Exception ex)
                {
                    success = false;
                    Logger.Warn(() => ex.Format($"Failed to load mod from rml_mods: {file}"));
                }

                LoadProgressReporter.ExitSubphase();

                if (success)
                    yield return rmlMod!;
            }
        }
    }
}