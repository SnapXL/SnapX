// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SnapX.Core.Utils.Miscellaneous;

public class SevenZipManager
{
    public string? SevenZipPath { get; set; }

    public SevenZipManager()
    {
        SevenZipPath = FileHelpers.GetAbsolutePath(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "7z.exe" : "7z");
    }

    public SevenZipManager(string? sevenZipPath)
    {
        SevenZipPath = sevenZipPath;
    }

    public bool Extract(string archivePath, string destination)
    {
        string arguments = $"x \"{archivePath}\" -o\"{destination}\" -y";
        return Run(arguments) == 0;
    }

    public bool Extract(string archivePath, string destination, List<string> files)
    {
        string fileArgs = string.Join(" ", files.Select(x => $"\"{x}\""));
        string arguments = $"e \"{archivePath}\" -o\"{destination}\" {fileArgs} -r -y";
        return Run(arguments) == 0;
    }

    public bool Compress(string archivePath, List<string> files, string workingDirectory = "")
    {
        if (System.IO.File.Exists(archivePath))
        {
            System.IO.File.Delete(archivePath);
        }

        string fileArgs = string.Join(" ", files.Select(x => $"\"{x}\""));
        string arguments = $"a -tzip \"{archivePath}\" {fileArgs} -mx=9";
        return Run(arguments, workingDirectory) == 0;
    }

    private int Run(string arguments, string workingDirectory = "")
    {
        using (Process process = new Process())
        {
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = SevenZipPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                psi.WorkingDirectory = workingDirectory;
            }

            process.StartInfo = psi;
            process.Start();
            process.WaitForExit();

            return process.ExitCode;
        }
    }
}

