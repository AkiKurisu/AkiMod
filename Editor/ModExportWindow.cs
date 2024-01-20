using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using System.IO;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
namespace Kurisu.Mod.Editor
{
    public class ModExportWindow : EditorWindow
    {
        public delegate Vector2 BeginVerticalScrollViewFunc(Vector2 scrollPosition, bool alwaysShowVertical, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options);
        private static BeginVerticalScrollViewFunc s_func;
        private Vector2 m_ScrollPosition;
        private ModExportConfig exportConfig;
        private SerializedObject exportConfigObject;
        private List<IModBuilder> builders;
        private static BeginVerticalScrollViewFunc BeginVerticalScrollView
        {
            get
            {
                if (s_func == null)
                {
                    var methods = typeof(EditorGUILayout).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(x => x.Name == "BeginVerticalScrollView").ToArray();
                    var method = methods.First(x => x.GetParameters()[1].ParameterType == typeof(bool));
                    s_func = (BeginVerticalScrollViewFunc)method.CreateDelegate(typeof(BeginVerticalScrollViewFunc));
                }
                return s_func;
            }
        }
        private static readonly FieldInfo m_UseCustomPaths = typeof(BundledAssetGroupSchema).GetField("m_UseCustomPaths", BindingFlags.Instance | BindingFlags.NonPublic);
        private static string ConfigGUIDKey => Application.productName + "_ModConfigGUID";
        private static string GetBuildPath(string modName)
        {
            return Application.persistentDataPath + "/Export/" + modName.Replace(" ", string.Empty) + "-" + EditorUserBuildSettings.activeBuildTarget;
        }
        [MenuItem("Tools/Mod Exporter")]
        public static void OpenEditor()
        {
            var window = GetWindow<ModExportWindow>("Mod Exporter");
            window.minSize = new Vector2(400, 300);
        }
        private void OnGUI()
        {
            m_ScrollPosition = BeginVerticalScrollView(m_ScrollPosition, false, GUI.skin.verticalScrollbar, "OL Box");
            ShowExportEditor();
            EditorGUILayout.EndScrollView();
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var orgColor = GUI.backgroundColor;
            if (GUILayout.Button(new GUIContent("Select Export Config", "Select Export Config")))
            {
                string path = EditorUtility.OpenFilePanel("Select export config", Application.dataPath, "asset");
                if (!string.IsNullOrEmpty(path))
                {
                    path = path.Replace(Application.dataPath, string.Empty);
                    var config = AssetDatabase.LoadAssetAtPath($"Assets/{path}", typeof(ModExportConfig)) as ModExportConfig;
                    if (config != null)
                    {
                        exportConfig = config;
                        exportConfigObject = new(exportConfig);
                    }
                    else
                    {
                        ShowNotification(new GUIContent($"Invalid path: Assets/{path}, please pick export config"));
                    }
                }
            }
            GUI.enabled = exportConfig.Validate();
            if (GUILayout.Button("Create Group", GUILayout.MinWidth(100)))
            {
                var group = ModBuildUtility.GetOrCreateGroup($"Mod_{exportConfig.modName}");
                //Set not include in packed build
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                schema.IncludeInBuild = false;
                schema.BuildPath.SetVariableByName(AddressableAssetSettingsDefaultObject.Settings, AddressableAssetSettings.kRemoteBuildPath);
                schema.LoadPath.SetVariableByName(AddressableAssetSettingsDefaultObject.Settings, AddressableAssetSettings.kRemoteLoadPath);
                m_UseCustomPaths.SetValue(schema, false);
            }
            GUI.backgroundColor = new Color(253 / 255f, 163 / 255f, 255 / 255f);
            if (GUILayout.Button("Export", GUILayout.MinWidth(100)))
            {
                Export();
            }
            GUI.enabled = true;
            GUI.backgroundColor = orgColor;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        private void DrawExportConfig()
        {
            EditorGUI.BeginChangeCheck();
            SerializedProperty iterator = exportConfigObject.GetIterator();
            iterator.NextVisible(true);
            while (iterator.NextVisible(false))
            {
                EditorGUILayout.PropertyField(iterator, true);
            }
            if (EditorGUI.EndChangeCheck())
            {
                exportConfigObject.ApplyModifiedProperties();
            }
        }
        private void ShowExportEditor()
        {
            EditorGUILayout.HelpBox($"Export your Mod.\nCurrent Platform: {EditorUserBuildSettings.activeBuildTarget}", MessageType.Info);
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
            {
                if (GUILayout.Button("Switch To Android"))
                {
                    // Switch to android build.
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                }
            }
            else
            {
                if (GUILayout.Button("Switch To Windows"))
                {
                    // Switch to Windows standalone build.
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                }
            }
            if (exportConfig == null)
            {
                exportConfig = LoadExportConfig();
                exportConfigObject = new(exportConfig);
            }
            var newConfig = EditorGUILayout.ObjectField("Export Config", exportConfig, typeof(ModExportConfig), false) as ModExportConfig;
            if (newConfig != exportConfig && newConfig != null)
            {
                exportConfig = newConfig;
                exportConfigObject = new(exportConfig);
                EditorPrefs.SetString(ConfigGUIDKey, GetGUID(exportConfig));
            }
            DrawExportConfig();
        }
        private static ModExportConfig LoadExportConfig()
        {
            var configs = AssetDatabase.FindAssets($"t:{typeof(ModExportConfig)}").Select(x => AssetDatabase.LoadAssetAtPath<ModExportConfig>(AssetDatabase.GUIDToAssetPath(x))).ToArray();
            ModExportConfig config = null;
            if (configs.Length != 0)
            {
                string configGuid = EditorPrefs.GetString(ConfigGUIDKey, null);
                config = configs.FirstOrDefault(x => GetGUID(x) == configGuid);
                if (config == null)
                {
                    config = configs[0];
                    EditorPrefs.SetString(ConfigGUIDKey, GetGUID(config));
                }
            }
            if (config == null)
            {
                config = CreateInstance<ModExportConfig>();
                string k_SettingsPath = $"Assets/ModExportConfig.asset";
                Debug.Log($"<color=#3aff48>Exporter</color>: Mod export config saved path: {k_SettingsPath}");
                AssetDatabase.CreateAsset(config, k_SettingsPath);
                AssetDatabase.SaveAssets();
            }
            return config;
        }
        private void Export()
        {
            string buildPath = GetBuildPath(exportConfig.modName);
            InitDirectory(buildPath);
            BuildPipeline(buildPath);
            WritePipeline(buildPath);
            if (BuildContent(buildPath))
            {
                Debug.Log($"<color=#3aff48>Exporter</color>: Export succeed, export Path: {buildPath}");
                System.Diagnostics.Process.Start(ExportConstants.ExportPath);
            }
            else
            {
                Debug.LogError($"Export failed!");
            }
        }
        private void WritePipeline(string buildPath)
        {
            var info = new ModInfo
            {
                authorName = exportConfig.authorName,
                description = exportConfig.description,
                modName = exportConfig.modName,
                version = exportConfig.version,
                modIconBytes = exportConfig.modIcon != null ? exportConfig.modIcon.EncodeToPNG() : new byte[0] { },
                apiVersion = ExportConstants.APIVersion
            };
            foreach (var builder in builders)
            {
                builder.Write(ref info);
            }
            var stream = JsonUtility.ToJson(info);
            File.WriteAllText(buildPath + "/ModConfig.cfg", stream);
        }
        private bool BuildContent(string buildPath)
        {
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            exportConfig.lastExportPath = buildPath;
            CleanupPipeline(buildPath);
            EditorUtility.SetDirty(exportConfig);
            AssetDatabase.SaveAssets();
            return string.IsNullOrEmpty(result.Error);
        }
        private static void InitDirectory(string buildPath)
        {
            if (!Directory.Exists(ExportConstants.ExportPath)) Directory.CreateDirectory(ExportConstants.ExportPath);
            if (Directory.Exists(buildPath)) FileUtil.DeleteFileOrDirectory(buildPath);
            Directory.CreateDirectory(buildPath);
        }
        private void BuildPipeline(string dynamicBuildPath)
        {
            builders = new List<IModBuilder>
            {
                new PathBuilder()
            };
            builders.AddRange(exportConfig.customBuilders);
            foreach (var builder in builders)
            {
                builder.Build(exportConfig, dynamicBuildPath);
            }
        }
        private void CleanupPipeline(string dynamicBuildPath)
        {
            foreach (var builder in builders)
            {
                builder.Cleanup(exportConfig, dynamicBuildPath);
            }
        }
        private static string GetGUID(Object asset)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }

    }
}
