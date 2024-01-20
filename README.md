# AkiMod

Simple mod system based on Addressables. 

Editor part and runtime part can be separated, so you can build mod resources in another project.

## Runtime API

```C#
private async void LoadMod()
{
    List<ModInfo> modInfos = new();
    await new ModImporter(new ModSetting()).LoadAllModsAsync(modInfos); 
}
```

## Example Mod Manager

Example need install dependency [AkiFramework](https://github.com/AkiKurisu/AkiFramework)

## Editor Export

Use Mod Exporter to create new addressable group and build only the mod group you edited.

You can inherit ``CustomBuilder`` and add it to export config to write mod additional meta data such as game assets sub catalog into `ModInfo` or make a pre-process such as looping the group's addressable entries.