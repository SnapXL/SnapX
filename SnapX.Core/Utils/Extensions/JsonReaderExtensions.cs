using System.Text.Json;

namespace SnapX.Core.Utils.Extensions;

internal static class JsonReaderExtensions
{
    public static (long line, long bytePos)? GetPosition(this Utf8JsonReader reader)
    {
        try
        {
            var state = reader.CurrentState;

            var stateType = state.GetType();

            var lineNumberField = stateType.GetField("_lineNumber", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var bytePosField = stateType.GetField("_bytePositionInLine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (lineNumberField != null && bytePosField != null)
            {
                long line = (long)lineNumberField.GetValue(state)!;
                long bytePos = (long)bytePosField.GetValue(state)!;

                return (line, bytePos);
            }
        }
        catch
        {
            // Swallow — fallback will be used
        }

        return null;
    }
}
