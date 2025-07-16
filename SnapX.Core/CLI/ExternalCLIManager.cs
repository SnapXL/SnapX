// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics;
using System.Text;

namespace SnapX.Core.CLI;
public abstract class ExternalCLIManager : IDisposable
{
    public event DataReceivedEventHandler OutputDataReceived;
    public event DataReceivedEventHandler ErrorDataReceived;

    public bool IsProcessRunning { get; private set; }

    protected Process? process;

    public virtual int Open(string? path, string? args = null)
    {
        if (!File.Exists(path)) return -1;
        using (process = new Process())
        {
            var psi = new ProcessStartInfo
            {
                FileName = path,
                WorkingDirectory = Path.GetDirectoryName(path),
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            process.EnableRaisingEvents = true;
            if (psi.RedirectStandardOutput) process.OutputDataReceived += cli_OutputDataReceived;
            if (psi.RedirectStandardError) process.ErrorDataReceived += cli_ErrorDataReceived;
            process.StartInfo = psi;

            Console.WriteLine($"CLI: \"{psi.FileName}\" {psi.Arguments}");
            process.Start();

            if (psi.RedirectStandardOutput) process.BeginOutputReadLine();
            if (psi.RedirectStandardError) process.BeginErrorReadLine();

            try
            {
                IsProcessRunning = true;
                process.WaitForExit();
            }
            finally
            {
                IsProcessRunning = false;
            }

            return process.ExitCode;
        }

    }

    private void cli_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            OutputDataReceived?.Invoke(sender, e);
        }
    }

    private void cli_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            ErrorDataReceived?.Invoke(sender, e);
        }
    }

    public void WriteInput(string input)
    {
        if (IsProcessRunning && process is { StartInfo.RedirectStandardInput: true })
        {
            process.StandardInput.WriteLine(input);
        }
    }

    public virtual void Close()
    {
        if (IsProcessRunning && process != null)
        {
            process.CloseMainWindow();
        }
    }

    public void Dispose()
    {
        process?.Dispose();
        GC.SuppressFinalize(this);
    }
}

