using System.IO;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;
using UnityEngine;
using System;
using Object = UnityEngine.Object;
using System.Text;
namespace Kurisu.Mod
{
    public class ModImporter : IDisposable
    {
        private readonly ModSetting modSettingData;
        public ModImporter(ModSetting modSettingData)
        {
            this.modSettingData = modSettingData;
        }
        private readonly List<Texture2D> tempTextures = new();
        public static bool IsValidAPIVersion(string version)
        {
            if (float.TryParse(version, out var version2))
            {
                return version2 >= ModConstants.APIVersion;
            }
            return version == ModConstants.APIVersion.ToString();
        }
        public async Task<bool> LoadAllModsAsync(List<ModInfo> modInfos)
        {
            string modPath = ModConstants.LoadingPath;
            if (!File.Exists(modPath)) Directory.CreateDirectory(modPath);
            string[] subDirectories = Directory.GetDirectories(modPath);
            if (subDirectories.Length == 0)
            {
                return false;
            }
            List<string> configPaths = new();
            List<string> directoryPaths = new();
            foreach (var directory in subDirectories)
            {
                string[] files = Directory.GetFiles(directory);
                foreach (var file in files)
                {
                    if (Path.GetExtension(file) == ".cfg")
                    {
                        configPaths.Add(file);
                        directoryPaths.Add(directory);
                        break;
                    }
                }
            }
            if (configPaths.Count == 0)
            {
                return false;
            }
            for (int i = configPaths.Count - 1; i >= 0; i--)
            {
                var stream = await File.ReadAllTextAsync(configPaths[i]);
                var modInfo = InitModInfo(stream, directoryPaths[i]);
                var state = modSettingData.GetModState(modInfo);
                if (state == ModStateInfo.ModState.Enabled)
                {
                    modInfos.Add(modInfo);
                }
                else if (state == ModStateInfo.ModState.Disabled)
                {
                    directoryPaths.RemoveAt(i);
                    modInfos.Add(modInfo);
                    continue;
                }
                else
                {
                    DelateMod(modInfo);
                    directoryPaths.RemoveAt(i);
                    continue;
                }

            }
            foreach (var directory in directoryPaths)
            {
                await LoadModCatalogAsync(directory);
            }
            return true;
        }
        public static void DelateMod(ModInfo modInfo)
        {
            Directory.Delete(modInfo.downloadPath, true);
            var orgFile = modInfo.downloadPath + ".zip";
            if (File.Exists(orgFile))
            {
                File.Delete(orgFile);
            }
        }
        public async Task<ModInfo> LoadModAsync(ModSetting settingData, string path)
        {
            string config = null;
            foreach (var file in Directory.GetFiles(path))
            {
                if (Path.GetExtension(file) == ".cfg")
                {
                    config = file;
                    break;
                }
            }
            if (config == null) return null;
            var modInfo = InitModInfo(await File.ReadAllTextAsync(config), path);
            var state = settingData.GetModState(modInfo);
            if (state == ModStateInfo.ModState.Enabled)
            {
                if (!IsValidAPIVersion(modInfo.apiVersion))
                {
                    return modInfo;
                }
            }
            else if (state == ModStateInfo.ModState.Disabled)
            {
                return modInfo;
            }
            else
            {
                DelateMod(modInfo);
                return null;
            }
            await LoadModCatalogAsync(path);
            return modInfo;
        }
        public async static Task<bool> LoadModCatalogAsync(string path)
        {
            foreach (var file in Directory.GetFiles(path))
            {
                if (Path.GetExtension(file) == ".json")
                {
                    await TryLoadCatalogAsync(file, path);
                    break;
                }
            }
            return true;
        }
        private ModInfo InitModInfo(string stream, string path)
        {
            var modInfo = JsonUtility.FromJson<ModInfo>(stream);
            modInfo.downloadPath = path.Replace(@"\", "/");
            if (modInfo.modIconBytes.Length != 0)
                modInfo.ModIcon = CreateSpriteFromBytes(modInfo.modIconBytes);
            return modInfo;
        }
        private Sprite CreateSpriteFromBytes(byte[] bytes)
        {
            Texture2D texture = new(2, 2);
            texture.LoadImage(bytes);
            tempTextures.Add(texture);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }
        public static async Task<bool> TryLoadCatalogAsync(string catalogPath, string directoryPath)
        {
            catalogPath = catalogPath.Replace(@"\", "/");
            string contentCatalog = File.ReadAllText(catalogPath, Encoding.UTF8);
            File.Delete(catalogPath);
            contentCatalog = contentCatalog.Replace(ModConstants.DynamicLoadPath, directoryPath.Replace(@"\", "/"));
            File.WriteAllText(catalogPath, contentCatalog, Encoding.UTF8);
            Debug.Log($"Load mod catalog {catalogPath}");
            await Addressables.LoadContentCatalogAsync(catalogPath).Task;
            return true;
        }
        private void ClearAllTempTexture()
        {
            foreach (var texture in tempTextures)
            {
                Object.Destroy(texture);
            }
            tempTextures.Clear();
        }

        public void Dispose()
        {
            ClearAllTempTexture();
        }
    }
}