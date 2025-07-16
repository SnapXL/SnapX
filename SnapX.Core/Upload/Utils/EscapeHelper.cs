// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Utils;

namespace SnapX.Core.Upload.Utils;

public class EscapeHelper
{
    public string EscapeCharacter { get; set; } = @"\";
    public string EscapeableCharacter { get; set; } = "%";
    public bool KeepEscapeCharacter { get; set; }

    private string? escapeCharacterReserve = Helpers.GetRandomAlphanumeric(32);
    private string? escapeableCharacterReserve = Helpers.GetRandomAlphanumeric(32);

    public string? Parse(string? input, Func<string, string> action)
    {
        input = input.Replace(EscapeCharacter + EscapeCharacter, escapeCharacterReserve);
        input = input.Replace(EscapeCharacter + EscapeableCharacter, escapeableCharacterReserve);
        input = action(input);
        if (KeepEscapeCharacter)
        {
            input = input.Replace(escapeableCharacterReserve, EscapeCharacter + EscapeableCharacter);
            input = input.Replace(escapeCharacterReserve, EscapeCharacter + EscapeCharacter);
        }
        else
        {
            input = input.Replace(escapeableCharacterReserve, EscapeableCharacter);
            input = input.Replace(escapeCharacterReserve, EscapeCharacter);
        }
        return input;
    }
}

