using System;
using System.Runtime.CompilerServices;

namespace UnityShell.Editor.CompilerServices
{
    public struct ShellCommandAwaiter : ICriticalNotifyCompletion
    {
        private readonly ShellCommandEditorToken _shellCommandEditorToken;

        public ShellCommandAwaiter(ShellCommandEditorToken shellCommandEditorToken)
        {
            _shellCommandEditorToken = shellCommandEditorToken;
        }

        public int GetResult()
        {
            return _shellCommandEditorToken.ExitCode;
        }

        public bool IsCompleted => _shellCommandEditorToken.IsDone;

        public void OnCompleted(Action continuation)
        {
            UnsafeOnCompleted(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (IsCompleted)
            {
                continuation();
            }
            else
            {
                _shellCommandEditorToken.OnExit += (_) => { continuation(); };
            }
        }
    }
}
