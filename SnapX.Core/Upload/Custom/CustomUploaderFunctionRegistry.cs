using SnapX.Core.Upload.Custom.Functions;

namespace SnapX.Core.Upload.Custom;

internal static class CustomUploaderFunctionRegistry
{
    private static readonly List<CustomUploaderFunction> _functions = new();

    internal static IReadOnlyList<CustomUploaderFunction> Functions => _functions;

    static CustomUploaderFunctionRegistry()
    {
        // Must manually do this shit wtf
        r(new CustomUploaderFunctionBase64());
        r(new CustomUploaderFunctionFileName());
        r(new CustomUploaderFunctionHeader());
        r(new CustomUploaderFunctionInput());
        r(new CustomUploaderFunctionInputBox());
        r(new CustomUploaderFunctionJson());
        r(new CustomUploaderFunctionOutputBox());
        r(new CustomUploaderFunctionRandom());
        r(new CustomUploaderFunctionRegex());
        r(new CustomUploaderFunctionResponse());
        r(new CustomUploaderFunctionResponseURL());
        r(new CustomUploaderFunctionSelect());
        r(new CustomUploaderFunctionXml());
    }

    internal static void r(CustomUploaderFunction function)
    {
        _functions.Add(function);
    }
}
