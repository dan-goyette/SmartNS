using System;
using System.Linq;
using System.Text.RegularExpressions;
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
        public const string SmartNSVersionNumber = "1.5.0";
        private static string PathSeparator = "/";

        #region Asset Creation

        /// <summary>
        /// Intercepts the creation of assets, looking for new .cs scripts, and inserts a namespace.
        /// </summary>
        /// <param name="path"></param>
        public static void OnWillCreateAsset(string path)
        {
            try
            {
                // We only intercept C# scripts.
                if (!path.EndsWith(".cs.meta"))
                {
                    return;
                }


                var smartNSSettings = SmartNSSettings.GetSerializedSettings();
                var scriptRootSettingsValue = smartNSSettings.FindProperty("m_ScriptRoot").stringValue;
                var prefixSettingsValue = smartNSSettings.FindProperty("m_NamespacePrefix").stringValue;
                var universalNamespaceSettingsValue = smartNSSettings.FindProperty("m_UniversalNamespace").stringValue;
                var useSpacesSettingsValue = smartNSSettings.FindProperty("m_IndentUsingSpaces").boolValue;
                var numberOfSpacesSettingsValue = smartNSSettings.FindProperty("m_NumberOfSpaces").intValue;
                var defaultScriptCreationDirectorySettingsValue = smartNSSettings.FindProperty("m_DefaultScriptCreationDirectory").stringValue;




                path = path.Replace(".meta", "");
                path = path.Trim();
                int index = path.LastIndexOf(".");
                if (index < 0)
                {
                    return;
                }


                // If this script was created directly under the Assets folder, and the settings tell us a default script location
                // (other than "Assets") move it there.
                if (!string.IsNullOrWhiteSpace(defaultScriptCreationDirectorySettingsValue))
                {
                    var trimmedDefaultDir = defaultScriptCreationDirectorySettingsValue.Trim();

                    if (path.Split('/').Length == 2)
                    {
                        var destinationDirectory = trimmedDefaultDir;
                        if (!destinationDirectory.StartsWith("Assets"))
                        {
                            destinationDirectory = string.Join(PathSeparator, "Assets", destinationDirectory);
                        }
                        // Replace anu double-slashes.
                        destinationDirectory = Regex.Replace(destinationDirectory, "//", "/");

                        // Make sure the directory exists
                        if (AssetDatabase.IsValidFolder(destinationDirectory))
                        {
                            var preferredPath = path.Replace("Assets", destinationDirectory);

                            var moveAssetTestResult = AssetDatabase.ValidateMoveAsset(path, preferredPath);
                            if (string.IsNullOrWhiteSpace(moveAssetTestResult))
                            {
                                WriteDebug(string.Format("Moving file from {0} to Default Script Creation Directory: {1}", path, preferredPath));
                                AssetDatabase.MoveAsset(path, preferredPath);
                                return;
                            }
                            else
                            {
                                Debug.LogError(string.Format("SmartNS unable to move script to default script creation directory, '{0}': {1}", destinationDirectory, moveAssetTestResult));
                            }
                        }
                        else
                        {
                            Debug.LogError(string.Format("SmartNS unable to move script to default script creation directory, '{0}', because the folder does not exist. Make sure the 'Default Script Creation Directory' specified in the Project Settings is valid.", destinationDirectory));
                        }
                    }
                }




                // We depend on a properly created Project Settings file. Create it now, if it doesn't exist.
                if (!SmartNSSettings.SettingsFileExists())
                {
                    SmartNSSettings.GetOrCreateSettings();
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

                // We try to keep line endings consistent. Detect the file's current line endings, and 
                // from that, determine which kind of line ending we should use.
                var lineEnding = DetectLineEndings(fullFilePath);

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

                System.IO.File.WriteAllText(fullFilePath, string.Join(lineEnding, modifiedLines.ToArray()));


                // Without this, the file won't update in Unity, and won't look right.
                AssetDatabase.ImportAsset(path);
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("Something went really wrong trying to execute SmartNS: {0}", ex.Message));
                Debug.LogError(string.Format("SmartNS Failure Stack Trace: {0}", ex.StackTrace));
            }
        }

        private static string DetectLineEndings(string filePath)
        {
            // There are three options for line endings: Mac, Unix, and Windows. More generally, they are:
            //  - \r = CR
            //  - \n = LF
            //  - \r\n = CR LF
            // 
            // Ideally we'll remain consistent in the line endings we use when composing the file. So, we
            // inspect the raw file data here to determine which line endings are being used.

            var allText = System.IO.File.ReadAllText(filePath);

            if (allText.Contains("\r\n"))
            {
                //Debug.Log($"{filePath}'s line endings are \\r\\n");
                return "\r\n";
            }
            else if (allText.Contains("\r"))
            {
                //Debug.Log($"{filePath}'s line endings are \\r");
                return "\r";
            }
            else if (allText.Contains("\n"))
            {
                //Debug.Log($"{filePath}'s line endings are \\n");
                return "\n";
            }
            else
            {
                //Debug.Log($"{filePath}'s line endings are indeterminate, so using system default.");
                // Default to returning the OS default
                return Environment.NewLine;
            }
        }

        #endregion



        public static string GetNamespaceValue(string path, string scriptRootValue, string prefixValue, string universalNamespaceValue)
        {
            string namespaceValue = null;

            // TODO: Do we need something else for Mac?


            if (string.IsNullOrEmpty(universalNamespaceValue) || string.IsNullOrEmpty(universalNamespaceValue.Trim()))
            {
                // We're not using a Universal namespace. So, generate the smart namespace.
                namespaceValue = path;

                if (scriptRootValue.Trim().Length > 0)
                {
                    // Old Version: Used to require an exact match between the scriptRootValue and the path. 
                    // That had a defect where using a ScriptRoot of "Assets/ABC", then created a script in "Assets"
                    // would not strip off the "Assets" from the start of the namespace.
                    /*
                    var toTrim = scriptRootValue.Trim();
                    if (namespaceValue.StartsWith(toTrim))
                    {
                        WriteDebug(string.Format("Trimming script root '{0}' from start of namespace", toTrim));
                        namespaceValue = namespaceValue.Substring(toTrim.Length);
                    }
                    */

                    // New Version: Remove as much of the ScriptRoot as exists in the path, as long as the elements line up
                    // exactly. 
                    foreach (var scriptRootPathPart in Regex.Split(scriptRootValue.Trim(), PathSeparator))
                    {
                        var toTrim = scriptRootPathPart.Trim();

                        // We need to match exactly on each element in the path. We used to just check for StartsWith, but if we 
                        // had a prefix of "A" when the path was "ABC", we used to strip the A off the ABC, which was wrong.
                        if (namespaceValue == toTrim || namespaceValue.StartsWith(toTrim + PathSeparator))
                        {
                            WriteDebug(string.Format("Trimming script root part '{0}' from start of namespace", toTrim));
                            namespaceValue = namespaceValue.Substring(toTrim.Length);

                            // If this leaves the namespace with a "/" at the start, remove that.
                            if (namespaceValue.StartsWith(PathSeparator))
                            {
                                namespaceValue = namespaceValue.Substring(1);
                            }
                        }
                    }

                }


                var rawPathParts = namespaceValue.Split(PathSeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                // Ignore the last "part", as that's the file name.
                var namespaceParts = rawPathParts.Take(rawPathParts.Count() - 1).ToArray();


                // Namespace identifiers can't start with a number. So we prefix those with underscores
                // if they exist.
                for (int namespacePartIndex = 0; namespacePartIndex < namespaceParts.Length; namespacePartIndex++)
                {
                    var match = Regex.Match(namespaceParts[namespacePartIndex], "^\\d");
                    if (match.Success)
                    {
                        namespaceParts[namespacePartIndex] = "_" + namespaceParts[namespacePartIndex];
                    }
                }


                namespaceValue = string.Join("/", namespaceParts);

                // Trim any spaces.
                namespaceValue = namespaceValue.Replace(" ", "");
                // Invalid characters replaced with _
                namespaceValue = namespaceValue.Replace(".", "_");
                // Separators replaced with .
                namespaceValue = namespaceValue.Replace("/", ".");

                WriteDebug(string.Format("Script Root = {0}", scriptRootValue));




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
                //WriteDebug(string.Format("Namespace is empty, probably because it was placed directly within Script Root. Not adding namespace to script."));
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
