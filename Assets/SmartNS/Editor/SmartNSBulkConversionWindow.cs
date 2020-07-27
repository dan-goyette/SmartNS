using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GraviaSoftware.SmartNS.Editor
{


    public class SmartNSBulkConversionWindow : EditorWindow
    {
        [MenuItem("Window/SmartNS Bulk Namespace Conversion")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(SmartNSBulkConversionWindow));
        }


        private string _baseDirectory = "Assets";
        private bool _isProcessing = false;
        private List<string> _assetsToProcess;
        private int _progressCount;

        Vector2 scrollPos;

        void OnGUI()
        {
            GUILayout.Label("SmartNS Bulk Namespace Conversion", EditorStyles.boldLabel);

            var baseDirectoryLabel = new GUIContent("Base Directory", "SmartNS will search all scripts in, or below, this directory. Use this to limit the search to a subdirectory.");
            if (_isProcessing)
            {
                var baseDirectoryLabelStyle = new GUIStyle(GUI.skin.button);
                baseDirectoryLabelStyle.normal.textColor = Color.black;
                EditorGUILayout.LabelField(baseDirectoryLabel, baseDirectoryLabelStyle);
            }
            else
            {
                _baseDirectory = EditorGUILayout.TextField(baseDirectoryLabel, _baseDirectory);
            }



            if (!_isProcessing)
            {
                var submitButtonContent = new GUIContent("Begin Namespace Conversion", "Begin processing scripts");
                var submitButtonStyle = new GUIStyle(GUI.skin.button);
                submitButtonStyle.normal.textColor = new Color(0, .5f, 0);
                if (GUI.Button(new Rect(10, 70, 350, 30), submitButtonContent, submitButtonStyle))
                {
                    string assetBasePath = string.IsNullOrWhiteSpace(_baseDirectory) ? "Assets" : _baseDirectory;

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
                        _progressCount = 0;
                        _isProcessing = true;
                    }
                }
            }

            if (_isProcessing)
            {
                var cancelButtonContent = new GUIContent("Cancel", "Cancel script conversion");
                var cancelButtonStyle = new GUIStyle(GUI.skin.button);
                cancelButtonStyle.normal.textColor = new Color(.5f, 0, 0);
                if (GUI.Button(new Rect(10, 70, 50, 30), cancelButtonContent, cancelButtonStyle))
                {
                    _isProcessing = false;
                    Debug.Log("Cancelled");
                }

                if (_progressCount < _assetsToProcess.Count)
                {
                    EditorGUI.ProgressBar(new Rect(3, 45, position.width - 6, 20), (float)_progressCount / (float)_assetsToProcess.Count, string.Format("Processing {0} ({1}/{2})", _assetsToProcess[_progressCount], _progressCount, _assetsToProcess.Count));
                    Debug.Log("Processing " + _assetsToProcess[_progressCount]);




                    _progressCount++;
                }
                else
                {
                    _isProcessing = false;
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
    }
}
