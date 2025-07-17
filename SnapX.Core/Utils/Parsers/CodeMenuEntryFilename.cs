namespace SnapX.Core.Utils.Parsers;

public class CodeMenuEntryFilename(string Value, string Description, string Category = null)
    : CodeMenuEntry(Value, Description, Category)
{
    protected override string Prefix => "%";

    public static readonly CodeMenuEntryFilename t = new("t", "Title of window", "Window");
    public static readonly CodeMenuEntryFilename pn = new("pn", "Process name of window", "Window");
    public static readonly CodeMenuEntryFilename y = new("y", "Current year", "Date and time");
    public static readonly CodeMenuEntryFilename yy = new("yy", "Year (2 digits)", "Date and time");
    public static readonly CodeMenuEntryFilename mo = new("mo", "Month", "Date and time");
    public static readonly CodeMenuEntryFilename mon = new("mon", "Month name (Local language)", "Date and time");
    public static readonly CodeMenuEntryFilename mon2 = new("mon2", "Month name (English)", "Date and time");
    public static readonly CodeMenuEntryFilename w = new("w", "Week name (Local language)", "Date and time");
    public static readonly CodeMenuEntryFilename w2 = new("w2", "Week name (English)", "Date and time");
    public static readonly CodeMenuEntryFilename wy = new("wy", "Week of year", "Date and time");
    public static readonly CodeMenuEntryFilename d = new("d", "Day", "Date and time");
    public static readonly CodeMenuEntryFilename h = new("h", "Hour", "Date and time");
    public static readonly CodeMenuEntryFilename mi = new("mi", "Minute", "Date and time");
    public static readonly CodeMenuEntryFilename s = new("s", "Second", "Date and time");
    public static readonly CodeMenuEntryFilename ms = new("ms", "Millisecond", "Date and time");
    public static readonly CodeMenuEntryFilename pm = new("pm", "AM/PM", "Date and time");
    public static readonly CodeMenuEntryFilename unix = new("unix", "Unix timestamp", "Date and time");
    public static readonly CodeMenuEntryFilename i = new("i", "Auto increment number", "Incremental");
    public static readonly CodeMenuEntryFilename ia = new("ia", "Auto increment alphanumeric case-insensitive (0 pad left using {n})", "Incremental");
    public static readonly CodeMenuEntryFilename iAa = new("iAa", "Auto increment alphanumeric case-sensitive (0 pad left using {n})", "Incremental");
    public static readonly CodeMenuEntryFilename ib = new("ib", "Auto increment by base {n} using alphanumeric (1 &lt; n &lt; 63)", "Incremental");
    public static readonly CodeMenuEntryFilename ix = new("ix", "Auto increment hexadecimal (0 pad left using {n})", "Incremental");
    public static readonly CodeMenuEntryFilename rn = new("rn", "Random number 0 to 9 (Repeat using {n})", "Random");
    public static readonly CodeMenuEntryFilename ra = new("ra", "Random alphanumeric char (Repeat using {n})", "Random");
    public static readonly CodeMenuEntryFilename rna = new("rna", "Random non ambiguous alphanumeric char (Repeat using {n})", "Random");
    public static readonly CodeMenuEntryFilename rx = new("rx", "Random hexadecimal char (Repeat using {n})", "Random");
    public static readonly CodeMenuEntryFilename guid = new("guid", "Random GUID", "Random");
    public static readonly CodeMenuEntryFilename rf = new("rf", "Random line from a file (Use {filepath} to determine the file)", "Random");
    public static readonly CodeMenuEntryFilename width = new("width", "Image width", "Image");
    public static readonly CodeMenuEntryFilename height = new("height", "Image height", "Image");
    public static readonly CodeMenuEntryFilename un = new("un", "Username", "Computer");
    public static readonly CodeMenuEntryFilename uln = new("uln", "User login name", "Computer");
    public static readonly CodeMenuEntryFilename cn = new("cn", "Computer name/HOSTNAME", "Computer");
    public static readonly CodeMenuEntryFilename n = new("n", "New line");
}

