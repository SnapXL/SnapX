
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics;
using System.Text;

namespace SnapX.Core.CLI;
public abstract class ExternalCLIManager : IDisposable
{
    public event DataReceivedEventHandler OutputDataReceived;
    public event DataReceivedEventHandler ErrorDataReceived;

    public bool IsProcessRunning { get; private set; }

    protected Process process;

    public virtual int Open(string? path, string args = null)
    {
        if (System.IO.File.Exists(path))
        {
            using (process = new Process())
            {
                ProcessStartInfo psi = new ProcessStartInfo()
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

        return -1;
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
        if (IsProcessRunning && process != null && process.StartInfo != null && process.StartInfo.RedirectStandardInput)
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
        if (process != null)
        {
            process.Dispose();
        }
    }
}

