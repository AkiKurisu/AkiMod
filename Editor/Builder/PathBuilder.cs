using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
namespace Kurisu.Mod.Editor
{
    public class PathBuilder : IModBuilder
    {
        private bool buildRemoteCatalog;
        public void Build(ModExportConfig exportConfig, string buildPath)
        {
            buildRemoteCatalog = AddressableAssetSettingsDefaultObject.Settings.BuildRemoteCatalog;
            AddressableAssetSettingsDefaultObject.Settings.BuildRemoteCatalog = true;
            foreach (var group in AddressableAssetSettingsDefaultObject.Settings.groups)
            {
                if (group.HasSchema<BundledAssetGroupSchema>())
                    group.GetSchema<BundledAssetGroupSchema>().IncludeInBuild = false;
            }
            {
                var group = ModBuildUtility.GetOrCreateGroup($"Mod_{exportConfig.modName}");
                group.GetSchema<BundledAssetGroupSchema>().IncludeInBuild = true;
                group.Settings.profileSettings.SetValue(group.Settings.activeProfileId, "Remote.LoadPath", ExportConstants.DynamicLoadPath);
                group.Settings.profileSettings.SetValue(group.Settings.activeProfileId, "Remote.BuildPath", buildPath);
            }
        }

        public void Cleanup(ModExportConfig exportConfig, string buildPath)
        {
            AddressableAssetSettingsDefaultObject.Settings.BuildRemoteCatalog = buildRemoteCatalog;
            foreach (var group in AddressableAssetSettingsDefaultObject.Settings.groups)
            {
                if (group.HasSchema<BundledAssetGroupSchema>())
                    group.GetSchema<BundledAssetGroupSchema>().IncludeInBuild = !group.Name.StartsWith("Mod_");
            }
            ModBuildUtility.GetOrCreateGroup($"Mod_{exportConfig.modName}").GetSchema<BundledAssetGroupSchema>().IncludeInBuild = false;
        }

        public void Write(ref ModInfo modInfo) { }
    }
}
