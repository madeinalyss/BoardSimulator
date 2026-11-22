namespace BoardSimulator.Runtime
{
    public readonly record struct WorkerResult(
        string StandardOutput,
        string StandardError,
        int ExitCode
    );
}
