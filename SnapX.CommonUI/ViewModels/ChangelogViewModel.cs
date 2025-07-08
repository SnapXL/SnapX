using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnapX.Core.Utils;

namespace SnapX.CommonUI.ViewModels;

public partial class ChangelogViewModel : ViewModelBase
{
    public ObservableCollection<Changelog.ChangelogVersion> Versions { get; } = [];
    [ObservableProperty]
    public Changelog.ChangelogVersion selectedChangelogVersion = new();

    [RelayCommand]
    public void SelectVersion() { }
    [RelayCommand]
    public async Task Load()
    {
        var changelogEntries = await await Task.Factory.StartNew(async () =>
                AvaloniaChangelog.ParseChangelogEntries(
                    AvaloniaChangelog.SeparateChangelogEntries(
                    [
                        await new AvaloniaChangelog(Helpers.GetApplicationVersion()).GetChangeSummary()
                    ])),
            TaskCreationOptions.LongRunning);
        if (changelogEntries != null)
        {
            foreach (var entry in changelogEntries)
            {
                Versions.Add(entry);
            }
        }
    }
}
