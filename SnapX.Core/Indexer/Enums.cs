// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;

namespace SnapX.Core.Indexer;

public enum IndexerOutput
{
    [Description("Text")]
    Txt,
    [Description("HTML")]
    Html,
    [Description("XML")]
    Xml,
    [Description("JSON")]
    Json
}

