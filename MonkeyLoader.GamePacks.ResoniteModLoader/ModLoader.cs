using FrooxEngine;
using HarmonyLib;
using MonkeyLoader;
using MonkeyLoader.Meta;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite.Features.FrooxEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ResoniteModLoader
{
    /// <summary>
    /// Contains the actual mod loader.
    /// </summary>
    [HarmonyPatchCategory(nameof(ModLoader))]
    [HarmonyPatch(typeof(EngineInitializer), nameof(EngineInitializer.InitializeFrooxEngine))]
    public sealed class ModLoader : Monkey<ModLoader>
    {
        /// <summary>
        /// ResoniteModLoader's version
        /// </summary>
        public static readonly string VERSION = VERSION_CONSTANT;

        internal const string VERSION_CONSTANT = "2.5.1";

        private static readonly Lazy<bool> _isHeadless = new(() => AccessTools.AllTypes().Any(type => type.Namespace == "FrooxEngine.Headless"));

        /// <summary>
        /// Returns <c>true</c> if ResoniteModLoader was loaded by a headless
        /// </summary>
        public static bool IsHeadless => _isHeadless.Value;

        /// <inheritdoc/>
        public override string Name { get; } = "ResoniteModLoader";

        /// <summary>
        /// Allows reading metadata for all loaded mods
        /// </summary>
        /// <returns>A new list containing each loaded mod</returns>
        public static IEnumerable<ResoniteModBase> Mods()
        {
            return Mod.Loader.RegularMods
                .SelectCastable<Mod, RmlMod>()
                .SelectMany(rmlMod => rmlMod.Monkeys)
                .Cast<ResoniteModBase>();
        }

        /// <inheritdoc/>
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches()
        {
            yield return new FeaturePatch<EngineInitialization>(PatchCompatibility.HookOnly);
        }

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
        private static void InitializeFrooxEnginePostfix()
        {
            try
            {
                foreach (var file in GetAssemblyPaths("rml_libs"))
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(file);
                        Logger.Info(() => $"Loaded library from rml_libs: {file}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(() => ex.Format($"Failed to load library from rml_libs: {file}"));
                    }
                }

                var rmlMods = LoadMods().ToArray();
                foreach (var rmlMod in rmlMods)
                    Mod.Loader.AddMod(rmlMod);

                Mod.Loader.RunMods(rmlMods);
            }
            catch (Exception ex)
            {
                Logger.Error(() => ex.Format("Exception in execution hook!"));
            }
        }

        private static IEnumerable<RmlMod> LoadMods()
        {
            foreach (var file in GetAssemblyPaths("rml_mods"))
            {
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

                if (success)
                    yield return rmlMod!;
            }
        }
    }
}