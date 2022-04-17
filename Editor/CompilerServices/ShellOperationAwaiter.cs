using System;
using System.Runtime.CompilerServices;

namespace UnityShell.Editor.CompilerServices
{
    public struct ShellOperationAwaiter : ICriticalNotifyCompletion
    {
        private readonly ShellOperationToken _shellOperationToken;

        public ShellOperationAwaiter(ShellOperationToken shellOperationToken)
        {
            _shellOperationToken = shellOperationToken;
        }

        public int GetResult()
        {
            return _shellOperationToken.ExitCode;
        }

        public bool IsCompleted => _shellOperationToken.IsDone;

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
                _shellOperationToken.OnExit += (_) => { continuation(); };
            }
        }
    }
}
