
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text.RegularExpressions;

namespace SnapX.Core.History;

public record HistoryFilter
{
    public string Filename { get; set; }
    public string URL { get; set; }
    public bool FilterDate { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public bool FilterType { get; set; }
    public string Type { get; set; }
    public bool FilterHost { get; set; }
    public string Host { get; set; }

    public int MaxItemCount { get; set; }
    public bool SearchInTags { get; set; } = true;

    public HistoryFilter()
    {
    }

    public IEnumerable<HistoryItem> ApplyFilter(IEnumerable<HistoryItem> historyItems)
    {
        if (FilterType && !string.IsNullOrEmpty(Type))
        {
            historyItems = historyItems.Where(x => !string.IsNullOrEmpty(x.Type) && x.Type.Equals(Type, StringComparison.InvariantCultureIgnoreCase));
        }

        if (FilterHost && !string.IsNullOrEmpty(Host))
        {
            historyItems = historyItems.Where(x => !string.IsNullOrEmpty(x.Host) && x.Host.Contains(Host, StringComparison.InvariantCultureIgnoreCase));
        }

        if (!string.IsNullOrEmpty(Filename))
        {
            string pattern = Regex.Escape(Filename).Replace("\\?", ".").Replace("\\*", ".*");
            Regex regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            historyItems = historyItems.Where(x => (x.FileName != null && regex.IsMatch(x.FileName)) ||
                (SearchInTags && x.Tags != null && x.Tags.Any(tag => regex.IsMatch(tag.Name))));
        }

        if (!string.IsNullOrEmpty(URL))
        {
            historyItems = historyItems.Where(x => x.URL != null && x.URL.Contains(URL, StringComparison.InvariantCultureIgnoreCase));
        }

        if (FilterDate)
        {
            historyItems = historyItems.Where(x => x.DateTime.Date >= FromDate && x.DateTime.Date <= ToDate);
        }

        if (MaxItemCount > 0)
        {
            historyItems = historyItems.Take(MaxItemCount);
        }

        return historyItems;
    }
}

