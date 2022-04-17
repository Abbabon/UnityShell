using System.Diagnostics;
using UnityEngine.Events;

namespace UnityShell.Editor
{
    public class ShellCommandEditorToken
    {
        public event UnityAction<UnityShellLogType, string> OnLog;
        public event UnityAction<int> OnExit;
       
        private Process _process;

        internal void BindProcess(Process process)
        {
            _process = process;
        }

        internal void FeedLog(UnityShellLogType unityShellLogType, string log)
        {
            OnLog?.Invoke(unityShellLogType, log);

            if (unityShellLogType == UnityShellLogType.Error)
            {
                HasError = true;
            }
        }

        public bool isKillRequested { get; private set; }

        public void Kill()
        {
            if (isKillRequested)
            {
                return;
            }

            isKillRequested = true;
            if (_process != null)
            {
                _process.Kill();
                _process = null;
            }
            else
            {
                MarkAsDone(137);
            }
        }

        public bool HasError { get; private set; }

        public int ExitCode { get; private set; }

        public bool IsDone { get; private set; }

        internal void MarkAsDone(int exitCode)
        {
            ExitCode = exitCode;
            IsDone = true;
            OnExit?.Invoke(exitCode);
        }

        /// <summary>
        /// This method is intended for compiler use. Don't call it in your code.
        /// </summary>
        public CompilerServices.ShellCommandAwaiter GetAwaiter()
        {
            return new CompilerServices.ShellCommandAwaiter(this);
        }
    }
}