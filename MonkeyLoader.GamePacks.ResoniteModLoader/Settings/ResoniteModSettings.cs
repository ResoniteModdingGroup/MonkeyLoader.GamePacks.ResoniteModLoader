// This doesn't work, throws error in OnStart

//using FrooxEngine;
//using MonkeyLoader.Logging;
//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace ResoniteModLoader;

//[AutoRegisterSetting]
//[SettingCategory("ResoniteModLoader")]
//public sealed class ModLoaderSettings : SettingComponent<ModLoaderSettings>
//{
//    /// <inheritdoc/>
//    public override bool UserspaceOnly => true;

//    [SettingIndicatorProperty]
//    public Sync<string> LoadedMods;
//    [SettingIndicatorProperty]
//    public Sync<string> ModLoaderVersion;

//    [SettingProperty("Debug Mode")]
//    public Sync<bool> DebugMode;

//    [SettingProperty("Hide ModLoader progress during startup")]
//    public Sync<bool> HideVisuals;

//    /// <inheritdoc/>
//    public override void ResetToDefault()
//    {
//        DebugMode.Value = false;
//        HideVisuals.Value = false;
//    }

//    /// <inheritdoc/>
//    public override void OnChanges() // should be protected but publicizer go brrr
//    {
//        base.OnChanges();
//        //ModLoaderConfiguration.Get().Debug = DebugMode.Value;
//        ModLoader.Logger.Debug(() => $"Setting changed, changed debug values {DebugMode.Value}");

//        ModLoaderVersion.Value = ModLoader.VERSION;
//        LoadedMods.Value = ModLoader.Mods().Count().ToString(CultureInfo.InvariantCulture);
//    }
//    public override void OnStart() // should be protected but publicizer go brrr
//    {
//        base.OnStart();
//        ModLoaderVersion.Value = ModLoader.VERSION;
//        LoadedMods.Value = ModLoader.Mods().Count().ToString(CultureInfo.InvariantCulture);
//        ModLoader.Logger.Info(() => "OnStart in ModLoaderSettings");
//        DebugMode.Value = false;
//        HideVisuals.Value = false;
//    }

//    // The following methods are normally added automatically by FrooxEngine.Weaver when a plugin is loaded for the first time,
//    // However we are not a plugin, so we add these manually ourselves.

//    public override void InitializeSyncMembers() // should be protected but publicizer go brrr
//    {
//        base.InitializeSyncMembers();
//        LoadedMods = new Sync<string>();
//        ModLoaderVersion = new Sync<string>();
//        DebugMode = new Sync<bool>();
//        HideVisuals = new Sync<bool>();
//    }

//    public override ISyncMember GetSyncMember(int index)
//    {
//        return index switch
//        {
//            0 => persistent,
//            1 => updateOrder,
//            2 => EnabledField,
//            3 => LoadedMods,
//            4 => ModLoaderVersion,
//            5 => DebugMode,
//            6 => HideVisuals,
//            _ => throw new ArgumentOutOfRangeException(),
//        };
//    }

//    public static ModLoaderSettings __New()
//    {
//        return new ModLoaderSettings();
//    }
//}