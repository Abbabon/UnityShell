using UnityEditor;
using UnityEngine;

namespace UnityShell.Editor
{
    [CustomEditor(typeof(ShellCommandPreset))]
    public class ShellCommandPresetInspector : UnityEditor.Editor
    {
        private bool _expandSystemEnvironments;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            _expandSystemEnvironments = EditorGUILayout.Foldout(_expandSystemEnvironments, "SystemEnvironmentVars");
            if (_expandSystemEnvironments)
            {
                var envs = System.Environment.GetEnvironmentVariables();
                foreach (var key in envs.Keys)
                {
                    var value = envs[key];
                    EditorGUILayout.LabelField((string)key, (string)value);
                }
            }

            if (GUILayout.Button("Execute"))
            {
                (target as ShellCommandPreset)?.Execute();
            }
        }
    }
}