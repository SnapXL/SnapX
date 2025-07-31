
// SPDX-License-Identifier: GPL-3.0-or-later



using System.Text.Json.Nodes;
using Json.Path;

namespace SnapX.Core.Upload.Custom.Functions;

// Example: {json:files[0].url}
// Example: {json:{response}|files[0].url}
internal class CustomUploaderFunctionJson : CustomUploaderFunction
{
    public override string Name { get; } = "json";

    public override int MinParameterCount { get; } = 1;

    public override string? Call(ShareXCustomUploaderSyntaxParser parser, string?[] parameters)
    {
        // https://goessner.net/articles/JsonPath/
        string? input;
        string? jsonPath;

        if (parameters.Length > 1)
        {
            // {json:input|jsonPath}
            input = parameters[0];
            jsonPath = parameters[1];
        }
        else
        {
            // {json:jsonPath}
            input = parser.ResponseInfo?.ResponseText;
            jsonPath = parameters[0];
        }

        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(jsonPath)) return null;
        if (!jsonPath.StartsWith("$."))
        {
            jsonPath = "$." + jsonPath;
        }
        var json = JsonPath.Parse(jsonPath);
        var parsed = JsonNode.Parse(input);
        return json.Evaluate(parsed).ToString();

    }
}
