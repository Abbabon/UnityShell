using System.Collections.Generic;
using UnityEngine.Events;
using UnityEditor;
using System.Diagnostics;
using System.Text;

namespace UnityShell.Editor
{
    public class EditorShell
    {
        // TODO: make this configurable
        public static string ShellApp
        {
            get{
                #if UNITY_EDITOR_WIN
                string app = "cmd.exe";
                #elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
                string app = "bash";
                #else
                string app = "unsupport-platform"
                #endif
                return app;
            }
	    }
        
        private static List<UnityAction> _actionsQueue;

        static EditorShell()
        {
            _actionsQueue = new List<UnityAction>();
            EditorApplication.update += OnUpdate;          
        }

        // while running the Unity Editor update loop, we'll unqueue and execute any tasks if such exist.
        private static void OnUpdate()
        {
            while (_actionsQueue.Count > 0)
            {
                lock (_actionsQueue)
                {
                    var action = _actionsQueue[0];
                    try
                    {
                        if (action != null)
                        {
                            action();
                        }
                    }
                    catch (System.Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                    finally
                    {
                        _actionsQueue.RemoveAt(0);
                    }
                }
            }
        }

        private static void Enqueue(UnityAction action)
        {
            lock (_actionsQueue)
            {
                _actionsQueue.Add(action);
            }
        }

        public static ShellOperationToken Execute(string cmd, Options options = null)
        {
            //TODO: split to methods
            var shellOperationToken = new ShellOperationToken();
            System.Threading.ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                Process p = null;
                try
                {
                    ProcessStartInfo start = new ProcessStartInfo(ShellApp);
                    #if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
                    start.Arguments = "-c";
                    #elif UNITY_EDITOR_WIN
                    start.Arguments = "/c";
                    #endif

                    if (options == null)
                    {
                        options = new Options();
                    }

                    if (options.EnvironmentVariables != null)
                    {
                        foreach (var pair in options.EnvironmentVariables)
                        {
                            var value = System.Environment.ExpandEnvironmentVariables(pair.Value);
                            if (start.EnvironmentVariables.ContainsKey(pair.Key))
                            {
                                // UnityEngine.Debug.LogWarningFormat("Override EnvironmentVar, original = {0}, new = {1}",start.EnvironmentVariables[pair.Key],pair.Value);
                                start.EnvironmentVariables[pair.Key] = value;
                            }
                            else
                            {
                                start.EnvironmentVariables.Add(pair.Key, value);
                            }
                        }
                    }

                    start.Arguments += (" \"" + cmd + " \"");
                    start.CreateNoWindow = true;
                    start.ErrorDialog = true;
                    start.UseShellExecute = false;
                    start.WorkingDirectory = options.WorkDirectory == null ? "./" : options.WorkDirectory;
                    start.RedirectStandardOutput = true;
                    start.RedirectStandardError = true;
                    start.RedirectStandardInput = true;
                    start.StandardOutputEncoding = options.Encoding;
                    start.StandardErrorEncoding = options.Encoding;

                    if (shellOperationToken.isKillRequested)
                    {
                        return;
                    }

                    p = Process.Start(start);
                    shellOperationToken.BindProcess(p);

                    p.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                    {
                        UnityEngine.Debug.LogError(e.Data);
                    };
                    p.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                    {
                        UnityEngine.Debug.LogError(e.Data);
                    };
                    p.Exited += delegate(object sender, System.EventArgs e)
                    {
                        UnityEngine.Debug.LogError(e.ToString());
                    };

                    do
                    {
                        string line = p.StandardOutput.ReadLine();
                        if (line == null)
                        {
                            break;
                        }

                        line = line.Replace("\\", "/");
                        Enqueue(delegate() { shellOperationToken.FeedLog(UnityShellLogType.Log, line); });
                    } while (true);

                    while (true)
                    {
                        string error = p.StandardError.ReadLine();
                        if (string.IsNullOrEmpty(error))
                        {
                            break;
                        }

                        Enqueue(delegate() { shellOperationToken.FeedLog(UnityShellLogType.Error, error); });
                    }

                    p.WaitForExit();
                    var exitCode = p.ExitCode;
                    p.Close();
                    Enqueue(() => { shellOperationToken.MarkAsDone(exitCode); });
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                    if (p != null)
                    {
                        p.Close();
                    }

                    Enqueue(() =>
                    {
                        shellOperationToken.FeedLog(UnityShellLogType.Error, e.ToString());
                        shellOperationToken.MarkAsDone(-1);
                    });
                }
            });
            return shellOperationToken;
        }

        public class Options
        {
            public Encoding Encoding = Encoding.UTF8;
            public string WorkDirectory = "./";
            public readonly Dictionary<string,string> EnvironmentVariables = new();
        }
    }
}
