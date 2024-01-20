using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Kurisu.Framework.Utility;
using System.Linq;
using Kurisu.Mod;
namespace Kurisu.Framework.Mod
{
    /// <summary>
    /// Mod manager class for AkiFramework
    /// </summary>
    public class ModManager : Singleton<ModManager>
    {
        [SerializeField]
        private List<ModInfo> modInfos = new();
        public AkiEvent OnModInit { get; } = new();
        public AkiEvent OnModRefresh { get; } = new();
        private ModSetting settingData;
        public bool IsModInit { get; private set; }
        private ModImporter modImporter;
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
            if (settingData == null)
            {
                settingData = SaveUtility.LoadOrNew<ModSetting>();
                modImporter = new(settingData);
            }
        }
        protected override void OnDestroy()
        {
            modImporter.Dispose();
            base.OnDestroy();
        }
        /// <summary>
        /// Load all mods
        /// </summary>
        /// <returns></returns>
        public async Task<bool> LoadAllMods()
        {
            if (settingData == null)
            {
                settingData = SaveUtility.LoadOrNew<ModSetting>();
                modImporter = new(settingData);
            }
            await modImporter.LoadAllModsAsync(modInfos);
            settingData.stateInfos.RemoveAll(x => !modInfos.Any(y => y.FullName == x.modFullName));
            SaveData();
            IsModInit = true;
            OnModInit.Trigger();
            return true;
        }

        public bool IsModActivated(ModInfo modInfo)
        {
            if (!ModImporter.IsValidAPIVersion(modInfo.apiVersion)) return false;
            return settingData.IsModActivated(modInfo);
        }
        public void DeleteMod(ModInfo modInfo)
        {
            settingData.DelateMod(modInfo);
            SaveData();
            modInfos.Remove(modInfo);
            OnModRefresh.Trigger();
        }
        public void EnabledMod(ModInfo modInfo, bool isEnabled)
        {
            if (isEnabled) settingData.EnableMod(modInfo);
            else settingData.DisableMod(modInfo);
            SaveData();
            OnModRefresh.Trigger();
        }
        public List<ModInfo> GetModInfos()
        {
            return modInfos.ToList();
        }
        private void SaveData()
        {
            SaveUtility.Save(settingData);
        }
    }
}
