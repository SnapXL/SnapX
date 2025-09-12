namespace SnapX.NativeMessagingHost;

public class NativeMessagingHost
{
    public string Read()
    {
        string input = null;

        Stream inputStream = Console.OpenStandardInput();

        byte[] bytesLength = new byte[4];
        inputStream.ReadExactly(bytesLength);
        int inputLength = BitConverter.ToInt32(bytesLength, 0);

        if (inputLength > 0)
        {
            byte[] bytesInput = new byte[inputLength];
            inputStream.ReadExactly(bytesInput);
            input = Encoding.UTF8.GetString(bytesInput);
        }

        return input;
    }

    public void Write(string data)
    {
        Stream outputStream = Console.OpenStandardOutput();

        byte[] bytesData = Encoding.UTF8.GetBytes(data);
        byte[] bytesLength = BitConverter.GetBytes(bytesData.Length);

        outputStream.Write(bytesLength, 0, bytesLength.Length);

        if (bytesData.Length > 0)
        {
            outputStream.Write(bytesData, 0, bytesData.Length);
        }

        outputStream.Flush();
    }
}


