// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text;
using System.Xml;

namespace SnapX.Core.Indexer;
public class IndexerXml : Indexer
{
    protected XmlWriter xmlWriter;
    public IndexerXml(IndexerSettings indexerSettings) : base(indexerSettings)
    {
    }

    public string Index(string folderPath)
    {
        var folderInfo = new FolderInfo(folderPath);
        folderInfo.Update();

        var xmlWriterSettings = new XmlWriterSettings();
        xmlWriterSettings.Encoding = new UTF8Encoding(false);
        xmlWriterSettings.ConformanceLevel = ConformanceLevel.Document;
        xmlWriterSettings.Indent = true;

        using var ms = new MemoryStream();
        using (xmlWriter = XmlWriter.Create(ms, xmlWriterSettings))
        {
            xmlWriter.WriteStartDocument();
            IndexFolder(folderInfo);
            xmlWriter.WriteEndDocument();
            xmlWriter.Flush();
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    protected override void IndexFolder(FolderInfo dir, int level = 0)
    {
        xmlWriter.WriteStartElement("Folder");

        if (dir.Files.Count > 0)
        {
            xmlWriter.WriteStartElement("Files");

            foreach (var _ in dir.Files)
            {
                xmlWriter.WriteStartElement("File");

                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
        }

        if (dir.Folders.Count > 0)
        {
            xmlWriter.WriteStartElement("Folders");

            foreach (var subDir in dir.Folders)
            {
                IndexFolder(subDir);
            }

            xmlWriter.WriteEndElement();
        }

        xmlWriter.WriteEndElement();
    }
}

