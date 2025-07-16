// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text.RegularExpressions;

namespace SnapX.Core.Upload.Custom.Functions;

// Example: {regex:(?<=href=").+(?=")}
// Example: {regex:href="(.+)"|1}
// Example: {regex:href="(?<url>.+)"|url}
// Example: {regex:{response}|href="(.+)"|1}
internal class CustomUploaderFunctionRegex : CustomUploaderFunction
{
    public override string Name { get; } = "regex";

    public override int MinParameterCount { get; } = 1;

    public override string? Call(ShareXCustomUploaderSyntaxParser parser, string?[] parameters)
    {
        string? input;
        string? pattern;
        string? group = "";

        if (parameters.Length > 2)
        {
            // {regex:input|pattern|group}
            input = parameters[0];
            pattern = parameters[1];
            group = parameters[2];
        }
        else
        {
            // {regex:pattern}
            input = parser.ResponseInfo.ResponseText;
            pattern = parameters[0];

            if (parameters.Length > 1)
            {
                // {regex:pattern|group}
                group = parameters[1];
            }
        }

        if (!string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(pattern))
        {
            Match match = Regex.Match(input, pattern);

            if (match.Success)
            {
                if (!string.IsNullOrEmpty(group))
                {
                    if (int.TryParse(group, out int groupNumber))
                    {
                        return match.Groups[groupNumber].Value;
                    }
                    else
                    {
                        return match.Groups[group].Value;
                    }
                }

                return match.Value;
            }
        }

        return null;
    }
}
