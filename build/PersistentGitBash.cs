using System.Diagnostics;
using System.Text;

namespace DefaultNamespace;

// For Windows only. More importantly, when build.ps1 is invoked.
// The packaging logic has a hard dependency on the UNIX* Coreutils

class PersistentGitBash : IDisposable
{
    private Process bashProcess;
    private readonly StringBuilder outputBuffer = new();
    private readonly StringBuilder errorBuffer = new();
    private TaskCompletionSource<(bool Success, string Output, string Error)>? commandCompletion;
    private readonly Lock omegaLock = new();

    public PersistentGitBash(string bashPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = bashPath,
            Arguments = "--noprofile --norc -i",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        bashProcess = Process.Start(psi) ?? throw new Exception("Failed to start bash.exe");

        bashProcess.OutputDataReceived += OnOutputDataReceived;
        bashProcess.ErrorDataReceived += OnErrorDataReceived;
        bashProcess.BeginOutputReadLine();
        bashProcess.BeginErrorReadLine();
    }

    private void OnOutputDataReceived(object? sender, DataReceivedEventArgs e)
    {
        if (e.Data == null) return;
        lock (omegaLock)
        {
            outputBuffer.AppendLine(e.Data);

            if (!e.Data.StartsWith("__CMD_DONE__")) return;
            var success = e.Data.EndsWith("0");
            var output = outputBuffer.ToString();
            var error = errorBuffer.ToString();
            outputBuffer.Clear();
            errorBuffer.Clear();

            commandCompletion?.TrySetResult((success, output, error));
        }
    }

    private void OnErrorDataReceived(object? sender, DataReceivedEventArgs e)
    {
        if (e.Data == null) return;
        lock (omegaLock)
        {
            errorBuffer.AppendLine(e.Data);
        }
    }

    public async Task<(bool Success, string Output, string Error)> RunCommandAsync(string command)
    {
        if (bashProcess == null || bashProcess.HasExited)
            throw new Exception("Bash process is not running");

        lock (omegaLock)
        {
            commandCompletion = new TaskCompletionSource<(bool, string, string)>();
            outputBuffer.Clear();
            errorBuffer.Clear();
        }

        var wrappedCommand = $"{command}; printf \"\\n__CMD_DONE__$?\\n\"";

        await bashProcess.StandardInput.WriteLineAsync(wrappedCommand);
        await bashProcess.StandardInput.FlushAsync();

        var result = await commandCompletion!.Task;
        return result;
    }

    public void Dispose()
    {
        if (bashProcess == null) return;
        try
        {
            bashProcess.StandardInput.WriteLine("exit");
            bashProcess.StandardInput.Flush();
            bashProcess.WaitForExit(2000);
        }
        catch
        {
            // Silence!
        }
        bashProcess?.Dispose();
        bashProcess = null!;
    }
}
