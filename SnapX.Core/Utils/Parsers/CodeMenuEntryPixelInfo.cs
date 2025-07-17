using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Utils.Parsers;

public class CodeMenuEntryPixelInfo(string Value, string Description) : CodeMenuEntry(Value, Description)
{
    protected override string Prefix { get; } = "$";

    // This shouldn't show up in the list of options, but will continue to work for backwards compatibility's sake.
    private static readonly CodeMenuEntryPixelInfo r = new("r", "Red color (0-255)");
    private static readonly CodeMenuEntryPixelInfo g = new("g", "Green color (0-255)");
    private static readonly CodeMenuEntryPixelInfo b = new("b", "Blue color (0-255)");

    public static readonly CodeMenuEntryPixelInfo r255 = new("r255", "Red color (0-255)");
    public static readonly CodeMenuEntryPixelInfo g255 = new("g255", "Green color (0-255)");
    public static readonly CodeMenuEntryPixelInfo b255 = new("b255", "Blue color (0-255)");
    public static readonly CodeMenuEntryPixelInfo r1 = new("r1", "Red color (0-1). Specify decimal precision with {n}, defaults to 3.");
    public static readonly CodeMenuEntryPixelInfo g1 = new("g1", "Green color (0-1). Specify decimal precision with {n}, defaults to 3.");
    public static readonly CodeMenuEntryPixelInfo b1 = new("b1", "Blue color (0-1). Specify decimal precision with {n}, defaults to 3.");
    public static readonly CodeMenuEntryPixelInfo hex = new("hex", "Hex color value (Lowercase)");
    public static readonly CodeMenuEntryPixelInfo rhex = new("rhex", "Red hex color value (00-ff)");
    public static readonly CodeMenuEntryPixelInfo ghex = new("ghex", "Green hex color value (00-ff)");
    public static readonly CodeMenuEntryPixelInfo bhex = new("bhex", "Blue hex color value (00-ff)");
    public static readonly CodeMenuEntryPixelInfo HEX = new("HEX", "Hex color value (Uppercase)");
    public static readonly CodeMenuEntryPixelInfo rHEX = new("rHEX", "Red hex color value (00-FF)");
    public static readonly CodeMenuEntryPixelInfo gHEX = new("gHEX", "Green hex color value (00-FF)");
    public static readonly CodeMenuEntryPixelInfo bHEX = new("bHEX", "Blue hex color value (00-FF)");
    public static readonly CodeMenuEntryPixelInfo c100 = new("c100", "Cyan color (0-100)");
    public static readonly CodeMenuEntryPixelInfo m100 = new("m100", "Magenta color (0-100)");
    public static readonly CodeMenuEntryPixelInfo y100 = new("y100", "Yellow color (0-100)");
    public static readonly CodeMenuEntryPixelInfo k100 = new("k100", "Key color (0-100)");
    public static readonly CodeMenuEntryPixelInfo name = new("name", "Color name");
    public static readonly CodeMenuEntryPixelInfo x = new("x", "X position");
    public static readonly CodeMenuEntryPixelInfo y = new("y", "Y position");
    public static readonly CodeMenuEntryPixelInfo n = new("n", "New line");

    public static string Parse(string input, Rgba64 color, Point position)
    {
        input = input.Replace(r255.ToPrefixString(), color.R.ToString(), StringComparison.InvariantCultureIgnoreCase).
            Replace(g255.ToPrefixString(), color.G.ToString(), StringComparison.InvariantCultureIgnoreCase).
            Replace(b255.ToPrefixString(), color.B.ToString(), StringComparison.InvariantCultureIgnoreCase).
            Replace(rHEX.ToPrefixString(), color.R.ToString("X2"), StringComparison.InvariantCulture).
            Replace(gHEX.ToPrefixString(), color.G.ToString("X2"), StringComparison.InvariantCulture).
            Replace(bHEX.ToPrefixString(), color.B.ToString("X2"), StringComparison.InvariantCulture).
            Replace(rhex.ToPrefixString(), color.R.ToString("X2").ToLowerInvariant(), StringComparison.InvariantCultureIgnoreCase).
            Replace(ghex.ToPrefixString(), color.G.ToString("X2").ToLowerInvariant(), StringComparison.InvariantCultureIgnoreCase).
            Replace(bhex.ToPrefixString(), color.B.ToString("X2").ToLowerInvariant(), StringComparison.InvariantCultureIgnoreCase).
            Replace(HEX.ToPrefixString(), ColorHelpers.ColorToHex(color), StringComparison.InvariantCulture).
            Replace(hex.ToPrefixString(), ColorHelpers.ColorToHex(color).ToLowerInvariant(), StringComparison.InvariantCultureIgnoreCase).
            Replace(c100.ToPrefixString(), System.Math.Round(ColorHelpers.ColorToCMYK(color).C, 2, MidpointRounding.AwayFromZero).ToString(), StringComparison.InvariantCultureIgnoreCase).
            Replace(m100.ToPrefixString(), System.Math.Round(ColorHelpers.ColorToCMYK(color).M, 2, MidpointRounding.AwayFromZero).ToString(), StringComparison.InvariantCultureIgnoreCase).
            Replace(y100.ToPrefixString(), System.Math.Round(ColorHelpers.ColorToCMYK(color).Y, 2, MidpointRounding.AwayFromZero).ToString(), StringComparison.InvariantCultureIgnoreCase).
            Replace(k100.ToPrefixString(), System.Math.Round(ColorHelpers.ColorToCMYK(color).K, 2, MidpointRounding.AwayFromZero).ToString(), StringComparison.InvariantCultureIgnoreCase).
            Replace(name.ToPrefixString(), ColorHelpers.GetColorName(color), StringComparison.InvariantCultureIgnoreCase).
            Replace(x.ToPrefixString(), position.X.ToString(), StringComparison.InvariantCultureIgnoreCase).
            Replace(y.ToPrefixString(), position.Y.ToString(), StringComparison.InvariantCultureIgnoreCase).
            Replace(n.ToPrefixString(), Environment.NewLine, StringComparison.InvariantCultureIgnoreCase);

        foreach (var entry in ListEntryWithValue(input, r1.ToPrefixString()))
        {
            input = input.Replace(entry.Item1, System.Math.Round(color.R / 255d, MathHelpers.Clamp(entry.Item2, 0, 15), MidpointRounding.AwayFromZero).ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        foreach (var entry in ListEntryWithValue(input, g1.ToPrefixString()))
        {
            input = input.Replace(entry.Item1, System.Math.Round(color.G / 255d, MathHelpers.Clamp(entry.Item2, 0, 15), MidpointRounding.AwayFromZero).ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        foreach (var entry in ListEntryWithValue(input, b1.ToPrefixString()))
        {
            input = input.Replace(entry.Item1, System.Math.Round(color.B / 255d, MathHelpers.Clamp(entry.Item2, 0, 15), MidpointRounding.AwayFromZero).ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        input = input.Replace(r1.ToPrefixString(), System.Math.Round(color.R / 255d, 3, MidpointRounding.AwayFromZero).ToString(), StringComparison.InvariantCultureIgnoreCase).
            Replace(g1.ToPrefixString(), System.Math.Round(color.G / 255d, 3, MidpointRounding.AwayFromZero).ToString(), StringComparison.InvariantCultureIgnoreCase).
            Replace(b1.ToPrefixString(), System.Math.Round(color.B / 255d, 3, MidpointRounding.AwayFromZero).ToString(), StringComparison.InvariantCultureIgnoreCase);

        input = input.Replace(r.ToPrefixString(), color.R.ToString(), StringComparison.InvariantCultureIgnoreCase).
            Replace(g.ToPrefixString(), color.G.ToString(), StringComparison.InvariantCultureIgnoreCase).
            Replace(b.ToPrefixString(), color.B.ToString(), StringComparison.InvariantCultureIgnoreCase);

        return input;
    }

    private static IEnumerable<Tuple<string, string[]>> ListEntryWithArguments(string text, string entry, int elements)
    {
        return text.ForEachBetween(entry + "{", "}")
            .Select(o =>
            {
                var s = o.Item2.Split(',');
                if (elements > s.Length)
                {
                    Array.Resize(ref s, elements);
                }
                return new Tuple<string, string[]>(o.Item1, s);
            });
    }

    private static IEnumerable<Tuple<string, int[]>> ListEntryWithValues(string text, string entry, int elements)
    {
        return ListEntryWithArguments(text, entry, elements)
            .Select(o => new Tuple<string, int[]>(
                o.Item1,
                o.Item2
                    .Reverse() // Reverse the array first
                    .Select((value, index) => int.TryParse(value, out int n) ? n : 0) // Convert to int or 0 if parsing fails
                    .Reverse() // Reverse it back to the original order
                    .ToArray()
            ));
    }

    private static IEnumerable<Tuple<string, int>> ListEntryWithValue(string text, string entry)
    {
        return ListEntryWithValues(text, entry, 1)
            .Select(o => new Tuple<string, int>(o.Item1, o.Item2[0]));
    }
}

