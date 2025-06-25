
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using SixLabors.ImageSharp;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.ImageEffects;

public abstract class ImageEffect
{
    [DefaultValue(true), Browsable(false)]
    public bool Enabled { get; set; }

    [DefaultValue(""), Browsable(false)]
    public string Name { get; set; }

    protected ImageEffect()
    {
        Enabled = true;
    }

    public abstract Image Apply(Image img);

    protected virtual string? GetSummary()
    {
        return null;
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(Name))
        {
            return Name;
        }

        string name = GetType().GetDescription();
        string? summary = GetSummary();

        if (!string.IsNullOrEmpty(summary))
        {
            name = $"{name}: {summary}";
        }

        return name;
    }
}
