using Elements.Assets;
using FrooxEngine;
using HarmonyLib;
using MonkeyLoader;
using MonkeyLoader.Meta;
using MonkeyLoader.NuGet;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using MonkeyLoader.Resonite.Features.FrooxEngine;
using MonkeyLoader.Resonite.Locale;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System.Reflection;
using System.Text.Json;

namespace ResoniteModLoader
{
    /// <summary>
    /// Contains the actual mod loading hook executed by MonkeyLoader.
    /// </summary>
    [HarmonyPatchCategory(nameof(ModLoaderHook))]
    [HarmonyPatch(typeof(EngineInitializer), nameof(EngineInitializer.InitializeFrooxEngine))]
    internal sealed class ModLoaderHook : ResoniteAsyncEventHandlerMonkey<ModLoaderHook, LocaleLoadingEvent>
    {
        /// <summary>
        /// Gets a sequence of all currently loaded <see cref="RmlMod.IsLocalized">localized</see> <see cref="ResoniteModBase"/>s.
        /// </summary>
        public static IEnumerable<ResoniteModBase> LocalizedResoniteMods
            => RmlMods.Where(static rmlMod => rmlMod.IsLocalized)
                .SelectMany(static rmlMod => rmlMod.Monkeys)
                .Cast<ResoniteModBase>();

        /// <summary>
        /// Gets a sequence of all currently loaded <see cref="ResoniteModBase"/>s.
        /// </summary>
        public static IEnumerable<ResoniteModBase> ResoniteMods
            => RmlMods.SelectMany(static rmlMod => rmlMod.Monkeys)
                .Cast<ResoniteModBase>();

        /// <summary>
        /// Gets a sequence of all currently loaded <see cref="RmlMod"/>s.
        /// </summary>
        public static IEnumerable<RmlMod> RmlMods
            => Mod.Loader.RegularMods.OfType<RmlMod>();

        /// <inheritdoc/>
        public override string Name { get; } = "ModLoader";

        public override int Priority => HarmonyLib.Priority.VeryLow;

        /// <inheritdoc/>
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches()
        {
            yield return new FeaturePatch<EngineInitialization>(PatchCompatibility.HookOnly);
        }

        /// <summary>
        /// Loads locale json files from the loaded <see cref="RmlMod"/> <see cref="Assembly">assemblies</see>.
        /// </summary>
        protected override async Task Handle(LocaleLoadingEvent eventData)
        {
            // We don't need to load the ResoniteModLoader locale here, as that will be handled by the ML Resonite Integration
            // However the files are also included as embedded resources for completeness and compatibility with the reference

            foreach (var mod in LocalizedResoniteMods)
            {
                var type = mod.GetType();
                var assembly = type.Assembly;
                var localeResourceName = $"{type.Namespace}.Locale.{eventData.LocaleCode}.json";

                var realName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(resourceName => localeResourceName.Equals(resourceName, StringComparison.OrdinalIgnoreCase));

                if (realName is null)
                    continue;

                try
                {
                    if (assembly.GetManifestResourceStream(realName) is not System.IO.Stream resourceStream)
                        continue;

                    var localeData = await JsonSerializer.DeserializeAsync<LocaleData>(resourceStream);

                    if (localeData is null)
                        continue;

                    if (!eventData.LocaleCode.Equals(localeData.LocaleCode, StringComparison.OrdinalIgnoreCase))
                        Logger.Warn(() => $"Detected locale data with wrong locale code from locale resource! Wanted [{eventData.LocaleCode}] - got [{localeData.LocaleCode}] in file: {mod.Mod.Id}:{realName}");

                    eventData.LocaleResource.LoadDataAdditively(localeData);
                }
                catch (Exception ex)
                {
                    Logger.Error(() => ex.Format($"Failed to deserialize resource as LocaleData: {mod.Mod.Id}:{realName}"));
                }
            }
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
                    LoadProgressReporter.SetSubphase($"{Environment.NewLine}  {Path.GetFileNameWithoutExtension(file)}");

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

                LoadProgressReporter.AdvanceFixedPhase("Collecting RML Mods...");

                var rmlMods = await LoadModsAsync().ToArrayAsync();

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
                LoadProgressReporter.SetSubphase($"{Environment.NewLine}  {modAssembly.GetName().Name!}");

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