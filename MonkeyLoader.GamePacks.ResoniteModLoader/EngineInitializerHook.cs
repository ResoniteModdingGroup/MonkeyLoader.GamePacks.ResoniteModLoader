using FrooxEngine;
using HarmonyLib;
using MonkeyLoader;
using MonkeyLoader.Configuration;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite.Features.FrooxEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ResoniteModLoader
{
    [HarmonyPatchCategory(nameof(EngineInitializerHook))]
    [HarmonyPatch(typeof(EngineInitializer), nameof(EngineInitializer.InitializeFrooxEngine))]
    internal sealed class EngineInitializerHook : Monkey<EngineInitializerHook>
    {
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