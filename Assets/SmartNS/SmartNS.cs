using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GraviaSoftware.SmartNS
{
    /// <summary>
    /// An Asset Creation Post-processor that acts on newly created c# scripts to insert a smart namespace based
    /// on the path of the script.
    /// </summary>
    public class SmartNS : UnityEditor.AssetModificationProcessor
    {



        #region Asset Creation

        /// <summary>
        /// Intercepts the creation of assets, looking for new .cs scripts, and inserts a namespace.
        /// </summary>
        /// <param name="path"></param>
        public static void OnWillCreateAsset( string path )
        {
            try {
                EnsurePreferencesAreInitialized();

                path = path.Replace( ".meta", "" );
                int index = path.LastIndexOf( "." );
                if ( index < 0 ) {
                    return;
                }

                string file = path.Substring( index );
                if ( file != ".cs" ) {
                    return;
                }

                WriteDebug( string.Format( "Acting on new C# file: {0}", path ) );
                index = Application.dataPath.LastIndexOf( "Assets" );
                var fullFilePath = Application.dataPath.Substring( 0, index ) + path;
                WriteDebug( string.Format( "Full Path: {0}", fullFilePath ) );
                if ( !System.IO.File.Exists( fullFilePath ) ) {
                    return;
                }

                // Generate the namespace.
                string namespaceValue = "";

                if ( string.IsNullOrEmpty( universalNamespacePreference ) || string.IsNullOrEmpty( universalNamespacePreference.Trim() ) ) {
                    // We're not using a Universal namespace. So, generate the smart namespace.
                    namespaceValue = path;

                    // TODO: Do we need something else for Mac?
                    var pathSeparator = '/';

                    var rawPathParts = path.Split( pathSeparator );

                    // Ignore the last "part", as that's the file name.
                    var namespaceParts = rawPathParts.Take( rawPathParts.Count() - 1 );

                    namespaceValue = string.Join( "/", namespaceParts.ToArray() );

                    // Trim any spaces.
                    namespaceValue = namespaceValue.Replace( " ", "" );
                    // Invalid characters replaced with _
                    namespaceValue = namespaceValue.Replace( ".", "_" );
                    // Separators replaced with .
                    namespaceValue = namespaceValue.Replace( "/", "." );

                    WriteDebug( string.Format( "Script Root Preference = {0}", scriptRootPreference ) );
                    if ( scriptRootPreference.Trim().Length > 0 ) {
                        var toTrim = scriptRootPreference.Trim();
                        if ( namespaceValue.StartsWith( toTrim ) ) {
                            WriteDebug( string.Format( "Trimming script root '{0}' from start of namespace", toTrim ) );
                            namespaceValue = namespaceValue.Substring( toTrim.Length );
                        }
                    }

                    if ( namespaceValue.StartsWith( "." ) ) {
                        namespaceValue = namespaceValue.Substring( 1 );
                    }

                    if ( prefixPreference.Trim().Length > 0 ) {
                        namespaceValue = string.Format( "{0}{1}{2}",
                            prefixPreference,
                            prefixPreference.EndsWith( "." ) ? "" : ".",
                            namespaceValue );
                    }

                    WriteDebug( string.Format( "Using smart namespace: '{0}'", namespaceValue ) );

                }
                else {
                    // Use the universal namespace.
                    namespaceValue = universalNamespacePreference.Trim();
                    WriteDebug( string.Format( "Using 'Universal' namespace: {0}", namespaceValue ) );
                }

                if ( namespaceValue.Trim().Length == 0 ) {
                    WriteDebug( string.Format( "Namespace is empty, probably because it was placed directly within Script Root. Not adding namespace to script." ) );
                    return;
                }

                // Read the file contents, so we can insert the namespace line, and indent other lines under it.
                string[] lines = System.IO.File.ReadAllLines( fullFilePath );


                // Determining exactly where to insert the namespace is a little tricky. A user could have modified the
                // default template in any number of ways. It seems the safest approach is to assume that all 
                // 'using' statements come first, and that the namespace declaration will appear after the last using statement.

                // Initially ensure that this template doesn't already contain a namespace.
                foreach ( var line in lines ) {
                    if ( line.StartsWith( "namespace " ) ) {
                        WriteDebug( string.Format( "This script already contains a namespace declaration ({0}). Skipping.", line ) );
                        return;
                    }
                }

                var lastUsingLineIndex = 0;
                // Find the last "using" statement in the file.
                for ( var i = lines.Length - 1; i >= 0; i-- ) {
                    if ( lines[i].StartsWith( "using " ) ) {
                        lastUsingLineIndex = i;
                        break;
                    }
                }

                var modifiedLines = lines.ToList();

                // A blank line, followed by the namespace declaration.
                modifiedLines.Insert( lastUsingLineIndex + 1, "" );
                modifiedLines.Insert( lastUsingLineIndex + 2, string.Format( "namespace {0} {{", namespaceValue ) );

                // Indent all lines in between.
                for ( var i = lastUsingLineIndex + 3; i < modifiedLines.Count; i++ ) {
                    var prefix = useSpacesPreference
                        ? new string( ' ', numberOfSpacesPreference )
                        : "\t";
                    modifiedLines[i] = string.Format( "{0}{1}", prefix, modifiedLines[i] );
                }

                // Add the closing brace.
                modifiedLines.Add( "}" );

                // Don't add a namespace if one already exists in the file.

                //fileContent = fileContent.Replace( "#AUTHOR#", AuthorName );
                //fileContent = fileContent.Replace( "#NAMESPACE#", GetNamespaceForPath( path ) );

                System.IO.File.WriteAllText( fullFilePath, string.Join( Environment.NewLine, modifiedLines.ToArray() ) );


                // Without this, the file won't update in Unity, and won't look right.
                AssetDatabase.ImportAsset( path );
            }
            catch ( Exception ex ) {
                Debug.LogError( string.Format( "Something went really wrong trying to execute SmartNS: {0}", ex.Message ) );
                Debug.LogError( string.Format( "SmartNS Failure Stack Trace: {0}", ex.StackTrace ) );
            }
        }

        #endregion




        #region Preference Menu

        private static string versionNumber = "1.2.0";

        // Have we loaded the prefs yet
        private static bool prefsLoaded = false;

        // The Preferences
        public static string prefixPreference;
        public static string scriptRootPreference;
        public static string universalNamespacePreference;
        public static bool useSpacesPreference;
        public static int numberOfSpacesPreference;
        public static bool logDebugMessagesPreference;



        private static string scriptRootPreferenceKey = "GraviaSoftware.SmartNS.scriptRootPreferenceKey";
        private static string prefixPreferenceKey = "GraviaSoftware.SmartNS.prefixPreferenceKey";
        private static string useSpacesPreferenceKey = "GraviaSoftware.SmartNS.useSpacesPreferenceKey";
        private static string numberOfSpacesPreferenceKey = "GraviaSoftware.SmartNS.numberOfSpacesPreferenceKey";
        private static string universalNamespacePreferenceKey = "GraviaSoftware.SmartNS.constantNamespacePreferenceKey";
        private static string logDebugMessagesPreferenceKey = "GraviaSoftware.SmartNS.logDebugMessagesPreferenceKey";

        private static string defaultScriptRoot = "Assets";
        private static string defaultPrefix = "";
        private static bool defaultUseSpaces = false;
        private static int defaultNumberOfSpaces = 4;
        private static string defaultUniversalNamespace = "";
        private static bool defaultLogDebugMessages = false;


        // Add preferences section named "SmartNS" to the Preferences Window
        [PreferenceItem( "SmartNS" )]
        public static void PreferencesGUI()
        {
            // Load the preferences
            EnsurePreferencesAreInitialized();

            EditorGUILayout.LabelField( string.Format( "Version {0}", versionNumber ) );

            // Preferences GUI
            EditorGUILayout.HelpBox( "SmartNS adds a namespace to new C# scripts based on the directory in which they are created. Optionally, a 'Universal' namespace can be used for all scripts.", MessageType.None );


            var scriptRootPreferenceLabel = new GUIContent( "Script Root", "This text will be trimmed from the start of the namespace. Useful for removing 'Assets' from the start of the namespace." );
            scriptRootPreference = EditorGUILayout.TextField( scriptRootPreferenceLabel, scriptRootPreference );

            EditorGUILayout.Space();
            var prefixPreferenceLabel = new GUIContent( "Namespace Prefix", "Included prior to the Smart namespace" );
            prefixPreference = EditorGUILayout.TextField( prefixPreferenceLabel, prefixPreference );

            EditorGUILayout.Space();
            var universalNamespacePreferenceLabel = new GUIContent( "Universal Namespace", "Makes SmartNS less 'smart', and uses the following namespace for all scripts, no matter where they are created." );
            universalNamespacePreference = EditorGUILayout.TextField( universalNamespacePreferenceLabel, universalNamespacePreference );

            EditorGUILayout.Space();
            var useSpacesPreferenceLabel = new GUIContent( "Indent using Spaces", "The indentation applied to the script will use spaces instead of tabs if this is selected." );
            useSpacesPreference = EditorGUILayout.Toggle( useSpacesPreferenceLabel, useSpacesPreference );

            if ( useSpacesPreference ) {
                var numberOfSpacesPreferenceLabel = new GUIContent( "# of Spaces", "The number of spaces to indent per 'tab'" );
                numberOfSpacesPreference = EditorGUILayout.IntField( numberOfSpacesPreferenceLabel, numberOfSpacesPreference );
            }




            EditorGUILayout.Space();
            EditorGUILayout.Space();
            var logDebugMessagesPreferenceLabel = new GUIContent( "Enable Debug Logging", "When enabled, debug info will be written to the log." );
            logDebugMessagesPreference = EditorGUILayout.Toggle( logDebugMessagesPreferenceLabel, logDebugMessagesPreference );


            // Save the preferences
            if ( GUI.changed ) {
                EditorPrefs.SetString( scriptRootPreferenceKey, scriptRootPreference );
                EditorPrefs.SetString( prefixPreferenceKey, prefixPreference );
                EditorPrefs.SetBool( useSpacesPreferenceKey, useSpacesPreference );
                EditorPrefs.SetInt( numberOfSpacesPreferenceKey, numberOfSpacesPreference );
                EditorPrefs.SetString( universalNamespacePreferenceKey, universalNamespacePreference );
                EditorPrefs.SetBool( logDebugMessagesPreferenceKey, logDebugMessagesPreference );

            }
        }

        private static void EnsurePreferencesAreInitialized()
        {
            if ( !prefsLoaded ) {
                scriptRootPreference = EditorPrefs.GetString( scriptRootPreferenceKey, defaultScriptRoot );
                prefixPreference = EditorPrefs.GetString( prefixPreferenceKey, defaultPrefix );
                useSpacesPreference = EditorPrefs.GetBool( useSpacesPreferenceKey, defaultUseSpaces );
                numberOfSpacesPreference = EditorPrefs.GetInt( numberOfSpacesPreferenceKey, defaultNumberOfSpaces );
                universalNamespacePreference = EditorPrefs.GetString( universalNamespacePreferenceKey, defaultUniversalNamespace );
                logDebugMessagesPreference = EditorPrefs.GetBool( logDebugMessagesPreferenceKey, defaultLogDebugMessages );

                prefsLoaded = true;
            }
        }



        #endregion


        #region Debug

        private static void WriteDebug( string message )
        {
            EnsurePreferencesAreInitialized();

            if ( logDebugMessagesPreference ) {
                Debug.Log( string.Format( "SmartNS Debug: {0}", message ) );
            }
        }

        #endregion
    }
}
