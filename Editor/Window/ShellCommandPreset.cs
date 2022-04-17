using System.Collections.Generic;
using UnityEngine;

namespace UnityShell.Editor
{
    [CreateAssetMenu(menuName = "UnityShell/ShellCommandPreset")]
    public class ShellCommandPreset : ScriptableObject
    {
        [SerializeField] 
        private string _displayName;

        [SerializeField] 
        private string _command;

        [SerializeField] 
        private string _workingDir;

        [SerializeField] 
        private string _encoding = "";

        [SerializeField] 
        private List<KeyValuePair> _customEnvironmentVars;

        private Status _status = Status.Idle;

        public void ClearStatus()
        {
            _status = Status.Idle;
        }

        public Status status => _status;

        public ShellCommandEditorToken Execute()
        {
            if (_status == Status.Running)
            {
                Debug.LogError("ShellExecutor is running. can not duplicate execute");
                return null;
            }

            _status = Status.Running;
            var options = new UnityEditorShell.Options();
            if (!string.IsNullOrEmpty(_workingDir))
            {
                options.WorkDirectory = _workingDir;
            }

            if (_customEnvironmentVars != null)
            {
                foreach (var pair in _customEnvironmentVars)
                {
                    options.EnvironmentVariables.Add(pair.key, pair.value);
                }
            }

            if (!string.IsNullOrEmpty(_encoding))
            {
                options.Encoding = System.Text.Encoding.GetEncoding(_encoding);
            }

            var task = UnityEditorShell.Execute(_command, options);
            task.OnLog += (logType, log) =>
            {
                if (logType == UnityShellLogType.Error)
                {
                    Debug.LogError(log);
                }
                else
                {
                    Debug.Log(log);
                }
            };

            task.OnExit += (exitCode) =>
            {
                Debug.Log($"{this.name} done. Exit code = " + exitCode);
                _status = exitCode == 0 ? Status.Success : Status.Failed;
            };
            return task;
        }

        public string displayName
        {
            get
            {
                if (string.IsNullOrEmpty(_displayName))
                {
                    return this.name;
                }

                return _displayName;
            }
        }

        [System.Serializable]
        public class KeyValuePair
        {
            public string key;
            public string value;
        }

        public enum Status
        {
            Idle,
            Running,
            Success,
            Failed,
        }

    }
}
