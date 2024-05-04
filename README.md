# ResoniteModLoader Compatibility Game Pack

<img align="right" width="128" height="128" src="./Icon.png"/>

This Game Pack for [MonkeyLoader](https://github.com/MonkeyModdingTroop/MonkeyLoader)
provides compatibility for [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) (RML).

This means that any RML mods from the `rml_mods` folder can be loaded directly through MonkeyLoader,
without adding RML as a library that [Resonite](https://resonite.com) should load.
Any libraries from the `rml_libs` folder will be loaded as well.

## Installation

1. Download `MonkeyLoader-v...+Resonite-v....zip` [from the latest Resonite gamepack release](https://github.com/ResoniteModdingGroup/MonkeyLoader.GamePacks.Resonite/releases/latest)
2. Extract the zip into Resonite's install folder (`C:\Program Files (x86)\Steam\steamapps\common\Resonite`)
3. Download `MonkeyLoader.GamePacks.ResoniteModLoader.nupkg` gamepack from it's [latest release](https://github.com/ResoniteModdingGroup/MonkeyLoader.GamePacks.ResoniteModLoader/releases/latest) to `MonkeyLoader/GamePacks`
4. Remove RML's library launch option from Steam if you had set it up previously

## Settings Integration

Because the RML mods are loaded as fully integrated MonkeyLoader mods,
they are integrated into the MonkeyLoader Settings Category inside Resonite as well.  
The settings files will be created under `./MonkeyLoader/Configs` however,
so any customized settings will have to recreated or manually copied into the json file.


![Screenshot of the Resonite Settings with an RML mod opened in the MonkeyLoader category.](https://raw.githubusercontent.com/ResoniteModdingGroup/MonkeyLoader.GamePacks.ResoniteModLoader/master/MonkeyLoaderSettings.png)

## Contributing

Issues can and should be opened here instead of the mods' issue trackers if they're designed for RML, and work with it, but not with this gamepack.
The GitHub issues can also be used for feature requests.

For code contributions, getting started is a bit involved due to [Resonite-Issues#456](https://github.com/Yellow-Dog-Man/Resonite-Issues/issues/456).
The short summary of it is:

1. [Setup a private nuget feed](https://github.com/MonkeyModdingTroop/ReferencePackageGenerator).
2. [Generate the game's reference assemblies](https://github.com/MonkeyModdingTroop/ReferencePackageGenerator).
3. Run `dotnet build`, or build with your IDE of preference.

The long version is that you'll probably want to set it up privately on GitHub nuget packages.
Though this isn't legal advice and you should check that [Resonite's TOS](https://resonite.com/policies/TermsOfService.html) allows it.
