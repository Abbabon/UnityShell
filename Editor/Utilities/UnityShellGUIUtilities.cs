using UnityEngine;
using UnityEditor;

namespace UnityShell.Editor
{
    public class UnityShellGUIUtilities
    {

        private static int _waitSpineTotal = 12;

        private static GUIContent iconRunning
        {
            get
            {
                var index = Mathf.FloorToInt((float)(EditorApplication.timeSinceStartup % 12)).ToString("00");
                return EditorGUIUtility.IconContent("d_WaitSpin" + index);
            }
        }

        public static GUIContent iconIdle
        {
            get { return EditorGUIUtility.IconContent("d_winbtn_mac_inact"); }
        }

        public static GUIContent iconFail
        {
            get { return EditorGUIUtility.IconContent("d_winbtn_mac_close"); }
        }

        public static GUIContent iconSuccess
        {
            get { return EditorGUIUtility.IconContent("d_winbtn_mac_max"); }
        }

        public static void Status(ShellCommandPreset.Status status)
        {
            GUIContent content = GUIContent.none;
            ;
            if (status == ShellCommandPreset.Status.Idle)
            {
                content = iconIdle;
            }
            else if (status == ShellCommandPreset.Status.Running)
            {
                content = iconRunning;
            }
            else if (status == ShellCommandPreset.Status.Failed)
            {
                content = iconFail;
            }
            else if (status == ShellCommandPreset.Status.Success)
            {
                content = iconSuccess;
            }

            GUILayout.Label(content, GUILayout.Width(20));
        }
    }
}