using SnapX.Core.Upload.Custom.Functions;

namespace SnapX.Core.Upload.Custom;

internal static class CustomUploaderFunctionRegistry
{
    private static readonly List<CustomUploaderFunction> _functions = new();

    internal static IReadOnlyList<CustomUploaderFunction> Functions => _functions;

    internal static void Register(CustomUploaderFunction function)
    {
        _functions.Add(function);
    }
}
