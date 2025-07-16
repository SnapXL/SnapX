// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics.CodeAnalysis;
using SnapX.Core.Upload.Custom.Functions;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Parsers;

namespace SnapX.Core.Upload.Custom;

public class ShareXCustomUploaderSyntaxParser : ShareXSyntaxParser
{
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private static IEnumerable<CustomUploaderFunction> Functions = Helpers.GetInstances<CustomUploaderFunction>();

    public string? FileName { get; set; }
    public string? Input { get; set; }
    public ResponseInfo ResponseInfo { get; set; }
    public bool URLEncode { get; set; } // Only URL encodes file name and input
    public bool UseNameParser { get; set; }
    public NameParserType NameParserType { get; set; } = NameParserType.Text;

    public ShareXCustomUploaderSyntaxParser()
    {
    }

    public ShareXCustomUploaderSyntaxParser(CustomUploaderInput input)
    {
        FileName = input.FileName;
        Input = input.Input;
    }

    public override string? Parse(string? text)
    {
        if (UseNameParser && !string.IsNullOrEmpty(text))
        {
            NameParser nameParser = new NameParser(NameParserType);
            EscapeHelper escapeHelper = new EscapeHelper();
            escapeHelper.KeepEscapeCharacter = true;
            text = escapeHelper.Parse(text, nameParser.Parse);
        }

        return base.Parse(text);
    }

    protected override string? CallFunction(string functionName, string?[] parameters = null)
    {
        if (string.IsNullOrEmpty(functionName))
        {
            throw new Exception("Function name cannot be empty.");
        }

        foreach (CustomUploaderFunction function in Functions)
        {
            if (function.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase) ||
                (function.Aliases != null && function.Aliases.Any(x => x.Equals(functionName, StringComparison.OrdinalIgnoreCase))))
            {
                if (function.MinParameterCount > 0 && (parameters == null || parameters.Length < function.MinParameterCount))
                {
                    throw new Exception($"Minimum parameter count for function \"{function.Name}\" is {function.MinParameterCount}.");
                }

                return function.Call(this, parameters);
            }
        }

        throw new Exception("Invalid function name: " + functionName);
    }
}

