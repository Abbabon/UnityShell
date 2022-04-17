using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using System.Threading.Tasks;

namespace UnityShell.Editor.Tests
{
    public class UnityShellTests
    {
        [UnityTest]
        public IEnumerator EchoHelloWorld()
        {
            var task = UnityEditorShell.Execute("echo hello world", new UnityEditorShell.Options());
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
            var operation = UnityEditorShell.Execute("sleep 5", new UnityEditorShell.Options());
            KillAfter1Second(operation);
            var task = GetOperationTask(operation);
            yield return new TaskYieldable<int>(task);
            Debug.Log("exit with code = " + task.Result);
            Assert.True(task.Result == 137);
        }

        private async void KillAfter1Second(ShellCommandEditorToken shellCommandEditorToken)
        {
            await Task.Delay(1000);
            shellCommandEditorToken.Kill();
        }

        private async Task<int> GetOperationTask(ShellCommandEditorToken shellCommandEditorToken)
        {
            var code = await shellCommandEditorToken;
            return code;
        }

        private async Task<int> ExecuteShellAsync(string cmd)
        {
            var task = UnityEditorShell.Execute(cmd, new UnityEditorShell.Options());
            var code = await task;
            return code;
        }


        private class ShellOperationYieldable : CustomYieldInstruction
        {
            private readonly ShellCommandEditorToken _shellCommandEditorToken;

            public ShellOperationYieldable(ShellCommandEditorToken shellCommandEditorToken)
            {
                _shellCommandEditorToken = shellCommandEditorToken;
            }

            public override bool keepWaiting => !_shellCommandEditorToken.IsDone;
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