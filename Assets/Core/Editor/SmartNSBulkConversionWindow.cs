using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GraviaSoftware.SmartNS.Core.Editor
{
    public class SmartNSBulkConversionWindow : EditorWindow
    {
        [MenuItem("Window/SmartNS/Bulk Namespace Conversion...")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            SmartNSBulkConversionWindow window = (SmartNSBulkConversionWindow)EditorWindow.GetWindow(typeof(SmartNSBulkConversionWindow));
            window.titleContent = new GUIContent("Bulk Namespace Converter");
            window.Show();
        }

        private string _baseDirectory = "Assets";
        private bool _isProcessing = false;
        private bool _isPostProcessing = false;
        private List<string> _assetsToProcess;
        private int _progressCount;


        private string _scriptRootSettingsValue;
        private string _prefixSettingsValue;
        private string _universalNamespaceSettingsValue;
        private bool _useSpacesSettingsValue;
        private int _numberOfSpacesSettingsValue;

        Vector2 scrollPos;


        void OnGUI()
        {
            GUILayout.Label("SmartNS Bulk Namespace Conversion", EditorStyles.boldLabel);

            int yPos = 20;
            GUI.Box(new Rect(0, yPos, position.width, 30), "This tool will automatically ");

            yPos += 40;


            var baseDirectoryLabel = new GUIContent(string.Format("Base Directory: {0}", _baseDirectory), "SmartNS will search all scripts in, or below, this directory. Use this to limit the search to a subdirectory.");

            if (GUI.Button(new Rect(3, yPos, position.width - 6, 20), baseDirectoryLabel))
            {
                var fullPath = EditorUtility.OpenFolderPanel("Choose root folder", _baseDirectory, "");
                _baseDirectory = fullPath.Replace(Application.dataPath, "Assets");
            }


            yPos += 30;



            if (!_isProcessing)
            {
                var submitButtonContent = new GUIContent("Begin Namespace Conversion", "Begin processing scripts");
                var submitButtonStyle = new GUIStyle(GUI.skin.button);
                submitButtonStyle.normal.textColor = new Color(0, .5f, 0);
                if (GUI.Button(new Rect(position.width / 2 - 350 / 2, yPos, 350, 30), submitButtonContent, submitButtonStyle))
                {
                    string assetBasePath = (string.IsNullOrWhiteSpace(_baseDirectory) ? "Assets" : _baseDirectory).Trim();
                    if (!assetBasePath.EndsWith("/"))
                    {
                        assetBasePath += "/";
                    }


                    _assetsToProcess = AssetDatabase.GetAllAssetPaths()
                        .Where(s => s.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                            // We ALWAYS require that the scripts be within Assets, regardless of anything else. We don't want to clobber Packages, for example.
                            && s.StartsWith("Assets", StringComparison.OrdinalIgnoreCase)
                            && s.StartsWith(assetBasePath, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (EditorUtility.DisplayDialog("Are you sure?",
                        string.Format("This will process a total of {0} scripts found in or under the '{1}' directory, updating their namespaces based on your current SmartNS settings. You should back up your project before doing this, in case something goes wrong. Really continue?", _assetsToProcess.Count, assetBasePath),
                        string.Format("I'm sure. Process {0} scripts", _assetsToProcess.Count),
                        "Cancel"))
                    {
                        var smartNSSettings = SmartNSSettings.GetSerializedSettings();
                        _scriptRootSettingsValue = smartNSSettings.FindProperty("m_ScriptRoot").stringValue;
                        _prefixSettingsValue = smartNSSettings.FindProperty("m_NamespacePrefix").stringValue;
                        _universalNamespaceSettingsValue = smartNSSettings.FindProperty("m_UniversalNamespace").stringValue;
                        _useSpacesSettingsValue = smartNSSettings.FindProperty("m_IndentUsingSpaces").boolValue;
                        _numberOfSpacesSettingsValue = smartNSSettings.FindProperty("m_NumberOfSpaces").intValue;

                        _progressCount = 0;
                        _isProcessing = true;
                        _isPostProcessing = false;
                    }
                }
            }


            if (_isProcessing)
            {
                var cancelButtonContent = new GUIContent("Cancel", "Cancel script conversion");
                var cancelButtonStyle = new GUIStyle(GUI.skin.button);
                cancelButtonStyle.normal.textColor = new Color(.5f, 0, 0);
                if (GUI.Button(new Rect(position.width / 2 - 50 / 2, yPos, 50, 30), cancelButtonContent, cancelButtonStyle))
                {
                    _isProcessing = false;
                    Log("Cancelled");
                }

                yPos += 40;

                if (_progressCount < _assetsToProcess.Count)
                {
                    EditorGUI.ProgressBar(new Rect(3, yPos, position.width - 6, 20), (float)_progressCount / (float)_assetsToProcess.Count, string.Format("Processing {0} ({1}/{2})", _assetsToProcess[_progressCount], _progressCount, _assetsToProcess.Count));
                    Log("Processing " + _assetsToProcess[_progressCount]);

                    SmartNS.UpdateAssetNamespace(_assetsToProcess[_progressCount],
                        _scriptRootSettingsValue,
                        _prefixSettingsValue,
                        _universalNamespaceSettingsValue,
                        _useSpacesSettingsValue,
                        _numberOfSpacesSettingsValue);

                    _progressCount++;
                }
                else
                {
                    // We use this _isPostProcessing flag to skip over performing the post-processing
                    // for one frame. This gives the UI a chance to update first.
                    if (_isPostProcessing)
                    {
                        foreach (var path in _assetsToProcess)
                        {
                            // Without this, the script won't recompile. This will hang the UI, and that's okay, since the 
                            // alternative is to essentially kick off a project recompile for every file, which is not a good idea.
                            AssetDatabase.ImportAsset(path);
                        }

                        _isProcessing = false;
                    }
                    else
                    {
                        var message = "Finishing up. This could take a while, as the project needs to reimport and compile all affected scripts.";
                        GUI.Box(new Rect(0, yPos, position.width, 60), message);
                        Log(message);
                        _isPostProcessing = true;
                    }
                }
            }

        }

        void Update()
        {
            if (_isProcessing)
            {
                // Without this, we don't get updates every frame, and the whole window just creeps along.
                Repaint();
            }
        }

        private void Log(string message)
        {
            Debug.Log(string.Format("[SmartNS] {0}", message));
        }
    }
}