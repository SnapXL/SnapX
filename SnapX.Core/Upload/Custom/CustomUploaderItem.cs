// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Miscellaneous;
using SnapX.Core.Utils.Parsers;

namespace SnapX.Core.Upload.Custom;

public record CustomUploaderItem
{
    [DefaultValue("")]
    public string Version { get; set; }

    [DefaultValue("")]
    public string? Name { get; set; }

    public bool ShouldSerializeName() => !string.IsNullOrEmpty(Name) && Name != URLHelpers.GetHostName(RequestURL);
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [DefaultValue(CustomUploaderDestinationType.None)]
    public CustomUploaderDestinationType DestinationType { get; set; }
    [JsonConverter(typeof(HttpMethodConverter))]
    [JsonInclude]
    // System.Text.Json does not automatically apply custom converters to built-in reference types like HttpMethod,
    // even if the converter is registered globally.
    // To ensure the converter is used during serialization and deserialization,
    // we must explicitly declare [JsonConverter(typeof(HttpMethodConverter))] on the property or type.
    public HttpMethod RequestMethod { get; set; } = HttpMethod.Post;

    [DefaultValue("")]
    public string? RequestURL { get; set; }

    [DefaultValue(null)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string?>? Parameters { get; set; }

    public bool ShouldSerializeParameters() => Parameters is { Count: > 0 };

    [DefaultValue(null)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string?> Headers { get; set; }

    public bool ShouldSerializeHeaders() => Headers is { Count: > 0 };
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [DefaultValue(CustomUploaderBody.None)]
    public CustomUploaderBody Body { get; set; }

    [DefaultValue(null)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string?>? Arguments { get; set; }

    public bool ShouldSerializeArguments() => (Body == CustomUploaderBody.MultipartFormData || Body == CustomUploaderBody.FormURLEncoded) &&
                                              Arguments is { Count: > 0 };

    [DefaultValue("")] public string FileFormName { get; set; } = "";

    public bool ShouldSerializeFileFormName() => Body == CustomUploaderBody.MultipartFormData && !string.IsNullOrEmpty(FileFormName);

    [DefaultValue(null)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Data { get; set; }

    public bool ShouldSerializeData() => (Body == CustomUploaderBody.JSON || Body == CustomUploaderBody.XML) && !string.IsNullOrEmpty(Data);
    [JsonConverter(typeof(JsonStringEnumConverter))]
    // TEMP: For backward compatibility
    public ResponseType ResponseType { private get; set; }

    [DefaultValue("")]
    public string? URL { get; set; }

    [DefaultValue("")]
    public string? ThumbnailURL { get; set; }

    [DefaultValue("")]
    public string? DeletionURL { get; set; }

    [DefaultValue("")]
    public string? ErrorMessage { get; set; }

    public CustomUploaderItem()
    {
        Version = Helpers.GetApplicationVersion();
        Body = CustomUploaderBody.None;
    }

    // public static CustomUploaderItem Init()
    // {
    //     return new CustomUploaderItem()
    //     {
    //         Version = Helpers.GetApplicationVersion(),
    //         RequestMethod = HttpMethod.Post,
    //         Body = CustomUploaderBody.MultipartFormData
    //     };
    // }

    public override string? ToString()
    {
        if (!string.IsNullOrEmpty(Name))
        {
            return Name;
        }

        string? name = URLHelpers.GetHostName(RequestURL);

        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        return "Name";
    }

    public string GetFileName()
    {
        return ToString() + ".sxcu";
    }

    public string? GetRequestURL(CustomUploaderInput input)
    {
        if (string.IsNullOrEmpty(RequestURL))
        {
            throw new Exception("Custom uploader RequestURL must be configured.");
        }

        var parser = new ShareXCustomUploaderSyntaxParser(input)
        {
            URLEncode = true
        };
        var url = parser.Parse(RequestURL);

        url = URLHelpers.FixPrefix(url);

        var parameters = GetParameters(input);
        return URLHelpers.CreateQueryString(url, parameters);
    }

    public Dictionary<string, string?> GetParameters(CustomUploaderInput input)
    {
        Dictionary<string, string?> parameters = [];

        if (Parameters == null) return parameters;
        var parser = new ShareXCustomUploaderSyntaxParser(input)
        {
            UseNameParser = true
        };

        foreach (KeyValuePair<string, string?> parameter in Parameters)
        {
            parameters.Add(parameter.Key, parser.Parse(parameter.Value));
        }

        return parameters;
    }

    public string GetContentType()
    {
        switch (Body)
        {
            case CustomUploaderBody.MultipartFormData:
                return RequestHelpers.ContentTypeMultipartFormData;
            case CustomUploaderBody.FormURLEncoded:
                return RequestHelpers.ContentTypeURLEncoded;
            case CustomUploaderBody.JSON:
                return RequestHelpers.ContentTypeJSON;
            case CustomUploaderBody.XML:
                return RequestHelpers.ContentTypeXML;
            case CustomUploaderBody.Binary:
                return RequestHelpers.ContentTypeOctetStream;
        }

        return null;
    }

    public string? GetData(CustomUploaderInput input)
    {
        var nameParser = new NameParser(NameParserType.Text);
        string? result = nameParser.Parse(Data);

        Dictionary<string, string?> replace = new Dictionary<string, string?>
        {
            { "{input}", EncodeBodyData(input.Input) },
            { "{filename}", EncodeBodyData(input.FileName) }
        };
        result = result.BatchReplace(replace, StringComparison.OrdinalIgnoreCase);

        return result;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private string? EncodeBodyData(string? input)
    {
        if (!string.IsNullOrEmpty(input))
        {
            if (Body == CustomUploaderBody.JSON)
            {
                return URLHelpers.JSONEncode(input);
            }
            else if (Body == CustomUploaderBody.XML)
            {
                return URLHelpers.XMLEncode(input);
            }
        }

        return input;
    }

    public string GetFileFormName()
    {
        if (string.IsNullOrEmpty(FileFormName))
        {
            throw new Exception("Custom uploader FileFormName must be configured.");
        }

        return FileFormName;
    }

    public Dictionary<string, string?> GetArguments(CustomUploaderInput input)
    {
        Dictionary<string, string?> arguments = [];

        if (Arguments != null)
        {
            var parser = new ShareXCustomUploaderSyntaxParser(input)
            {
                UseNameParser = true
            };

            foreach (KeyValuePair<string, string?> arg in Arguments)
            {
                arguments.Add(arg.Key, parser.Parse(arg.Value));
            }
        }

        return arguments;
    }

    public NameValueCollection GetHeaders(CustomUploaderInput input)
    {
        if (Headers is { Count: > 0 })
        {
            var collection = new NameValueCollection();

            var parser = new ShareXCustomUploaderSyntaxParser(input)
            {
                UseNameParser = true
            };

            foreach (KeyValuePair<string, string?> header in Headers)
            {
                collection.Add(header.Key, parser.Parse(header.Value));
            }

            return collection;
        }

        return null;
    }

    public void ParseResponse(UploadResult result, ResponseInfo responseInfo, UploaderErrorManager errors, CustomUploaderInput input, bool isShortenedURL = false)
    {
        if (result == null || responseInfo == null) return;
        result.ResponseInfo = responseInfo;

        responseInfo.ResponseText ??= "";

        var parser = new ShareXCustomUploaderSyntaxParser()
        {
            FileName = input.FileName,
            ResponseInfo = responseInfo,
            URLEncode = true
        };

        if (responseInfo.IsSuccess)
        {
            string? url;

            if (!string.IsNullOrEmpty(URL))
            {
                url = parser.Parse(URL);

                if (string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(URL) && URL.Contains("{output:"))
                {
                    result.IsURLExpected = false;
                }
            }
            else
            {
                url = parser.ResponseInfo.ResponseText;
            }

            if (isShortenedURL)
            {
                result.ShortenedURL = url;
            }
            else
            {
                result.URL = url;
            }

            result.ThumbnailURL = parser.Parse(ThumbnailURL);
            result.DeletionURL = parser.Parse(DeletionURL);
        }
        else
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                string? parsedErrorMessage = parser.Parse(ErrorMessage);

                if (!string.IsNullOrEmpty(parsedErrorMessage))
                {
                    errors.AddFirst(parsedErrorMessage);
                }
            }
        }
    }

    public void TryParseResponse(UploadResult result, ResponseInfo responseInfo, UploaderErrorManager errors, CustomUploaderInput input, bool isShortenedURL = false)
    {
        try
        {
            ParseResponse(result, responseInfo, errors, input, isShortenedURL);
        }
        catch (JsonException e)
        {
            var hostName = URLHelpers.GetHostName(RequestURL);
            errors.AddFirst($"Invalid response content is returned from host ({hostName}), expected response content is JSON." +
                Environment.NewLine + Environment.NewLine + e);
        }
        catch (Exception e)
        {
            var hostName = URLHelpers.GetHostName(RequestURL);
            errors.AddFirst($"Unable to parse response content returned from host ({hostName})." +
                Environment.NewLine + Environment.NewLine + e);
        }
    }

    public void CheckBackwardCompatibility()
    {
        CheckRequestURL();

        if (string.IsNullOrEmpty(Version) || Helpers.CompareVersion(Version, "12.3.1") <= 0)
        {
            if (RequestMethod == HttpMethod.Post)
            {
                Body = CustomUploaderBody.MultipartFormData;
            }
            else
            {
                Body = CustomUploaderBody.None;

                if (Arguments != null)
                {
                    Parameters ??= [];

                    foreach (KeyValuePair<string, string?> pair in Arguments)
                    {
                        if (!Parameters.ContainsKey(pair.Key))
                        {
                            Parameters.Add(pair.Key, pair.Value);
                        }
                    }

                    Arguments = null;
                }
            }

            if (ResponseType == ResponseType.RedirectionURL)
            {
                if (string.IsNullOrEmpty(URL))
                {
                    URL = "$responseurl$";
                }

                URL = URL.Replace("$response$", "$responseurl$");
                ThumbnailURL = ThumbnailURL?.Replace("$response$", "$responseurl$");
                DeletionURL = DeletionURL?.Replace("$response$", "$responseurl$");
            }
            else if (ResponseType == ResponseType.Headers)
            {
                URL = "Response type option is deprecated, please use \\$header:header_name\\$ syntax instead.";
            }
            else if (ResponseType == ResponseType.LocationHeader)
            {
                if (string.IsNullOrEmpty(URL))
                {
                    URL = "$header:Location$";
                }

                URL = URL.Replace("$response$", "$header:Location$");
                ThumbnailURL = ThumbnailURL?.Replace("$response$", "$header:Location$");
                DeletionURL = DeletionURL?.Replace("$response$", "$header:Location$");
            }

            ResponseType = ResponseType.Text;

            Version = "13.7.1";
        }

        if (Helpers.CompareVersion(Version, "13.7.1") <= 0)
        {
            RequestURL = MigrateOldSyntax(RequestURL);

            if (Parameters != null)
            {
                foreach (string key in Parameters.Keys.ToList())
                {
                    Parameters[key] = MigrateOldSyntax(Parameters[key]);
                }
            }

            if (Headers != null)
            {
                foreach (string key in Headers.Keys.ToList())
                {
                    Headers[key] = MigrateOldSyntax(Headers[key]);
                }
            }

            if (Arguments != null)
            {
                foreach (string key in Arguments.Keys.ToList())
                {
                    Arguments[key] = MigrateOldSyntax(Arguments[key]);
                }
            }

            if (Data != null)
            {
                Data = Data.Replace("$input$", "{input}", StringComparison.OrdinalIgnoreCase).
                    Replace("$filename$", "{filename}", StringComparison.OrdinalIgnoreCase);
            }
            URL = MigrateOldSyntax(URL);
            ThumbnailURL = MigrateOldSyntax(ThumbnailURL);
            DeletionURL = MigrateOldSyntax(DeletionURL);
            ErrorMessage = MigrateOldSyntax(ErrorMessage);

            Version = Helpers.GetApplicationVersion();
        }
    }

    private string? MigrateOldSyntax(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        StringBuilder sbInput = new StringBuilder();

        bool start = true;

        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '$')
            {
                sbInput.Append(start ? '{' : '}');
                start = !start;
                continue;
            }
            else if (input[i] == '\\')
            {
                i++;
                continue;
            }
            else if (input[i] == '{' || input[i] == '}')
            {
                sbInput.Append('\\');
            }

            sbInput.Append(input[i]);
        }

        return sbInput.ToString();
    }

    private void CheckRequestURL()
    {
        if (string.IsNullOrEmpty(RequestURL)) return;
        var nvc = URLHelpers.ParseQueryString(RequestURL);

        if (nvc is not { Count: > 0 }) return;
        Parameters ??= [];

        foreach (string key in nvc)
        {
            if (key == null)
            {
                foreach (string value in nvc.GetValues(key))
                {
                    Parameters.TryAdd(value, "");
                }
            }
            else if (!Parameters.ContainsKey(key))
            {
                var value = nvc[key];
                Parameters.Add(key, value);
            }
        }

        RequestURL = URLHelpers.RemoveQueryString(RequestURL);
    }
}
