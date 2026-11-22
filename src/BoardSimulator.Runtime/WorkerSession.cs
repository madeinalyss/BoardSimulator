using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BoardSimulator.Runtime
{
    public sealed class WorkerSession : IAsyncDisposable
    {
        private readonly Process _process;
        private bool _faulted;
        private readonly Task<string> _stderr;

        internal WorkerSession(Process process)
        {
            _process = process;
            _process.StandardInput.NewLine = "\n";
            _stderr = process.StandardError.ReadToEndAsync();
        }

        public Task<string> GetStandardErrorAsync(CancellationToken cancellationToken)
        {
            return _stderr.WaitAsync(cancellationToken);
        }

        public async Task<string> SendCommandAsync(string command, CancellationToken cancellationToken, Func<string, string>? handleHal = null)
        {
            if (_faulted)
                throw new InvalidOperationException("Worker session is faulted.");

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _process.StandardInput.WriteLineAsync(command.AsMemory(), cancellationToken);
                await _process.StandardInput.FlushAsync(cancellationToken);

                while (true)
                {
                    string? response = await _process.StandardOutput.ReadLineAsync(cancellationToken);

                    if (response is null)
                        throw new EndOfStreamException("Worker closed stdout before replying.");

                    if (!response.StartsWith("HAL ", StringComparison.Ordinal))
                        return response;

                    if (handleHal is null)
                        throw new InvalidOperationException("HAL handler is not configured.");

                    string reply = handleHal(response);

                    await _process.StandardInput.WriteLineAsync(reply.AsMemory(), cancellationToken);
                    await _process.StandardInput.FlushAsync(cancellationToken);
                }
            }
            catch (Exception)
            {
                _faulted = true;
                throw;
            }
        }

        public async Task<int> CompleteAsync(CancellationToken cancellationToken)
        {
            _process.StandardInput.Close();
            await _process.WaitForExitAsync(cancellationToken);
            return _process.ExitCode;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                    await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2));
                }

            }
            finally
            {
                try
                {
                    await _stderr.WaitAsync(TimeSpan.FromSeconds(2));
                }
                finally
                {
                    _process.Dispose();
                }
            }
        }
    }
}
