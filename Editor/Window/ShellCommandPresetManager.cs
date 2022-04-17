using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace UnityShell.Editor
{
    public class ShellCommandPresetsManager : EditorWindow
    {
        [MenuItem("Tools/ShellCommandPresetsManager")]
        private static void ShowWindow()
        {
            var window = GetWindow<ShellCommandPresetsManager>();
            window.titleContent = new GUIContent("ShellCommandPresets");
            window.Show();
        }

        private List<ShellCommandPreset> _commandPresets = new List<ShellCommandPreset>();

        private void OnFocus()
        {
            _commandPresets = AssetDatabase.FindAssets("t:ShellCommandPreset").Select((guid) =>
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                return AssetDatabase.LoadAssetAtPath<ShellCommandPreset>(path);
            }).ToList();
        }

        private void OnGUI()
        {

            EditorGUILayout.LabelField("Command Presets:");
            foreach (var executor in _commandPresets)
            {
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                UnityShellGUIUtilities.Status(executor.status);
                EditorGUILayout.LabelField(executor.displayName);
                if (GUILayout.Button("Edit"))
                {
                    ShellCommandPresetEditorWindow.Open(executor);
                }

                EditorGUI.BeginDisabledGroup(executor.status == ShellCommandPreset.Status.Running);
                if (GUILayout.Button("Execute"))
                {
                    executor.Execute().OnExit += (exitCode) => { this.Repaint(); };
                }

                EditorGUI.EndDisabledGroup();
                GUILayout.EndHorizontal();
            }
        }
    }

    public class ShellCommandPresetEditorWindow : EditorWindow
    {
        private static ShellCommandPreset _commandPreset;
        private static ShellCommandPresetEditorWindow _instance;

        public static void Open(ShellCommandPreset commandPreset)
        {
            if (_instance != null)
            {
                _instance.Close();
            }

            _commandPreset = commandPreset;
            var win = CreateInstance<ShellCommandPresetEditorWindow>();
            win.titleContent = new GUIContent("Editor");
            win.ShowUtility();
            _instance = win;
        }


        private UnityEditor.Editor _inspector;

        private void OnEnable()
        {
            _inspector = UnityEditor.Editor.CreateEditor(_commandPreset, typeof(ShellCommandPresetInspector));
        }


        private void OnDisable()
        {
            _inspector = null;
            _commandPreset = null;
        }

        private void OnGUI()
        {
            if (_inspector == null)
            {
                return;
            }

            _inspector.OnInspectorGUI();
        }
    }
}