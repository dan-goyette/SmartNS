using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GraviaSoftware.SmartNS.Editor
{
    /// <summary>
    /// An Asset Creation Post-processor that acts on newly created c# scripts to insert a smart namespace based
    /// on the path of the script.
    /// </summary>
    public class SmartNS : UnityEditor.AssetModificationProcessor
    {
        public const string SmartNSVersionNumber = "1.3.1";

        #region Asset Creation

        /// <summary>
        /// Intercepts the creation of assets, looking for new .cs scripts, and inserts a namespace.
        /// </summary>
        /// <param name="path"></param>
        public static void OnWillCreateAsset(string path)
        {
            try
            {
                Debug.Log($"SmartNS is intercepting creation of '{path}'");


                // Special case: Creation of our own SmartNS Project Settings will get handled by this code. So we need to 
                // exclude the path of our own project settings.
                if (path.StartsWith(SmartNSSettings.k_SmartNSSettingsPath))
                {
                    return;
                }

                //EnsurePreferencesAreInitialized();

                var smartNSSettings = SmartNSSettings.GetSerializedSettings();

                if (smartNSSettings == null)
                {
                    throw new Exception("Unable to find SmartNS Project Settings.");
                }

                var scriptRootSettingsValue = smartNSSettings.FindProperty("m_ScriptRoot").stringValue;
                var prefixSettingsValue = smartNSSettings.FindProperty("m_NamespacePrefix").stringValue;
                var universalNamespaceSettingsValue = smartNSSettings.FindProperty("m_UniversalNamespace").stringValue;
                var useSpacesSettingsValue = smartNSSettings.FindProperty("m_IndentUsingSpaces").boolValue;
                var numberOfSpacesSettingsValue = smartNSSettings.FindProperty("m_NumberOfSpaces").intValue;


                path = path.Replace(".meta", "");
                int index = path.LastIndexOf(".");
                if (index < 0)
                {
                    return;
                }

                string file = path.Substring(index);
                if (file != ".cs")
                {
                    return;
                }

                WriteDebug(string.Format("Acting on new C# file: {0}", path));
                index = Application.dataPath.LastIndexOf("Assets");
                var fullFilePath = Application.dataPath.Substring(0, index) + path;
                WriteDebug(string.Format("Full Path: {0}", fullFilePath));
                if (!System.IO.File.Exists(fullFilePath))
                {
                    return;
                }

                // Generate the namespace.
                string namespaceValue = GetNamespaceValue(path, scriptRootSettingsValue, prefixSettingsValue, universalNamespaceSettingsValue);
                if (namespaceValue == null)
                {
                    return;
                }


                // Read the file contents, so we can insert the namespace line, and indent other lines under it.
                string[] lines = System.IO.File.ReadAllLines(fullFilePath);


                // Determining exactly where to insert the namespace is a little tricky. A user could have modified the
                // default template in any number of ways. It seems the safest approach is to assume that all 
                // 'using' statements come first, and that the namespace declaration will appear after the last using statement.

                // Initially ensure that this template doesn't already contain a namespace.
                foreach (var line in lines)
                {
                    if (line.StartsWith("namespace "))
                    {
                        WriteDebug(string.Format("This script already contains a namespace declaration ({0}). Skipping.", line));
                        return;
                    }
                }

                var lastUsingLineIndex = 0;
                // Find the last "using" statement in the file.
                for (var i = lines.Length - 1; i >= 0; i--)
                {
                    if (lines[i].StartsWith("using "))
                    {
                        lastUsingLineIndex = i;
                        break;
                    }
                }

                var modifiedLines = lines.ToList();

                // A blank line, followed by the namespace declaration.
                modifiedLines.Insert(lastUsingLineIndex + 1, "");
                modifiedLines.Insert(lastUsingLineIndex + 2, string.Format("namespace {0} {{", namespaceValue));

                // Indent all lines in between.
                for (var i = lastUsingLineIndex + 3; i < modifiedLines.Count; i++)
                {
                    var prefix = useSpacesSettingsValue
                        ? new string(' ', numberOfSpacesSettingsValue)
                        : "\t";
                    modifiedLines[i] = string.Format("{0}{1}", prefix, modifiedLines[i]);
                }

                // Add the closing brace.
                modifiedLines.Add("}");

                // Don't add a namespace if one already exists in the file.

                //fileContent = fileContent.Replace( "#AUTHOR#", AuthorName );
                //fileContent = fileContent.Replace( "#NAMESPACE#", GetNamespaceForPath( path ) );

                System.IO.File.WriteAllText(fullFilePath, string.Join(Environment.NewLine, modifiedLines.ToArray()));


                // Without this, the file won't update in Unity, and won't look right.
                AssetDatabase.ImportAsset(path);
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("Something went really wrong trying to execute SmartNS: {0}", ex.Message));
                Debug.LogError(string.Format("SmartNS Failure Stack Trace: {0}", ex.StackTrace));
            }
        }

        #endregion


        public static string GetNamespaceValue(string path, string scriptRootValue, string prefixValue, string universalNamespaceValue)
        {
            string namespaceValue = null;

            if (string.IsNullOrEmpty(universalNamespaceValue) || string.IsNullOrEmpty(universalNamespaceValue.Trim()))
            {
                // We're not using a Universal namespace. So, generate the smart namespace.
                namespaceValue = path;

                // TODO: Do we need something else for Mac?
                var pathSeparator = '/';

                var rawPathParts = path.Split(pathSeparator);

                // Ignore the last "part", as that's the file name.
                var namespaceParts = rawPathParts.Take(rawPathParts.Count() - 1);

                namespaceValue = string.Join("/", namespaceParts.ToArray());

                // Trim any spaces.
                namespaceValue = namespaceValue.Replace(" ", "");
                // Invalid characters replaced with _
                namespaceValue = namespaceValue.Replace(".", "_");
                // Separators replaced with .
                namespaceValue = namespaceValue.Replace("/", ".");

                WriteDebug(string.Format("Script Root = {0}", scriptRootValue));
                if (scriptRootValue.Trim().Length > 0)
                {
                    var toTrim = scriptRootValue.Trim();
                    if (namespaceValue.StartsWith(toTrim))
                    {
                        WriteDebug(string.Format("Trimming script root '{0}' from start of namespace", toTrim));
                        namespaceValue = namespaceValue.Substring(toTrim.Length);
                    }
                }

                if (namespaceValue.StartsWith("."))
                {
                    namespaceValue = namespaceValue.Substring(1);
                }

                if (prefixValue.Trim().Length > 0)
                {
                    if (string.IsNullOrEmpty(namespaceValue.Trim()))
                    {
                        // This script was likely added at Assets root.
                        namespaceValue = prefixValue;
                    }
                    else
                    {
                        namespaceValue = string.Format("{0}{1}{2}",
                            prefixValue,
                            prefixValue.EndsWith(".") ? "" : ".",
                            namespaceValue);
                    }

                }

                WriteDebug(string.Format("Using smart namespace: '{0}'", namespaceValue));

            }
            else
            {
                // Use the universal namespace.
                namespaceValue = universalNamespaceValue.Trim();
                WriteDebug(string.Format("Using 'Universal' namespace: {0}", namespaceValue));
            }

            if (namespaceValue.Trim().Length == 0)
            {
                WriteDebug(string.Format("Namespace is empty, probably because it was placed directly within Script Root. Not adding namespace to script."));
                return null;
            }

            return namespaceValue;
        }


        #region Debug

        private static void WriteDebug(string message)
        {
            var smartNSSettings = SmartNSSettings.GetSerializedSettings();
            var logDebugMessagesSettingsValue = smartNSSettings.FindProperty("m_EnableDebugLogging").boolValue;

            if (logDebugMessagesSettingsValue)
            {
                Debug.Log(string.Format("SmartNS Debug: {0}", message));
            }
        }

        #endregion
    }
}
