# ResoniteModLoader Compatibility Game Pack

This Game Pack for [MonkeyLoader](https://github.com/MonkeyModdingTroop/MonkeyLoader)
provides compatibility for [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) (RML).

This means that any RML mods from the `rml_mods` folder can be loaded directly through MonkeyLoader,
without adding RML as a library that [Resonite](https://resonite.com) should load.
Any libraries from the `rml_libs` folder will be loaded as well.

Because the RML mods are loaded as fully integrated MonkeyLoader mods,
they are integrated into the MonkeyLoader Settings Category inside Resonite as well.  
The settings files will be created under `./MonkeyLoader/Configs` however,
so any customized settings will have to recreated or manually copied into the json file.

## Installation

1. Download `MonkeyLoader-v...+Resonite-v....zip` [from the latest Resonite gamepack release](https://github.com/ResoniteModdingGroup/MonkeyLoader.GamePacks.Resonite/releases/latest)
2. Extract the zip into Resonite's install folder (`C:\Program Files (x86)\Steam\steamapps\common\Resonite`)
3. Download `MonkeyLoader.GamePacks.ResoniteModLoader.nupkg` gamepack from it's [latest release](https://github.com/ResoniteModdingGroup/MonkeyLoader.GamePacks.ResoniteModLoader/releases/latest) to `MonkeyLoader/GamePacks`
4. Remove RML's library launch option from Steam if you had set it up previously

## Settings Integration

![Screenshot of the Resonite Settings with an RML mod opened in the MonkeyLoader category.](https://raw.githubusercontent.com/ResoniteModdingGroup/MonkeyLoader.GamePacks.ResoniteModLoader/master/MonkeyLoaderSettings.png)
