// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics;
using System.Text;
using SnapX.Core.Utils;

if (args.Length == 0)
{
    Console.WriteLine("This executable is used to receive data from a browser addon and send it to SnapX.");
    return;
}

try
{
    var host = new SnapX.Core.CLI.NativeMessagingHost();
    var input = host.Read();

    if (!string.IsNullOrEmpty(input))
    {
        host.Write(input);
        var snapXPath = FileHelpers.FindSnapX();

        var tempFilePath = FileHelpers.GetTempFilePath("json");
        File.WriteAllText(tempFilePath, input, Encoding.UTF8);

        var startInfo = new ProcessStartInfo
        {
            FileName = snapXPath,
            Arguments = $"-NativeMessagingInput \"{tempFilePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null) return;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Debug.WriteLine($"Output: {output}");
        if (process.ExitCode == 0) return;
        Console.Error.WriteLine($"Process exited with error code {process.ExitCode}");
        Console.Error.WriteLine($"Error output: {error}");
    }
}
catch (Exception e)
{
    Console.Error.WriteLine($"{e.GetType()}: {e.Message}\n{e.StackTrace}");
}

