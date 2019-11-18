using System.ComponentModel;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// This package changed in 2019.1
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;    
#elif UNITY_2018_4_OR_NEWER
using UnityEngine.Experimental.UIElements;
#endif




namespace GraviaSoftware.SmartNS.Editor
{


    // Create a new type of Settings Asset.
    public class SmartNSSettings : ScriptableObject
    {
#pragma warning disable 0414
        [SerializeField]
        private string m_ScriptRoot;
        [SerializeField]
        private string m_NamespacePrefix;
        [SerializeField]
        private string m_UniversalNamespace;
        [SerializeField]
        private bool m_IndentUsingSpaces;
        [SerializeField]
        private int m_NumberOfSpaces;
        [SerializeField]
        private bool m_EnableDebugLogging;
#pragma warning restore 0414


        private const string _defaultSmartNSSettingsPath = "Assets/SmartNS/Editor/SmartNSSettings.asset";

        internal static SmartNSSettings GetOrCreateSettings()
        {
            var smartNSSettings = GetSmartNSSettingsAsset();

            if (smartNSSettings == null)
            {
                // We don't have any setting. Create one wherever the c# class is.

                smartNSSettings = ScriptableObject.CreateInstance<SmartNSSettings>();
                smartNSSettings.m_ScriptRoot = "Assets";
                smartNSSettings.m_NamespacePrefix = "";
                smartNSSettings.m_UniversalNamespace = "";
                smartNSSettings.m_IndentUsingSpaces = true;
                smartNSSettings.m_NumberOfSpaces = 4;
                smartNSSettings.m_EnableDebugLogging = false;
                AssetDatabase.CreateAsset(smartNSSettings, _defaultSmartNSSettingsPath);
                AssetDatabase.SaveAssets();
            }
            return smartNSSettings;
        }


        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }

        public static bool SettingsFileExists()
        {
            return GetSmartNSSettingsAsset() != null;
        }

        public static SmartNSSettings GetSmartNSSettingsAsset()
        {
            SmartNSSettings smartNSSettings = null;

            // Although there is a default location for thr Settings, we want to be able to find it even if the 
            // player has moved them around. This will locate the settings even if they're not in the default location.
            var smartNSSettingsAssetGuids = AssetDatabase.FindAssets("t:SmartNSSettings");

            if (smartNSSettingsAssetGuids.Length > 1)
            {
                var paths = string.Join(", ", smartNSSettingsAssetGuids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)));
                Debug.LogWarning($"Multiple SmartNSSettings.asset files exist in this project. This may lead to confusion, as any of the settings files may be chosen arbitrarily. You should remove all but one of the following so that you only have one SmartNSSettings.asset files: {paths}");
            }

            if (smartNSSettingsAssetGuids.Length > 0)
            {
                smartNSSettings = AssetDatabase.LoadAssetAtPath<SmartNSSettings>(AssetDatabase.GUIDToAssetPath(smartNSSettingsAssetGuids.First()));
            }

            return smartNSSettings;
        }


        /// <summary>
        /// A special method used to decide whether a given asset path is an instance of these settings        /// 
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public static bool PathIsSettingsFile(string assetPath)
        {
            Debug.Log($"Is {assetPath} a settings asset?");
            return assetPath == _defaultSmartNSSettingsPath;
        }
    }



    // Create SmartNSSettingsProvider by deriving from SettingsProvider:
    public class SmartNSSettingsProvider : SettingsProvider
    {


        private SerializedObject m_SmartNSSettings;

        class Styles
        {
            public static GUIContent ScriptRoot = new GUIContent("Script Root", "Whatever you place here will be stripped off the beginning of the namespace. Normally this should be 'Assets', as Unity will automatically place new scripts in '/Assets'. But if you keep all your scripts in 'Assets/Code', you could out 'Assets/Code' here to strip that out of the namespace. Note that any scripts created at the level of the Script Root will not be given a namespace, unless Universal namespacing is used.");
            public static GUIContent NamespacePrefix = new GUIContent("Namespace Prefix");
            public static GUIContent UniversalNamespace = new GUIContent("Universal Namespace");
            public static GUIContent IndentUsingSpaces = new GUIContent("Indent using Spaces");
            public static GUIContent NumberOfSpaces = new GUIContent("Number of Spaces");
            public static GUIContent EnableDebugLogging = new GUIContent("Enable Debug Logging");
        }

        public SmartNSSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        public static bool IsSettingsAvailable()
        {
            return SmartNSSettings.SettingsFileExists();
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            // This function is called when the user clicks on the SmartNSSettings element in the Settings window.
            m_SmartNSSettings = SmartNSSettings.GetSerializedSettings();
        }

        public override void OnDeactivate()
        {
            AssetDatabase.SaveAssets();
        }

        public override void OnGUI(string searchContext)
        {
            m_SmartNSSettings.Update();

            // Use IMGUI to display UI:
            EditorGUILayout.LabelField(string.Format("Version {0}", SmartNS.SmartNSVersionNumber));

            // Preferences GUI
            EditorGUILayout.HelpBox("SmartNS adds a namespace to new C# scripts based on the directory in which they are created. Optionally, a 'Universal' namespace can be used for all scripts.", MessageType.None);

            EditorGUILayout.PropertyField(m_SmartNSSettings.FindProperty("m_ScriptRoot"), Styles.ScriptRoot);
            EditorGUILayout.PropertyField(m_SmartNSSettings.FindProperty("m_NamespacePrefix"), Styles.NamespacePrefix);
            EditorGUILayout.PropertyField(m_SmartNSSettings.FindProperty("m_UniversalNamespace"), Styles.UniversalNamespace);
            var useSpacesProperty = m_SmartNSSettings.FindProperty("m_IndentUsingSpaces");
            var useSpaces = EditorGUILayout.PropertyField(useSpacesProperty, Styles.IndentUsingSpaces);
            if (useSpacesProperty.boolValue)
            {
                EditorGUILayout.PropertyField(m_SmartNSSettings.FindProperty("m_NumberOfSpaces"), Styles.NumberOfSpaces);
            }
            EditorGUILayout.PropertyField(m_SmartNSSettings.FindProperty("m_EnableDebugLogging"), Styles.EnableDebugLogging);

            m_SmartNSSettings.ApplyModifiedProperties();
        }

        // Register the SettingsProvider
        [SettingsProvider]
        public static SettingsProvider CreateSmartNSSettingsProvider()
        {
            if (IsSettingsAvailable())
            {
                //Debug.Log("Settings Available");
                var provider = new SmartNSSettingsProvider("Project/SmartNS", SettingsScope.Project);

                // Automatically extract all keywords from the Styles.
                provider.keywords = GetSearchKeywordsFromGUIContentProperties<Styles>();
                return provider;
            }

            // Debug.Log("Settings Not Available");
            // Settings Asset doesn't exist yet; no need to display anything in the Settings window.
            return null;
        }
    }

}
