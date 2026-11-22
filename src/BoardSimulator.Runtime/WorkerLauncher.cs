using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };
            return processStartInfo;
        }

        public static async Task<WorkerSession> StartAsync(string executablePath, CancellationToken cancellationToken)
        {
            Process process = new() { StartInfo = CreateStartInfo(executablePath) };
            try
            {
                process.Start();
            }
            catch
            {
                process.Dispose();
                throw;
            }
            WorkerSession session = new WorkerSession(process);
            try
            {
                string? greeting = await process.StandardOutput.ReadLineAsync(cancellationToken);
                if (greeting is null)
                    throw new EndOfStreamException("Worker closed stdout before greeting.");
                if (greeting != "HELLO 1")
                    throw new InvalidDataException("Unexpected worker greeting.");
                return session;
            }
            catch
            {
                await session.DisposeAsync();
                throw;
            }
        }
    }
}
