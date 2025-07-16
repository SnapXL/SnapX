// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text;

namespace SnapX.Core.Utils.Miscellaneous;

public class StringLineReader
{
    public string Text { get; private set; }
    public int Position { get; private set; }
    public int Length { get; private set; }

    public StringLineReader(string text)
    {
        Text = text;
        Length = Text.Length;
    }

    public string ReadLine()
    {
        var builder = new StringBuilder();

        while (!string.IsNullOrEmpty(Text) && Position < Length)
        {
            var ch = Text[Position];
            builder.Append(ch);
            Position++;

            if (ch != '\r' && ch != '\n' && Position != Length)
            {
                return builder.ToString();
            }

            if (ch == '\r' && Position < Length && Text[Position] == '\n')
            {
                continue;
            }

            return builder.ToString();
        }

        return null;
    }

    public string[] ReadAllLines(bool autoTrim = true)
    {
        List<string> lines = [];

        string line;

        while ((line = ReadLine()) != null)
        {
            if (autoTrim) line = line.Trim();
            lines.Add(line);
        }

        return lines.ToArray();
    }

    public void Reset()
    {
        Position = 0;
    }
}

