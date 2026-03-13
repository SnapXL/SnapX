using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using SnapX.Core.Utils.Extensions;
using YamlDotNet.Serialization;

namespace SnapX.Core.Indexer;

public class IndexerSettings
{
    [Category("Indexer"), DefaultValue(IndexerOutput.Html), Description("Indexer output type.")]
    public IndexerOutput Output { get; set; }

    [Category("Indexer"), DefaultValue(true), Description("Don't index hidden folders.")]
    public bool SkipHiddenFolders { get; set; }

    [Category("Indexer"), DefaultValue(true), Description("Don't index hidden files.")]
    public bool SkipHiddenFiles { get; set; }

    [Category("Indexer"), DefaultValue(0), Description("Maximum folder depth level for indexing. 0 means unlimited.")]
    public int MaxDepthLevel { get; set; }

    [Category("Indexer"), DefaultValue(true), Description("Write folder and file size.")]
    public bool ShowSizeInfo { get; set; }

    [Category("Indexer"), DefaultValue(true), Description("Add footer information to show application and generated time.")]
    public bool AddFooter { get; set; }

    [Category("Indexer / Text"), DefaultValue("|___"), Description("Padding text to show indentation in the folder hierarchy.")]
    public string IndentationText { get; set; }

    [Category("Indexer / Text"), DefaultValue(false), Description("Adds empty line after folders.")]
    public bool AddEmptyLineAfterFolders { get; set; }

    [Category("Indexer / HTML"), DefaultValue(false), Description("Use custom Cascading Style Sheet file.")]
    public bool UseCustomCSSFile { get; set; }

    [Category("Indexer / HTML"), DefaultValue(false), Description("Display the path for each subfolder.")]
    public bool DisplayPath { get; set; }

    [Category("Indexer / HTML"), DefaultValue(false), Description("Limit the display path to the selected root folder. Must have DisplayPath enabled.")]
    public bool DisplayPathLimited { get; set; }

    [Category("Indexer / HTML"), DefaultValue(""), Description("Custom Cascading Style Sheet file path.")]
    public string CustomCSSFilePath { get; set; }

    [Category("Indexer / XML"), DefaultValue(true), Description("Folder/File information (name, size etc.) will be written as attribute.")]
    public bool UseAttribute { get; set; }

    [Category("Indexer / JSON"), DefaultValue(true), Description("Creates parseable but longer json output.")]
    public bool CreateParseableJson { get; set; }

    [JsonIgnore, YamlIgnore]
    public bool BinaryUnits;

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public IndexerSettings()
    {
        this.ApplyDefaultPropertyValues();
    }
}
