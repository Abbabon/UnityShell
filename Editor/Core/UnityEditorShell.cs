using System.Collections.Generic;
using UnityEngine.Events;
using UnityEditor;
using System.Diagnostics;
using System.Text;

namespace UnityShell.Editor
{
    public class UnityEditorShell
    {
        // TODO: move to options
        public static string DefaultShellApp
        {
            get{
                #if UNITY_EDITOR_WIN
                var app = "cmd.exe";
                #elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
                var app = "bash";
                #else
                var app = "unsupport-platform"
                #endif
                
                return app;
            }
	    }
        
        // we are using unity actions for posterity in case we want to inspect those in-editor someday
        private static readonly List<UnityAction> ActionsQueue;

        static UnityEditorShell()
        {
            ActionsQueue = new List<UnityAction>();
            EditorApplication.update += OnUpdate;          
        }

        // while running the Unity Editor update loop, we'll unqueue any tasks if such exist.
        // actions can be 
        private static void OnUpdate()
        {
            while (ActionsQueue.Count > 0)
            {
                lock (ActionsQueue)
                {
                    var action = ActionsQueue[0];
                    try
                    {
                        action?.Invoke();
                    }
                    catch (System.Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                    finally
                    {
                        ActionsQueue.RemoveAt(0);
                    }
                }
            }
        }

        private static void Enqueue(UnityAction action)
        {
            lock (ActionsQueue)
            {
                ActionsQueue.Add(action);
            }
        }

        public static ShellCommandEditorToken Execute(string cmd, Options options = null)
        {
            var shellCommandEditorToken = new ShellCommandEditorToken();
            System.Threading.ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                Process process = null;
                options ??= new Options();
                
                try
                {
                    var processStartInfo = CreateProcessStartInfo(cmd, options);

                    // in case the command was already killed from the editor when the thread was queued
                    if (shellCommandEditorToken.isKillRequested)
                    {
                        return;
                    }

                    process = Process.Start(processStartInfo);
                    SetupProcessCallbacks(process, processStartInfo, shellCommandEditorToken);
                    ReadProcessOutput(process, shellCommandEditorToken);
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                    process?.Close();

                    Enqueue(() =>
                    {
                        shellCommandEditorToken.FeedLog(UnityShellLogType.Error, e.ToString());
                        shellCommandEditorToken.MarkAsDone(-1);
                    });
                }
            });
            return shellCommandEditorToken;
        }
        
        private static ProcessStartInfo CreateProcessStartInfo(string cmd, Options options)
        {
            var processStartInfo = new ProcessStartInfo(DefaultShellApp);
            #if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            processStartInfo.Arguments = "-c";
            #elif UNITY_EDITOR_WIN
            start.Arguments = "/c";
            #endif

            if (options.EnvironmentVariables != null)
            {
                foreach (var pair in options.EnvironmentVariables)
                {
                    var value = System.Environment.ExpandEnvironmentVariables(pair.Value);
                    if (processStartInfo.EnvironmentVariables.ContainsKey(pair.Key))
                    {
                        // UnityEngine.Debug.LogWarningFormat("Override EnvironmentVar, original = {0}, new = {1}",start.EnvironmentVariables[pair.Key],pair.Value);
                        processStartInfo.EnvironmentVariables[pair.Key] = value;
                    }
                    else
                    {
                        processStartInfo.EnvironmentVariables.Add(pair.Key, value);
                    }
                }
            }

            processStartInfo.Arguments += (" \"" + cmd + " \"");
            processStartInfo.CreateNoWindow = true;
            processStartInfo.ErrorDialog = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.WorkingDirectory = options.WorkDirectory == null ? "./" : options.WorkDirectory;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.StandardOutputEncoding = options.Encoding;
            processStartInfo.StandardErrorEncoding = options.Encoding;
            return processStartInfo;
        }

        private static void SetupProcessCallbacks(Process process, ProcessStartInfo processStartInfo, ShellCommandEditorToken shellCommandEditorToken)
        {
            shellCommandEditorToken.BindProcess(process);

            process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
            {
                UnityEngine.Debug.LogError(e.Data);
            };
            process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
            {
                UnityEngine.Debug.LogError(e.Data);
            };
            process.Exited += delegate(object sender, System.EventArgs e)
            {
                UnityEngine.Debug.LogError(e.ToString());
            };
        }

        private static void ReadProcessOutput(Process process, ShellCommandEditorToken shellCommandEditorToken)
        {
            do
            {
                var line = process.StandardOutput.ReadLine();
                if (line == null)
                {
                    break;
                }

                line = line.Replace("\\", "/");
                Enqueue(delegate() { shellCommandEditorToken.FeedLog(UnityShellLogType.Log, line); });
            } while (true);

            while (true)
            {
                var error = process.StandardError.ReadLine();
                if (string.IsNullOrEmpty(error))
                {
                    break;
                }

                Enqueue(delegate() { shellCommandEditorToken.FeedLog(UnityShellLogType.Error, error); });
            }

            process.WaitForExit();
            var exitCode = process.ExitCode;
            process.Close();
            Enqueue(() => { shellCommandEditorToken.MarkAsDone(exitCode); });
        }
        
        public class Options
        {
            public Encoding Encoding = Encoding.UTF8;
            public string WorkDirectory = "./";
            public readonly Dictionary<string,string> EnvironmentVariables = new();
        }
    }
}
