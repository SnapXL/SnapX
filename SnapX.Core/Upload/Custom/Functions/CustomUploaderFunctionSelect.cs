
// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.Custom.Functions;

// Example: {select:domain1.com|domain2.com}
internal class CustomUploaderFunctionSelect : CustomUploaderFunction
{
    public override string Name { get; } = "select";

    public override int MinParameterCount { get; } = 1;

    public override string? Call(ShareXCustomUploaderSyntaxParser parser, string?[] parameters)
    {
        string[] values = parameters.Where(x => !string.IsNullOrEmpty(x)).ToArray();

        if (values.Length > 0)
        {
            // TODO: Reimplement CustomUploaderFunctionSelectForm
            // using (ParserSelectForm form = new ParserSelectForm(values))
            // {
            //     form.ShowDialog();
            //     return form.SelectedText;
            // }
        }

        return null;
    }
}
