using System.Diagnostics;

namespace BoardSimulator.Runtime
{
    public static class WorkerLauncher
    {
        public static ProcessStartInfo CreateStartInfo(string executablePath)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            return processStartInfo;
        }
    }
}
