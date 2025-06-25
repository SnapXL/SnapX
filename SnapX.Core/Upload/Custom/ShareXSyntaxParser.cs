
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text;

namespace SnapX.Core.Upload.Custom;

public abstract class ShareXSyntaxParser
{
    public virtual char SyntaxStart => '{';
    public virtual char SyntaxEnd => '}';
    public virtual char SyntaxParameterStart => ':';
    public virtual char SyntaxParameterDelimiter => '|';
    public virtual char SyntaxEscape => '\\';

    public virtual string? Parse(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        return Parse(text, false, 0, out _);
    }

    private string? Parse(string? text, bool isFunction, int startPosition, out int endPosition)
    {
        var sbOutput = new StringBuilder();
        bool escape = false;
        int i;

        for (i = startPosition; i < text.Length; i++)
        {
            char c = text[i];

            if (!escape)
            {
                if (c == SyntaxStart)
                {
                    string? parsed = Parse(text, true, i + 1, out i);
                    sbOutput.Append(parsed);
                    continue;
                }
                else if (c == SyntaxEnd || c == SyntaxParameterDelimiter)
                {
                    break;
                }
                else if (c == SyntaxEscape)
                {
                    escape = true;
                    continue;
                }
                else if (isFunction && c == SyntaxParameterStart)
                {
                    List<string?> parameters = [];

                    do
                    {
                        string? parsed = Parse(text, false, i + 1, out i);
                        parameters.Add(parsed);
                    } while (i < text.Length && text[i] == SyntaxParameterDelimiter);

                    endPosition = i;

                    return CallFunction(sbOutput.ToString(), parameters.ToArray());
                }
            }

            escape = false;
            sbOutput.Append(c);
        }

        endPosition = i;

        if (isFunction)
        {
            return CallFunction(sbOutput.ToString());
        }

        return sbOutput.ToString();
    }

    protected abstract string? CallFunction(string functionName, string?[] parameters = null);
}
