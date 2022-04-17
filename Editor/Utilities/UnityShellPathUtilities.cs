using System.Text;

namespace UnityShell.Editor
{
    public class UnityShellPathUtilities
    {
        #if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
        private static char PATH_SPLIT_CHAR = ':';
        #elif UNITY_EDITOR_WIN
        private static char PATH_SPLIT_CHAR = ';';
        #else
        private static char PATH_SPLIT_CHAR = ':';
        #endif

        public static string JoinPaths(string[] paths)
        {
            var builder = new StringBuilder();
            for (var pathPart = 0; pathPart < paths.Length; pathPart++)
            {
                builder.Append(paths[pathPart]);
                if (pathPart < paths.Length - 1)
                {
                    builder.Append(PATH_SPLIT_CHAR);
                }
            }

            return builder.ToString();
        }

        public static string[] GetPaths()
        {
            var path = System.Environment.GetEnvironmentVariable("PATH");
            return path?.Split(PATH_SPLIT_CHAR);
        }
    }
}