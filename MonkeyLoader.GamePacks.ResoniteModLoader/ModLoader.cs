using Elements.Core;
using System.Reflection;

namespace ResoniteModLoader
{
    /// <summary>
    /// Contains the actual mod loader.
    /// </summary>
    public sealed class ModLoader
    {
        /// <summary>
        /// ResoniteModLoader's version
        /// </summary>
        public static readonly string VERSION = VERSION_CONSTANT;

        internal const string VERSION_CONSTANT = "4.2.0";

        private static readonly Lazy<bool> _isHeadless = new(()
            => AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(static assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException typeLoadException)
                    {
                        return typeLoadException.Types ?? [];
                    }
                })
                .Any(static type => type?.Namespace is "FrooxEngine.Headless"));

        /// <summary>
        /// Gets whether this is running on a headless client.
        /// </summary>
        /// <value><c>true</c> if ResoniteModLoader was loaded by a headless; otherwise, <c>false</c>.</value>
        public static bool IsHeadless => _isHeadless.Value;

        /// <summary>
        /// Allows reading metadata for all loaded mods.
        /// </summary>
        /// <returns>A sequence of all loaded <see cref="ResoniteModBase">resonite mods</see>.</returns>
        public static IEnumerable<ResoniteModBase> Mods()
            => ModLoaderHook.RmlMods
                .SelectMany(rmlMod => rmlMod.Monkeys)
                .Cast<ResoniteModBase>();
    }
}