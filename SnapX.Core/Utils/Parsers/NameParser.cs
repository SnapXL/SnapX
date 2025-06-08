using System.Globalization;
using System.Text;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Utils.Parsers;

public enum NameParserType
{
    Default,
    Text, // Allows new line
    FileName,
    FilePath,
    URL // URL path encodes
}

public class NameParser
{
    public NameParserType Type { get; private set; }
    public int MaxNameLength { get; set; }
    public int MaxTitleLength { get; set; }
    public int AutoIncrementNumber { get; set; } // %i, %ia, %ib, %iAa, %ix
    public int ImageWidth { get; set; } // %width
    public int ImageHeight { get; set; } // %height
    public string WindowText { get; set; } // %t
    public string ProcessName { get; set; } // %pn
    public TimeZoneInfo CustomTimeZone { get; set; }

    // If we're trying to preview via TaskSettings or not
    // Used so that %rf throws "File not found" exceptions and brings up a popup on upload
    // But only returns an error message when previewing to avoid popup spam
    public bool IsPreviewMode { get; set; } = false;

    protected NameParser()
    {
    }

    public NameParser(NameParserType nameParserType)
    {
        Type = nameParserType;
    }

    public static string Parse(NameParserType nameParserType, string pattern)
    {
        return new NameParser(nameParserType).Parse(pattern);
    }

    public string Parse(string pattern)
    {
        return Parse(pattern, DateTime.Now);
    }

    public string Parse(string pattern, DateTime dt)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return "";
        }

        var sb = new StringBuilder(pattern);

        if (WindowText != null)
        {
            var windowText = SanitizeInput(WindowText);

            if (MaxTitleLength > 0)
            {
                windowText = windowText.Truncate(MaxTitleLength);
            }

            sb.Replace(CodeMenuEntryFilename.t.ToPrefixString(), windowText);
        }

        if (ProcessName != null)
        {
            var processName = SanitizeInput(ProcessName);

            sb.Replace(CodeMenuEntryFilename.pn.ToPrefixString(), processName);
        }

        string width = "", height = "";

        if (ImageWidth > 0)
        {
            width = ImageWidth.ToString();
        }

        if (ImageHeight > 0)
        {
            height = ImageHeight.ToString();
        }

        sb.Replace(CodeMenuEntryFilename.width.ToPrefixString(), width);
        sb.Replace(CodeMenuEntryFilename.height.ToPrefixString(), height);


        if (CustomTimeZone != null)
        {
            dt = TimeZoneInfo.ConvertTime(dt, CustomTimeZone);
        }

        sb.Replace(CodeMenuEntryFilename.mon2.ToPrefixString(), CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(dt.Month))
            .Replace(CodeMenuEntryFilename.mon.ToPrefixString(), CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(dt.Month))
            .Replace(CodeMenuEntryFilename.yy.ToPrefixString(), dt.ToString("yy"))
            .Replace(CodeMenuEntryFilename.y.ToPrefixString(), dt.Year.ToString())
            .Replace(CodeMenuEntryFilename.mo.ToPrefixString(), Helpers.AddZeroes(dt.Month))
            .Replace(CodeMenuEntryFilename.d.ToPrefixString(), Helpers.AddZeroes(dt.Day));

        string hour;

        if (sb.ToString().Contains(CodeMenuEntryFilename.pm.ToPrefixString()))
        {
            hour = Helpers.HourTo12(dt.Hour);
        }
        else
        {
            hour = Helpers.AddZeroes(dt.Hour);
        }

        sb.Replace(CodeMenuEntryFilename.h.ToPrefixString(), hour)
            .Replace(CodeMenuEntryFilename.mi.ToPrefixString(), Helpers.AddZeroes(dt.Minute))
            .Replace(CodeMenuEntryFilename.s.ToPrefixString(), Helpers.AddZeroes(dt.Second))
            .Replace(CodeMenuEntryFilename.ms.ToPrefixString(), Helpers.AddZeroes(dt.Millisecond, 3))
            .Replace(CodeMenuEntryFilename.wy.ToPrefixString(), dt.WeekOfYear().ToString())
            .Replace(CodeMenuEntryFilename.w2.ToPrefixString(), CultureInfo.InvariantCulture.DateTimeFormat.GetDayName(dt.DayOfWeek))
            .Replace(CodeMenuEntryFilename.w.ToPrefixString(), CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(dt.DayOfWeek))
            .Replace(CodeMenuEntryFilename.pm.ToPrefixString(), dt.Hour >= 12 ? "PM" : "AM");

        sb.Replace(CodeMenuEntryFilename.unix.ToPrefixString(), DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

        if (sb.ToString().Contains(CodeMenuEntryFilename.i.ToPrefixString())
            || sb.ToString().Contains(CodeMenuEntryFilename.ib.ToPrefixString())
            || sb.ToString().Contains(CodeMenuEntryFilename.ib.ToPrefixString().Replace('b', 'B'))
            || sb.ToString().Contains(CodeMenuEntryFilename.iAa.ToPrefixString())
            || sb.ToString().Contains(CodeMenuEntryFilename.iAa.ToPrefixString().Replace("Aa", "aA"))
            || sb.ToString().Contains(CodeMenuEntryFilename.ia.ToPrefixString())
            || sb.ToString().Contains(CodeMenuEntryFilename.ia.ToPrefixString().Replace('a', 'A'))
            || sb.ToString().Contains(CodeMenuEntryFilename.ix.ToPrefixString())
            || sb.ToString().Contains(CodeMenuEntryFilename.ix.ToPrefixString().Replace('x', 'X')))
        {
            AutoIncrementNumber++;

            // Base
            try
            {
                foreach (var entry in ListEntryWithValues(sb.ToString(), CodeMenuEntryFilename.ib.ToPrefixString(), 2))
                {
                    sb.Replace(entry.Item1, Helpers.AddZeroes(AutoIncrementNumber.ToBase(entry.Item2[0], Helpers.AlphanumericInverse), entry.Item2[1]));
                }
                foreach (var entry in ListEntryWithValues(sb.ToString(), CodeMenuEntryFilename.ib.ToPrefixString().Replace('b', 'B'), 2))
                {
                    sb.Replace(entry.Item1, Helpers.AddZeroes(AutoIncrementNumber.ToBase(entry.Item2[0], Helpers.Alphanumeric), entry.Item2[1]));
                }
            }
            catch
            {
            }

            // Alphanumeric Dual Case (Base 62)
            foreach (var entry in ListEntryWithValue(sb.ToString(), CodeMenuEntryFilename.iAa.ToPrefixString()))
            {
                sb.Replace(entry.Item1, Helpers.AddZeroes(AutoIncrementNumber.ToBase(62, Helpers.Alphanumeric), entry.Item2));
            }
            sb.Replace(CodeMenuEntryFilename.iAa.ToPrefixString(), AutoIncrementNumber.ToBase(62, Helpers.Alphanumeric));

            // Alphanumeric Dual Case (Base 62)
            foreach (var entry in ListEntryWithValue(sb.ToString(), CodeMenuEntryFilename.iAa.ToPrefixString().Replace("Aa", "aA")))
            {
                sb.Replace(entry.Item1, Helpers.AddZeroes(AutoIncrementNumber.ToBase(62, Helpers.AlphanumericInverse), entry.Item2));
            }
            sb.Replace(CodeMenuEntryFilename.iAa.ToPrefixString().Replace("Aa", "aA"), AutoIncrementNumber.ToBase(62, Helpers.AlphanumericInverse));

            // Alphanumeric Single Case (Base 36)
            foreach (var entry in ListEntryWithValue(sb.ToString(), CodeMenuEntryFilename.ia.ToPrefixString()))
            {
                sb.Replace(entry.Item1, Helpers.AddZeroes(AutoIncrementNumber.ToBase(36, Helpers.Alphanumeric), entry.Item2).ToLowerInvariant());
            }
            sb.Replace(CodeMenuEntryFilename.ia.ToPrefixString(), AutoIncrementNumber.ToBase(36, Helpers.Alphanumeric).ToLowerInvariant());

            // Alphanumeric Single Case Capital (Base 36)
            foreach (var entry in ListEntryWithValue(sb.ToString(), CodeMenuEntryFilename.ia.ToPrefixString().Replace('a', 'A')))
            {
                sb.Replace(entry.Item1, Helpers.AddZeroes(AutoIncrementNumber.ToBase(36, Helpers.Alphanumeric), entry.Item2).ToUpperInvariant());
            }
            sb.Replace(CodeMenuEntryFilename.ia.ToPrefixString().Replace('a', 'A'), AutoIncrementNumber.ToBase(36, Helpers.Alphanumeric).ToUpperInvariant());

            // Hexadecimal (Base 16)
            foreach (var entry in ListEntryWithValue(sb.ToString(), CodeMenuEntryFilename.ix.ToPrefixString()))
            {
                sb.Replace(entry.Item1, AutoIncrementNumber.ToString("x" + entry.Item2.ToString()));
            }
            sb.Replace(CodeMenuEntryFilename.ix.ToPrefixString(), AutoIncrementNumber.ToString("x"));

            // Hexadecimal Capital (Base 16)
            foreach (var entry in ListEntryWithValue(sb.ToString(), CodeMenuEntryFilename.ix.ToPrefixString().Replace('x', 'X')))
            {
                sb.Replace(entry.Item1, AutoIncrementNumber.ToString("X" + entry.Item2.ToString()));
            }
            sb.Replace(CodeMenuEntryFilename.ix.ToPrefixString().Replace('x', 'X'), AutoIncrementNumber.ToString("X"));

            // Number (Base 10)
            foreach (var entry in ListEntryWithValue(sb.ToString(), CodeMenuEntryFilename.i.ToPrefixString()))
            {
                sb.Replace(entry.Item1, AutoIncrementNumber.ToString("d" + entry.Item2.ToString()));
            }
            sb.Replace(CodeMenuEntryFilename.i.ToPrefixString(), AutoIncrementNumber.ToString("d"));
        }

        sb.Replace(CodeMenuEntryFilename.un.ToPrefixString(), Environment.UserName);
        sb.Replace(CodeMenuEntryFilename.uln.ToPrefixString(), Environment.UserDomainName);
        sb.Replace(CodeMenuEntryFilename.cn.ToPrefixString(), Environment.MachineName);

        if (Type == NameParserType.Text)
        {
            sb.Replace(CodeMenuEntryFilename.n.ToPrefixString(), Environment.NewLine);
        }

        var result = sb.ToString();

        // result = result.ReplaceAll(CodeMenuEntryFilename.radjective.ToPrefixString(),
        //     () => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Helpers.GetRandomLine(Resources.adjectives)));
        // result = result.ReplaceAll(CodeMenuEntryFilename.ranimal.ToPrefixString(),
        //     () => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Helpers.GetRandomLine(Resources.animals)));

        foreach (var entry in ListEntryWithArgument(result, CodeMenuEntryFilename.rf.ToPrefixString()))
        {
            result = result.ReplaceAll(entry.Item1, () =>
            {
                try
                {
                    string path = entry.Item2;

                    if (FileHelpers.IsTextFile(path))
                    {
                        return Helpers.GetRandomLineFromFile(path);
                    }
                    else
                    {
                        throw new Exception("Valid text file path is required.");
                    }
                }
                catch (Exception e) when (IsPreviewMode)
                {
                    return e.Message;
                }
            });
        }

        foreach (var entry in ListEntryWithValue(result, CodeMenuEntryFilename.rna.ToPrefixString()))
        {
            result = result.ReplaceAll(entry.Item1, () => Helpers.RepeatGenerator(entry.Item2, () => Helpers.GetRandomChar(Helpers.Base56).ToString()));
        }

        foreach (var entry in ListEntryWithValue(result, CodeMenuEntryFilename.rn.ToPrefixString()))
        {
            result = result.ReplaceAll(entry.Item1, () => Helpers.RepeatGenerator(entry.Item2, () => Helpers.GetRandomChar(Helpers.Numbers).ToString()));
        }

        foreach (var entry in ListEntryWithValue(result, CodeMenuEntryFilename.ra.ToPrefixString()))
        {
            result = result.ReplaceAll(entry.Item1, () => Helpers.RepeatGenerator(entry.Item2, () => Helpers.GetRandomChar(Helpers.Alphanumeric).ToString()));
        }

        foreach (var entry in ListEntryWithValue(result, CodeMenuEntryFilename.rx.ToPrefixString()))
        {
            result = result.ReplaceAll(entry.Item1, () => Helpers.RepeatGenerator(entry.Item2, () => Helpers.GetRandomChar(Helpers.Hexadecimal.ToLowerInvariant()).ToString()));
        }

        foreach (var entry in ListEntryWithValue(result, CodeMenuEntryFilename.rx.ToPrefixString().Replace('x', 'X')))
        {
            result = result.ReplaceAll(entry.Item1, () => Helpers.RepeatGenerator(entry.Item2, () => Helpers.GetRandomChar(Helpers.Hexadecimal.ToUpperInvariant()).ToString()));
        }

        // foreach (var entry in ListEntryWithValue(result, CodeMenuEntryFilename.remoji.ToPrefixString()))
        // {
        //     result = result.ReplaceAll(entry.Item1, () => Helpers.RepeatGenerator(entry.Item2, () => RandomCrypto.Pick(Emoji.Emojis)));
        // }

        result = result.ReplaceAll(CodeMenuEntryFilename.rna.ToPrefixString(), () => Helpers.GetRandomChar(Helpers.Base56).ToString());
        result = result.ReplaceAll(CodeMenuEntryFilename.rn.ToPrefixString(), () => Helpers.GetRandomChar(Helpers.Numbers).ToString());
        result = result.ReplaceAll(CodeMenuEntryFilename.ra.ToPrefixString(), () => Helpers.GetRandomChar(Helpers.Alphanumeric).ToString());
        result = result.ReplaceAll(CodeMenuEntryFilename.rx.ToPrefixString(), () => Helpers.GetRandomChar(Helpers.Hexadecimal.ToLowerInvariant()).ToString());
        result = result.ReplaceAll(CodeMenuEntryFilename.rx.ToPrefixString().Replace('x', 'X'), () => Helpers.GetRandomChar(Helpers.Hexadecimal.ToUpperInvariant()).ToString());
        result = result.ReplaceAll(CodeMenuEntryFilename.guid.ToPrefixString().ToLowerInvariant(), () => Guid.NewGuid().ToString().ToLowerInvariant());
        result = result.ReplaceAll(CodeMenuEntryFilename.guid.ToPrefixString().ToUpperInvariant(), () => Guid.NewGuid().ToString().ToUpperInvariant());
        // I DO NOT WANT EMOJIS IN THE REPOSITORY. NO ADJECTIVES. NO VERBS. NOTHING.
        // result = result.ReplaceAll(CodeMenuEntryFilename.remoji.ToPrefixString(), () => RandomCrypto.Pick(Emoji.Emojis));

        if (Type == NameParserType.FileName)
        {
            result = FileHelpers.SanitizeFileName(result);
        }
        else if (Type == NameParserType.FilePath)
        {
            result = FileHelpers.SanitizePath(result);
        }
        else if (Type == NameParserType.URL)
        {
            result = Helpers.GetValidURL(result);
        }

        if (MaxNameLength > 0)
        {
            result = result.Truncate(MaxNameLength);
        }

        return result;
    }

    private string SanitizeInput(string input)
    {
        input = input.Trim().Replace(' ', '_');

        if (Type == NameParserType.FileName || Type == NameParserType.FilePath)
        {
            input = FileHelpers.SanitizeFileName(input);
        }

        return input;
    }

    private IEnumerable<Tuple<string, string[]>> ListEntryWithArguments(string text, string entry, int elements)
    {
        foreach (var o in text.ForEachBetween(entry + "{", "}"))
        {
            string[] s = o.Item2.Split(',');
            if (elements > s.Length)
            {
                Array.Resize(ref s, elements);
            }
            yield return new Tuple<string, string[]>(o.Item1, s);
        }
    }

    private IEnumerable<Tuple<string, string>> ListEntryWithArgument(string text, string entry)
    {
        foreach (Tuple<string, string[]> o in ListEntryWithArguments(text, entry, 1))
        {
            yield return new Tuple<string, string>(o.Item1, o.Item2[0]);
        }
    }

    private IEnumerable<Tuple<string, int[]>> ListEntryWithValues(string text, string entry, int elements)
    {
        foreach (Tuple<string, string[]> o in ListEntryWithArguments(text, entry, elements))
        {
            int[] a = new int[o.Item2.Length];
            for (int i = o.Item2.Length - 1; i >= 0; --i)
            {
                if (int.TryParse(o.Item2[i], out int n))
                {
                    a[i] = n;
                }
            }
            yield return new Tuple<string, int[]>(o.Item1, a);
        }
    }

    private IEnumerable<Tuple<string, int>> ListEntryWithValue(string text, string entry)
    {
        foreach (var o in ListEntryWithValues(text, entry, 1))
        {
            yield return new Tuple<string, int>(o.Item1, o.Item2[0]);
        }
    }
}

