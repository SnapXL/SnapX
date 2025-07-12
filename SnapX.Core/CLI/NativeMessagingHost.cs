
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text;

namespace SnapX.Core.CLI;
public record NativeMessagingHost
{
    public string Read()
    {
        var input = "";

        var inputStream = Console.OpenStandardInput();

        var bytesLength = new byte[4];
        inputStream.ReadExactly(bytesLength);
        var inputLength = BitConverter.ToInt32(bytesLength, 0);

        if (inputLength <= 0) return input;
        var bytesInput = new byte[inputLength];
        inputStream.ReadExactly(bytesInput);
        input = Encoding.UTF8.GetString(bytesInput);

        return input;
    }

    public void Write(string data)
    {
        var outputStream = Console.OpenStandardOutput();

        var bytesData = Encoding.UTF8.GetBytes(data);
        var bytesLength = BitConverter.GetBytes(bytesData.Length);

        outputStream.Write(bytesLength, 0, bytesLength.Length);

        if (bytesData.Length > 0)
        {
            outputStream.Write(bytesData, 0, bytesData.Length);
        }

        outputStream.Flush();
    }
}

