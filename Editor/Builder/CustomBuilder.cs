using UnityEngine;
namespace Kurisu.Mod.Editor
{
    public abstract class CustomBuilder : ScriptableObject, IModBuilder
    {
        public virtual void Build(ModExportConfig exportConfig, string buildPath)
        {

        }

        public virtual void Cleanup(ModExportConfig exportConfig, string buildPath)
        {

        }

        public void Write(ref ModInfo modInfo)
        {

        }
    }
}