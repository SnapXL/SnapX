// SPDX-License-Identifier: GPL-3.0-or-later


using System.Xml.XPath;

namespace SnapX.Core.Upload.Custom.Functions;

// Example: {xml:/files/file[1]/url}
// Example: {xml:{response}|/files/file[1]/url}
internal class CustomUploaderFunctionXml : CustomUploaderFunction
{
    public override string Name { get; } = "xml";

    public override int MinParameterCount { get; } = 1;

    public override string? Call(ShareXCustomUploaderSyntaxParser parser, string?[] parameters)
    {
        // https://www.w3schools.com/xml/xpath_syntax.asp
        string? input;
        string? xpath;

        if (parameters.Length > 1)
        {
            // {xml:input|xpath}
            input = parameters[0];
            xpath = parameters[1];
        }
        else
        {
            // {xml:xpath}
            input = parser.ResponseInfo.ResponseText;
            xpath = parameters[0];
        }

        if (!string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(xpath))
        {
            using (StringReader sr = new StringReader(input))
            {
                XPathDocument doc = new XPathDocument(sr);
                XPathNavigator nav = doc.CreateNavigator();
                XPathNavigator node = nav.SelectSingleNode(xpath);

                if (node != null)
                {
                    return node.Value;
                }
            }
        }

        return null;
    }
}
