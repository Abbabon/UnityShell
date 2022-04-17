using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using System.Threading.Tasks;

namespace UnityShell.Editor.Tests
{
    public class EditorShellTests
    {
        [UnityTest]
        public IEnumerator EchoHelloWorld()
        {
            var task = EditorShell.Execute("echo hello world", new EditorShell.Options());
            task.OnLog += (logType, log) =>
            {
                Debug.Log(log);
                LogAssert.Expect(LogType.Log, "hello world");
            };
            task.OnExit += (code) =>
            {
                Debug.Log("Exit with code = " + code);
                Assert.True(code == 0);
            };
            yield return new ShellOperationYieldable(task);
        }

        [UnityTest]
        public IEnumerator EchoAsync()
        {
            var task = ExecuteShellAsync("echo hello world");
            yield return new TaskYieldable<int>(task);
            Assert.True(task.Result == 0);
        }


        [UnityTest]
        public IEnumerator ExitWithCode1Async()
        {
            var task = ExecuteShellAsync("exit 1");
            yield return new TaskYieldable<int>(task);
            Debug.Log("exit with code = " + task.Result);
            Assert.True(task.Result == 1);
        }

        [UnityTest]
        public IEnumerator KillAsyncOperation()
        {
            var operation = EditorShell.Execute("sleep 5", new EditorShell.Options());
            KillAfter1Second(operation);
            var task = GetOperationTask(operation);
            yield return new TaskYieldable<int>(task);
            Debug.Log("exit with code = " + task.Result);
            Assert.True(task.Result == 137);
        }

        private async void KillAfter1Second(ShellOperationToken shellOperationToken)
        {
            await Task.Delay(1000);
            shellOperationToken.Kill();
        }

        private async Task<int> GetOperationTask(ShellOperationToken shellOperationToken)
        {
            var code = await shellOperationToken;
            return code;
        }

        private async Task<int> ExecuteShellAsync(string cmd)
        {
            var task = EditorShell.Execute(cmd, new EditorShell.Options());
            var code = await task;
            return code;
        }


        private class ShellOperationYieldable : CustomYieldInstruction
        {
            private readonly ShellOperationToken _shellOperationToken;

            public ShellOperationYieldable(ShellOperationToken shellOperationToken)
            {
                _shellOperationToken = shellOperationToken;
            }

            public override bool keepWaiting => !_shellOperationToken.IsDone;
        }


        private class TaskYieldable<T> : CustomYieldInstruction
        {
            private readonly Task<T> _task;

            public TaskYieldable(Task<T> task)
            {
                _task = task;
            }

            public override bool keepWaiting => !_task.IsCompleted;
        }
    }
}